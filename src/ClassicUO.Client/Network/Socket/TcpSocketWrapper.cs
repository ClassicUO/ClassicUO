using System;
using System.Net;
using System.Net.Sockets;
using ClassicUO.Utility.Logging;
using System.Threading; // for CancellationTokenSource
using System.Threading.Tasks;

namespace ClassicUO.Network.Socket;

sealed class TcpSocketWrapper : SocketWrapper
{
    private TcpClient _socket;

    private readonly CircularBuffer readBuffer = new CircularBuffer();
    private CancellationTokenSource readCancellationTokenSource;
    private Task readTask;

    public override bool IsConnected => _socket?.Client?.Connected ?? false;

    public override EndPoint LocalEndPoint => _socket?.Client?.LocalEndPoint;


    private async Task ConnectAsync(IPAddress ip, int port, CancellationToken cancellationToken)
    {
        if (IsConnected)
            return;

        _socket = new TcpClient();
        _socket.NoDelay = true;

        Log.Trace($"Trying address [{ip}]:{port}");
        await _socket.ConnectAsync(ip, port, cancellationToken);

        readCancellationTokenSource = new CancellationTokenSource();
        readTask = ReadTask(_socket.GetStream(), readCancellationTokenSource.Token);
    }

    public override async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        Exception lastError = null;
        var addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        foreach (var address in addresses)
        {
            try
            {
                await ConnectAsync(address, uri.Port, cancellationToken).WaitAsync(TimeSpan.FromSeconds(5));
                return;
            }
            catch (TimeoutException ex)
            {
                Log.Warn($"Connection to [{address}]:{uri.Port} timed out");
                lastError = new TimeoutException($"Connection to [{address}]:{uri.Port} timed out");
            }
            catch (SocketException ex)
            {
                Log.Warn($"Connection to [{address}]:{uri.Port} failed: {ex.Message}");
                lastError = ex;
            }

            /* disconnect and try the next address */
            Disconnect();
        }

        /* rethrow the last exception */
        throw lastError;
    }

    /**
     * Fills #readBuffer asynchronously with data received on the
     * TCP connection.  This data will be consumed by method Read().
     */
    private async Task ReadTask(NetworkStream stream, CancellationToken cancellationToken)
    {
        byte[] tempBuffer = new byte[4096];

        while (true)
        {
            int nbytes = await stream.ReadAsync(tempBuffer, cancellationToken);
            if (nbytes <= 0)
                break;

            lock (readBuffer)
            {
                readBuffer.Enqueue(tempBuffer.AsSpan(0, nbytes));
            }
        }
    }

    public override void Send(byte[] buffer, int offset, int count)
    {
        var stream = _socket.GetStream();
        stream.Write(buffer, offset, count);
        stream.Flush();
    }

    public override int Read(byte[] buffer)
    {
        if (_socket == null) return 0;

        if (!IsConnected)
        {
            InvokeOnDisconnected();
            Disconnect();

            return 0;
        }

        lock (readBuffer)
        {
            int nbytes = readBuffer.Dequeue(buffer, 0, buffer.Length);
            if (nbytes > 0)
                return nbytes;
        }

        if (readTask.IsCompleted)
        {
            /* the readTask finishes only if the connection was
               closed (by the peer) */
            InvokeOnDisconnected();
            Disconnect();
            return 0;
        }

        /* no data in the buffer (yet) */
        return 0;
    }

    public override void Disconnect()
    {
        readCancellationTokenSource?.Cancel();
        _socket?.Close();
        Dispose();
    }

    public override void Dispose()
    {
        _socket?.Dispose();
        _socket = null;
        readCancellationTokenSource = null;
        readBuffer.Clear();
    }
}
