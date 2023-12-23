using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

sealed class Rpc : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly BinaryWriter _writer;
    private readonly ConcurrentDictionary<Guid, RpcMessage> _messages = new ConcurrentDictionary<Guid, RpcMessage>();

    public Rpc(Stream reader, Stream writer)
    {
        _reader = new BinaryReader(reader);
        _writer = new BinaryWriter(writer);
    }


    public void Dispose()
    {
        _messages?.Clear();
        _reader?.Dispose();
        _writer?.Dispose();
    }

    public RpcMessage Request(ArraySegment<byte> payload)
    {
        var reqId = SendMessage(payload);
        //var response = WaitForMessageAsync(reqId, TimeSpan.FromSeconds(10)).ConfigureAwait(false).GetAwaiter().GetResult(); // ReceiveMessage();
        var response = WaitForMessage(reqId, TimeSpan.FromSeconds(10));
        //var response = ReceiveMessage();

        Trace.Assert(reqId.Equals(response.ID));
        Trace.Assert(response.Command == RpcCommand.Response);

        return response;
    }

    RpcMessage WaitForMessage(Guid id, TimeSpan timeout)
    {
        RpcMessage msg = default;
        //var dt = DateTime.UtcNow;
        while (_reader.BaseStream.CanRead && !_messages.TryRemove(id, out msg))
        {
        }

        return msg;
    }

    async Task<RpcMessage> WaitForMessageAsync(Guid id, TimeSpan timeout)
    {
        await Task.Yield();
        return WaitForMessage(id, timeout);
    }

    internal void ResponseTo(RpcMessage request)
    {
        _writer.Write((byte)RpcCommand.Response);
        _writer.Write(request.ID.ToByteArray());
        _writer.Write((ushort)request.Payload.Count);
        _writer.Write(request.Payload.Array, request.Payload.Offset, request.Payload.Count);
        _writer.Flush();
    }

    private Guid SendMessage(ArraySegment<byte> payload)
    {
        var id = Guid.NewGuid();
        _writer.Write((byte)RpcCommand.Request);
        _writer.Write(id.ToByteArray());
        _writer.Write((ushort)payload.Count);
        _writer.Write(payload.Array, payload.Offset, payload.Count);
        _writer.Flush();
        return id;
    }

    internal RpcMessage ReceiveMessage()
    {
        var cmd = (RpcCommand)_reader.BaseStream.ReadByte();
        if (cmd == RpcCommand.Invalid)
        {
            return RpcMessage.Invalid;
        }

        var id = new Guid(
            _reader.ReadUInt32(),
            _reader.ReadUInt16(),
            _reader.ReadUInt16(),
            _reader.ReadByte(),
            _reader.ReadByte(),
            _reader.ReadByte(),
            _reader.ReadByte(),
            _reader.ReadByte(),
            _reader.ReadByte(),
            _reader.ReadByte(),
            _reader.ReadByte()
        );
        var payloadSize = _reader.ReadUInt16();

        if (_reader.BaseStream.CanSeek && _reader.BaseStream.Position - 19 + payloadSize > _reader.BaseStream.Length)
        {
            return RpcMessage.Invalid;
        }

#if NETFRAMEWORK
        var payload = new ArraySegment<byte>(payloadSize == 0 ? Array.Empty<byte>() : new byte[payloadSize], 0, payloadSize);
#else
        var payload = payloadSize == 0 ? ArraySegment<byte>.Empty : new ArraySegment<byte>(new byte[payloadSize], 0, payloadSize);
#endif
        var read = payloadSize == 0 ? 0 : _reader.Read(payload.Array, payload.Offset, payload.Count);

        if (read != payloadSize)
        {

        }

        var msg = new RpcMessage(cmd, id, payload);

        if (cmd == RpcCommand.Response)
        {
            if (!_messages.TryAdd(id, msg))
            {

            }
        }

        return msg;
    }
}

static class RpcConst
{
    public const int READ_WRITE_TIMEOUT = 0;
}

abstract class TcpServerRpc
{
    private TcpListener _server;
    private readonly ConcurrentDictionary<Guid, ClientSession> _clients = new ConcurrentDictionary<Guid, ClientSession>();


    public IReadOnlyDictionary<Guid, ClientSession> Clients => _clients;

    public void Start(string address, int port)
    {
        _server = new TcpListener(IPAddress.Parse(address), port);
        _server.Server.NoDelay = true;
        _server.Server.ReceiveTimeout = RpcConst.READ_WRITE_TIMEOUT;
        _server.Server.SendTimeout = RpcConst.READ_WRITE_TIMEOUT;

        _server.Start();
        _server.BeginAcceptTcpClient(OnAccept, null);
    }

    public void Stop()
    {
        _server.Stop();
    }

