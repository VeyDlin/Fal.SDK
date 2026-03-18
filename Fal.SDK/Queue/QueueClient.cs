using System.Text.Json;
using System.Web;
using Fal.SDK.Http;
using Fal.SDK.Models;

namespace Fal.SDK.Queue;


public class QueueClient : IQueueClient {
    private readonly FalHttpClient httpClient;

    public QueueClient(FalHttpClient httpClient) {
        this.httpClient = httpClient;
    }


    public QueueSubmitResult Submit(string endpointId, SubmitOptions? options = null) {
        options ??= new SubmitOptions();
        var headers = BuildSubmitHeaders(options);
        string url = httpClient.BuildQueueUrl(endpointId, options.path);

        if (options.webhookUrl is not null) {
            url += "?" + $"fal_webhook={HttpUtility.UrlEncode(options.webhookUrl)}";
        }

        var result = httpClient.SendRequest(url, HttpMethod.Post, options.input, headers, options.apiKey);
        return ParseSubmitResult(result);
    }


    public async Task<QueueSubmitResult> SubmitAsync(string endpointId, SubmitOptions? options = null, CancellationToken cancellationToken = default) {
        options ??= new SubmitOptions();
        var headers = BuildSubmitHeaders(options);
        string url = httpClient.BuildQueueUrl(endpointId, options.path);

        if (options.webhookUrl is not null) {
            url += "?" + $"fal_webhook={HttpUtility.UrlEncode(options.webhookUrl)}";
        }

        var result = await httpClient.SendRequestAsync(url, HttpMethod.Post, options.input, headers, options.apiKey, cancellationToken: cancellationToken).ConfigureAwait(false);
        return ParseSubmitResult(result);
    }


    public QueueStatus Status(string endpointId, string requestId, bool withLogs = false) {
        var parsed = EndpointId.Parse(endpointId);
        string prefix = parsed.ns is not null ? $"{parsed.ns}/" : "";
        string url = httpClient.BuildQueueUrl($"{prefix}{parsed.owner}/{parsed.alias}", $"requests/{requestId}/status");
        url += $"?logs={withLogs.ToString().ToLowerInvariant()}";

        var result = httpClient.SendRequest(url, HttpMethod.Get);
        return JsonSerializer.Deserialize<QueueStatus>(result.GetRawText())
            ?? throw new InvalidOperationException("Failed to deserialize queue status");
    }


    public async Task<QueueStatus> StatusAsync(string endpointId, string requestId, bool withLogs = false, CancellationToken cancellationToken = default) {
        var parsed = EndpointId.Parse(endpointId);
        string prefix = parsed.ns is not null ? $"{parsed.ns}/" : "";
        string url = httpClient.BuildQueueUrl($"{prefix}{parsed.owner}/{parsed.alias}", $"requests/{requestId}/status");
        url += $"?logs={withLogs.ToString().ToLowerInvariant()}";

        var result = await httpClient.SendRequestAsync(url, HttpMethod.Get, cancellationToken: cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<QueueStatus>(result.GetRawText())
            ?? throw new InvalidOperationException("Failed to deserialize queue status");
    }


    public JsonElement Result(string endpointId, string requestId) {
        var parsed = EndpointId.Parse(endpointId);
        string prefix = parsed.ns is not null ? $"{parsed.ns}/" : "";
        string url = httpClient.BuildQueueUrl($"{prefix}{parsed.owner}/{parsed.alias}", $"requests/{requestId}");

        return httpClient.SendRequest(url, HttpMethod.Get);
    }


    public async Task<JsonElement> ResultAsync(string endpointId, string requestId, CancellationToken cancellationToken = default) {
        var parsed = EndpointId.Parse(endpointId);
        string prefix = parsed.ns is not null ? $"{parsed.ns}/" : "";
        string url = httpClient.BuildQueueUrl($"{prefix}{parsed.owner}/{parsed.alias}", $"requests/{requestId}");

        return await httpClient.SendRequestAsync(url, HttpMethod.Get, cancellationToken: cancellationToken).ConfigureAwait(false);
    }


    public void Cancel(string endpointId, string requestId) {
        var parsed = EndpointId.Parse(endpointId);
        string prefix = parsed.ns is not null ? $"{parsed.ns}/" : "";
        string url = httpClient.BuildQueueUrl($"{prefix}{parsed.owner}/{parsed.alias}", $"requests/{requestId}/cancel");

        httpClient.SendRequest(url, HttpMethod.Put);
    }


    public async Task CancelAsync(string endpointId, string requestId, CancellationToken cancellationToken = default) {
        var parsed = EndpointId.Parse(endpointId);
        string prefix = parsed.ns is not null ? $"{parsed.ns}/" : "";
        string url = httpClient.BuildQueueUrl($"{prefix}{parsed.owner}/{parsed.alias}", $"requests/{requestId}/cancel");

        await httpClient.SendRequestAsync(url, HttpMethod.Put, cancellationToken: cancellationToken).ConfigureAwait(false);
    }


    private static Dictionary<string, string> BuildSubmitHeaders(SubmitOptions options) {
        var headers = new Dictionary<string, string>();
        if (options.headers is not null) {
            foreach (var (key, value) in options.headers) {
                headers[key] = value;
            }
        }
        if (options.hint is not null) {
            FalHeaders.AddHintHeader(options.hint, headers);
        }
        if (options.priority.HasValue) {
            FalHeaders.AddPriorityHeader(options.priority.Value, headers);
        }
        if (options.startTimeout.HasValue) {
            FalHeaders.AddTimeoutHeader(options.startTimeout.Value, headers);
        }
        return headers;
    }


    private static QueueSubmitResult ParseSubmitResult(JsonElement result) {
        return new QueueSubmitResult(
            requestId: result.GetProperty("request_id").GetString()!,
            responseUrl: result.GetProperty("response_url").GetString()!,
            statusUrl: result.GetProperty("status_url").GetString()!,
            cancelUrl: result.GetProperty("cancel_url").GetString()!
        );
    }
}
