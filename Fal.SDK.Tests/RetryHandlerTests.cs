using Fal.SDK.Http;

namespace Fal.SDK.Tests;


public class RetryHandlerTests {
    public class IsIngressErrorTests {
        [Fact]
        public void NonIngressStatusCode_ReturnsFalse() {
            Assert.False(RetryHandler.IsIngressError(500, "error", hasFalRequestId: false));
        }


        [Fact]
        public void IngressCodeWithFalRequestId_ReturnsFalse() {
            Assert.False(RetryHandler.IsIngressError(504, "error", hasFalRequestId: true));
        }


        [Fact]
        public void IngressCodeWithNginxInBody_ReturnsTrue() {
            Assert.True(RetryHandler.IsIngressError(502, "<html>nginx error</html>", hasFalRequestId: false));
        }


        [Fact]
        public void IngressCodeWithoutNginx_ReturnsFalse() {
            Assert.False(RetryHandler.IsIngressError(503, "Service unavailable", hasFalRequestId: false));
        }
    }


    public class ShouldRetryResponseTests {
        [Fact]
        public void UserTimeout504_DoesNotRetry() {
            Assert.False(RetryHandler.ShouldRetryResponse(
                statusCode: 504,
                responseBody: "nginx timeout",
                hasFalRequestId: false,
                timeoutType: "user"
            ));
        }


        [Fact]
        public void IngressErrorWithoutTimeoutType_Retries() {
            Assert.True(RetryHandler.ShouldRetryResponse(
                statusCode: 502,
                responseBody: "nginx error",
                hasFalRequestId: false,
                timeoutType: null
            ));
        }


        [Theory]
        [InlineData(408)]
        [InlineData(409)]
        [InlineData(429)]
        public void RetryCodesAlwaysRetry(int statusCode) {
            Assert.True(RetryHandler.ShouldRetryResponse(
                statusCode: statusCode,
                responseBody: null,
                hasFalRequestId: false,
                timeoutType: null
            ));
        }


        [Fact]
        public void ExtraRetryCodesRetry() {
            Assert.False(RetryHandler.ShouldRetryResponse(418, null, false, null));
            Assert.True(RetryHandler.ShouldRetryResponse(418, null, false, null, [418]));
        }


        [Fact]
        public void NonRetryableCode_DoesNotRetry() {
            Assert.False(RetryHandler.ShouldRetryResponse(400, null, false, null));
        }


        [Fact]
        public void Server504WithRequestId_DoesNotRetry() {
            Assert.False(RetryHandler.ShouldRetryResponse(
                statusCode: 504,
                responseBody: "timeout",
                hasFalRequestId: true,
                timeoutType: null
            ));
        }
    }


    public class CalculateBackoffDelayTests {
        [Fact]
        public void ExponentialBackoff_CorrectValues() {
            double delay1 = RetryHandler.CalculateBackoffDelay(1, 1.0, 30.0, enableJitter: false);
            double delay2 = RetryHandler.CalculateBackoffDelay(2, 1.0, 30.0, enableJitter: false);
            double delay3 = RetryHandler.CalculateBackoffDelay(3, 1.0, 30.0, enableJitter: false);

            Assert.Equal(1.0, delay1);
            Assert.Equal(2.0, delay2);
            Assert.Equal(4.0, delay3);
        }


        [Fact]
        public void RespectsMaxDelay() {
            double delay = RetryHandler.CalculateBackoffDelay(10, 1.0, 5.0, enableJitter: false);
            Assert.Equal(5.0, delay);
        }


        [Fact]
        public void JitterAddsVariation() {
            double delay1 = RetryHandler.CalculateBackoffDelay(2, 1.0, 30.0, enableJitter: true);
            Assert.InRange(delay1, 1.0, 3.0);
        }
    }
}
