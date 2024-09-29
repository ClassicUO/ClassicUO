using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Network.Sockets;

#nullable enable

internal abstract class NetSocket : IDisposable
{
    public abstract IPEndPoint? LocalEndPoint { get; }

    public abstract ValueTask ConnectAsync(Uri uri, CancellationToken token);
    public abstract ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken token);
    public abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken token);
    public abstract ValueTask DisconnectAsync();
    public abstract void Dispose();
}

#nullable disable