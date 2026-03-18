namespace Fal.SDK.Storage;


public interface IStorageClient {
    string Upload(byte[] data, string contentType, string? fileName = null);
    Task<string> UploadAsync(byte[] data, string contentType, string? fileName = null, CancellationToken cancellationToken = default);

    string UploadFile(string filePath);
    Task<string> UploadFileAsync(string filePath, CancellationToken cancellationToken = default);
}
