using System.Text.Json;

namespace Fal.SDK.Errors;


public class FalApiException : Exception {
    public int statusCode { get; }
    public string? requestId { get; }
    public string? errorType { get; }
    public bool? isRetryable { get; }
    public JsonElement? responseBody { get; }

    public FalApiException(
        string message,
        int statusCode,
        string? requestId = null,
        string? errorType = null,
        bool? isRetryable = null,
        JsonElement? responseBody = null
    ) : base(message) {
        this.statusCode = statusCode;
        this.requestId = requestId;
        this.errorType = errorType;
        this.isRetryable = isRetryable;
        this.responseBody = responseBody;
    }
}


public class FalValidationException : FalApiException {
    public List<ValidationErrorInfo> fieldErrors { get; }

    public FalValidationException(
        string message,
        string? requestId = null,
        bool? isRetryable = null,
        List<ValidationErrorInfo>? fieldErrors = null,
        JsonElement? responseBody = null
    ) : base(message, statusCode: 422, requestId: requestId, isRetryable: isRetryable, responseBody: responseBody) {
        this.fieldErrors = fieldErrors ?? [];
    }
}


public record ValidationErrorInfo(
    string message,
    List<string> location,
    string type,
    string? url = null,
    JsonElement? context = null,
    JsonElement? input = null
);


public class FalTimeoutException : Exception {
    public double timeoutSeconds { get; }
    public string? requestId { get; }

    public FalTimeoutException(
        double timeoutSeconds,
        string? requestId = null
    ) : base(requestId is null
        ? $"Request timed out after {timeoutSeconds} seconds"
        : $"Request {requestId} timed out after {timeoutSeconds} seconds") {
        this.timeoutSeconds = timeoutSeconds;
        this.requestId = requestId;
    }
}
