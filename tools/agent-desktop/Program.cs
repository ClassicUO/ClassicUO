// SPDX-License-Identifier: BSD-2-Clause
//
// Supervisor CLI for the ClassicUO desktop agent dev loop. Orchestrates a
// ModernUO instance + the desktop client (built with AGENT_BUILD) + a
// Playwright-less screen-capture sidecar, and drives them over a JSON-RPC
// channel exposed by the in-process AgentServer inside the client.
//
// Mirrors the verb shape of the web agent CLI at
// classicuo-wasm/web/apps/agent/ so muscle memory transfers between the two
// loops.

using System.CommandLine;
using System.Threading.Tasks;
using ClassicUO.Agent.Desktop.Commands;

namespace ClassicUO.Agent.Desktop;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var root = new RootCommand(
            "Supervisor CLI for the ClassicUO desktop agent dev loop. Brings up ModernUO + the agent-flavor desktop client, then drives them over JSON-RPC.");

        root.AddCommand(UpCommand.Build());
        root.AddCommand(DownCommand.Build());
        root.AddCommand(SmokeCommand.Build());
        root.AddCommand(PingCommand.Build());

        if (OperatingSystem.IsWindowsVersionAtLeast(5))
        {
            root.AddCommand(WindowsListCommand.Build());
            root.AddCommand(ShotCommand.Build());
            root.AddCommand(InputCommands.BuildMove());
            root.AddCommand(InputCommands.BuildClick());
            root.AddCommand(InputCommands.BuildDoubleClick());
            root.AddCommand(InputCommands.BuildHold());
            root.AddCommand(InputCommands.BuildRelease());
            root.AddCommand(InputCommands.BuildType());
            root.AddCommand(DiffCommand.Build());
            root.AddCommand(ScenarioCommand.Build());
        }

        // RPC-based commands work on any platform with an active agent
        // server connection (loopback TCP). These exercise the in-process
        // synthetic input + backbuffer capture path (preferred). The os-*
        // commands above stay as a branch-agnostic fallback.
        root.AddCommand(RpcInputCommands.BuildMove());
        root.AddCommand(RpcInputCommands.BuildClick());
        root.AddCommand(RpcInputCommands.BuildDoubleClick());
        root.AddCommand(RpcInputCommands.BuildHold());
        root.AddCommand(RpcInputCommands.BuildRelease());
        root.AddCommand(RpcInputCommands.BuildClear());
        root.AddCommand(RpcInputCommands.BuildType());
        root.AddCommand(RpcInputCommands.BuildShot());

        var invokeExit = await root.InvokeAsync(args);
        // System.CommandLine's InvokeAsync returns its own code (0 on success);
        // verbs that fail set Environment.ExitCode instead of throwing. Honour
        // whichever is non-zero.
        return invokeExit != 0 ? invokeExit : Environment.ExitCode;
    }
}
