using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fal.SDK.Http;
using Fal.SDK.Queue;
using Fal.SDK.Storage;
using Fal.SDK.Streaming;
using Fal.SDK.Realtime;

namespace Fal.SDK.Extensions;


public static class ServiceCollectionExtensions {
    public static IServiceCollection AddFalClient(this IServiceCollection services, Action<FalClientOptions>? configure = null) {
        var options = new FalClientOptions();
        if (configure is not null) {
            configure(options);
        }

        services.AddSingleton(options);
        services.AddHttpClient(FalHttpClient.HttpClientName);
        services.TryAddSingleton<FalHttpClient>(sp => new FalHttpClient(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<FalClientOptions>(),
            sp.GetService<IFalKeyProvider>()
        ));
        services.TryAddSingleton<IFalClient>(sp => new FalClient(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<FalClientOptions>(),
            sp.GetService<IFalKeyProvider>()
        ));

        return services;
    }


    public static IServiceCollection AddFalClient(this IServiceCollection services, string apiKey) {
        return services.AddFalClient(options => options.apiKey = apiKey);
    }


    public static IServiceCollection AddFalClient(this IServiceCollection services, Func<IServiceProvider, string> apiKeyFactory) {
        services.AddHttpClient(FalHttpClient.HttpClientName);
        services.AddSingleton(sp => {
            string key = apiKeyFactory(sp);
            return new FalClientOptions { apiKey = key };
        });
        services.TryAddSingleton<FalHttpClient>(sp => new FalHttpClient(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<FalClientOptions>(),
            sp.GetService<IFalKeyProvider>()
        ));
        services.TryAddSingleton<IFalClient>(sp => new FalClient(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<FalClientOptions>(),
            sp.GetService<IFalKeyProvider>()
        ));

        return services;
    }


    public static IServiceCollection AddFalClientProvider(this IServiceCollection services) {
        services.AddHttpClient(FalHttpClient.HttpClientName);
        services.TryAddSingleton<FalClientProvider>(sp => {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var keyProvider = sp.GetService<IFalKeyProvider>();
            return new FalClientProvider(factory, keyProvider);
        });
        return services;
    }
}
