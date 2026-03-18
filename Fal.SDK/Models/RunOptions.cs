using System.Text.Json;

namespace Fal.SDK.Models;


public enum QueuePriority {
    Normal,
    Low
}


public record RunOptions {
    public JsonElement? input { get; init; }
    public string path { get; init; } = "";
    public string? hint { get; init; }
    public double? startTimeout { get; init; }
    public double? timeout { get; init; }
    public Dictionary<string, string>? headers { get; init; }
    public string? apiKey { get; init; }
}


public record SubmitOptions : RunOptions {
    public string? webhookUrl { get; init; }
    public QueuePriority? priority { get; init; }
}


public record SubscribeOptions : SubmitOptions {
    public bool withLogs { get; init; } = false;
    public double? clientTimeout { get; init; }
    public TimeSpan pollInterval { get; init; } = TimeSpan.FromMilliseconds(500);
    public Action<QueueStatus>? onQueueUpdate { get; init; }
    public Action<string>? onEnqueue { get; init; }
}
