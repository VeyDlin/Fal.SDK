using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fal.SDK.Models;


public record RequestLog(
    string message,
    string level,
    string source,
    string timestamp
);


[JsonConverter(typeof(QueueStatusConverter))]
public abstract record QueueStatus {
    public abstract string status { get; }
}


public record QueuedStatus(
    int queuePosition
) : QueueStatus {
    public override string status { get; } = "IN_QUEUE";
}


public record InProgressStatus(
    List<RequestLog>? logs
) : QueueStatus {
    public override string status { get; } = "IN_PROGRESS";
}


public record CompletedStatus(
    List<RequestLog>? logs,
    Dictionary<string, JsonElement>? metrics,
    string? error,
    string? errorType
) : QueueStatus {
    public override string status { get; } = "COMPLETED";
}


public class QueueStatusConverter : JsonConverter<QueueStatus> {
    public override QueueStatus? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var statusStr = root.GetProperty("status").GetString();

        return statusStr switch {
            "IN_QUEUE" => new QueuedStatus(
                queuePosition: root.GetProperty("queue_position").GetInt32()
            ),
            "IN_PROGRESS" => new InProgressStatus(
                logs: DeserializeLogs(root, options)
            ),
            "COMPLETED" => new CompletedStatus(
                logs: DeserializeLogs(root, options),
                metrics: root.TryGetProperty("metrics", out var m) ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(m.GetRawText(), options) : null,
                error: root.TryGetProperty("error", out var e) ? e.GetString() : null,
                errorType: root.TryGetProperty("error_type", out var et) ? et.GetString() : null
            ),
            _ => throw new JsonException($"Unknown queue status: {statusStr}")
        };
    }

    public override void Write(Utf8JsonWriter writer, QueueStatus value, JsonSerializerOptions options) {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

    private static List<RequestLog>? DeserializeLogs(JsonElement root, JsonSerializerOptions options) {
        if (!root.TryGetProperty("logs", out var logsEl) || logsEl.ValueKind == JsonValueKind.Null) {
            return null;
        }
        return JsonSerializer.Deserialize<List<RequestLog>>(logsEl.GetRawText(), options);
    }
}
