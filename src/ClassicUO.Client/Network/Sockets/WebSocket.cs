using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Network.Sockets;

#nullable enable

internal sealed class WebSocket : NetSocket
{
    private const int WS_KEEP_ALIVE_INTERVAL = 5;

    private readonly ClientWebSocket _webSocket;
    private IPEndPoint? localEndPoint;

    public override IPEndPoint? LocalEndPoint => localEndPoint;

    public WebSocket()
    {
        _webSocket = new ClientWebSocket();
        _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(WS_KEEP_ALIVE_INTERVAL);
    }

    public override async ValueTask ConnectAsync(Uri uri, CancellationToken token)
    {
        using SocketsHttpHandler handler = new()
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            ConnectCallback = onConnect
        };

        using HttpMessageInvoker invoker = new(handler);

        await _webSocket.ConnectAsync(uri, invoker, token);

        async ValueTask<Stream> onConnect(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            Socket socket = new(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            try
            {
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                context.InitialRequestMessage.Options.TryAdd("IP", socket.RemoteEndPoint);

                localEndPoint = socket.LocalEndPoint as IPEndPoint;

                return new NetworkStream(socket, true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }

    public override async ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken token)
    {
        await _webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, token);
        return buffer.Length;
    }

    public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken token)
    {
        ValueWebSocketReceiveResult result = await _webSocket.ReceiveAsync(buffer, token);
        return result.Count;
    }

    public override ValueTask DisconnectAsync()
    {
        return new(_webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None));
    }

    public override void Dispose()
    {
        _webSocket.Dispose();
    }
}
