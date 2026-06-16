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
        Process? child = null;

        try
        {
            child = await ClientProcess.SpawnAgentClientAsync(repoRoot, cts.Token);
        }
        catch (Exception ex)
        {
            EmitError($"spawn failed: {ex.Message}");
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
            Environment.ExitCode = 1;
            return;
        }

        EmitStatus(new { status = "up", pid = child.Id, port });

        if (persistMode)
        {
            try { Pids.SavePids(child.Id, port); }
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
        Pids.Clear();
        EmitStatus(new { status = "down" });
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
