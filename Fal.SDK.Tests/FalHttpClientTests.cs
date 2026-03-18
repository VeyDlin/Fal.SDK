using Fal.SDK.Http;

namespace Fal.SDK.Tests;


public class FalHttpClientTests {
    private class FakeHttpClientFactory : IHttpClientFactory {
        public HttpClient CreateClient(string name) {
            return new HttpClient();
        }
    }


    private class FakeKeyProvider : IFalKeyProvider {
        private readonly string key;
        public FakeKeyProvider(string key) {
            this.key = key;
        }
        public string? GetApiKey() {
            return key;
        }
    }


    [Fact]
    public void ResolveApiKey_PerRequestKey_HasHighestPriority() {
        var options = new FalClientOptions { apiKey = "options-key" };
        var provider = new FakeKeyProvider("provider-key");
        var client = new FalHttpClient(new FakeHttpClientFactory(), options, provider);

        Assert.Equal("per-request-key", client.ResolveApiKey("per-request-key"));
    }


    [Fact]
    public void ResolveApiKey_ProviderKey_SecondPriority() {
        var options = new FalClientOptions { apiKey = "options-key" };
        var provider = new FakeKeyProvider("provider-key");
        var client = new FalHttpClient(new FakeHttpClientFactory(), options, provider);

        Assert.Equal("provider-key", client.ResolveApiKey(null));
    }


    [Fact]
    public void ResolveApiKey_OptionsKey_ThirdPriority() {
        var options = new FalClientOptions { apiKey = "options-key" };
        var client = new FalHttpClient(new FakeHttpClientFactory(), options);

        Assert.Equal("options-key", client.ResolveApiKey(null));
    }


    [Fact]
    public void ResolveApiKey_NoKey_Throws() {
        var options = new FalClientOptions();
        var client = new FalHttpClient(new FakeHttpClientFactory(), options);

        Environment.SetEnvironmentVariable("FAL_KEY", null);
        Assert.Throws<InvalidOperationException>(() => client.ResolveApiKey(null));
    }


    [Fact]
    public void BuildRunUrl_Simple() {
        var options = new FalClientOptions();
        var client = new FalHttpClient(new FakeHttpClientFactory(), options);

        Assert.Equal("https://fal.run/fal-ai/flux/dev", client.BuildRunUrl("fal-ai/flux/dev"));
    }


    [Fact]
    public void BuildRunUrl_WithPath() {
        var options = new FalClientOptions();
        var client = new FalHttpClient(new FakeHttpClientFactory(), options);

        Assert.Equal("https://fal.run/fal-ai/flux/dev/stream", client.BuildRunUrl("fal-ai/flux/dev", "stream"));
    }


    [Fact]
    public void BuildQueueUrl_Simple() {
        var options = new FalClientOptions();
        var client = new FalHttpClient(new FakeHttpClientFactory(), options);

        Assert.Equal("https://queue.fal.run/fal-ai/flux/dev", client.BuildQueueUrl("fal-ai/flux/dev"));
    }
}
