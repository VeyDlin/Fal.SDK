using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Fal.SDK.Errors;

namespace Fal.SDK.Http;


public class FalHttpClient {
    private readonly IHttpClientFactory httpClientFactory;
    private readonly FalClientOptions options;
    private readonly IFalKeyProvider? keyProvider;
    private readonly RetryOptions retryOptions;
    private readonly JsonSerializerOptions jsonOptions;

    public const string HttpClientName = "FalClient";

    public FalHttpClient(
        IHttpClientFactory httpClientFactory,
        FalClientOptions options,
        IFalKeyProvider? keyProvider = null
    ) {
        this.httpClientFactory = httpClientFactory;
        this.options = options;
        this.keyProvider = keyProvider;
        this.retryOptions = new RetryOptions();
        this.jsonOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }


    public string ResolveApiKey(string? perRequestKey) {
        if (perRequestKey is not null) {
            return perRequestKey;
        }
        if (keyProvider is not null) {
            var key = keyProvider.GetApiKey();
            if (key is not null) {
                return key;
            }
        }
        if (options.apiKey is not null) {
            return options.apiKey;
        }
        var envKey = Environment.GetEnvironmentVariable("FAL_KEY");
        if (envKey is not null) {
            return envKey;
        }
        throw new InvalidOperationException("No API key configured. Set FAL_KEY environment variable, configure FalClientOptions.apiKey, register IFalKeyProvider, or pass apiKey per request.");
    }


    public string BuildRunUrl(string endpointId, string path = "") {
        string url = $"https://{options.runHost}/{endpointId}";
        if (!string.IsNullOrEmpty(path)) {
            url += "/" + path.TrimStart('/');
        }
        return url;
    }


    public string BuildQueueUrl(string endpointId, string path = "") {
        string url = $"https://queue.{options.runHost}/{endpointId}";
        if (!string.IsNullOrEmpty(path)) {
            url += "/" + path.TrimStart('/');
        }
        return url;
    }


    private HttpClient CreateClient(string? apiKey) {
        var client = httpClientFactory.CreateClient(HttpClientName);
        string resolvedKey = ResolveApiKey(apiKey);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Key", resolvedKey);
        client.DefaultRequestHeaders.Add("User-Agent", "fal-client-csharp/0.1.0");
        client.Timeout = options.defaultTimeout;
        return client;
    }


    public async Task<JsonElement> SendRequestAsync(
        string url,
        HttpMethod method,
        JsonElement? input = null,
        Dictionary<string, string>? extraHeaders = null,
        string? apiKey = null,
        double? timeout = null,
        CancellationToken cancellationToken = default
    ) {
        var response = await RetryHandler.ExecuteWithRetryAsync(async () => {
            var client = CreateClient(apiKey);
            if (timeout.HasValue) {
                client.Timeout = TimeSpan.FromSeconds(timeout.Value);
            }

            var request = new HttpRequestMessage(method, url);
            if (input.HasValue) {
                request.Content = new StringContent(
                    input.Value.GetRawText(),
                    Encoding.UTF8,
                    "application/json"
                );
            }
            if (extraHeaders is not null) {
                foreach (var (key, value) in extraHeaders) {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }

            return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }, retryOptions, cancellationToken).ConfigureAwait(false);

        return await HandleResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }


    public JsonElement SendRequest(
        string url,
        HttpMethod method,
        JsonElement? input = null,
        Dictionary<string, string>? extraHeaders = null,
        string? apiKey = null,
        double? timeout = null
    ) {
        var response = RetryHandler.ExecuteWithRetry(() => {
            var client = CreateClient(apiKey);
            if (timeout.HasValue) {
                client.Timeout = TimeSpan.FromSeconds(timeout.Value);
            }

            var request = new HttpRequestMessage(method, url);
            if (input.HasValue) {
                request.Content = new StringContent(
                    input.Value.GetRawText(),
                    Encoding.UTF8,
                    "application/json"
                );
            }
            if (extraHeaders is not null) {
                foreach (var (key, value) in extraHeaders) {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }

            return client.Send(request);
        }, retryOptions);

        return HandleResponse(response);
    }


    private async Task<JsonElement> HandleResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode) {
            ThrowApiException(response, body);
        }

        if (string.IsNullOrEmpty(body)) {
            return default;
        }

        return JsonDocument.Parse(body).RootElement.Clone();
    }


