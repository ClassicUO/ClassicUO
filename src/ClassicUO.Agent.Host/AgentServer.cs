// SPDX-License-Identifier: BSD-2-Clause
//
// TCP accept loop + frame reader/writer for the agent dev loop. Runs on a
// dedicated background Task started during plugin / bootstrap registration.
// Never touches engine / ECS state directly — only the Channels in
// AgentServerState and the socket.

#nullable enable

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Contracts.Dto;

namespace ClassicUO.Agent.Host;

public static class AgentServer
{
    private static Task? s_acceptLoop;

    public static void StartAcceptLoop(AgentServerState state)
    {
        if (s_acceptLoop is not null) return;
        s_acceptLoop = Task.Run(() => AcceptLoopAsync(state, state.Cancellation.Token));
    }

    private static async Task AcceptLoopAsync(AgentServerState state, CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        state.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        AdvertisePort(state);
        Console.WriteLine($"[agent] listening on 127.0.0.1:{state.Port}");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                var prev = Interlocked.Exchange(ref state.CurrentClient, client);
                prev?.Dispose();
                var stream = client.GetStream();
                Interlocked.Exchange(ref state.CurrentStream, stream);
                _ = Task.Run(() => ReadFramesAsync(state, stream, ct));
            }
        }
        catch (OperationCanceledException) { /* clean shutdown */ }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task ReadFramesAsync(AgentServerState state, Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, false, 8192, leaveOpen: true);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break; // disconnect

                JsonRpcRequest? req;
                try
                {
                    req = JsonSerializer.Deserialize(line, AgentJsonContext.Default.JsonRpcRequest);
                }
                catch (JsonException ex)
                {
                    await state.Outbox.Writer.WriteAsync(
                        ErrorResponse(null, JsonRpcErrorCodes.ParseError, ex.Message), ct);
                    continue;
                }

                if (req is null) continue;

                // Fast path: lifecycle.ping is a transport-level
                // connectivity check and must NOT depend on the engine /
                // ECS dispatcher. We answer it directly on the background
                // thread so the CLI can probe the listener even when the
                // engine is still booting (or has crashed mid-startup).
                if (req.Method == RpcVerbs.LifecyclePing)
                {
                    var pong = new JsonRpcResponse
                    {
                        Id = req.Id,
                        Result = new System.Text.Json.Nodes.JsonObject
                        {
                            ["pong"] = true,
                            ["port"] = state.Port,
                        },
                    };
                    var bytes = Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(pong, AgentJsonContext.Default.JsonRpcResponse) + "\n");
                    try { await stream.WriteAsync(bytes, ct); }
                    catch (IOException) { break; }
                    continue;
                }

                await state.Inbox.Writer.WriteAsync(req, ct);
            }
        }
        catch (OperationCanceledException) { /* clean shutdown */ }
        catch (IOException) { /* peer dropped */ }
    }

    public static void FlushOutbox(AgentServerState state)
    {
        // Per-frame: drain any responses/events into the socket. We use
        // the synchronous Channel API + TryRead so a slow socket can't
        // block the game loop. If the write would block, we leave the
        // remaining items for next frame (channel is unbounded).
        var stream = state.CurrentStream;
        if (stream is null) return;

        while (state.Outbox.Reader.TryRead(out var msg))
        {
            try
            {
                var bytes = SerializeFrame(msg);
                stream.Write(bytes, 0, bytes.Length);
            }
            catch (IOException)
            {
                // Connection dropped; clear the slot. Next accept will
                // populate a fresh CurrentClient.
                Interlocked.Exchange(ref state.CurrentStream, null);
                state.CurrentClient?.Dispose();
                Interlocked.Exchange(ref state.CurrentClient, null);
                return;
            }
        }
    }

    public static JsonRpcResponse ErrorResponse(long? id, int code, string message) => new()
    {
        Id = id,
        Error = new JsonRpcError { Code = code, Message = message }
    };

    private static byte[] SerializeFrame(object msg)
    {
        // Hot path; one allocation per message, newline terminator.
        var json = msg switch
        {
            JsonRpcResponse r => JsonSerializer.Serialize(r, AgentJsonContext.Default.JsonRpcResponse),
            JsonRpcEvent e => JsonSerializer.Serialize(e, AgentJsonContext.Default.JsonRpcEvent),
            _ => throw new InvalidOperationException($"unexpected outbox payload: {msg.GetType()}")
        };
        return Encoding.UTF8.GetBytes(json + "\n");
    }

    private static void AdvertisePort(AgentServerState state)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClassicUO",
            "agent");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "port.json");
        File.WriteAllText(path, $"{{\"port\":{state.Port},\"pid\":{Environment.ProcessId}}}");
        state.PortAdvertisedAt = path;
    }
}
