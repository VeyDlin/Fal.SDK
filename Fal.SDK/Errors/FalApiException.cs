using System.Text.Json;

namespace Fal.SDK.Errors;


public class FalApiException : Exception {
    public int statusCode { get; }
    public string? requestId { get; }
    public string? errorType { get; }
    public JsonElement? responseBody { get; }

    public FalApiException(
        string message,
        int statusCode,
        string? requestId = null,
        string? errorType = null,
        JsonElement? responseBody = null
    ) : base(message) {
        this.statusCode = statusCode;
        this.requestId = requestId;
        this.errorType = errorType;
        this.responseBody = responseBody;
    }
}


public class FalValidationException : FalApiException {
    public List<ValidationErrorInfo> fieldErrors { get; }

    public FalValidationException(
        string message,
        string? requestId = null,
        List<ValidationErrorInfo>? fieldErrors = null,
        JsonElement? responseBody = null
    ) : base(message, statusCode: 422, requestId: requestId, responseBody: responseBody) {
        this.fieldErrors = fieldErrors ?? [];
    }
}


public record ValidationErrorInfo(
    string message,
    List<string> location,
    string type
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
