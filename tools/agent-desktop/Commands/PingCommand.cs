// SPDX-License-Identifier: BSD-2-Clause
//
// `agent-desktop ping` — smallest end-to-end sanity check: connect to the
// running agent server, send lifecycle.ping, print the JSON response.

using System.CommandLine;
using System.Text.Json;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Contracts.Dto;

namespace ClassicUO.Agent.Desktop.Commands;

internal static class PingCommand
{
    public static Command Build()
    {
        var host = new Option<string?>("--host", "Server host (default 127.0.0.1).");
        var port = new Option<int?>("--port", "Server port (default: read from port.json sidecar).");

        var cmd = new Command("ping", "Send lifecycle.ping to the running agent server; print the JSON reply.");
        cmd.AddOption(host);
        cmd.AddOption(port);

        cmd.SetHandler(async (string? hostValue, int? portValue) =>
        {
            try
            {
                var (h, p) = RpcClient.ResolveEndpoint(hostValue, portValue);
                await using var rpc = await RpcClient.ConnectAsync(h, p);
                var resp = await rpc.CallAsync(RpcVerbs.LifecyclePing);
                Console.WriteLine(JsonSerializer.Serialize(resp, AgentJsonContext.Default.JsonRpcResponse));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{{\"error\":{JsonSerializer.Serialize(ex.Message)}}}");
                Environment.ExitCode = 1;
            }
        }, host, port);

        return cmd;
    }
}
