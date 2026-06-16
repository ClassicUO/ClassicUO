// SPDX-License-Identifier: BSD-2-Clause
//
// `agent-desktop down` — idempotent teardown of a persisted rig. Reads
// .runtime/pids.json, asks the client to `lifecycle.shutdown` (graceful),
// then kills the client and any ModernUO we launched by pid; SIGKILLs
// anything still alive after a short grace. No pids file → no-op.

using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Desktop.Services;

namespace ClassicUO.Agent.Desktop.Commands;

internal static class DownCommand
{
    public static Command Build()
    {
        var cmd = new Command("down", "Tear down the rig idempotently.");
        cmd.SetHandler(async () => await RunAsync());
        return cmd;
    }

    private static async Task RunAsync()
    {
        var registry = Pids.LoadPids();
        if (registry?.Client is null && registry?.Server is null)
        {
            Console.WriteLine("{\"status\":\"down\",\"note\":\"no persisted rig\"}");
            return;
        }

        if (registry.Client is { } client)
        {
            // Best-effort graceful shutdown before the hard kill.
            try
            {
                await using var rpc = await RpcClient.ConnectAsync("127.0.0.1", client.Port);
                await rpc.CallAsync(RpcVerbs.LifecycleShutdown);
            }
            catch
            {
                // Already gone, or refusing connections. Kill below handles it.
            }
            KillByPid(client.Pid);
        }

        if (registry.Server is { } server)
            KillByPid(server.Pid);

        Pids.Clear();
        Console.WriteLine("{\"status\":\"down\"}");
    }

    private static void KillByPid(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            if (!proc.HasExited)
            {
                proc.Kill(entireProcessTree: true);
                proc.WaitForExit(5000);
            }
        }
        catch
        {
            // Process already exited or pid recycled — nothing to do.
        }
    }
}
