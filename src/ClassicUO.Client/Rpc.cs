using System;
<<<<<<< HEAD
<<<<<<< HEAD
using System.Buffers;
<<<<<<< HEAD
=======
using System.Buffers;
>>>>>>> + move managed plugin to rpc
using System.Buffers.Binary;
=======
using System.Collections;
>>>>>>> rpc support
=======
using System.Buffers.Binary;
>>>>>>> more rpc
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

=======
=======
=======
using System.Runtime.CompilerServices;
>>>>>>> performance improvement
using System.Runtime.ExceptionServices;
>>>>>>> rpc
using System.Threading;
using System.Threading.Tasks;

<<<<<<< HEAD
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
>>>>>>> rpc support
=======
>>>>>>> more rpc

static class RpcConst
{
    public const int READ_WRITE_TIMEOUT = 0;
<<<<<<< HEAD
<<<<<<< HEAD
    public const int RPC_MESSAGE_SIZE = 1 + 8 + 2;
=======
>>>>>>> rpc support
=======
    public const int RPC_MESSAGE_SIZE = 1 + 8 + 2;
>>>>>>> performance improvement
}

abstract class TcpServerRpc
{
    private TcpListener _server;
<<<<<<< HEAD
<<<<<<< HEAD
    private readonly ConcurrentDictionary<Guid, TcpSession> _clients = new ConcurrentDictionary<Guid, TcpSession>();


    public IReadOnlyDictionary<Guid, TcpSession> Clients => _clients;

    public event EventHandler<Guid> OnSocketConnected, OnSocketDisconnected;
=======
    private readonly ConcurrentDictionary<Guid, ClientSession> _clients = new ConcurrentDictionary<Guid, ClientSession>();


    public IReadOnlyDictionary<Guid, ClientSession> Clients => _clients;
>>>>>>> rpc support
=======
    private readonly ConcurrentDictionary<Guid, TcpSession> _clients = new ConcurrentDictionary<Guid, TcpSession>();


    public IReadOnlyDictionary<Guid, TcpSession> Clients => _clients;
>>>>>>> more rpc

    public event EventHandler<Guid> OnSocketConnected, OnSocketDisconnected;

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

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
    public async ValueTask<ArraySegment<byte>> RequestAsync(Guid clientId, ArraySegment<byte> payload)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            return await client.RequestAsync(payload);
        }

        return new ArraySegment<byte>(Array.Empty<byte>());
    }

    public ArraySegment<byte> Request(Guid clientId, ArraySegment<byte> payload)
        => AsyncHelpers.RunSync(() => RequestAsync(clientId, payload));

    protected abstract ArraySegment<byte> OnRequest(Guid id, ArraySegment<byte> msg);
=======
    public RpcMessage Request(Guid clientId, ArraySegment<byte> payload)
=======
    public async Task<RpcMessage> Request(Guid clientId, ArraySegment<byte> payload)
>>>>>>> more rpc
=======
    public async Task<RpcMessage> RequestAsync(Guid clientId, ArraySegment<byte> payload)
>>>>>>> + move managed plugin to rpc
=======
    public async ValueTask<RpcMessage> RequestAsync(Guid clientId, ArraySegment<byte> payload)
>>>>>>> performance improvement
=======
    public async ValueTask<ArraySegment<byte>> RequestAsync(Guid clientId, ArraySegment<byte> payload)
>>>>>>> + removed rpc message
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            return await client.RequestAsync(payload);
        }

        return new ArraySegment<byte>(Array.Empty<byte>());
    }

    public ArraySegment<byte> Request(Guid clientId, ArraySegment<byte> payload)
        => AsyncHelpers.RunSync(() => RequestAsync(clientId, payload));

<<<<<<< HEAD
<<<<<<< HEAD
    public event EventHandler<Guid> OnSocketConnected, OnSocketDisconnected;

<<<<<<< HEAD
    protected abstract void OnMessage(Guid id, RpcMessage msg);
>>>>>>> rpc support
=======
=======
>>>>>>> performance improvement
    protected abstract ArraySegment<byte> OnRequest(Guid id, RpcMessage msg);
>>>>>>> message serialization
=======
    protected abstract ArraySegment<byte> OnRequest(Guid id, ArraySegment<byte> msg);
>>>>>>> + removed rpc message
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
<<<<<<< HEAD
<<<<<<< HEAD
=======
            l.Value.Rpc.Dispose();
