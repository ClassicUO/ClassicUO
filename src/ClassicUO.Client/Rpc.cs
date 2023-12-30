using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;


static class RpcConst
{
    public const int READ_WRITE_TIMEOUT = 0;
}

abstract class TcpServerRpc
{
    private TcpListener _server;
    private readonly ConcurrentDictionary<Guid, TcpSession> _clients = new ConcurrentDictionary<Guid, TcpSession>();


    public IReadOnlyDictionary<Guid, TcpSession> Clients => _clients;

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

    public async Task<RpcMessage> Request(Guid clientId, ArraySegment<byte> payload)
    {
        if (_clients.TryGetValue(clientId, out var client))
        {
            return await client.Request(payload);
        }

        return RpcMessage.Invalid;
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
            l.Value.Client.Close();
            l.Value.Client.Dispose();
        }

        _server.Server.Dispose();
        _clients.Clear();
    }

    void ProcessClient(TcpClient client)
    {
        var session = new TcpSession(Guid.NewGuid(), client);

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
}

sealed class TcpSession : IDisposable
{
    private bool _disposed;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<RpcMessage>> _messages = new ConcurrentDictionary<Guid, TaskCompletionSource<RpcMessage>>();
    private readonly Channel<byte[]> _channelSending;
    private readonly BlockingCollection<RpcMessage> _collection = new BlockingCollection<RpcMessage>(new ConcurrentQueue<RpcMessage>());
    private readonly Thread _thread;


    public TcpSession(Guid guid, TcpClient client)
    {
        Guid = guid;
        Client = client;
        _channelSending = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true
        });
        _thread = new Thread(ParseMessages) { IsBackground = true };
        _thread.SetApartmentState(ApartmentState.STA);
    }

    public event Action<RpcMessage> OnMessage;
    public event Action OnDisconnected;

    public Guid Guid { get; }
    public TcpClient Client { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_thread.IsAlive)
            _thread.Abort();

        _disposed = true;
        _collection.Dispose();
        Client.Close();
        Client.Dispose();

        OnDisconnected?.Invoke();
    }

    public void Start()
    {
        _thread.Start();
        SendLoopAsync();
    }

    public async Task<RpcMessage> Request(ArraySegment<byte> payload)
    {
        var buffer = SendMessage(payload, out var reqId);

        var taskSrc = new TaskCompletionSource<RpcMessage>();
        _messages[reqId] = taskSrc;

        if (!_channelSending.Writer.TryWrite(buffer))
        {
            Console.WriteLine("cannot write {0} request to sending channel", reqId);
        }

        var response = await taskSrc.Task.ConfigureAwait(false);

        Trace.Assert(reqId.Equals(response.ID));
        Trace.Assert(response.Command == RpcCommand.Response);

        return response;
    }

    private void ResponseTo(RpcMessage request)
    {
        var buf = CreateMessage(RpcCommand.Response, request.ID, request.Payload);

        if (!_channelSending.Writer.TryWrite(buf))
        {
            Console.WriteLine("cannot write {0} response to sending channel", request.ID);
        }
    }

    private byte[] SendMessage(ArraySegment<byte> payload, out Guid id)
    {
        id = Guid.NewGuid();

        return CreateMessage(RpcCommand.Request, id, payload);
    }

    private byte[] CreateMessage(RpcCommand cmd, Guid id, ArraySegment<byte> payload)
    {
        var buf = new byte[19 + payload.Count];
        using var ms = new MemoryStream(buf);
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)cmd);
        writer.Write(id.ToByteArray());
        writer.Write((ushort)payload.Count);
        writer.Write(payload.Array, payload.Offset, payload.Count);
        writer.Flush();
        return buf;
    }

    async void RunReceiveLoop()
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
                    var read = await socket.ReceiveAsync(xs, SocketFlags.None).ConfigureAwait(false);
                    if (read <= 0)
                        break;

                    readBuffer = buf.AsMemory(0, read);
                }
                else if (readBuffer.Length < 19)
                {
                    var xs = new ArraySegment<byte>(buf, 0, buf.Length);
                    var readLen = await socket.ReceiveAsync(xs, SocketFlags.None).ConfigureAwait(false);
                    if (readLen == 0) break;
                    var newBuffer = new byte[readBuffer.Length + readLen];
                    readBuffer.CopyTo(newBuffer);
                    buf.AsSpan(readLen).CopyTo(newBuffer.AsSpan(readBuffer.Length));
                    readBuffer = newBuffer;
                }

                if (readBuffer.Span.Length < 19)
                {
                    continue;
                }

                var cmd = (RpcCommand)readBuffer.Span[0];
                var id = new Guid
                (
                    BinaryPrimitives.ReadUInt32LittleEndian(readBuffer.Span.Slice(1, 4)),
                    BinaryPrimitives.ReadUInt16LittleEndian(readBuffer.Span.Slice(1 + 4, 2)),
                    BinaryPrimitives.ReadUInt16LittleEndian(readBuffer.Span.Slice(1 + 4 + 2, 2)),
                    readBuffer.Span[1 + 4 + 2 + 2],
                    readBuffer.Span[1 + 4 + 2 + 2 + 1],
                    readBuffer.Span[1 + 4 + 2 + 2 + 1 + 1],
                    readBuffer.Span[1 + 4 + 2 + 2 + 1 + 1 + 1],
                    readBuffer.Span[1 + 4 + 2 + 2 + 1 + 1 + 1 + 1],
                    readBuffer.Span[1 + 4 + 2 + 2 + 1 + 1 + 1 + 1 + 1],
                    readBuffer.Span[1 + 4 + 2 + 2 + 1 + 1 + 1 + 1 + 1 + 1],
                    readBuffer.Span[1 + 4 + 2 + 2 + 1 + 1 + 1 + 1 + 1 + 1 + 1]
                );

                var payloadLen = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer.Span.Slice(1 + 16, 2));

                if (readBuffer.Length == (payloadLen + 19)) // just size
                {
                    value = readBuffer.Slice(19, payloadLen); // skip length header
                    readBuffer = Array.Empty<byte>();
                    goto PARSE_MESSAGE;
                }
                else if (readBuffer.Length > (payloadLen + 19)) // over size
                {
                    value = readBuffer.Slice(19, payloadLen);
                    readBuffer = readBuffer.Slice(payloadLen + 19);
                    goto PARSE_MESSAGE;
                }
                else
                {
                    continue;
                }

            PARSE_MESSAGE:
                var msg = new RpcMessage(cmd, id, new ArraySegment<byte>(value.ToArray()));

                if (cmd == RpcCommand.Response)
                {
                    if (_messages.TryRemove(msg.ID, out var task))
                    {
                        if (!task.TrySetResult(msg))
                        {
                            Console.WriteLine("cannot set result of msg {0}", msg.ID);
                        }
                    }
                }
                else
                {
                    while (!_collection.TryAdd(msg))
                        Thread.Sleep(1);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Client session exception: {0}", ex);
        }

        Dispose();
    }

    void ParseMessages()
    {
        while (Client.Connected)
        {
            var msg = _collection.Take();

            ParseMessage(msg);
        }
    }

    private void ParseMessage(RpcMessage msg)
    {
        OnMessage(msg);

        switch (msg.Command)
        {
            case RpcCommand.Request:
                ResponseTo(msg);
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
        RunReceiveLoop();

        while (await reader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (reader.TryRead(out var item))
            {
                var xs = new ArraySegment<byte>(item);
                _ = await socket.SendAsync(xs, SocketFlags.None).ConfigureAwait(false);
            }
        }
    }
}