    private JsonElement HandleResponse(HttpResponseMessage response) {
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode) {
            ThrowApiException(response, body);
        }

        if (string.IsNullOrEmpty(body)) {
            return default;
        }

        return JsonDocument.Parse(body).RootElement.Clone();
    }


    private void ThrowApiException(HttpResponseMessage response, string body) {
        int statusCode = (int)response.StatusCode;
        string? requestId = response.Headers.TryGetValues(FalHeaders.RequestId, out var ridValues)
            ? ridValues.FirstOrDefault() : null;
        string? errorType = null;
        string message = body;
        JsonElement? responseBody = null;
        List<ValidationErrorInfo>? fieldErrors = null;

        // X-Fal-Retryable header
        bool? isRetryable = null;
        if (response.Headers.TryGetValues(FalHeaders.Retryable, out var retryValues)) {
            string? retryStr = retryValues.FirstOrDefault();
            if (bool.TryParse(retryStr, out var retryBool)) {
                isRetryable = retryBool;
            }
        }

        // X-Fal-Error-Type header (fallback for error_type field)
        string? headerErrorType = response.Headers.TryGetValues(FalHeaders.ErrorType, out var etHeaderValues)
            ? etHeaderValues.FirstOrDefault() : null;

        try {
            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            responseBody = root.Clone();

            // error_type field (request/infrastructure errors)
            if (root.TryGetProperty("error_type", out var et)) {
                errorType = et.GetString();
            }

            if (root.TryGetProperty("detail", out var detail)) {
                switch (detail.ValueKind) {
                    // Format 2: Request/Infrastructure errors — detail is a string
                    // { "detail": "Request timed out", "error_type": "request_timeout" }
                    case JsonValueKind.String:
                        message = detail.GetString() ?? body;
                        break;

                    // Format 1: Model/Validation errors — detail is an array of error objects
                    // { "detail": [{ "loc": [...], "msg": "...", "type": "...", "url": "...", "ctx": {...} }] }
                    case JsonValueKind.Array:
                        fieldErrors = [];
                        var messages = new List<string>();
                        foreach (var item in detail.EnumerateArray()) {
                            string msg = item.TryGetProperty("msg", out var msgProp) ? msgProp.GetString() ?? "" : "";
                            string type = item.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "" : "";
                            var loc = item.TryGetProperty("loc", out var locProp)
                                ? locProp.EnumerateArray().Select(x => x.ToString()).ToList()
                                : [];
                            string? url = item.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                            JsonElement? ctx = item.TryGetProperty("ctx", out var ctxProp) ? ctxProp.Clone() : null;
                            JsonElement? input = item.TryGetProperty("input", out var inputProp) ? inputProp.Clone() : null;

                            fieldErrors.Add(new ValidationErrorInfo(msg, loc, type, url, ctx, input));
                            messages.Add(msg);
                        }
                        message = messages.Count > 0 ? string.Join("; ", messages) : body;
                        break;

                    default:
                        message = detail.ToString();
                        break;
                }
            }
        } catch (JsonException) {
            // body is not JSON, message stays as raw body
        }

        errorType ??= headerErrorType;

        // Model/validation errors with detail array → FalValidationException
        if (fieldErrors is not null) {
            throw new FalValidationException(
                message: message,
                requestId: requestId,
                isRetryable: isRetryable,
                fieldErrors: fieldErrors,
                responseBody: responseBody
            );
        }

        throw new FalApiException(
            message: message,
            statusCode: statusCode,
            requestId: requestId,
            errorType: errorType,
            isRetryable: isRetryable,
            responseBody: responseBody
        );
    }
}