>>>>>>> rpc support
=======
>>>>>>> more rpc
            l.Value.Client.Close();
            l.Value.Client.Dispose();
        }

        _server.Server.Dispose();
        _clients.Clear();
    }

    void ProcessClient(TcpClient client)
    {
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        var id = Guid.NewGuid();
        var session = new TcpSession(id, client, (msg) => OnRequest(id, msg));

        void onDisconnected()
        {
            _clients.TryRemove(session.Guid, out var _);
            OnSocketDisconnected?.Invoke(this, session.Guid);
            OnClientDisconnected(session.Guid);

            session.OnDisconnected -= onDisconnected;
        }

        session.OnDisconnected += onDisconnected;
=======
        var session = new ClientSession(Guid.NewGuid(), client);
=======
        var session = new TcpSession(Guid.NewGuid(), client);
>>>>>>> more rpc
=======
        var id = Guid.NewGuid();
        var session = new TcpSession(id, client, (msg) => OnRequest(id, msg));
>>>>>>> minor improvements

        void onDisconnected()
        {
            _clients.TryRemove(session.Guid, out var _);
            OnSocketDisconnected?.Invoke(this, session.Guid);
            OnClientDisconnected(session.Guid);
<<<<<<< HEAD
        };
<<<<<<< HEAD
<<<<<<< HEAD
        session.OnMessage += msg => OnMessage(session.Guid, msg);
>>>>>>> rpc support
=======
        session.OnMessage += msg => {
            switch (msg.Command)
            {
                case RpcCommand.Request:
                    OnRequest?.Invoke(this, msg);
                    break;
                case RpcCommand.Response:
                    OnResponse?.Invoke(this, msg);
                    break;
            }
            OnMessage(session.Guid, msg);
        };
>>>>>>> + move managed plugin to rpc
=======
=======

            session.OnDisconnected -= onDisconnected;
        }

        session.OnDisconnected += onDisconnected;
<<<<<<< HEAD
>>>>>>> performance improvement
        session.OnRequest += msg => OnRequest(session.Guid, msg);
>>>>>>> message serialization
=======
>>>>>>> minor improvements
        session.Start();

        _clients.TryAdd(session.Guid, session);

<<<<<<< HEAD
<<<<<<< HEAD
        OnSocketConnected?.Invoke(this, session.Guid);
        OnClientConnected(session.Guid);
    }
}

sealed class TcpSession : IDisposable
{
    private bool _disposed;
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<ArraySegment<byte>>> _messages = new ConcurrentDictionary<ulong, TaskCompletionSource<ArraySegment<byte>>>();
    private readonly BlockingCollection<(ulong, ArraySegment<byte>)> _collection = new BlockingCollection<(ulong, ArraySegment<byte>)>(new ConcurrentQueue<(ulong, ArraySegment<byte>)>());
<<<<<<< HEAD
    private readonly Thread _thread;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly BinaryWriter _writer;
    private readonly Func<ArraySegment<byte>, ArraySegment<byte>> _onRequest;

    private ulong _nextReqId = 1;

    public TcpSession(Guid guid, TcpClient client, Func<ArraySegment<byte>, ArraySegment<byte>> onRequest)
    {
        Guid = guid;
        Client = client;
        _writer = new BinaryWriter(client.GetStream());
        _thread = new Thread(ParseRequests) { IsBackground = true };
        _thread.TrySetApartmentState(ApartmentState.STA);
        _onRequest = onRequest;
    }

=======
=======
        OnSocketConnected?.Invoke(this, session.Guid);
>>>>>>> + move managed plugin to rpc
        OnClientConnected(session.Guid);
    }
}

sealed class TcpSession : IDisposable
{
    private bool _disposed;
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<RpcMessage>> _messages = new ConcurrentDictionary<ulong, TaskCompletionSource<RpcMessage>>();
    private readonly BlockingCollection<RpcMessage> _collection = new BlockingCollection<RpcMessage>(new ConcurrentQueue<RpcMessage>());
=======
>>>>>>> + removed rpc message
    private readonly Thread _thread;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly BinaryWriter _writer;
    private readonly Func<ArraySegment<byte>, ArraySegment<byte>> _onRequest;

    private ulong _nextReqId = 1;

    public TcpSession(Guid guid, TcpClient client, Func<ArraySegment<byte>, ArraySegment<byte>> onRequest)
    {
        Guid = guid;
        Client = client;
        _writer = new BinaryWriter(client.GetStream());
        _thread = new Thread(ParseRequests) { IsBackground = true };
        _thread.TrySetApartmentState(ApartmentState.STA);
        _onRequest = onRequest;
    }

<<<<<<< HEAD
    public event Action<RpcMessage> OnMessage;
>>>>>>> rpc support
=======
>>>>>>> message serialization
    public event Action OnDisconnected;

