namespace Fal.SDK.Realtime;


public record RealtimeOptions {
    public bool useJwt { get; init; } = true;
    public string path { get; init; } = "/realtime";
    public int? maxBuffering { get; init; }
    public int tokenExpiration { get; init; } = 120;
    public string? apiKey { get; init; }
}


public interface IRealtimeClient {
    Task<RealtimeConnection> ConnectAsync(string endpointId, RealtimeOptions? options = null, CancellationToken cancellationToken = default);
}
