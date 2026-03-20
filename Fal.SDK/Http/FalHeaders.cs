namespace Fal.SDK.Http;


public static class FalHeaders {
    public const string RequestTimeout = "X-Fal-Request-Timeout";
    public const string RequestTimeoutType = "X-Fal-Request-Timeout-Type";
    public const string QueuePriority = "X-Fal-Queue-Priority";
    public const string RunnerHint = "X-Fal-Runner-Hint";
    public const string RequestId = "x-fal-request-id";
    public const string ErrorType = "x-fal-error-type";
    public const string FileName = "X-Fal-File-Name";
    public const string Retryable = "X-Fal-Retryable";

    public const double MinRequestTimeoutSeconds = 1.0;


    public static void AddTimeoutHeader(double timeout, Dictionary<string, string> headers) {
        if (timeout <= MinRequestTimeoutSeconds) {
            throw new ArgumentException($"Timeout must be greater than {MinRequestTimeoutSeconds} seconds");
        }
        headers[RequestTimeout] = timeout.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }


    public static void AddHintHeader(string hint, Dictionary<string, string> headers) {
        headers[RunnerHint] = hint;
    }


    public static void AddPriorityHeader(Models.QueuePriority priority, Dictionary<string, string> headers) {
        string value = priority switch {
            Models.QueuePriority.Normal => "normal",
            Models.QueuePriority.Low => "low",
            _ => throw new ArgumentException($"Invalid priority: {priority}")
        };
        headers[QueuePriority] = value;
    }
}
