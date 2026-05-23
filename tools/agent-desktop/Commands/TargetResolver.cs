// SPDX-License-Identifier: BSD-2-Clause
//
// Common HWND resolution + JSON helper for the OS-driver commands.

using System;
using System.Runtime.Versioning;
using System.Text.Json;
using ClassicUO.Agent.Desktop.OsDriver;

namespace ClassicUO.Agent.Desktop.Commands;

[SupportedOSPlatform("windows5.0")]
internal static class TargetResolver
{
    public static IntPtr Resolve(int? pid, string? titleSubstring)
    {
        if (pid is int p)
        {
            var h = WindowFinder.FindByPid(p);
            if (h != IntPtr.Zero) return h;
        }
        if (!string.IsNullOrWhiteSpace(titleSubstring))
        {
            var h = WindowFinder.FindByTitleSubstring(titleSubstring);
            if (h != IntPtr.Zero) return h;
        }
        return IntPtr.Zero;
    }
}

internal static class CliJson
{
    public static void Status(object payload)
        => Console.WriteLine(JsonSerializer.Serialize(payload));

    public static void Error(string msg)
        => Console.Error.WriteLine($"{{\"error\":{JsonSerializer.Serialize(msg)}}}");
}
