# Fal.SDK

[![NuGet](https://img.shields.io/nuget/v/Fal.SDK.svg)](https://www.nuget.org/packages/Fal.SDK/)
[![License](https://img.shields.io/github/license/VeyDlin/Fal.SDK)](LICENSE)

Unofficial C# client SDK for [fal.ai](https://fal.ai) — run AI models, queue long-running requests, stream results via SSE, upload files to storage, and connect via realtime WebSocket.

## Install

```bash
dotnet add package Fal.SDK
```

## Quick start

```csharp
using Fal.SDK;
using Fal.SDK.Models;
using System.Text.Json;

var client = new FalClient("your-fal-key");

// Run synchronously (short requests)
var input = JsonDocument.Parse("""{"prompt": "a cute shih-tzu puppy"}""").RootElement;
var result = await client.RunAsync("fal-ai/flux/dev", new RunOptions { input = input });
Console.WriteLine(result);
```

## Queue (long-running requests)

```csharp
// Submit and poll until completion
var result = await client.SubscribeAsync("fal-ai/flux/dev", new SubscribeOptions {
    input = input,
    onQueueUpdate = status => Console.WriteLine(status)
});

// Or manage the queue manually
var handle = await client.queue.SubmitAsync("fal-ai/flux/dev", new SubmitOptions { input = input });
var status = await client.queue.StatusAsync("fal-ai/flux/dev", handle.requestId);
var result = await client.queue.ResultAsync("fal-ai/flux/dev", handle.requestId);
await client.queue.CancelAsync("fal-ai/flux/dev", handle.requestId);
```

## Streaming (SSE)

```csharp
await foreach (var item in client.streaming.StreamAsync("fal-ai/flux/dev", new RunOptions { input = input })) {
    Console.WriteLine(item);
}
```

## Storage

```csharp
string url = await client.storage.UploadAsync(fileBytes, "image/png", "photo.png");
string url = await client.storage.UploadFileAsync("/path/to/file.png");
```

## Realtime (WebSocket)

```csharp
await using var connection = await client.realtime.ConnectAsync("fal-ai/flux/dev");
await connection.SendAsync(input);
var result = await connection.ReceiveAsync();
```

## ASP.NET Core integration

### Registration

```csharp
// Key known at startup
services.AddFalClient("your-fal-key");

// Key from configuration
services.AddFalClient(provider => {
    var config = provider.GetRequiredService<IConfiguration>();
    return config["Fal:ApiKey"]!;
});

// Key loaded later (e.g. from database after startup)
services.AddFalClientProvider();
```

For deferred initialization, call `Configure` when the key becomes available:

```csharp
public class AppInitializer(FalClientProvider falProvider, IMyConfigService config) {
    public async Task InitializeAsync() {
        string key = await config.GetSecretAsync("FAL_KEY");
        falProvider.Configure(key);
    }
}
```

### Per-user API keys

```csharp
services.AddFalClient();
services.AddScoped<IFalKeyProvider, UserFalKeyProvider>();

public class UserFalKeyProvider(IHttpContextAccessor http, AppDbContext db) : IFalKeyProvider {
    public string? GetApiKey() {
        var userId = http.HttpContext?.User.FindFirst("sub")?.Value;
        return db.UserSettings.FirstOrDefault(s => s.userId == userId)?.falApiKey;
    }
}
```

### Per-request key override

```csharp
await fal.RunAsync("fal-ai/flux/dev", new RunOptions {
    input = input,
    apiKey = "specific-key-for-this-request"
});
```

**Key priority:** per-request > `IFalKeyProvider` > `FalClientOptions.apiKey` > `FAL_KEY` env var

## Sync API

Every async method has a sync counterpart:

```csharp
var result = client.Run("fal-ai/flux/dev", new RunOptions { input = input });
var handle = client.queue.Submit("fal-ai/flux/dev", new SubmitOptions { input = input });
var result = client.Subscribe("fal-ai/flux/dev");
string url = client.storage.Upload(fileBytes, "image/png");
```