    public RpcMessage Request(Guid clientId, ArraySegment<byte> payload)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            return client.Rpc.Request(payload);
        }

        return default;
    }

    protected abstract void OnMessage(Guid id, RpcMessage msg);
    protected virtual void OnClientConnected(Guid id) { }
    protected virtual void OnClientDisconnected(Guid id) { }

    void OnAccept(IAsyncResult ar)
    {
        try
        {
            var client = _server.EndAcceptTcpClient(ar);
            ProcessClient(client);

            _server.BeginAcceptTcpClient(OnAccept, null);

            return;
        }
        catch (SocketException ex)
        {
            Console.WriteLine("[SERVER] socket exception:\n{0}", ex);
        }
        catch (ObjectDisposedException)
        { }


        foreach (var l in _clients)
        {
            l.Value.Rpc.Dispose();
            l.Value.Client.Close();
            l.Value.Client.Dispose();
        }

        _server.Server.Dispose();
        _clients.Clear();
    }

    void ProcessClient(TcpClient client)
    {
        var session = new ClientSession(Guid.NewGuid(), client);

        session.OnDisconnected += () =>
        {
            _clients.TryRemove(session.Guid, out var _);
            OnClientDisconnected(session.Guid);
        };
        session.OnMessage += msg => OnMessage(session.Guid, msg);
        session.Start();

        _clients.TryAdd(session.Guid, session);

        OnClientConnected(session.Guid);
    }

    public void Tick()
    {
        foreach (var l in _clients)
            l.Value.ProcessIncomingMessages();
    }
}

sealed class ClientSession : IDisposable
{
    private bool _disposed;
    private readonly AsyncCallback _onRecv;
    private MemoryStream _incoming;
    private readonly ByteQueue _queue = new ByteQueue();

    public ClientSession(Guid guid, TcpClient client)
    {
        Guid = guid;
        Client = client;

        _onRecv = OnReceive;
    }

    public event Action<RpcMessage> OnMessage;
    public event Action OnDisconnected;

    public Guid Guid { get; }
    public TcpClient Client { get; }
    public Rpc Rpc { get; private set; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Rpc.Dispose();
        _incoming.Dispose();
        _queue.Clear();
        Client.Close();
        Client.Dispose();

        OnDisconnected?.Invoke();
    }

    public void Start()
    {
        var stream = Client.GetStream();
        _queue.Clear();

        _incoming = new MemoryStream(4096);
        Rpc = new Rpc(_incoming, stream);

        var buf = new byte[4096];
        stream.BeginRead(buf, 0, buf.Length, _onRecv, buf);

        //Task.Run(() => ReceiveAsync());
    }

    void ReceiveAsync()
    {
        var buf = new byte[4096];

        try
        {
            while (Client.Connected)
            {
                var stream = Client.GetStream();
                //var read = await stream.ReadAsync(buf, 0, buf.Length);
                var read = stream.Read(buf, 0, buf.Length);
                if (read <= 0)
                {
                    break;
                }

                lock (_queue)
                    _queue.Enqueue(buf, 0, read);

                ProcessIncomingMessages();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Client session exception: {0}", ex);
        }

        Dispose();
    }

    void OnReceive(IAsyncResult ar)
    {
        try
        {
            if (!Client.Connected)
            {
                Dispose();

                return;
            }

            var stream = Client.GetStream();
            var read = stream.EndRead(ar);
            if (read <= 0)
            {
                Dispose();

                return;
            }

            var buf = (byte[])ar.AsyncState;

            lock (_queue)
                _queue.Enqueue(buf, 0, read);

            //ProcessIncomingMessages();

            stream.BeginRead(buf, 0, buf.Length, _onRecv, buf);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Client session exception: {0}", ex);

            Dispose();
        }
    }

    public void ProcessIncomingMessages()
    {
        const int RPC_HEADER_SIZE = 19;

        var buffer = _queue;

        if (buffer == null || buffer.Length <= 0)
            return;

        lock (buffer)
        {
            var len = buffer.Length;
            while (len > 0)
            {
                var cmd = (RpcCommand)buffer.GetPacketID();
                if (cmd == RpcCommand.Invalid)
                    return;

                var packetLen = buffer.GetPacketLength() + RPC_HEADER_SIZE;
                if (len < packetLen)
                    return;

                var buf = ArrayPool<byte>.Shared.Rent(packetLen);
                packetLen = buffer.Dequeue(buf, 0, packetLen);

                lock (_incoming)
                {
                    // TODO: remove the memorystream and use the underlying SendQueue buffer
                    _incoming.Write(buf, 0, packetLen);

                    ArrayPool<byte>.Shared.Return(buf);

                    _incoming.Seek(0, SeekOrigin.Begin);

                    // TODO: read the msg structure just once. We already read the cmd + payloadSize
                    //       maybe worth an header change: [cmd][payloadsize][guid][payload] ?
                    var msg = Rpc.ReceiveMessage();

                    OnMessage?.Invoke(msg);

                    switch (cmd)
                    {
                        case RpcCommand.Request:
                            Rpc.ResponseTo(msg);
                            break;

                        case RpcCommand.Response:
                            break;
                    }

                    _incoming.Seek(0, SeekOrigin.Begin);
                }

                len = buffer.Length;
            }
        }
    }
}

abstract class TcpClientRpc
{
    private ClientSession _session;

    public bool IsConnected => _session?.Client?.Connected ?? false;

