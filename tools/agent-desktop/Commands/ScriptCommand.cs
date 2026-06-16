// SPDX-License-Identifier: BSD-2-Clause
//
// `agent-desktop script --file path.json` — execute a list of RPC calls
// against a single TCP connection. Avoids the per-verb `dotnet` startup
// (~400-600 ms) that dominates wall time when chaining many CLI calls.
//
// Step shapes (each element of the top-level JSON array):
//
//   { "verb": "<rpc.verb>", "params": { ... }, "queueWait": N }
//     Call <verb> with <params>; after the response, sleep
//     (queueWait ?? result.queued ?? 0) * frameMs ms before the next step.
//
//   { "sleepMs": N }
//     Wall-sleep N ms (no RPC). Use between visual changes that the
//     engine produces asynchronously (e.g. server packet → ECS spawn).
//
//   { "poll": { "verb": "lifecycle.inWorld", "field": "inWorld",
//               "expect": true, "timeoutMs": 15000, "intervalMs": 250 } }
//     Repeatedly call <verb> until result.<field> equals <expect> (or
//     the timeout fires). Use for in-world readiness and similar gates.
//
//   { "log": "marker text" }
//     Print { "log": "marker text" } to stdout. Useful for sectioning
//     the JSON output of a long script.
//
// Output: one JSON object per step + a final { "status": "script-ok" }
// or { "error": "..." } summary. ExitCode is non-zero on any failure.

using System;
using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Contracts.Dto;
using ClassicUO.Agent.Desktop.Services;

namespace ClassicUO.Agent.Desktop.Commands;

internal static class ScriptCommand
{
    public static Command Build()
    {
        var host = new Option<string?>("--host");
        var port = new Option<int?>("--port");
        var file = new Option<string>("--file",
            "Path to a JSON array of steps.") { IsRequired = true };
        var frameMs = new Option<int>("--frame-ms", () => 20,
            "Per-frame pacing for input.* steps when queueWait isn't pinned.");

        var cmd = new Command("script",
            "Run a JSON script of RPC calls against a single connection. Faster than chaining individual CLI invocations.");
        cmd.AddOption(host);
        cmd.AddOption(port);
        cmd.AddOption(file);
        cmd.AddOption(frameMs);

        cmd.SetHandler(async (string? h, int? p, string filePath, int fm) =>
        {
            await RunAsync(h, p, filePath, fm);
        }, host, port, file, frameMs);

        return cmd;
    }

    private static async Task RunAsync(string? h, int? p, string filePath, int frameMs)
    {
        if (!File.Exists(filePath))
        {
            EmitError($"file not found: {filePath}");
            Environment.ExitCode = 1;
            return;
        }

        JsonArray steps;
        try
        {
            var json = DotEnv.Expand(await File.ReadAllTextAsync(filePath));
            var node = JsonNode.Parse(json);
            if (node is not JsonArray arr)
            {
                EmitError("script root must be a JSON array");
                Environment.ExitCode = 1;
                return;
            }
            steps = arr;
        }
        catch (Exception ex)
        {
            EmitError($"parse error: {ex.Message}");
            Environment.ExitCode = 1;
            return;
        }

        var (host, port) = RpcClient.ResolveEndpoint(h, p);
        await using var rpc = await RpcClient.ConnectAsync(host, port);

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step is not JsonObject obj)
            {
                EmitError($"step #{i} must be a JSON object");
                Environment.ExitCode = 1;
                return;
            }

            try
            {
                if (obj["log"] is JsonNode logNode)
                {
                    Emit(new JsonObject { ["step"] = i, ["log"] = logNode.GetValue<string>() });
                    continue;
                }

                if (obj["sleepMs"] is JsonNode sleepNode)
                {
                    var ms = sleepNode.GetValue<int>();
                    Emit(new JsonObject { ["step"] = i, ["sleepMs"] = ms });
                    await Task.Delay(Math.Max(0, ms));
                    continue;
                }

                if (obj["poll"] is JsonObject pollObj)
                {
                    await RunPoll(rpc, i, pollObj);
                    continue;
                }

                if (obj["verb"] is JsonNode verbNode)
                {
                    var verb = verbNode.GetValue<string>();
                    var paramsNode = obj["params"];
                    var resp = await rpc.CallAsync(verb,
                        paramsNode is null ? null : JsonNode.Parse(paramsNode.ToJsonString()));
                    Emit(new JsonObject
                    {
                        ["step"] = i,
                        ["verb"] = verb,
                        ["response"] = JsonNode.Parse(JsonSerializer.Serialize(resp,
                            AgentJsonContext.Default.JsonRpcResponse)),
                    });
                    if (resp.Error is not null)
                    {
                        Environment.ExitCode = 1;
                        return;
                    }
                    // queueWait override; otherwise read 'queued' from result.
                    int waitFrames = 0;
                    if (obj["queueWait"] is JsonNode qw && qw.GetValue<int>() > 0)
                        waitFrames = qw.GetValue<int>();
                    else if (resp.Result is JsonObject r && r["queued"] is JsonNode qn)
                        waitFrames = qn.GetValue<int>();
                    if (waitFrames > 0)
                        await Task.Delay(waitFrames * frameMs);
                    continue;
                }

                EmitError($"step #{i}: unknown shape (need verb|sleepMs|poll|log)");
                Environment.ExitCode = 1;
                return;
            }
            catch (Exception ex)
            {
                EmitError($"step #{i}: {ex.Message}");
                Environment.ExitCode = 1;
                return;
            }
        }

        Emit(new JsonObject { ["status"] = "script-ok", ["steps"] = steps.Count });
    }

    private static async Task RunPoll(RpcClient rpc, int stepIdx, JsonObject pollObj)
    {
        var verb = pollObj["verb"]?.GetValue<string>()
            ?? throw new InvalidOperationException("poll: missing 'verb'");
        var field = pollObj["field"]?.GetValue<string>()
            ?? throw new InvalidOperationException("poll: missing 'field'");
        var timeoutMs = pollObj["timeoutMs"]?.GetValue<int>() ?? 15000;
        var intervalMs = pollObj["intervalMs"]?.GetValue<int>() ?? 250;
        var expect = pollObj["expect"];

        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var resp = await rpc.CallAsync(verb);
            if (resp.Error is null && resp.Result is JsonObject r && r[field] is JsonNode v)
            {
                var match = expect is null
                    ? v.GetValue<bool>()
                    : JsonNode.DeepEquals(v, expect);
                if (match)
                {
                    Emit(new JsonObject { ["step"] = stepIdx, ["poll"] = verb, [field] = v.DeepClone() });
                    return;
                }
            }
            await Task.Delay(intervalMs);
        }
        throw new TimeoutException($"poll {verb}.{field} did not satisfy expect within {timeoutMs}ms");
    }

    private static void Emit(JsonNode payload)
    {
        Console.WriteLine(payload.ToJsonString());
    }

    private static void EmitError(string message)
    {
        Console.WriteLine($"{{\"error\":{JsonSerializer.Serialize(message)}}}");
    }
}
