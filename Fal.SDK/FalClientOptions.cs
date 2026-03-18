namespace Fal.SDK;


public class FalClientOptions {
    public string? apiKey { get; set; }
    public TimeSpan defaultTimeout { get; set; } = TimeSpan.FromSeconds(120);
    public string runHost { get; set; } = "fal.run";
    public string restUrl { get; set; } = "https://rest.fal.ai";
    public string cdnUrl { get; set; } = "https://v3.fal.media";
    public string cdnFallbackUrl { get; set; } = "https://fal.media";
}


public interface IFalKeyProvider {
    string? GetApiKey();
}


public sealed class FalClientProvider {
    private FalClient? instance;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IFalKeyProvider? keyProvider;

    public FalClientProvider(IHttpClientFactory httpClientFactory, IFalKeyProvider? keyProvider = null) {
        this.httpClientFactory = httpClientFactory;
        this.keyProvider = keyProvider;
    }

    public IFalClient client => instance
        ?? throw new InvalidOperationException("FalClient is not configured yet. Call FalClientProvider.Configure() first.");

    public bool isConfigured => instance is not null;

    public void Configure(string apiKey) {
        if (instance is not null) {
            throw new InvalidOperationException("FalClient is already configured.");
        }
        var options = new FalClientOptions { apiKey = apiKey };
        instance = new FalClient(httpClientFactory, options, keyProvider);
    }
}
