using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Utility.Logging;
using TcpSocket = System.Net.Sockets.Socket;
using static System.Buffers.ArrayPool<byte>;

namespace ClassicUO.Network.Socket;

/// <summary>
/// Handles websocket connections to shards that support it. `ws(s)://[hostname]` as the ip in settings.json.
/// For testing see `tools/ws/README.md` 
/// </summary>
sealed class WebSocketWrapper : SocketWrapper
{
    private const int MAX_RECEIVE_BUFFER_SIZE = 1024 * 1024; // 1MB
    private const int WS_KEEP_ALIVE_INTERVAL = 5;            // seconds

    private ClientWebSocket _webSocket;
    private TcpSocket _rawSocket;

    public override bool IsConnected => _webSocket?.State is WebSocketState.Connecting or WebSocketState.Open;
    public override EndPoint LocalEndPoint => _rawSocket?.LocalEndPoint;
    public bool IsCanceled => _tokenSource.IsCancellationRequested;

    private CancellationTokenSource _tokenSource = new();
    private CircularBuffer _receiveStream;

    public override void Send(byte[] buffer, int offset, int count) =>
        _webSocket.SendAsync(buffer.AsMemory().Slice(offset, count), WebSocketMessageType.Binary, true, _tokenSource.Token);

    public override int Read(byte[] buffer)
    {
        lock (_receiveStream)
        {
            return _receiveStream.Dequeue(buffer, 0, buffer.Length);
        }
    }

    public override async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (IsConnected)
            return;

        _tokenSource = new CancellationTokenSource();
        _receiveStream = new CircularBuffer();

        await ConnectWebSocketAsyncCore(uri, cancellationToken);
    }


    private async Task ConnectWebSocketAsyncCore(Uri uri, CancellationToken cancellationToken)
    {
        // Take control of creating the raw socket, turn off Nagle, also lets us peek at `Available` bytes.
        _rawSocket = new TcpSocket(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        _webSocket = new ClientWebSocket();
        _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(WS_KEEP_ALIVE_INTERVAL); // ping/pong

        using var httpClient = new HttpClient
        (
            new SocketsHttpHandler
            {
                ConnectCallback = async (context, token) =>
                {
                    try
                    {
                        await _rawSocket.ConnectAsync(context.DnsEndPoint, token);

                        return new NetworkStream(_rawSocket, ownsSocket: true);
                    }
                    catch
                    {
                        _rawSocket?.Dispose();
                        _rawSocket = null;
                        _webSocket?.Dispose();
                        _webSocket = null;

                        throw;
                    }
                }
            }
        );


        await _webSocket.ConnectAsync(uri, httpClient, cancellationToken);

        Log.Trace($"Connected WebSocket: {uri}");

        // Kicks off the async receiving loop 
        StartReceiveAsync().ConfigureAwait(false);
    }

    private async Task StartReceiveAsync()
    {
        var buffer = Shared.Rent(4096);
        var memory = buffer.AsMemory();
        var position = 0;

        try
        {
            while (IsConnected)
            {
                GrowReceiveBufferIfNeeded(ref buffer, ref memory);

                var receiveResult = await _webSocket.ReceiveAsync(memory.Slice(position), _tokenSource.Token);

                // Ignoring message types:
                // 1. WebSocketMessageType.Text: shouldn't be sent by the server, though might be useful for multiplexing commands
                // 2. WebSocketMessageType.Close: will be handled by IsConnected
                if (receiveResult.MessageType == WebSocketMessageType.Binary)
                    position += receiveResult.Count;

                if (!receiveResult.EndOfMessage)
                    continue;

                lock (_receiveStream)
                {
                    _receiveStream.Enqueue(buffer, 0, position);
                }

                position = 0;
            }
        }
        catch (OperationCanceledException)
        {
            Log.Trace("WebSocket OperationCanceledException on websocket " + (IsCanceled ? "(was requested)" : "(remote cancelled)"));
        }
        catch (Exception e)
        {
            Log.Trace($"WebSocket error in StartReceiveAsync {e}");
            InvokeOnError(SocketError.SocketError);
        }
        finally
        {
            Shared.Return(buffer);
        }

        if (!IsCanceled)
            InvokeOnError(SocketError.ConnectionReset);
    }

    // This is probably unnecessary, but WebSocket frames can be up to 2^63 bytes so we put some cap on it, yet to see packets larger than 4KB come through.
    // We peek the raw tcp socket available bytes, grow if the frame is bigger, we're naively assuming no compression.
    private void GrowReceiveBufferIfNeeded(ref byte[] buffer, ref Memory<byte> memory)
    {
        if (_rawSocket.Available <= buffer.Length)
            return;

        if (_rawSocket.Available > MAX_RECEIVE_BUFFER_SIZE)
            throw new SocketException((int)SocketError.MessageSize, $"WebSocket message frame too large: {_rawSocket.Available} > {MAX_RECEIVE_BUFFER_SIZE}");

        Log.Trace($"WebSocket growing receive buffer {buffer.Length} bytes to {_rawSocket.Available} bytes");

        Shared.Return(buffer);
        buffer = Shared.Rent(_rawSocket.Available);
        memory = buffer.AsMemory();
    }

    public override void Disconnect()
    {
        if (!IsConnected)
            return;

        _tokenSource?.Cancel();
        _webSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", _tokenSource?.Token ?? default);
    }

    public override void Dispose()
    {
    }
}
