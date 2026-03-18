using System.Text.Json;
using Fal.SDK.Errors;
using Fal.SDK.Http;
using Fal.SDK.Models;
using Fal.SDK.Queue;
using Fal.SDK.Storage;
using Fal.SDK.Streaming;
using Fal.SDK.Realtime;

namespace Fal.SDK;


public class FalClient : IFalClient {
    private readonly FalHttpClient httpClient;
    private readonly QueueClient queueClient;
    private readonly StorageClient storageClient;
    private readonly StreamingClient streamingClient;
    private readonly RealtimeClient realtimeClient;

    public IQueueClient queue => queueClient;
    public IStorageClient storage => storageClient;
    public IStreamingClient streaming => streamingClient;
    public IRealtimeClient realtime => realtimeClient;

    public FalClient(IHttpClientFactory httpClientFactory, FalClientOptions options, IFalKeyProvider? keyProvider = null) {
        httpClient = new FalHttpClient(httpClientFactory, options, keyProvider);
        queueClient = new QueueClient(httpClient);
        storageClient = new StorageClient(httpClient, options, httpClientFactory);
        streamingClient = new StreamingClient(httpClient, options, httpClientFactory);
        realtimeClient = new RealtimeClient(httpClient, options);
    }


    public FalClient(string apiKey) {
        var options = new FalClientOptions { apiKey = apiKey };
        var factory = new SimpleHttpClientFactory();
        httpClient = new FalHttpClient(factory, options);
        queueClient = new QueueClient(httpClient);
        storageClient = new StorageClient(httpClient, options, factory);
        streamingClient = new StreamingClient(httpClient, options, factory);
        realtimeClient = new RealtimeClient(httpClient, options);
    }


    public JsonElement Run(string endpointId, RunOptions? options = null) {
        options ??= new RunOptions();
        var headers = BuildRunHeaders(options);
        string url = httpClient.BuildRunUrl(endpointId, options.path);

        return httpClient.SendRequest(url, HttpMethod.Post, options.input, headers, options.apiKey, options.timeout);
    }


    public async Task<JsonElement> RunAsync(string endpointId, RunOptions? options = null, CancellationToken cancellationToken = default) {
        options ??= new RunOptions();
        var headers = BuildRunHeaders(options);
        string url = httpClient.BuildRunUrl(endpointId, options.path);

        return await httpClient.SendRequestAsync(url, HttpMethod.Post, options.input, headers, options.apiKey, options.timeout, cancellationToken).ConfigureAwait(false);
    }


    public JsonElement Subscribe(string endpointId, SubscribeOptions? options = null) {
        options ??= new SubscribeOptions();
        var submitOptions = new SubmitOptions {
            input = options.input,
            path = options.path,
            hint = options.hint,
            startTimeout = options.startTimeout,
            priority = options.priority,
            headers = options.headers,
            apiKey = options.apiKey,
            webhookUrl = options.webhookUrl
        };

        var handle = queueClient.Submit(endpointId, submitOptions);

        if (options.onEnqueue is not null) {
            options.onEnqueue(handle.requestId);
        }

        while (true) {
            var status = queueClient.Status(endpointId, handle.requestId, options.withLogs);

            if (options.onQueueUpdate is not null) {
                options.onQueueUpdate(status);
            }

            if (status is CompletedStatus) {
                break;
            }

            Thread.Sleep(options.pollInterval);
        }

        return queueClient.Result(endpointId, handle.requestId);
    }


    public async Task<JsonElement> SubscribeAsync(string endpointId, SubscribeOptions? options = null, CancellationToken cancellationToken = default) {
        options ??= new SubscribeOptions();
        var submitOptions = new SubmitOptions {
            input = options.input,
            path = options.path,
            hint = options.hint,
            startTimeout = options.startTimeout,
            priority = options.priority,
            headers = options.headers,
            apiKey = options.apiKey,
            webhookUrl = options.webhookUrl
        };

        var handle = await queueClient.SubmitAsync(endpointId, submitOptions, cancellationToken).ConfigureAwait(false);

        if (options.onEnqueue is not null) {
            options.onEnqueue(handle.requestId);
        }

        using var timeoutCts = options.clientTimeout.HasValue
            ? new CancellationTokenSource(TimeSpan.FromSeconds(options.clientTimeout.Value))
            : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try {
            while (true) {
                var status = await queueClient.StatusAsync(endpointId, handle.requestId, options.withLogs, linkedCts.Token).ConfigureAwait(false);

                if (options.onQueueUpdate is not null) {
                    options.onQueueUpdate(status);
                }

                if (status is CompletedStatus) {
                    break;
                }

                await Task.Delay(options.pollInterval, linkedCts.Token).ConfigureAwait(false);
            }

            return await queueClient.ResultAsync(endpointId, handle.requestId, cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
            try {
                await queueClient.CancelAsync(endpointId, handle.requestId, CancellationToken.None).ConfigureAwait(false);
            } catch {
                // ignore cancel errors
            }
            throw new FalTimeoutException(
                timeoutSeconds: options.clientTimeout!.Value,
                requestId: handle.requestId
            );
        }
    }


    private static Dictionary<string, string> BuildRunHeaders(RunOptions options) {
        var headers = new Dictionary<string, string>();
        if (options.headers is not null) {
            foreach (var (key, value) in options.headers) {
                headers[key] = value;
            }
        }
        if (options.hint is not null) {
            FalHeaders.AddHintHeader(options.hint, headers);
        }
        if (options.startTimeout.HasValue) {
            FalHeaders.AddTimeoutHeader(options.startTimeout.Value, headers);
        }
        return headers;
    }
}


internal class SimpleHttpClientFactory : IHttpClientFactory {
    public HttpClient CreateClient(string name) {
        return new HttpClient();
    }
}
