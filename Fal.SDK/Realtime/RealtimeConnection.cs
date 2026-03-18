using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Fal.SDK.Realtime;


public class RealtimeConnection : IAsyncDisposable, IDisposable {
    private readonly ClientWebSocket webSocket;

    public RealtimeConnection(ClientWebSocket webSocket) {
        this.webSocket = webSocket;
    }


    public async Task SendAsync(JsonElement input, CancellationToken cancellationToken = default) {
        string json = input.GetRawText();
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
    }


    public async Task<JsonElement?> ReceiveAsync(CancellationToken cancellationToken = default) {
        var buffer = new byte[8192];
        using var ms = new MemoryStream();

        while (true) {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close) {
                return null;
            }

            ms.Write(buffer, 0, result.Count);

            if (result.EndOfMessage) {
                ms.Position = 0;

                if (result.MessageType == WebSocketMessageType.Text) {
                    string text = Encoding.UTF8.GetString(ms.ToArray());

                    try {
                        var doc = JsonDocument.Parse(text);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("type", out var typeProp)) {
                            string? type = typeProp.GetString();
                            if (type == "x-fal-error") {
                                string error = root.TryGetProperty("error", out var e) ? e.GetString() ?? "UNKNOWN" : "UNKNOWN";
                                string reason = root.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
                                throw new RealtimeException(error, reason);
                            }
                            if (type == "x-fal-message") {
                                ms.SetLength(0);
                                continue;
                            }
                        }

                        return root.Clone();
                    } catch (JsonException) {
                        return JsonDocument.Parse($"{{\"payload\":\"{text}\"}}").RootElement.Clone();
                    }
                }

                return JsonDocument.Parse(ms.ToArray()).RootElement.Clone();
            }
        }
    }


    public async Task CloseAsync(CancellationToken cancellationToken = default) {
        if (webSocket.State == WebSocketState.Open) {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken).ConfigureAwait(false);
        }
    }


    public async ValueTask DisposeAsync() {
        try {
            await CloseAsync().ConfigureAwait(false);
        } catch {
            // ignore close errors during dispose
        }
        webSocket.Dispose();
    }


    public void Dispose() {
        try {
            if (webSocket.State == WebSocketState.Open) {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).GetAwaiter().GetResult();
            }
        } catch {
            // ignore
        }
        webSocket.Dispose();
    }
}


public class RealtimeException : Exception {
    public string error { get; }
    public string reason { get; }

    public RealtimeException(string error, string reason)
        : base(string.IsNullOrEmpty(reason) ? error : $"{error}: {reason}") {
        this.error = error;
        this.reason = reason;
    }
}
