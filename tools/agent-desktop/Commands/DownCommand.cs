// SPDX-License-Identifier: BSD-2-Clause
//
// `agent-desktop down` — idempotent teardown of a persisted rig. Reads
// .runtime/pids.json, sends `lifecycle.shutdown` to the client (graceful),
// then SIGTERMs ModernUO; SIGKILLs anything still alive after 5 s.

using System.CommandLine;

namespace ClassicUO.Agent.Desktop.Commands;

internal static class DownCommand
{
    public static Command Build()
    {
        var cmd = new Command("down", "Tear down the rig idempotently.");
        cmd.SetHandler(() =>
        {
            // TODO: implement teardown from .runtime/pids.json.
            Console.WriteLine("{\"status\":\"unimplemented\",\"verb\":\"down\"}");
        });
        return cmd;
    }
}
