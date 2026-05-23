// SPDX-License-Identifier: BSD-2-Clause
//
// Spawn and lifecycle helpers for the agent-flavor desktop client
// (bin/agent/net9.0/cuo.agent.dll). The child writes its loopback port
// to %LOCALAPPDATA%/ClassicUO/agent/port.json on startup (see
// AgentServer.AdvertisePort) — we poll for that file, then probe the
// JSON-RPC port with `lifecycle.ping` to confirm the server is actually
// accepting connections (not just that the file exists).

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ClassicUO.Agent.Contracts;

namespace ClassicUO.Agent.Desktop.Services;

internal static class ClientProcess
{
    private const string AgentDllRelative = "bin/agent/net9.0/cuo.agent.dll";

    public static string GetAgentDllPath(string repoRoot)
        => Path.Combine(repoRoot, AgentDllRelative.Replace('/', Path.DirectorySeparatorChar));

    public static string GetPortJsonPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClassicUO",
            "agent",
            "port.json");

    public static Task<Process> SpawnAgentClientAsync(string repoRoot, CancellationToken ct)
    {
        var dll = GetAgentDllPath(repoRoot);
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add(dll);

        // Best-effort: clear stale port.json so we don't latch onto the
        // previous run's advertisement during the readiness wait.
        try
        {
            var portFile = GetPortJsonPath();
            if (File.Exists(portFile)) File.Delete(portFile);
        }
        catch
        {
            // Stale port detection in WaitForReadyAsync handles a connect
            // failure to the previous port gracefully.
        }

        var child = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdoutTail = new RingBuffer(64);
        var stderrTail = new RingBuffer(64);
        child.OutputDataReceived += (_, e) => { if (e.Data is not null) stdoutTail.Add(e.Data); };
        child.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderrTail.Add(e.Data); };

        if (!child.Start())
            throw new InvalidOperationException("failed to start cuo.agent (Process.Start returned false)");

        child.BeginOutputReadLine();
        child.BeginErrorReadLine();

        // Attach the tails so WaitForReadyAsync can include them in the
        // exit error without re-plumbing arguments.
        s_tails[child.Id] = (stdoutTail, stderrTail);
        child.Exited += (_, _) =>
        {
            // Keep the tails available briefly so the error reporter can
            // grab them after HasExited becomes true.
        };

        ct.ThrowIfCancellationRequested();
        return Task.FromResult(child);
    }

    public static async Task<int> WaitForReadyAsync(Process child, TimeSpan timeout, CancellationToken ct)
    {
        var portFile = GetPortJsonPath();
        var deadline = DateTime.UtcNow + timeout;

        int? port = null;
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            if (child.HasExited)
                throw BuildExitedException(child);

            if (port is null)
            {
                port = TryReadPort(portFile);
                if (port is null)
                {
                    await Task.Delay(200, ct);
                    continue;
                }
            }

            // Try a ping. The file may be present a few ms before the
            // listener is actually accepting; retry on connect failure.
            try
            {
                await using var rpc = await RpcClient.ConnectAsync("127.0.0.1", port.Value, ct);
                var resp = await rpc.CallAsync(RpcVerbs.LifecyclePing, ct: ct);
                if (resp.Error is null && IsPong(resp.Result))
                    return port.Value;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Listener not up yet, or stale port. Re-read on next loop.
                port = null;
            }

            await Task.Delay(200, ct);
        }

        if (child.HasExited)
            throw BuildExitedException(child);

        throw new TimeoutException(
            $"cuo.agent did not become ready within {timeout.TotalMilliseconds:F0} ms");
    }

    private static int? TryReadPort(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            // Open with FileShare.ReadWrite to coexist with the writer in
            // case it hasn't fsynced yet.
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8);
            var text = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(text)) return null;
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("port", out var p)) return null;
            if (p.ValueKind != JsonValueKind.Number) return null;
            return p.GetInt32();
        }
        catch
        {
            // Partial write, JSON parse error, or transient lock. The
            // caller will retry on the next poll tick.
            return null;
        }
    }

    private static bool IsPong(System.Text.Json.Nodes.JsonNode? result)
    {
        if (result is null) return false;
        try
        {
            var pong = result["pong"];
            if (pong is null) return false;
            return pong.GetValue<bool>();
        }
        catch
        {
            return false;
        }
    }

    private static Exception BuildExitedException(Process child)
    {
        s_tails.TryGetValue(child.Id, out var tails);
        var stdout = tails.stdout?.Render() ?? string.Empty;
        var stderr = tails.stderr?.Render() ?? string.Empty;
        var msg = new StringBuilder();
        msg.Append("cuo.agent exited (code ").Append(child.ExitCode).Append(") before signalling ready.");
        if (stderr.Length > 0)
        {
            msg.Append(" stderr-tail: ").Append(stderr);
        }
        if (stdout.Length > 0)
        {
            msg.Append(" stdout-tail: ").Append(stdout);
        }
        return new InvalidOperationException(msg.ToString());
    }

    // PID-keyed so a verb can spawn → wait → recover tails even though the
    // spawn call returns just a Process handle.
    private static readonly Dictionary<int, (RingBuffer stdout, RingBuffer stderr)> s_tails = new();

    private sealed class RingBuffer
    {
        private readonly Queue<string> _lines;
        private readonly int _capacity;
        public RingBuffer(int capacity) { _capacity = capacity; _lines = new(capacity); }
        public void Add(string line)
        {
            lock (_lines)
            {
                if (_lines.Count >= _capacity) _lines.Dequeue();
                _lines.Enqueue(line);
            }
        }
        public string Render()
        {
            lock (_lines)
            {
                return string.Join(" | ", _lines);
            }
        }
    }
}
