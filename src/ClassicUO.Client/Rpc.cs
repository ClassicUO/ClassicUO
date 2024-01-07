using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;


static class RpcConst
{
    public const int READ_WRITE_TIMEOUT = 0;
    public const int RPC_MESSAGE_SIZE = 1 + 8 + 2;
}

abstract class TcpServerRpc
{
    private TcpListener _server;
    private readonly ConcurrentDictionary<Guid, TcpSession> _clients = new ConcurrentDictionary<Guid, TcpSession>();


    public IReadOnlyDictionary<Guid, TcpSession> Clients => _clients;

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
            l.Value.Client.Close();
            l.Value.Client.Dispose();
        }

        _server.Server.Dispose();
        _clients.Clear();
    }

    void ProcessClient(TcpClient client)
    {
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
        session.Start();

        _clients.TryAdd(session.Guid, session);

        OnSocketConnected?.Invoke(this, session.Guid);
        OnClientConnected(session.Guid);
    }
}

sealed class TcpSession : IDisposable
{
    private bool _disposed;
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<ArraySegment<byte>>> _messages = new ConcurrentDictionary<ulong, TaskCompletionSource<ArraySegment<byte>>>();
    private readonly BlockingCollection<(ulong, ArraySegment<byte>)> _collection = new BlockingCollection<(ulong, ArraySegment<byte>)>(new ConcurrentQueue<(ulong, ArraySegment<byte>)>());
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

    public event Action OnDisconnected;

    public Guid Guid { get; }
    public TcpClient Client { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellationTokenSource.Cancel();
        if (_thread.IsAlive)
            _thread.Join();
        _cancellationTokenSource.Dispose();
        _collection.Dispose();
        Client.Close();
        Client.Dispose();

        OnDisconnected?.Invoke();
    }

    public void Start()
    {
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
        }
        catch (Exception ex)
        {
            Console.WriteLine("Client session exception: {0}", ex);
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
        }
    }
}

abstract class TcpClientRpc
{
    private TcpSession _session;

    public bool IsConnected => _session?.Client?.Connected ?? false;

    public event EventHandler OnSocketConnected, OnSocketDisconnected;


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

        _session = new TcpSession(Guid.Empty, tcp, OnRequest);
        _session.OnDisconnected += () => {
            OnSocketDisconnected?.Invoke(this, EventArgs.Empty);
            OnDisconnected();
        };
        _session.Start();

        OnSocketConnected?.Invoke(this, EventArgs.Empty);
        OnConnected();
    }

    public void Disconnect()
    {
        _session?.Client?.Client?.Disconnect(false);
    }

    public async ValueTask<ArraySegment<byte>> RequestAsync(ArraySegment<byte> payload)
    {
        if (_session == null)
            return new ArraySegment<byte>(Array.Empty<byte>());

        return await _session.RequestAsync(payload);
    }

    public ArraySegment<byte> Request(ArraySegment<byte> payload)
        => AsyncHelpers.RunSync(() => RequestAsync(payload));


    protected abstract ArraySegment<byte> OnRequest(ArraySegment<byte> msg);
    protected virtual void OnConnected() { }
    protected virtual void OnDisconnected() { }
}

//sealed class RpcMessage : IDisposable
//{
//    public readonly RpcCommand Command;
//    public readonly ulong ID;
//    public readonly ArraySegment<byte> Payload;

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

enum RpcCommand
{
    Invalid = -1,
    Request,
    Response
}

// Async helper got from RestSharp project!
static class AsyncHelpers
{
    private readonly static ConcurrentStack<CustomSynchronizationContext> _cache = new ConcurrentStack<CustomSynchronizationContext>();

    /// <summary>
    /// Executes a task synchronously on the calling thread by installing a temporary synchronization context that queues continuations
    /// </summary>
    /// <param name="task">Callback for asynchronous task to run</param>
    public static void RunSync(Func<ValueTask> task)
    {
        var currentContext = SynchronizationContext.Current;

        if (!_cache.TryPop(out var customContext))
        {
            customContext = new CustomSynchronizationContext();
        }

        try
        {
            SynchronizationContext.SetSynchronizationContext(customContext);
            customContext.Run(task);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(currentContext);
            _cache.Push(customContext);
        }
    }

    /// <summary>
    /// Executes a task synchronously on the calling thread by installing a temporary synchronization context that queues continuations
    /// </summary>
    /// <param name="task">Callback for asynchronous task to run</param>
    /// <typeparam name="T">Return type for the task</typeparam>
    /// <returns>Return value from the task</returns>
    public static T RunSync<T>(Func<ValueTask<T>> task)
    {
        T result = default!;
        RunSync(async () => { result = await task(); });
        return result;
    }

    /// <summary>
    /// Synchronization context that can be "pumped" in order to have it execute continuations posted back to it
    /// </summary>
    sealed class CustomSynchronizationContext : SynchronizationContext
    {
        readonly BlockingCollection<(SendOrPostCallback, object?)> _coll = new BlockingCollection<(SendOrPostCallback, object?)>(new ConcurrentQueue<(SendOrPostCallback, object?)>());
        Func<ValueTask> _task;
        ExceptionDispatchInfo? _caughtException;
        bool _done;

        readonly SendOrPostCallback _callback1 = PostCallback;
        readonly SendOrPostCallback _callback2 = Done;


        /// <summary>
        /// When overridden in a derived class, dispatches an asynchronous message to a synchronization context.
        /// </summary>
        /// <param name="function">Callback function</param>
        /// <param name="state">Callback state</param>
        public override void Post(SendOrPostCallback function, object? state)
        {
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
        }

        /// <summary>
        /// Enqueues the function to be executed and executes all resulting continuations until it is completely done
        /// </summary>
        public void Run(Func<ValueTask> task)
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

                _caughtException.Throw();
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
    }
}