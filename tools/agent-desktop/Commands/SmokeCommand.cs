// SPDX-License-Identifier: BSD-2-Clause
//
// `agent-desktop smoke` — end-to-end sanity check against a running rig.
// Exercises three RPC verbs in order:
//
//   1. agent.login         → dispatches a login request through the engine.
//   2. lifecycle.inWorld   → polled until the player has actually entered
//                            the world (or until the deadline expires).
//   3. world.dumpState     → reads back the world snapshot the agent can
//                            see; pretty-printed to stdout for humans.
//
// Output convention matches UpCommand: every status/error line is a single
// JSON object on stdout; the final "smoke-ok" summary is the last line on
// success. The pretty-printed world.dumpState result is emitted between
// the polling-done and summary lines so a human reader can eyeball it.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Contracts.Dto;

namespace ClassicUO.Agent.Desktop.Commands;

internal static class SmokeCommand
{
    private const int PollIntervalMs = 250;

    public static Command Build()
    {
        var attach = new Option<bool>(
            "--attach",
            "Drive the persisted rig instead of bringing up a new one. "
            + "(currently always attaches — no-op flag for forward-compatibility with --no-attach)");

        var username = new Option<string>(
            "--username",
            () => "admin",
            "Login username.");

        var password = new Option<string>(
            "--password",
            () => "admin",
            "Login password (plaintext; encrypted on the way through).");

        var shardAddress = new Option<string>(
            "--shard-address",
            () => "127.0.0.1",
            "Shard server address.");

        var shardPort = new Option<int>(
            "--shard-port",
            () => 2593,
            "Shard server port.");

        var inWorldTimeoutMs = new Option<int>(
            "--in-world-timeout-ms",
            () => 15000,
            "Max wait (ms) for lifecycle.inWorld → true after login dispatched.");

        var host = new Option<string?>(
            "--host",
            "Agent server host (default 127.0.0.1).");

        var port = new Option<int?>(
            "--port",
            "Agent server port (default: read from port.json sidecar).");

        var cmd = new Command("smoke",
            "Login + poll inWorld + dump world state. End-to-end sanity check against a running rig.");
        foreach (var o in new Option[] { attach, username, password, shardAddress, shardPort, inWorldTimeoutMs, host, port })
            cmd.AddOption(o);

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var parsed = ctx.ParseResult;
            var attachValue = parsed.GetValueForOption(attach);
            var usernameValue = parsed.GetValueForOption(username) ?? "admin";
            var passwordValue = parsed.GetValueForOption(password) ?? "admin";
            var shardAddressValue = parsed.GetValueForOption(shardAddress) ?? "127.0.0.1";
            var shardPortValue = parsed.GetValueForOption(shardPort);
            var timeoutValue = parsed.GetValueForOption(inWorldTimeoutMs);
            var hostValue = parsed.GetValueForOption(host);
            var portValue = parsed.GetValueForOption(port);

            await RunAsync(
                attachValue,
                usernameValue,
                passwordValue,
                shardAddressValue,
                shardPortValue,
                timeoutValue,
                hostValue,
                portValue);
        });

        return cmd;
    }

    private static async Task RunAsync(
        bool attachMode,
        string username,
        string password,
        string shardAddress,
        int shardPort,
        int inWorldTimeoutMs,
        string? hostValue,
        int? portValue)
    {
        _ = attachMode; // v1: smoke always attaches; flag reserved for forward-compat.

        string h;
        int p;
        try
        {
            (h, p) = RpcClient.ResolveEndpoint(hostValue, portValue);
        }
        catch (Exception ex)
        {
            EmitError(ex.Message);
            Environment.ExitCode = 1;
            return;
        }

        RpcClient rpc;
        try
        {
            rpc = await RpcClient.ConnectAsync(h, p);
        }
        catch
        {
            EmitError("no rig: run `agent-desktop up --persist` first");
            Environment.ExitCode = 1;
            return;
        }

        var sw = Stopwatch.StartNew();

        await using (rpc)
        {
            // 1. agent.login
            JsonRpcResponse loginResp;
            try
            {
                loginResp = await rpc.CallAsync(RpcVerbs.AgentLogin, new
                {
                    username,
                    password,
                    address = shardAddress,
                    port = shardPort,
                });
            }
            catch (Exception ex)
            {
                EmitError($"agent.login call failed: {ex.Message}");
                Environment.ExitCode = 1;
                return;
            }

            if (loginResp.Error is not null)
            {
                EmitRpcError("agent.login", loginResp.Error);
                Environment.ExitCode = 1;
                return;
            }

            var dispatched = loginResp.Result?["dispatched"]?.GetValue<bool>() ?? false;
            if (!dispatched)
            {
                EmitError("agent.login did not return dispatched=true");
                Environment.ExitCode = 1;
                return;
            }

            EmitStatus(new { status = "login-dispatched", elapsedMs = sw.ElapsedMilliseconds });

            // 2. Poll lifecycle.inWorld every PollIntervalMs ms until true or timeout.
            var pollStart = sw.ElapsedMilliseconds;
            var deadline = pollStart + inWorldTimeoutMs;
            var inWorld = false;
            while (sw.ElapsedMilliseconds < deadline)
            {
                JsonRpcResponse probe;
                try
                {
                    probe = await rpc.CallAsync(RpcVerbs.LifecycleInWorld);
                }
                catch (Exception ex)
                {
                    EmitError($"lifecycle.inWorld call failed: {ex.Message}");
                    Environment.ExitCode = 1;
                    return;
                }

                if (probe.Error is not null)
                {
                    EmitRpcError("lifecycle.inWorld", probe.Error);
                    Environment.ExitCode = 1;
                    return;
                }

                if (probe.Result?["inWorld"]?.GetValue<bool>() == true)
                {
                    inWorld = true;
                    break;
                }

                await Task.Delay(PollIntervalMs);
            }

            if (!inWorld)
            {
                var elapsed = sw.ElapsedMilliseconds - pollStart;
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    error = $"did not reach in-world within {inWorldTimeoutMs}ms",
                    elapsedMs = elapsed,
                }));
                Environment.ExitCode = 1;
                return;
            }

            EmitStatus(new { status = "in-world", elapsedMs = sw.ElapsedMilliseconds });

            // 3. world.dumpState
            JsonRpcResponse dumpResp;
            try
            {
                dumpResp = await rpc.CallAsync(RpcVerbs.WorldDumpState);
            }
            catch (Exception ex)
            {
                EmitError($"world.dumpState call failed: {ex.Message}");
                Environment.ExitCode = 1;
                return;
            }

            if (dumpResp.Error is not null)
            {
                EmitRpcError("world.dumpState", dumpResp.Error);
                Environment.ExitCode = 1;
                return;
            }

            // Pretty-print the dump so humans can eyeball it.
            Console.WriteLine(JsonSerializer.Serialize(
                dumpResp.Result,
                new JsonSerializerOptions { WriteIndented = true }));

            // 4. Final summary line with the player's serial + position.
            object? playerSummary = null;
            try
            {
                var player = dumpResp.Result?["player"];
                if (player is not null)
                {
                    playerSummary = new
                    {
                        serial = player["serial"]?.GetValue<long>(),
                        position = player["position"],
                    };
                }
            }
            catch
            {
                // Tolerate odd shapes: still print the summary without player.
                playerSummary = null;
            }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                status = "smoke-ok",
                elapsedMs = sw.ElapsedMilliseconds,
                player = playerSummary,
            }));
        }
    }

    private static void EmitStatus(object payload)
    {
        Console.WriteLine(JsonSerializer.Serialize(payload));
    }

    private static void EmitError(string message)
    {
        Console.WriteLine($"{{\"error\":{JsonSerializer.Serialize(message)}}}");
    }

    private static void EmitRpcError(string verb, JsonRpcError err)
    {
        // Surface the full JSON-RPC error object so callers can match on code.
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            error = err.Message,
            code = err.Code,
            verb,
            data = err.Data,
        }));
    }
}
