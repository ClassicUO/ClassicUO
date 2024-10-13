using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Network.Sockets;

#nullable enable

internal sealed class TcpSocket : NetSocket
{
    private readonly Socket _socket;

    public override IPEndPoint? LocalEndPoint => _socket.LocalEndPoint as IPEndPoint;

    public TcpSocket()
    {
        _socket = new(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };
    }

    public override ValueTask ConnectAsync(Uri uri, CancellationToken token)
    {
        return new(_socket.ConnectAsync(uri.Host, uri.Port));
    }

    public override ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken token)
    {
        return _socket.SendAsync(buffer, token);
    }

    public override ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken token)
    {
        return _socket.ReceiveAsync(buffer, token);
    }

    public override ValueTask DisconnectAsync()
    {
        _socket.Close();
        return ValueTask.CompletedTask;
    }

    public override void Dispose()
    {
        _socket.Dispose();
    }
}