    public Guid Guid { get; }
    public TcpClient Client { get; }
<<<<<<< HEAD
<<<<<<< HEAD
=======
    public Rpc Rpc { get; private set; }
>>>>>>> rpc support
=======
>>>>>>> more rpc

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> + move managed plugin to rpc
        _cancellationTokenSource.Cancel();
        if (_thread.IsAlive)
            _thread.Join();
        _cancellationTokenSource.Dispose();
<<<<<<< HEAD
        _collection.Dispose();
=======
        Rpc.Dispose();
        _incoming.Dispose();
        _queue.Clear();
>>>>>>> rpc support
=======
=======
>>>>>>> + move managed plugin to rpc
        _collection.Dispose();
>>>>>>> more rpc
        Client.Close();
        Client.Dispose();

        OnDisconnected?.Invoke();
    }

    public void Start()
    {
<<<<<<< HEAD
<<<<<<< HEAD
        _thread.Start();
        RunReceiveLoop(_cancellationTokenSource.Token);
    }

    public async ValueTask<ArraySegment<byte>> RequestAsync(ArraySegment<byte> payload)
    {
        var reqId = (ulong)Interlocked.Increment(ref Unsafe.As<ulong, long>(ref _nextReqId));

        var taskSrc = new TaskCompletionSource<ArraySegment<byte>>();
        if (!_messages.TryAdd(reqId, taskSrc))
        {
            Console.WriteLine("req {0} already added", reqId);
        }

        WriteMessage(RpcCommand.Request, reqId, payload);

        var response = await taskSrc.Task.ConfigureAwait(false);

        //Debug.Assert(reqId.Equals(response.ID));
        //Debug.Assert(response.Command == RpcCommand.Response);

        return response;
    }

    private void ResponseTo(ulong msgId, ArraySegment<byte> payload)
    {
        WriteMessage(RpcCommand.Response, msgId, payload);
    }

    private void WriteMessage(RpcCommand cmd, ulong id, ArraySegment<byte> payload)
    {
        var buf = ArrayPool<byte>.Shared.Rent(RpcConst.RPC_MESSAGE_SIZE + payload.Count);
        try
        {
            buf[0] = (byte)cmd;
            BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(sizeof(byte), sizeof(ulong)), id);
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(sizeof(byte) + sizeof(ulong), sizeof(ushort)), (ushort)payload.Count);
            Buffer.BlockCopy(payload.Array, payload.Offset, buf, RpcConst.RPC_MESSAGE_SIZE, payload.Count);

            _writer.Write(buf, 0, RpcConst.RPC_MESSAGE_SIZE + payload.Count);
            _writer.Flush();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    async void RunReceiveLoop(CancellationToken token)
    {
        var buf = new byte[ushort.MaxValue + 1];
        var readBuffer = Array.Empty<byte>().AsMemory();

        try
        {
            var socket = Client.Client;
            while (Client.Connected)
            {
                ReadOnlyMemory<byte> value = Array.Empty<byte>();

                if (readBuffer.Length == 0)
                {
                    var xs = new ArraySegment<byte>(buf, 0, buf.Length);
                    var read = await socket.ReceiveAsync(xs, SocketFlags.None
#if NET
                        , token
#endif
                        ).ConfigureAwait(false);
                    if (read <= 0)
                        break;

                    readBuffer = buf.AsMemory(0, read);
                }
                else if (readBuffer.Length < RpcConst.RPC_MESSAGE_SIZE)
                {
                    var xs = new ArraySegment<byte>(buf, 0, buf.Length);
                    var readLen = await socket.ReceiveAsync(xs, SocketFlags.None
#if NET
                        , token
#endif
                        ).ConfigureAwait(false);
                    if (readLen == 0) break;
                    var newBuffer = new byte[readBuffer.Length + readLen];
                    readBuffer.CopyTo(newBuffer);
                    buf.AsSpan(readLen).CopyTo(newBuffer.AsSpan(readBuffer.Length));
                    readBuffer = newBuffer;
                }

                if (readBuffer.Span.Length < RpcConst.RPC_MESSAGE_SIZE)
                {
                    continue;
                }


                var cmd = (RpcCommand)readBuffer.Span[0];
                var id = BinaryPrimitives.ReadUInt64LittleEndian(readBuffer.Span.Slice(sizeof(byte), sizeof(ulong)));
                var payloadLen = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer.Span.Slice(sizeof(byte) + sizeof(ulong), sizeof(ushort)));

                if (readBuffer.Length == (payloadLen + RpcConst.RPC_MESSAGE_SIZE)) // just size
                {
                    value = readBuffer.Slice(RpcConst.RPC_MESSAGE_SIZE, payloadLen); // skip length header
                    readBuffer = Array.Empty<byte>();
                    goto PARSE_MESSAGE;
                }
                else if (readBuffer.Length > (payloadLen + RpcConst.RPC_MESSAGE_SIZE)) // over size
                {
                    value = readBuffer.Slice(RpcConst.RPC_MESSAGE_SIZE, payloadLen);
                    readBuffer = readBuffer.Slice(payloadLen + RpcConst.RPC_MESSAGE_SIZE);
                    goto PARSE_MESSAGE;
                }
                else
                {
                    continue;
                }

            PARSE_MESSAGE:

                var rentBuf = value.IsEmpty ? Array.Empty<byte>() : ArrayPool<byte>.Shared.Rent(value.Length);
                value.Span.CopyTo(rentBuf);
                var payload = new ArraySegment<byte>(rentBuf, 0, value.Length);

                switch (cmd)
                {
                    case RpcCommand.Request:
                        //ParseRequest(msg);
                        while (!_collection.TryAdd((id, payload), -1, token))
                        {
                            Console.WriteLine("sleep!");
                            Thread.Sleep(1);
                        }
                        break;

                    case RpcCommand.Response:
                        if (_messages.TryRemove(id, out var task))
                        {
                            if (!task.TrySetResult(payload))
                            {
                                Console.WriteLine("cannot set result of msg {0}", id);
                            }
                        }
                        break;
                }
            }
=======
        var stream = Client.GetStream();
        _queue.Clear();
=======
        _thread.Start();
        RunReceiveLoop(_cancellationTokenSource.Token);
    }

    public async ValueTask<ArraySegment<byte>> RequestAsync(ArraySegment<byte> payload)
    {
        var reqId = (ulong)Interlocked.Increment(ref Unsafe.As<ulong, long>(ref _nextReqId));

        var taskSrc = new TaskCompletionSource<ArraySegment<byte>>();
        if (!_messages.TryAdd(reqId, taskSrc))
        {
            Console.WriteLine("req {0} already added", reqId);
        }

<<<<<<< HEAD
        if (!_channelSending.Writer.TryWrite(buffer))
        {
            Console.WriteLine("cannot write {0} request to sending channel", reqId);
        }
>>>>>>> more rpc
=======
        WriteMessage(RpcCommand.Request, reqId, payload);
>>>>>>> performance improvement

        var response = await taskSrc.Task.ConfigureAwait(false);

        //Debug.Assert(reqId.Equals(response.ID));
        //Debug.Assert(response.Command == RpcCommand.Response);

        return response;
    }

    private void ResponseTo(ulong msgId, ArraySegment<byte> payload)
    {
        WriteMessage(RpcCommand.Response, msgId, payload);
    }

    private void WriteMessage(RpcCommand cmd, ulong id, ArraySegment<byte> payload)
    {
        var buf = ArrayPool<byte>.Shared.Rent(RpcConst.RPC_MESSAGE_SIZE + payload.Count);
        try
        {
            buf[0] = (byte)cmd;
            BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(sizeof(byte), sizeof(ulong)), id);
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(sizeof(byte) + sizeof(ulong), sizeof(ushort)), (ushort)payload.Count);
            Buffer.BlockCopy(payload.Array, payload.Offset, buf, RpcConst.RPC_MESSAGE_SIZE, payload.Count);

            _writer.Write(buf, 0, RpcConst.RPC_MESSAGE_SIZE + payload.Count);
            _writer.Flush();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    async void RunReceiveLoop(CancellationToken token)
    {
        var buf = new byte[ushort.MaxValue + 1];
        var readBuffer = Array.Empty<byte>().AsMemory();

        try
        {
            var socket = Client.Client;
            while (Client.Connected)
            {
                ReadOnlyMemory<byte> value = Array.Empty<byte>();

                if (readBuffer.Length == 0)
                {
                    var xs = new ArraySegment<byte>(buf, 0, buf.Length);
                    var read = await socket.ReceiveAsync(xs, SocketFlags.None
#if NET
                        , token
#endif
                        ).ConfigureAwait(false);
                    if (read <= 0)
                        break;

                    readBuffer = buf.AsMemory(0, read);
                }
                else if (readBuffer.Length < RpcConst.RPC_MESSAGE_SIZE)
                {
                    var xs = new ArraySegment<byte>(buf, 0, buf.Length);
                    var readLen = await socket.ReceiveAsync(xs, SocketFlags.None
#if NET
                        , token
#endif
                        ).ConfigureAwait(false);
                    if (readLen == 0) break;
                    var newBuffer = new byte[readBuffer.Length + readLen];
                    readBuffer.CopyTo(newBuffer);
                    buf.AsSpan(readLen).CopyTo(newBuffer.AsSpan(readBuffer.Length));
                    readBuffer = newBuffer;
                }

                if (readBuffer.Span.Length < RpcConst.RPC_MESSAGE_SIZE)
                {
                    continue;
                }


                var cmd = (RpcCommand)readBuffer.Span[0];
                var id = BinaryPrimitives.ReadUInt64LittleEndian(readBuffer.Span.Slice(sizeof(byte), sizeof(ulong)));
                var payloadLen = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer.Span.Slice(sizeof(byte) + sizeof(ulong), sizeof(ushort)));

                if (readBuffer.Length == (payloadLen + RpcConst.RPC_MESSAGE_SIZE)) // just size
                {
                    value = readBuffer.Slice(RpcConst.RPC_MESSAGE_SIZE, payloadLen); // skip length header
                    readBuffer = Array.Empty<byte>();
                    goto PARSE_MESSAGE;
                }
                else if (readBuffer.Length > (payloadLen + RpcConst.RPC_MESSAGE_SIZE)) // over size
                {
                    value = readBuffer.Slice(RpcConst.RPC_MESSAGE_SIZE, payloadLen);
                    readBuffer = readBuffer.Slice(payloadLen + RpcConst.RPC_MESSAGE_SIZE);
                    goto PARSE_MESSAGE;
                }
                else
                {
                    continue;
                }

            PARSE_MESSAGE:

                var rentBuf = value.IsEmpty ? Array.Empty<byte>() : ArrayPool<byte>.Shared.Rent(value.Length);
                value.Span.CopyTo(rentBuf);
                var payload = new ArraySegment<byte>(rentBuf, 0, value.Length);

                switch (cmd)
                {
                    case RpcCommand.Request:
                        //ParseRequest(msg);
                        while (!_collection.TryAdd((id, payload), -1, token))
                        {
<<<<<<< HEAD
<<<<<<< HEAD

<<<<<<< HEAD
            stream.BeginRead(buf, 0, buf.Length, _onRecv, buf);
>>>>>>> rpc support
=======
=======
                            Console.WriteLine("cannot set result of msg {0}", msg.ID);
>>>>>>> rpc
=======
                            Console.WriteLine("sleep!");
                            Thread.Sleep(1);
>>>>>>> performance improvement
                        }
                        break;

                    case RpcCommand.Response:
                        if (_messages.TryRemove(id, out var task))
                        {
                            if (!task.TrySetResult(payload))
                            {
                                Console.WriteLine("cannot set result of msg {0}", id);
                            }
                        }
                        break;
                }
            }
>>>>>>> more rpc
        }
        catch (Exception ex)
        {
            Console.WriteLine("Client session exception: {0}", ex);
<<<<<<< HEAD
<<<<<<< HEAD
        }

        Dispose();
    }

    void ParseRequests()
    {
        var token = _cancellationTokenSource.Token;

        try
        {
            while (Client.Connected)
            {
                (var id, var payload) = _collection.Take(token);

                ParseRequest(id, payload);
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    private void ParseRequest(ulong msgId, ArraySegment<byte> payload)
    {
        var respPayload = _onRequest(payload);
        ResponseTo(msgId, respPayload);

        if (payload.Array != null && payload.Array.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(payload.Array);
=======

            Dispose();
=======
>>>>>>> more rpc
        }

        Dispose();
    }

    void ParseRequests()
    {
        var token = _cancellationTokenSource.Token;

        try
        {
            while (Client.Connected)
            {
                (var id, var payload) = _collection.Take(token);

                ParseRequest(id, payload);
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    private void ParseRequest(ulong msgId, ArraySegment<byte> payload)
    {
<<<<<<< HEAD
<<<<<<< HEAD
        switch (msg.Command)
        {
            case RpcCommand.Request:
                var respPayload = OnRequest(msg);
                ResponseTo(msg, respPayload);
                break;

            case RpcCommand.Response:
                if (_messages.TryRemove(msg.ID, out var task))
                {
                    if (!task.TrySetResult(msg))
                    {
                        Console.WriteLine("cannot set result of msg {0}", msg.ID);
                    }
                }
                break;
        }
    }

    async void SendLoopAsync()
    {
        var reader = _channelSending.Reader;
        var socket = Client.Client;
        var token = _cancellationTokenSource.Token;

        RunReceiveLoop(token);

        try
        {
            while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    var xs = new ArraySegment<byte>(item);
                    _ = await socket.SendAsync(xs, SocketFlags.None
#if NET
                        , token
#endif
                        ).ConfigureAwait(false);
                }
            }
>>>>>>> rpc support
        }
        catch (OperationCanceledException)
        {
=======
        Debug.Assert(msg.Command == RpcCommand.Request, "Message must be a request!");
>>>>>>> performance improvement
=======
        var respPayload = _onRequest(payload);
        ResponseTo(msgId, respPayload);
>>>>>>> + removed rpc message

        if (payload.Array != null && payload.Array.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(payload.Array);
        }
    }
}

abstract class TcpClientRpc
{
<<<<<<< HEAD
<<<<<<< HEAD
    private TcpSession _session;

    public bool IsConnected => _session?.Client?.Connected ?? false;

    public event EventHandler OnSocketConnected, OnSocketDisconnected;


=======
    private ClientSession _session;
=======
    private TcpSession _session;
>>>>>>> more rpc

    public bool IsConnected => _session?.Client?.Connected ?? false;

<<<<<<< HEAD
>>>>>>> rpc support
=======
    public event EventHandler OnSocketConnected, OnSocketDisconnected;


>>>>>>> performance improvement
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

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
        _session = new TcpSession(Guid.Empty, tcp, OnRequest);
        _session.OnDisconnected += () => {
            OnSocketDisconnected?.Invoke(this, EventArgs.Empty);
            OnDisconnected();
        };
        _session.Start();

        OnSocketConnected?.Invoke(this, EventArgs.Empty);
        OnConnected();
    }

=======
        _session = new ClientSession(Guid.Empty, tcp);
=======
        _session = new TcpSession(Guid.Empty, tcp);
<<<<<<< HEAD
>>>>>>> more rpc
        _session.OnDisconnected += OnDisconnected;
        _session.OnMessage += msg => OnMessage(msg);
=======
=======
        _session = new TcpSession(Guid.Empty, tcp, OnRequest);
>>>>>>> minor improvements
        _session.OnDisconnected += () => {
            OnSocketDisconnected?.Invoke(this, EventArgs.Empty);
            OnDisconnected();
        };
<<<<<<< HEAD
<<<<<<< HEAD
        _session.OnMessage += msg =>
        {
            switch (msg.Command)
            {
                case RpcCommand.Request:
                    OnRequest?.Invoke(this, msg);
                    break;
                case RpcCommand.Response:
                    OnResponse?.Invoke(this, msg);
                    break;
            }

            OnMessage(msg);
        };
>>>>>>> + move managed plugin to rpc
=======
        _session.OnRequest += OnRequest;
>>>>>>> message serialization
=======
>>>>>>> minor improvements
        _session.Start();

        OnSocketConnected?.Invoke(this, EventArgs.Empty);
        OnConnected();
    }

<<<<<<< HEAD
    public void Tick() => _session?.ProcessIncomingMessages();

>>>>>>> rpc support
=======
>>>>>>> more rpc
    public void Disconnect()
    {
        _session?.Client?.Client?.Disconnect(false);
    }

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
    public async ValueTask<ArraySegment<byte>> RequestAsync(ArraySegment<byte> payload)
    {
        if (_session == null)
            return new ArraySegment<byte>(Array.Empty<byte>());

        return await _session.RequestAsync(payload);
    }

    public ArraySegment<byte> Request(ArraySegment<byte> payload)
        => AsyncHelpers.RunSync(() => RequestAsync(payload));


    protected abstract ArraySegment<byte> OnRequest(ArraySegment<byte> msg);
=======
    public RpcMessage Request(ArraySegment<byte> payload)
=======
    public async Task<RpcMessage> Request(ArraySegment<byte> payload)
>>>>>>> more rpc
=======
    public async Task<RpcMessage> RequestAsync(ArraySegment<byte> payload)
>>>>>>> + move managed plugin to rpc
=======
    public async ValueTask<RpcMessage> RequestAsync(ArraySegment<byte> payload)
>>>>>>> performance improvement
=======
    public async ValueTask<ArraySegment<byte>> RequestAsync(ArraySegment<byte> payload)
>>>>>>> + removed rpc message
    {
        if (_session == null)
            return new ArraySegment<byte>(Array.Empty<byte>());

        return await _session.RequestAsync(payload);
    }

    public ArraySegment<byte> Request(ArraySegment<byte> payload)
        => AsyncHelpers.RunSync(() => RequestAsync(payload));

<<<<<<< HEAD
    public event EventHandler OnSocketConnected, OnSocketDisconnected;

<<<<<<< HEAD
    protected abstract void OnMessage(RpcMessage msg);
>>>>>>> rpc support
=======
=======
>>>>>>> performance improvement

<<<<<<< HEAD
    protected abstract ArraySegment<byte> OnRequest(RpcMessage msg);
>>>>>>> message serialization
=======
    protected abstract ArraySegment<byte> OnRequest(ArraySegment<byte> msg);
>>>>>>> + removed rpc message
    protected virtual void OnConnected() { }
    protected virtual void OnDisconnected() { }
}

<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
=======
readonly struct RpcMessage
=======
readonly struct RpcMessage : IDisposable
>>>>>>> use arraypool
{
    public readonly RpcCommand Command;
    public readonly ulong ID;
    public readonly ArraySegment<byte> Payload;
=======
//sealed class RpcMessage : IDisposable
//{
//    public readonly RpcCommand Command;
//    public readonly ulong ID;
//    public readonly ArraySegment<byte> Payload;
>>>>>>> + removed rpc message

//    public RpcMessage(RpcCommand cmd, ulong id, ArraySegment<byte> payload)
//        => (Command, ID, Payload) = (cmd, id, payload);

//    public void Dispose()
//    {
//        if (Payload.Array != null && Payload.Array.Length > 0)
//        {
//            ArrayPool<byte>.Shared.Return(Payload.Array);
//        }
//    }

//    public static readonly RpcMessage Invalid = new RpcMessage(RpcCommand.Invalid, 0, new ArraySegment<byte>(Array.Empty<byte>()));
//}

>>>>>>> rpc support
enum RpcCommand
{
    Invalid = -1,
    Request,
    Response
}
<<<<<<< HEAD
<<<<<<< HEAD

<<<<<<< HEAD
// Async helper got from RestSharp project!
static class AsyncHelpers
{
    private readonly static ConcurrentStack<CustomSynchronizationContext> _cache = new ConcurrentStack<CustomSynchronizationContext>();

<<<<<<< HEAD
=======

// Async helper got from RestSharp project!
static class AsyncHelpers
{
>>>>>>> rpc
=======
>>>>>>> + removed rpc message
    /// <summary>
    /// Executes a task synchronously on the calling thread by installing a temporary synchronization context that queues continuations
    /// </summary>
    /// <param name="task">Callback for asynchronous task to run</param>
<<<<<<< HEAD
<<<<<<< HEAD
    public static void RunSync(Func<ValueTask> task)
    {
        var currentContext = SynchronizationContext.Current;

        if (!_cache.TryPop(out var customContext))
        {
            customContext = new CustomSynchronizationContext();
        }
<<<<<<< HEAD
=======
    public static void RunSync(Func<Task> task)
=======
    public static void RunSync(Func<ValueTask> task)
>>>>>>> performance improvement
    {
        var currentContext = SynchronizationContext.Current;
        var customContext = new CustomSynchronizationContext(task);
>>>>>>> rpc
=======
>>>>>>> + removed rpc message

        try
        {
            SynchronizationContext.SetSynchronizationContext(customContext);
<<<<<<< HEAD
<<<<<<< HEAD
            customContext.Run(task);
=======
            customContext.Run();
>>>>>>> rpc
=======
            customContext.Run(task);
>>>>>>> + removed rpc message
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(currentContext);
<<<<<<< HEAD
<<<<<<< HEAD
            _cache.Push(customContext);
=======
>>>>>>> rpc
=======
            _cache.Push(customContext);
>>>>>>> + removed rpc message
        }
    }

    /// <summary>
    /// Executes a task synchronously on the calling thread by installing a temporary synchronization context that queues continuations
    /// </summary>
    /// <param name="task">Callback for asynchronous task to run</param>
    /// <typeparam name="T">Return type for the task</typeparam>
    /// <returns>Return value from the task</returns>
<<<<<<< HEAD
<<<<<<< HEAD
    public static T RunSync<T>(Func<ValueTask<T>> task)
=======
    public static T RunSync<T>(Func<Task<T>> task)
>>>>>>> rpc
=======
    public static T RunSync<T>(Func<ValueTask<T>> task)
>>>>>>> performance improvement
    {
        T result = default!;
        RunSync(async () => { result = await task(); });
        return result;
    }

    /// <summary>
    /// Synchronization context that can be "pumped" in order to have it execute continuations posted back to it
    /// </summary>
<<<<<<< HEAD
<<<<<<< HEAD
    sealed class CustomSynchronizationContext : SynchronizationContext
    {
        readonly BlockingCollection<(SendOrPostCallback, object?)> _coll = new BlockingCollection<(SendOrPostCallback, object?)>(new ConcurrentQueue<(SendOrPostCallback, object?)>());
        Func<ValueTask> _task;
        ExceptionDispatchInfo? _caughtException;
        bool _done;

        readonly SendOrPostCallback _callback1 = PostCallback;
        readonly SendOrPostCallback _callback2 = Done;

=======
    class CustomSynchronizationContext : SynchronizationContext
=======
    sealed class CustomSynchronizationContext : SynchronizationContext
>>>>>>> + removed rpc message
    {
        readonly BlockingCollection<(SendOrPostCallback, object?)> _coll = new BlockingCollection<(SendOrPostCallback, object?)>(new ConcurrentQueue<(SendOrPostCallback, object?)>());
        Func<ValueTask> _task;
        ExceptionDispatchInfo? _caughtException;
        bool _done;

<<<<<<< HEAD
        /// <summary>
        /// Constructor for the custom context
        /// </summary>
        /// <param name="task">Task to execute</param>
        public CustomSynchronizationContext(Func<ValueTask> task) =>
            _task = task ?? throw new ArgumentNullException(nameof(task), "Please remember to pass a Task to be executed");
>>>>>>> rpc
=======
        readonly SendOrPostCallback _callback1 = PostCallback;
        readonly SendOrPostCallback _callback2 = Done;

>>>>>>> + removed rpc message

        /// <summary>
        /// When overridden in a derived class, dispatches an asynchronous message to a synchronization context.
        /// </summary>
        /// <param name="function">Callback function</param>
        /// <param name="state">Callback state</param>
        public override void Post(SendOrPostCallback function, object? state)
        {
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> + removed rpc message
            _coll.Add((function, state));
        }

        static async void PostCallback(object? o)
        {
            try
            {
                await ((CustomSynchronizationContext)o)._task().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                ((CustomSynchronizationContext)o)._caughtException = ExceptionDispatchInfo.Capture(exception);
                throw;
            }
            finally
            {
                ((CustomSynchronizationContext)o).Post(((CustomSynchronizationContext)o)._callback2, o);
            }
        }

        static void Done(object? o)
        {
            ((CustomSynchronizationContext)o)._done = true;
<<<<<<< HEAD
=======
            _items.Enqueue(Tuple.Create(function, state));
=======
            _items.Enqueue((function, state));
>>>>>>> minor improvements
            _workItemsWaiting.Set();
>>>>>>> rpc
=======
>>>>>>> + removed rpc message
        }

        /// <summary>
        /// Enqueues the function to be executed and executes all resulting continuations until it is completely done
        /// </summary>
<<<<<<< HEAD
<<<<<<< HEAD
        public void Run(Func<ValueTask> task)
        {
            _done = false;
            _task = task;

            Post(_callback1, this);

            while (!_done)
            {
                (var callback, var obj) = _coll.Take();

                callback(obj);
                if (_caughtException == null)
                {
                    continue;
                }

                _caughtException.Throw();
=======
        public void Run()
=======
        public void Run(Func<ValueTask> task)
>>>>>>> + removed rpc message
        {
            _task = task;

            Post(_callback1, this);

            while (!_done)
            {
                (var callback, var obj) = _coll.Take();

                callback(obj);
                if (_caughtException == null)
                {
                    continue;
                }
<<<<<<< HEAD
>>>>>>> rpc
=======

                _caughtException.Throw();
>>>>>>> + removed rpc message
            }

            _done = false;
        }

        /// <summary>
        /// When overridden in a derived class, dispatches a synchronous message to a synchronization context.
        /// </summary>
        /// <param name="function">Callback function</param>
        /// <param name="state">Callback state</param>
        public override void Send(SendOrPostCallback function, object? state) => throw new NotSupportedException("Cannot send to same thread");

        /// <summary>
        /// When overridden in a derived class, creates a copy of the synchronization context. Not needed, so just return ourselves.
        /// </summary>
        /// <returns>Copy of the context</returns>
        public override SynchronizationContext CreateCopy() => this;
<<<<<<< HEAD
=======
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
>>>>>>> rpc support
    }
}
=======
>>>>>>> more rpc
=======
    }
}
>>>>>>> rpc
