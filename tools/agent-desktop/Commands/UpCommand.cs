// SPDX-License-Identifier: BSD-2-Clause
//
// `agent-desktop up [--persist]` — bring up the desktop client (agent
// flavor) and wait for the JSON-RPC server to answer `lifecycle.ping`.
// With --persist, return as soon as the rig is ready and record the
// child PID/port to .runtime/pids.json for a later `agent-desktop down`.
// Without --persist, block until SIGINT and tear the child down on exit.

using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.Agent.Desktop.Services;

namespace ClassicUO.Agent.Desktop.Commands;

internal static class UpCommand
{
    public static Command Build()
    {
        var persist = new Option<bool>(
            "--persist",
            "Leave the rig running in the background and return immediately.");

        var readyTimeoutMs = new Option<int>(
            "--ready-timeout-ms",
            () => 10000,
            "Maximum time (ms) to wait for the client to advertise its port and answer lifecycle.ping.");

        var cmd = new Command("up",
            "Stand up the rig: ClassicUO (agent flavor). Foreground; Ctrl-C tears it down unless --persist.");
        cmd.AddOption(persist);
        cmd.AddOption(readyTimeoutMs);

        cmd.SetHandler(async (bool persistMode, int timeoutMs) =>
        {
            await RunAsync(persistMode, timeoutMs);
        }, persist, readyTimeoutMs);

        return cmd;
    }

    private static async Task RunAsync(bool persistMode, int timeoutMs)
    {
        var repoRoot = RepoRoot.Find();
        if (repoRoot is null)
        {
            EmitError(
                "could not locate src/ClassicUO.Client/ClassicUO.Client.csproj by walking up from "
                + AppContext.BaseDirectory);
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            ApplyDotEnvToSettings(repoRoot);
        }
        catch (Exception ex)
        {
            // Non-fatal: stale/missing settings just means the client uses
            // whatever is already on disk. Warn and keep going.
            EmitWarning($"failed to apply .env to settings.json: {ex.Message}");
        }

        var agentDll = ClientProcess.GetAgentDllPath(repoRoot);
        if (!File.Exists(agentDll))
        {
            EmitError("no agent build; run dotnet build src/ClassicUO.Client -p:AGENT_BUILD=true first");
            Environment.ExitCode = 1;
            return;
        }

        using var cts = new CancellationTokenSource();
        Process? server = null;
        Process? child = null;

        // Optional: launch ModernUO from MODERNUO_PATH before the client so
        // its world is loading while the client boots to the login screen.
        // Absent/unresolvable key → assume an externally managed server.
        var serverExe = ServerProcess.ResolveExecutable();
        if (DotEnv.Get("MODERNUO_PATH") is { Length: > 0 } && serverExe is null)
            EmitWarning("MODERNUO_PATH set but no ModernUO executable found there; using external server");
        if (serverExe is not null)
        {
            try
            {
                server = ServerProcess.Spawn(serverExe);
            }
            catch (Exception ex)
            {
                EmitWarning($"failed to launch ModernUO: {ex.Message}; using external server");
            }
        }

        try
        {
            child = await ClientProcess.SpawnAgentClientAsync(repoRoot, cts.Token);
        }
        catch (Exception ex)
        {
            EmitError($"spawn failed: {ex.Message}");
            TryKill(server);
            Environment.ExitCode = 1;
            return;
        }

        int port;
        try
        {
            port = await ClientProcess.WaitForReadyAsync(child, TimeSpan.FromMilliseconds(timeoutMs), cts.Token);
        }
        catch (Exception ex)
        {
            EmitError(ex.Message);
            TryKill(child);
            TryKill(server);
            Environment.ExitCode = 1;
            return;
        }

        // Wait for the launched server to accept connections so a login
        // immediately after `up` doesn't race a still-booting world.
        // ponytail: fixed 30s; add a flag if a slow shard needs longer.
        if (server is not null)
        {
            var shardPort = ReadServerPort(repoRoot);
            var listening = await ServerProcess.WaitForListeningAsync(
                shardPort, TimeSpan.FromSeconds(30), cts.Token);
            if (!listening)
                EmitWarning($"ModernUO not accepting on 127.0.0.1:{shardPort} yet; login may need a retry");
        }

        EmitStatus(new { status = "up", pid = child.Id, port, serverPid = server?.Id });

        if (persistMode)
        {
            try { Pids.SavePids(child.Id, port, server?.Id); }
            catch (Exception ex)
            {
                // Non-fatal: the rig is already up. Report and exit 0.
                EmitWarning($"failed to write pids.json: {ex.Message}");
            }
            return;
        }

        // Foreground: block until SIGINT or child exit, then teardown.
        var done = new TaskCompletionSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            done.TrySetResult();
        };
        child.EnableRaisingEvents = true;
        child.Exited += (_, _) => done.TrySetResult();

        await done.Task;

        TryKill(child);
        TryKill(server);
        Pids.Clear();
        EmitStatus(new { status = "down" });
    }

    // The shard port the client (and thus our readiness probe) connects to.
    // Mirrors the `port` pin in repo settings.json; ModernUO default 2593.
    private static int ReadServerPort(string repoRoot)
    {
        try
        {
            var settingsPath = Path.Combine(repoRoot, "settings.json");
            if (File.Exists(settingsPath) &&
                JsonNode.Parse(File.ReadAllText(settingsPath)) is JsonObject root &&
                root["port"] is JsonNode p)
                return p.GetValue<int>();
        }
        catch
        {
            // Fall through to the default.
        }
        return 2593;
    }

    // Overlay .env onto the repo-level settings.json the client reads from
    // CWD. Only the client-config keys live here (creds are consumed by the
    // login verbs from .env directly). Other settings.json fields — window
    // size, ip/port pins — are preserved untouched.
    private static void ApplyDotEnvToSettings(string repoRoot)
    {
        var clientVersion = DotEnv.Get("UO_CLIENT_VERSION");
        var uoDirectory = DotEnv.Get("UO_DIRECTORY");
        if (clientVersion is null && uoDirectory is null)
            return;

        var settingsPath = Path.Combine(repoRoot, "settings.json");
        var root = File.Exists(settingsPath)
            ? JsonNode.Parse(File.ReadAllText(settingsPath)) as JsonObject
            : null;
        root ??= new JsonObject();

        if (clientVersion is not null)
            root["clientversion"] = clientVersion;
        if (uoDirectory is not null)
            root["ultimaonlinedirectory"] = uoDirectory;

        File.WriteAllText(settingsPath,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void TryKill(Process? child)
    {
        if (child is null) return;
        try
        {
            if (!child.HasExited)
            {
                child.Kill(entireProcessTree: true);
                child.WaitForExit(5000);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private static void EmitStatus(object payload)
    {
        Console.WriteLine(JsonSerializer.Serialize(payload));
    }

    private static void EmitError(string message)
    {
        Console.Error.WriteLine(
            $"{{\"error\":{JsonSerializer.Serialize(message)}}}");
    }

    private static void EmitWarning(string message)
    {
        Console.Error.WriteLine(
            $"{{\"warning\":{JsonSerializer.Serialize(message)}}}");
    }
}
