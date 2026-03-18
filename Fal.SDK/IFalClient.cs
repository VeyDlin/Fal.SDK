using System.Text.Json;
using Fal.SDK.Models;
using Fal.SDK.Queue;
using Fal.SDK.Storage;
using Fal.SDK.Streaming;
using Fal.SDK.Realtime;

namespace Fal.SDK;


public interface IFalClient {
    IQueueClient queue { get; }
    IStorageClient storage { get; }
    IStreamingClient streaming { get; }
    IRealtimeClient realtime { get; }

    JsonElement Run(string endpointId, RunOptions? options = null);
    Task<JsonElement> RunAsync(string endpointId, RunOptions? options = null, CancellationToken cancellationToken = default);

    JsonElement Subscribe(string endpointId, SubscribeOptions? options = null);
    Task<JsonElement> SubscribeAsync(string endpointId, SubscribeOptions? options = null, CancellationToken cancellationToken = default);
}