abstract class TcpClientRpc
{
    private TcpSession _session;

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

        _session = new TcpSession(Guid.Empty, tcp);
        _session.OnDisconnected += OnDisconnected;
        _session.OnMessage += msg => OnMessage(msg);
        _session.Start();

        OnConnected();
    }

    public void Disconnect()
    {
        _session?.Client?.Client?.Disconnect(false);
    }

    public async Task<RpcMessage> Request(ArraySegment<byte> payload)
    {
        if (_session == null)
            return RpcMessage.Invalid;

        return await _session.Request(payload);
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

// Async helper got from RestSharp project!
static class AsyncHelpers
{
    /// <summary>
    /// Executes a task synchronously on the calling thread by installing a temporary synchronization context that queues continuations
    /// </summary>
    /// <param name="task">Callback for asynchronous task to run</param>
    public static void RunSync(Func<Task> task)
    {
        var currentContext = SynchronizationContext.Current;
        var customContext = new CustomSynchronizationContext(task);

        try
        {
            SynchronizationContext.SetSynchronizationContext(customContext);
            customContext.Run();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(currentContext);
        }
    }

    /// <summary>
    /// Executes a task synchronously on the calling thread by installing a temporary synchronization context that queues continuations
    /// </summary>
    /// <param name="task">Callback for asynchronous task to run</param>
    /// <typeparam name="T">Return type for the task</typeparam>
    /// <returns>Return value from the task</returns>
    public static T RunSync<T>(Func<Task<T>> task)
    {
        T result = default!;
        RunSync(async () => { result = await task(); });
        return result;
    }

    /// <summary>
    /// Synchronization context that can be "pumped" in order to have it execute continuations posted back to it
    /// </summary>
    class CustomSynchronizationContext : SynchronizationContext
    {
        readonly ConcurrentQueue<Tuple<SendOrPostCallback, object?>> _items = new();
        readonly AutoResetEvent _workItemsWaiting = new(false);
        readonly Func<Task> _task;
        ExceptionDispatchInfo? _caughtException;
        bool _done;

        /// <summary>
        /// Constructor for the custom context
        /// </summary>
        /// <param name="task">Task to execute</param>
        public CustomSynchronizationContext(Func<Task> task) =>
            _task = task ?? throw new ArgumentNullException(nameof(task), "Please remember to pass a Task to be executed");

        /// <summary>
        /// When overridden in a derived class, dispatches an asynchronous message to a synchronization context.
        /// </summary>
        /// <param name="function">Callback function</param>
        /// <param name="state">Callback state</param>
        public override void Post(SendOrPostCallback function, object? state)
        {
            _items.Enqueue(Tuple.Create(function, state));
            _workItemsWaiting.Set();
        }

        /// <summary>
        /// Enqueues the function to be executed and executes all resulting continuations until it is completely done
        /// </summary>
        public void Run()
        {
            async void PostCallback(object? _)
            {
                try
                {
                    await _task().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _caughtException = ExceptionDispatchInfo.Capture(exception);
                    throw;
                }
                finally
                {
                    Post(_ => _done = true, null);
                }
            }

            Post(PostCallback, null);

            while (!_done)
            {
                if (_items.TryDequeue(out var task))
                {
                    task.Item1(task.Item2);
                    if (_caughtException == null)
                    {
                        continue;
                    }
                    _caughtException.Throw();
                }
                else
                {
                    _workItemsWaiting.WaitOne();
                }
            }
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