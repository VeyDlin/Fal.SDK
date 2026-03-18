using System.Text.Json;
using Fal.SDK.Models;

namespace Fal.SDK.Queue;


public record QueueSubmitResult(
    string requestId,
    string responseUrl,
    string statusUrl,
    string cancelUrl
);


public interface IQueueClient {
    QueueSubmitResult Submit(string endpointId, SubmitOptions? options = null);
    Task<QueueSubmitResult> SubmitAsync(string endpointId, SubmitOptions? options = null, CancellationToken cancellationToken = default);

    QueueStatus Status(string endpointId, string requestId, bool withLogs = false);
    Task<QueueStatus> StatusAsync(string endpointId, string requestId, bool withLogs = false, CancellationToken cancellationToken = default);

    JsonElement Result(string endpointId, string requestId);
    Task<JsonElement> ResultAsync(string endpointId, string requestId, CancellationToken cancellationToken = default);

    void Cancel(string endpointId, string requestId);
    Task CancelAsync(string endpointId, string requestId, CancellationToken cancellationToken = default);
}
