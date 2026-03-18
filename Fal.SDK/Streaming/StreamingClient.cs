using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Fal.SDK.Http;
using Fal.SDK.Models;

namespace Fal.SDK.Streaming;


public class StreamingClient : IStreamingClient {
    private readonly FalHttpClient httpClient;
    private readonly FalClientOptions options;
    private readonly IHttpClientFactory httpClientFactory;

    public StreamingClient(FalHttpClient httpClient, FalClientOptions options, IHttpClientFactory httpClientFactory) {
        this.httpClient = httpClient;
        this.options = options;
        this.httpClientFactory = httpClientFactory;
    }


    public async IAsyncEnumerable<JsonElement> StreamAsync(
        string endpointId,
        RunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    ) {
        options ??= new RunOptions();
        string path = string.IsNullOrEmpty(options.path) ? "/stream" : options.path;
        string url = httpClient.BuildRunUrl(endpointId, path);

        var client = httpClientFactory.CreateClient(FalHttpClient.HttpClientName);
        string apiKey = httpClient.ResolveApiKey(options.apiKey);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Key", apiKey);
        client.DefaultRequestHeaders.Add("User-Agent", "fal-client-csharp/0.1.0");
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        if (options.timeout.HasValue) {
            client.Timeout = TimeSpan.FromSeconds(options.timeout.Value);
        }

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (options.input.HasValue) {
            request.Content = new StringContent(
                options.input.Value.GetRawText(),
                Encoding.UTF8,
                "application/json"
            );
        }

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? dataBuffer = null;
        while (true) {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            if (line is null) {
                break;
            }

            if (line.StartsWith("data: ")) {
                dataBuffer = line[6..];
            } else if (line == "" && dataBuffer is not null) {
                var doc = JsonDocument.Parse(dataBuffer);
                yield return doc.RootElement.Clone();
                dataBuffer = null;
            }
        }
    }
}
