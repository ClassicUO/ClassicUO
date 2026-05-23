// SPDX-License-Identifier: BSD-2-Clause
//
// CLI verbs for OS-level mouse and keyboard synthesis: move, click,
// double-click, mouse-hold (down without up), mouse-release (up
// without down), and type. All take --pid or --title to resolve the
// target window plus --x/--y in client coordinates.

using System;
using System.CommandLine;
using System.Runtime.Versioning;
using ClassicUO.Agent.Desktop.OsDriver;

namespace ClassicUO.Agent.Desktop.Commands;

[SupportedOSPlatform("windows5.0")]
internal static class InputCommands
{
    public static Command BuildMove() => BuildSimple("mouse-move",
        "Move cursor to (x, y) in target window client coordinates.",
        (hwnd, x, y, _) => InputDriver.MouseMove(hwnd, x, y));

    public static Command BuildClick() => BuildButton("click",
        "Single click at (x, y).",
        (hwnd, x, y, btn) => InputDriver.Click(hwnd, x, y, btn));

    public static Command BuildDoubleClick() => BuildButton("double-click",
        "Double click at (x, y).",
        (hwnd, x, y, btn) => InputDriver.DoubleClick(hwnd, x, y, btn));

    public static Command BuildHold() => BuildButton("mouse-hold",
        "Press a mouse button at (x, y) and keep it pressed.",
        (hwnd, x, y, btn) => InputDriver.Hold(hwnd, x, y, btn));

    public static Command BuildRelease() => BuildButton("mouse-release",
        "Release a previously-held mouse button at (x, y).",
        (hwnd, x, y, btn) => InputDriver.Release(hwnd, x, y, btn));

    public static Command BuildType()
    {
        var pid = new Option<int?>("--pid", "Target process id.");
        var title = new Option<string?>("--title", "Substring of target window title.");
        var text = new Option<string>("--text", "Text to type.") { IsRequired = true };

        var cmd = new Command("type", "Send unicode key events for each character of --text.");
        cmd.AddOption(pid);
        cmd.AddOption(title);
        cmd.AddOption(text);
        cmd.SetHandler((int? pidVal, string? titleVal, string textVal) =>
        {
            var hwnd = TargetResolver.Resolve(pidVal, titleVal);
            if (hwnd == IntPtr.Zero)
            {
                CliJson.Error("could not locate target window");
                Environment.ExitCode = 1;
                return;
            }
            InputDriver.TypeText(hwnd, textVal);
            CliJson.Status(new { status = "typed", chars = textVal.Length });
        }, pid, title, text);
        return cmd;
    }

    private static Command BuildSimple(string name, string desc,
        Action<IntPtr, int, int, OsMouseButton> exec)
    {
        var pid = new Option<int?>("--pid", "Target process id.");
        var title = new Option<string?>("--title", "Substring of target window title.");
        var x = new Option<int>("--x", "Client-area X.") { IsRequired = true };
        var y = new Option<int>("--y", "Client-area Y.") { IsRequired = true };

        var cmd = new Command(name, desc);
        cmd.AddOption(pid);
        cmd.AddOption(title);
        cmd.AddOption(x);
        cmd.AddOption(y);
        cmd.SetHandler((int? pidVal, string? titleVal, int xVal, int yVal) =>
        {
            var hwnd = TargetResolver.Resolve(pidVal, titleVal);
            if (hwnd == IntPtr.Zero) { CliJson.Error("could not locate target window"); Environment.ExitCode = 1; return; }
            exec(hwnd, xVal, yVal, OsMouseButton.Left);
            CliJson.Status(new { status = name, x = xVal, y = yVal });
        }, pid, title, x, y);
        return cmd;
    }

    private static Command BuildButton(string name, string desc,
        Action<IntPtr, int, int, OsMouseButton> exec)
    {
        var pid = new Option<int?>("--pid", "Target process id.");
        var title = new Option<string?>("--title", "Substring of target window title.");
        var x = new Option<int>("--x", "Client-area X.") { IsRequired = true };
        var y = new Option<int>("--y", "Client-area Y.") { IsRequired = true };
        var button = new Option<string>("--button", () => "left", "left|right|middle");

        var cmd = new Command(name, desc);
        cmd.AddOption(pid);
        cmd.AddOption(title);
        cmd.AddOption(x);
        cmd.AddOption(y);
        cmd.AddOption(button);
        cmd.SetHandler((int? pidVal, string? titleVal, int xVal, int yVal, string btnVal) =>
        {
            var hwnd = TargetResolver.Resolve(pidVal, titleVal);
            if (hwnd == IntPtr.Zero) { CliJson.Error("could not locate target window"); Environment.ExitCode = 1; return; }
            if (!TryParseButton(btnVal, out var btn)) { CliJson.Error($"invalid --button '{btnVal}'"); Environment.ExitCode = 1; return; }
            exec(hwnd, xVal, yVal, btn);
            CliJson.Status(new { status = name, x = xVal, y = yVal, button = btn.ToString().ToLowerInvariant() });
        }, pid, title, x, y, button);
        return cmd;
    }

    private static bool TryParseButton(string raw, out OsMouseButton btn)
    {
        switch (raw.ToLowerInvariant())
        {
            case "left": btn = OsMouseButton.Left; return true;
            case "right": btn = OsMouseButton.Right; return true;
            case "middle": btn = OsMouseButton.Middle; return true;
            default: btn = OsMouseButton.Left; return false;
        }
    }
}
