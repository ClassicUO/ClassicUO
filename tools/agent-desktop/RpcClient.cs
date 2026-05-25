// SPDX-License-Identifier: BSD-2-Clause
//
// JSON-RPC client used by every verb in agent-desktop. Talks to the
// in-process AgentServer over loopback TCP. Newline-framed JSON, one
// request → one response, plus a callback for server-pushed events.
//
// Port discovery: by default reads from %LOCALAPPDATA%\ClassicUO\agent\port.json
// (the same file the server writes on startup — see AgentServer.AdvertisePort).
// Overridable via --host / --port on each verb.

using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Contracts.Dto;

namespace ClassicUO.Agent.Desktop;

internal sealed class RpcClient : IAsyncDisposable
{
    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private readonly StreamReader _reader;
    private long _nextId;

    private RpcClient(TcpClient tcp)
    {
        _tcp = tcp;
        _stream = tcp.GetStream();
        _reader = new StreamReader(_stream, Encoding.UTF8, false, 8192, leaveOpen: true);
    }

    public static async Task<RpcClient> ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port, ct);
        return new RpcClient(tcp);
    }

    public async Task<JsonRpcResponse> CallAsync(string method, object? @params = null, CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _nextId);
        var req = new JsonRpcRequest
        {
            Id = id,
            Method = method,
            Params = @params is null ? null : JsonSerializer.SerializeToElement(@params),
        };
        var json = JsonSerializer.Serialize(req, AgentJsonContext.Default.JsonRpcRequest);
        var bytes = Encoding.UTF8.GetBytes(json + "\n");
        await _stream.WriteAsync(bytes, ct);

        // Read frames until we see one whose id matches. Server-pushed
        // events arrive without an id; we ignore them here (event-aware
        // verbs use a different read loop).
        while (true)
        {
            var line = await _reader.ReadLineAsync(ct);
            if (line is null) throw new IOException("connection closed before response");
            var resp = JsonSerializer.Deserialize(line, AgentJsonContext.Default.JsonRpcResponse);
            if (resp?.Id == id) return resp;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
        _tcp.Dispose();
        _reader.Dispose();
    }

    public static (string host, int port) ResolveEndpoint(string? hostOverride, int? portOverride)
    {
        var host = hostOverride ?? "127.0.0.1";
        if (portOverride is int p) return (host, p);

        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClassicUO",
            "agent",
            "port.json");

        if (!File.Exists(path))
            throw new InvalidOperationException(
                $"agent server not advertised at '{path}'. Is the client running with -p:AGENT_BUILD=true? Override with --port.");

        var doc = JsonDocument.Parse(File.ReadAllText(path));
        var port = doc.RootElement.GetProperty("port").GetInt32();
        return (host, port);
    }
}
