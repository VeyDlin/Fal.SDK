using System.Text.Json;
using Fal.SDK.Models;

namespace Fal.SDK.Streaming;


public interface IStreamingClient {
    IAsyncEnumerable<JsonElement> StreamAsync(string endpointId, RunOptions? options = null, CancellationToken cancellationToken = default);
}
