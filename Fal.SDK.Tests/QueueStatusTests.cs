using System.Text.Json;
using Fal.SDK.Models;

namespace Fal.SDK.Tests;


public class QueueStatusTests {
    [Fact]
    public void Deserialize_InQueue() {
        string json = """{"status": "IN_QUEUE", "queue_position": 123}""";
        var status = JsonSerializer.Deserialize<QueueStatus>(json);

        var queued = Assert.IsType<QueuedStatus>(status);
        Assert.Equal(123, queued.queuePosition);
    }


    [Fact]
    public void Deserialize_InProgress() {
        string json = """{"status": "IN_PROGRESS", "logs": [{"message": "foo", "level": "INFO", "source": "USER", "timestamp": "2024-01-01"}]}""";
        var status = JsonSerializer.Deserialize<QueueStatus>(json);

        var inProgress = Assert.IsType<InProgressStatus>(status);
        Assert.NotNull(inProgress.logs);
        Assert.Single(inProgress.logs);
        Assert.Equal("foo", inProgress.logs[0].message);
    }


    [Fact]
    public void Deserialize_Completed_WithMetrics() {
        string json = """{"status": "COMPLETED", "logs": [], "metrics": {"inference_time": 1.5}}""";
        var status = JsonSerializer.Deserialize<QueueStatus>(json);

        var completed = Assert.IsType<CompletedStatus>(status);
        Assert.NotNull(completed.logs);
        Assert.Empty(completed.logs);
        Assert.NotNull(completed.metrics);
        Assert.True(completed.metrics.ContainsKey("inference_time"));
    }


    [Fact]
    public void Deserialize_Completed_WithError() {
        string json = """{"status": "COMPLETED", "logs": [], "metrics": {}, "error": "Runner disconnected", "error_type": "runner_disconnected"}""";
        var status = JsonSerializer.Deserialize<QueueStatus>(json);

        var completed = Assert.IsType<CompletedStatus>(status);
        Assert.Equal("Runner disconnected", completed.error);
        Assert.Equal("runner_disconnected", completed.errorType);
    }


    [Fact]
    public void Deserialize_Completed_NullLogs() {
        string json = """{"status": "COMPLETED", "logs": null, "metrics": {"inference_time": 1.5}, "error": "Request timed out", "error_type": "request_timeout"}""";
        var status = JsonSerializer.Deserialize<QueueStatus>(json);

        var completed = Assert.IsType<CompletedStatus>(status);
        Assert.Null(completed.logs);
        Assert.Equal("Request timed out", completed.error);
    }


    [Fact]
    public void Deserialize_Completed_NoMetrics() {
        string json = """{"status": "COMPLETED", "logs": []}""";
        var status = JsonSerializer.Deserialize<QueueStatus>(json);

        var completed = Assert.IsType<CompletedStatus>(status);
        Assert.Null(completed.metrics);
    }


    [Fact]
    public void Deserialize_UnknownStatus_Throws() {
        string json = """{"status": "FOO"}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<QueueStatus>(json));
    }
}
