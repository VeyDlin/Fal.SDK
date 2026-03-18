namespace Fal.SDK.Tests;


public class FalClientProviderTests {
    private class FakeHttpClientFactory : IHttpClientFactory {
        public HttpClient CreateClient(string name) {
            return new HttpClient();
        }
    }


    [Fact]
    public void IsConfigured_FalseBeforeConfigure() {
        var provider = new FalClientProvider(new FakeHttpClientFactory());
        Assert.False(provider.isConfigured);
    }


    [Fact]
    public void Client_ThrowsBeforeConfigure() {
        var provider = new FalClientProvider(new FakeHttpClientFactory());
        Assert.Throws<InvalidOperationException>(() => provider.client);
    }


    [Fact]
    public void Configure_MakesClientAvailable() {
        var provider = new FalClientProvider(new FakeHttpClientFactory());
        provider.Configure("test-key");

        Assert.True(provider.isConfigured);
        Assert.NotNull(provider.client);
    }


    [Fact]
    public void Configure_CalledTwice_Throws() {
        var provider = new FalClientProvider(new FakeHttpClientFactory());
        provider.Configure("test-key");

        Assert.Throws<InvalidOperationException>(() => provider.Configure("another-key"));
    }
}
