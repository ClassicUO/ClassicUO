// SPDX-License-Identifier: BSD-2-Clause
//
// `agent-desktop windows` — list visible top-level windows with PID +
// title. Helpful for picking a --pid or --title to drive.

using System;
using System.CommandLine;
using System.Runtime.Versioning;
using System.Text.Json;
using ClassicUO.Agent.Desktop.OsDriver;

namespace ClassicUO.Agent.Desktop.Commands;

[SupportedOSPlatform("windows5.0")]
internal static class WindowsListCommand
{
    public static Command Build()
    {
        var cmd = new Command("windows", "List visible top-level windows (hwnd, pid, title).");
        cmd.SetHandler(() =>
        {
            var list = WindowFinder.EnumerateVisible();
            foreach (var (h, t, p) in list)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    hwnd = h.ToInt64(),
                    pid = p,
                    title = t,
                }));
            }
        });
        return cmd;
    }
}
