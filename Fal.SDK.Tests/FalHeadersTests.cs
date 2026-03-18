using Fal.SDK.Http;
using Fal.SDK.Models;

namespace Fal.SDK.Tests;


public class FalHeadersTests {
    public class TimeoutHeaderTests {
        [Fact]
        public void ValidIntegerTimeout_AddsHeader() {
            var headers = new Dictionary<string, string>();
            FalHeaders.AddTimeoutHeader(30, headers);

            Assert.True(headers.ContainsKey(FalHeaders.RequestTimeout));
            Assert.Equal("30", headers[FalHeaders.RequestTimeout]);
        }


        [Fact]
        public void ValidFloatTimeout_AddsHeader() {
            var headers = new Dictionary<string, string>();
            FalHeaders.AddTimeoutHeader(45.5, headers);

            Assert.True(headers.ContainsKey(FalHeaders.RequestTimeout));
            Assert.Equal("45.5", headers[FalHeaders.RequestTimeout]);
        }


        [Fact]
        public void TimeoutBelowMinimum_Throws() {
            var headers = new Dictionary<string, string>();
            var ex = Assert.Throws<ArgumentException>(() => FalHeaders.AddTimeoutHeader(0.5, headers));
            Assert.Contains("must be greater than", ex.Message);
        }


        [Fact]
        public void ZeroTimeout_Throws() {
            var headers = new Dictionary<string, string>();
            Assert.Throws<ArgumentException>(() => FalHeaders.AddTimeoutHeader(0, headers));
        }


        [Fact]
        public void NegativeTimeout_Throws() {
            var headers = new Dictionary<string, string>();
            Assert.Throws<ArgumentException>(() => FalHeaders.AddTimeoutHeader(-5, headers));
        }


        [Fact]
        public void ExactMinimum_Throws() {
            var headers = new Dictionary<string, string>();
            Assert.Throws<ArgumentException>(() => FalHeaders.AddTimeoutHeader(0.99, headers));
        }


        [Fact]
        public void JustAboveMinimum_Succeeds() {
            var headers = new Dictionary<string, string>();
            FalHeaders.AddTimeoutHeader(1.01, headers);
            Assert.True(headers.ContainsKey(FalHeaders.RequestTimeout));
        }


        [Fact]
        public void PreservesExistingHeaders() {
            var headers = new Dictionary<string, string> { ["X-Existing"] = "value" };
            FalHeaders.AddTimeoutHeader(30, headers);

            Assert.Equal("value", headers["X-Existing"]);
            Assert.True(headers.ContainsKey(FalHeaders.RequestTimeout));
        }


        [Fact]
        public void OverwritesExistingTimeoutHeader() {
            var headers = new Dictionary<string, string> { [FalHeaders.RequestTimeout] = "10" };
            FalHeaders.AddTimeoutHeader(30, headers);

            Assert.Equal("30", headers[FalHeaders.RequestTimeout]);
        }
    }


    public class HintHeaderTests {
        [Fact]
        public void AddsHintHeader() {
            var headers = new Dictionary<string, string>();
            FalHeaders.AddHintHeader("gpu-a100", headers);

            Assert.Equal("gpu-a100", headers[FalHeaders.RunnerHint]);
        }
    }


    public class PriorityHeaderTests {
        [Fact]
        public void NormalPriority_AddsHeader() {
            var headers = new Dictionary<string, string>();
            FalHeaders.AddPriorityHeader(QueuePriority.Normal, headers);

            Assert.Equal("normal", headers[FalHeaders.QueuePriority]);
        }


        [Fact]
        public void LowPriority_AddsHeader() {
            var headers = new Dictionary<string, string>();
            FalHeaders.AddPriorityHeader(QueuePriority.Low, headers);

            Assert.Equal("low", headers[FalHeaders.QueuePriority]);
        }
    }
}
