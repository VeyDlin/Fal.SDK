using System.Net.WebSockets;
using System.Text.Json;
using System.Web;
using Fal.SDK.Http;

namespace Fal.SDK.Realtime;


public class RealtimeClient : IRealtimeClient {
    private readonly FalHttpClient httpClient;
    private readonly FalClientOptions options;

    public RealtimeClient(FalHttpClient httpClient, FalClientOptions options) {
        this.httpClient = httpClient;
        this.options = options;
    }


    public async Task<RealtimeConnection> ConnectAsync(string endpointId, RealtimeOptions? opts = null, CancellationToken cancellationToken = default) {
        opts ??= new RealtimeOptions();

        string? token = null;
        Dictionary<string, string>? headers = null;

        if (opts.useJwt) {
            token = await GetRealtimeTokenAsync(endpointId, opts.tokenExpiration, opts.apiKey, cancellationToken).ConfigureAwait(false);
        } else {
            string apiKey = httpClient.ResolveApiKey(opts.apiKey);
            headers = new Dictionary<string, string> {
                ["Authorization"] = $"Key {apiKey}",
                ["User-Agent"] = "fal-client-csharp/0.1.0"
            };
        }

        string url = BuildRealtimeUrl(endpointId, opts.path, token, opts.maxBuffering);

        var ws = new ClientWebSocket();
        if (headers is not null) {
            foreach (var (key, value) in headers) {
                ws.Options.SetRequestHeader(key, value);
            }
        }

        await ws.ConnectAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
        return new RealtimeConnection(ws);
    }


    private async Task<string> GetRealtimeTokenAsync(string endpointId, int tokenExpiration, string? apiKey, CancellationToken cancellationToken) {
        var parsed = EndpointId.Parse(endpointId);
        var payload = JsonDocument.Parse(JsonSerializer.Serialize(new {
            allowed_apps = new[] { parsed.alias },
            token_expiration = tokenExpiration
        })).RootElement.Clone();

        var result = await httpClient.SendRequestAsync(
            $"{options.restUrl}/tokens/",
            HttpMethod.Post,
            payload,
            apiKey: apiKey,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (result.ValueKind == JsonValueKind.String) {
            return result.GetString()!;
        }
        if (result.TryGetProperty("token", out var tokenProp)) {
            return tokenProp.GetString()!;
        }
        if (result.TryGetProperty("detail", out var detailProp)) {
            return detailProp.GetString()!;
        }
        throw new InvalidOperationException("Unexpected realtime token response format");
    }


    private string BuildRealtimeUrl(string endpointId, string path, string? token, int? maxBuffering) {
        var parsed = EndpointId.Parse(endpointId);
        string appPath = parsed.FormatPath();
        string url = $"wss://{options.runHost}/{appPath}";
        if (!string.IsNullOrEmpty(path)) {
            url += "/" + path.TrimStart('/');
        }

        var queryParams = new List<string>();
        if (token is not null) {
            queryParams.Add($"fal_jwt_token={HttpUtility.UrlEncode(token)}");
        }
        if (maxBuffering.HasValue) {
            if (maxBuffering.Value < 1 || maxBuffering.Value > 60) {
                throw new ArgumentException("maxBuffering must be between 1 and 60");
            }
            queryParams.Add($"max_buffering={maxBuffering.Value}");
        }
        if (queryParams.Count > 0) {
            url += "?" + string.Join("&", queryParams);
        }
        return url;
    }
}
