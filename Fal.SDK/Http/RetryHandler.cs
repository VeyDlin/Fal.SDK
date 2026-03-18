namespace Fal.SDK.Http;


public record RetryOptions {
    public int maxAttempts { get; init; } = 10;
    public double baseDelay { get; init; } = 0.1;
    public double maxDelay { get; init; } = 30.0;
    public bool enableJitter { get; init; } = true;
}


public static class RetryHandler {
    private static readonly int[] RetryCodes = [408, 409, 429];
    private static readonly int[] IngressErrorCodes = [502, 503, 504];
    private static readonly Random Rng = new();

    public static bool IsIngressError(int statusCode, string? responseBody, bool hasFalRequestId) {
        if (!IngressErrorCodes.Contains(statusCode)) {
            return false;
        }
        if (hasFalRequestId) {
            return false;
        }
        if (responseBody is not null && responseBody.Contains("nginx")) {
            return true;
        }
        return false;
    }


    public static bool ShouldRetryResponse(
        int statusCode,
        string? responseBody,
        bool hasFalRequestId,
        string? timeoutType,
        int[]? extraRetryCodes = null
    ) {
        if (statusCode == 504 && timeoutType is not null) {
            return false;
        }
        if (IsIngressError(statusCode, responseBody, hasFalRequestId)) {
            return true;
        }
        if (RetryCodes.Contains(statusCode)) {
            return true;
        }
        if (extraRetryCodes is not null && extraRetryCodes.Contains(statusCode)) {
            return true;
        }
        return false;
    }


    public static double CalculateBackoffDelay(int attempt, double baseDelay, double maxDelay, bool enableJitter) {
        double delay = Math.Min(baseDelay * Math.Pow(2, attempt - 1), maxDelay);
        if (enableJitter) {
            lock (Rng) {
                delay *= 0.5 + Rng.NextDouble();
            }
        }
        return Math.Min(delay, maxDelay);
    }


    public static async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> operation,
        RetryOptions options,
        CancellationToken cancellationToken = default
    ) {
        HttpResponseMessage? lastResponse = null;

        for (int attempt = 1; attempt <= options.maxAttempts; attempt++) {
            try {
                var response = await operation().ConfigureAwait(false);

                if (response.IsSuccessStatusCode) {
                    return response;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                bool hasFalRequestId = response.Headers.Contains(FalHeaders.RequestId);
                string? timeoutType = response.Headers.TryGetValues(FalHeaders.RequestTimeoutType, out var ttValues)
                    ? ttValues.FirstOrDefault() : null;

                if (attempt < options.maxAttempts && ShouldRetryResponse(
                    (int)response.StatusCode, body, hasFalRequestId, timeoutType)) {
                    lastResponse?.Dispose();
                    lastResponse = response;
                    double delay = CalculateBackoffDelay(attempt, options.baseDelay, options.maxDelay, options.enableJitter);
                    await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return response;
            } catch (HttpRequestException) when (attempt < options.maxAttempts) {
                double delay = CalculateBackoffDelay(attempt, options.baseDelay, options.maxDelay, options.enableJitter);
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken).ConfigureAwait(false);
            } catch (TaskCanceledException) when (attempt < options.maxAttempts && !cancellationToken.IsCancellationRequested) {
                double delay = CalculateBackoffDelay(attempt, options.baseDelay, options.maxDelay, options.enableJitter);
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new HttpRequestException("Request failed after all retry attempts");
    }


    public static HttpResponseMessage ExecuteWithRetry(
        Func<HttpResponseMessage> operation,
        RetryOptions options
    ) {
        HttpResponseMessage? lastResponse = null;

        for (int attempt = 1; attempt <= options.maxAttempts; attempt++) {
            try {
                var response = operation();

                if (response.IsSuccessStatusCode) {
                    return response;
                }

                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                bool hasFalRequestId = response.Headers.Contains(FalHeaders.RequestId);
                string? timeoutType = response.Headers.TryGetValues(FalHeaders.RequestTimeoutType, out var ttValues)
                    ? ttValues.FirstOrDefault() : null;

                if (attempt < options.maxAttempts && ShouldRetryResponse(
                    (int)response.StatusCode, body, hasFalRequestId, timeoutType)) {
                    lastResponse?.Dispose();
                    lastResponse = response;
                    double delay = CalculateBackoffDelay(attempt, options.baseDelay, options.maxDelay, options.enableJitter);
                    Thread.Sleep(TimeSpan.FromSeconds(delay));
                    continue;
                }

                return response;
            } catch (HttpRequestException) when (attempt < options.maxAttempts) {
                double delay = CalculateBackoffDelay(attempt, options.baseDelay, options.maxDelay, options.enableJitter);
                Thread.Sleep(TimeSpan.FromSeconds(delay));
            }
        }

        throw new HttpRequestException("Request failed after all retry attempts");
    }
}
