using System.Net.Http.Headers;
using System.Text.Json;
using Fal.SDK.Http;

namespace Fal.SDK.Storage;


public class StorageClient : IStorageClient {
    private readonly FalHttpClient httpClient;
    private readonly FalClientOptions options;
    private readonly IHttpClientFactory httpClientFactory;

    public StorageClient(FalHttpClient httpClient, FalClientOptions options, IHttpClientFactory httpClientFactory) {
        this.httpClient = httpClient;
        this.options = options;
        this.httpClientFactory = httpClientFactory;
    }


    public string Upload(byte[] data, string contentType, string? fileName = null) {
        var initResult = httpClient.SendRequest(
            $"{options.restUrl}/storage/upload/initiate?storage_type=gcs",
            HttpMethod.Post,
            BuildInitPayload(fileName, contentType)
        );

        string uploadUrl = initResult.GetProperty("upload_url").GetString()!;
        string fileUrl = initResult.GetProperty("file_url").GetString()!;

        var client = httpClientFactory.CreateClient(FalHttpClient.HttpClientName);
        var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
        request.Content = new ByteArrayContent(data);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var response = client.Send(request);
        response.EnsureSuccessStatusCode();

        return fileUrl;
    }


    public async Task<string> UploadAsync(byte[] data, string contentType, string? fileName = null, CancellationToken cancellationToken = default) {
        var initResult = await httpClient.SendRequestAsync(
            $"{options.restUrl}/storage/upload/initiate?storage_type=gcs",
            HttpMethod.Post,
            BuildInitPayload(fileName, contentType),
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        string uploadUrl = initResult.GetProperty("upload_url").GetString()!;
        string fileUrl = initResult.GetProperty("file_url").GetString()!;

        var client = httpClientFactory.CreateClient(FalHttpClient.HttpClientName);
        var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
        request.Content = new ByteArrayContent(data);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return fileUrl;
    }


    public string UploadFile(string filePath) {
        string mimeType = MimeTypes.GetMimeType(filePath);
        byte[] data = File.ReadAllBytes(filePath);
        return Upload(data, mimeType, Path.GetFileName(filePath));
    }


    public async Task<string> UploadFileAsync(string filePath, CancellationToken cancellationToken = default) {
        string mimeType = MimeTypes.GetMimeType(filePath);
        byte[] data = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        return await UploadAsync(data, mimeType, Path.GetFileName(filePath), cancellationToken).ConfigureAwait(false);
    }


    private static JsonElement BuildInitPayload(string? fileName, string contentType) {
        var payload = new Dictionary<string, string> {
            ["file_name"] = fileName ?? "upload.bin",
            ["content_type"] = contentType
        };
        var json = JsonSerializer.Serialize(payload);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}


internal static class MimeTypes {
    private static readonly Dictionary<string, string> ExtensionMap = new(StringComparer.OrdinalIgnoreCase) {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".mp4"] = "video/mp4",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".ogg"] = "audio/ogg",
        [".json"] = "application/json",
        [".txt"] = "text/plain",
        [".pdf"] = "application/pdf",
    };

    public static string GetMimeType(string filePath) {
        string ext = Path.GetExtension(filePath);
        if (ExtensionMap.TryGetValue(ext, out var mimeType)) {
            return mimeType;
        }
        return "application/octet-stream";
    }
}