    public void Connect(string address, int port)
    {
        if (IsConnected)
        {
            throw new Exception("Client is already connected");
        }

        var tcp = new TcpClient()
        {
            NoDelay = true,
            SendTimeout = RpcConst.READ_WRITE_TIMEOUT,
            ReceiveTimeout = RpcConst.READ_WRITE_TIMEOUT
        };

        tcp.BeginConnect(address, port, OnConnection, tcp);
    }

    void OnConnection(IAsyncResult ar)
    {
        var tcp = (TcpClient)ar.AsyncState;
        tcp.EndConnect(ar);

        _session = new ClientSession(Guid.Empty, tcp);
        _session.OnDisconnected += OnDisconnected;
        _session.OnMessage += OnMessage;
        _session.Start();

        OnConnected();
    }

    public void Tick() => _session?.ProcessIncomingMessages();

    public void Disconnect()
    {
        _session?.Client?.Client?.Disconnect(false);
    }

    public RpcMessage Request(ArraySegment<byte> payload)
    {
        return _session?.Rpc?.Request(payload) ?? RpcMessage.Invalid;
    }

    protected abstract void OnMessage(RpcMessage msg);
    protected virtual void OnConnected() { }
    protected virtual void OnDisconnected() { }
}

readonly struct RpcMessage
{
    public readonly RpcCommand Command;
    public readonly Guid ID;
    public readonly ArraySegment<byte> Payload;

    public RpcMessage(RpcCommand cmd, Guid id, ArraySegment<byte> payload)
        => (Command, ID, Payload) = (cmd, id, payload);

    public static readonly RpcMessage Invalid = new RpcMessage(RpcCommand.Invalid, Guid.Empty, new ArraySegment<byte>(Array.Empty<byte>()));
}

enum RpcCommand
{
    Invalid = -1,
    Request,
    Response
}

sealed class ByteQueue
{
    private int m_Head;
    private int m_Tail;
    private int m_Size;

    private byte[] m_Buffer;

    public int Length { get { return m_Size; } }

    public ByteQueue()
    {
        m_Buffer = new byte[2048];
    }

    public void Clear()
    {
        m_Head = 0;
        m_Tail = 0;
        m_Size = 0;
    }

    private void SetCapacity(int capacity)
    {
        var newBuffer = new byte[capacity];

        if (m_Size > 0)
        {
            if (m_Head < m_Tail)
            {
                Buffer.BlockCopy(m_Buffer, m_Head, newBuffer, 0, m_Size);
            }
            else
            {
                Buffer.BlockCopy(m_Buffer, m_Head, newBuffer, 0, m_Buffer.Length - m_Head);
                Buffer.BlockCopy(m_Buffer, 0, newBuffer, m_Buffer.Length - m_Head, m_Tail);
            }
        }

        m_Head = 0;
        m_Tail = m_Size;
        m_Buffer = newBuffer;
    }

    public byte GetPacketID()
    {
        if (m_Size >= 19)
        {
            return m_Buffer[m_Head];
        }

        return 0xFF;
    }

    public int GetPacketLength()
    {
        if (m_Size >= 19)
        {
            const int OFFSET = 1 + 16 - 1;
            return (m_Buffer[(m_Head + OFFSET + 2) % m_Buffer.Length] << 8) | m_Buffer[(m_Head + OFFSET + 1) % m_Buffer.Length];
        }

        return 0;
    }

    public int Dequeue(byte[] buffer, int offset, int size)
    {
        if (size > m_Size)
        {
            size = m_Size;
        }

        if (size == 0)
        {
            return 0;
        }

        if (buffer != null)
        {
            if (m_Head < m_Tail)
            {
                Buffer.BlockCopy(m_Buffer, m_Head, buffer, offset, size);
            }
            else
            {
                int rightLength = (m_Buffer.Length - m_Head);

                if (rightLength >= size)
                {
                    Buffer.BlockCopy(m_Buffer, m_Head, buffer, offset, size);
                }
                else
                {
                    Buffer.BlockCopy(m_Buffer, m_Head, buffer, offset, rightLength);
                    Buffer.BlockCopy(m_Buffer, 0, buffer, offset + rightLength, size - rightLength);
                }
            }
        }

        m_Head = (m_Head + size) % m_Buffer.Length;
        m_Size -= size;

        if (m_Size == 0)
        {
            m_Head = 0;
            m_Tail = 0;
        }

        return size;
    }

    public void Enqueue(byte[] buffer, int offset, int size)
    {
        if ((m_Size + size) > m_Buffer.Length)
        {
            SetCapacity((m_Size + size + 2047) & ~2047);
        }

        if (m_Head < m_Tail)
        {
            int rightLength = (m_Buffer.Length - m_Tail);

            if (rightLength >= size)
            {
                Buffer.BlockCopy(buffer, offset, m_Buffer, m_Tail, size);
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, m_Buffer, m_Tail, rightLength);
                Buffer.BlockCopy(buffer, offset + rightLength, m_Buffer, 0, size - rightLength);
            }
        }
        else
        {
            Buffer.BlockCopy(buffer, offset, m_Buffer, m_Tail, size);
        }

        m_Tail = (m_Tail + size) % m_Buffer.Length;
        m_Size += size;
    }
}