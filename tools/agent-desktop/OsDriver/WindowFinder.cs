// SPDX-License-Identifier: BSD-2-Clause
//
// Resolve the target game window HWND from a process id or a window-title
// substring. Used by every OS-driver command before it can send input or
// capture pixels.

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace ClassicUO.Agent.Desktop.OsDriver;

[SupportedOSPlatform("windows5.0")]
internal static class WindowFinder
{
    public static IntPtr FindByPid(int pid)
    {
        IntPtr found = IntPtr.Zero;
        Win32.EnumWindows((hWnd, _) =>
        {
            if (!Win32.IsWindowVisible(hWnd)) return true;
            Win32.GetWindowThreadProcessId(hWnd, out var wpid);
            if ((int)wpid != pid) return true;

            // Prefer windows with non-empty title (skip tool/hidden child windows).
            var buf = new char[512];
            var len = Win32.GetWindowTextW(hWnd, buf, buf.Length);
            if (len <= 0) return true;

            found = hWnd;
            return false;
        }, IntPtr.Zero);
        return found;
    }

    public static IntPtr FindByTitleSubstring(string substring)
    {
        IntPtr found = IntPtr.Zero;
        var needle = substring;
        Win32.EnumWindows((hWnd, _) =>
        {
            if (!Win32.IsWindowVisible(hWnd)) return true;
            var buf = new char[512];
            var len = Win32.GetWindowTextW(hWnd, buf, buf.Length);
            if (len <= 0) return true;
            var title = new string(buf, 0, len);
            if (title.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                found = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    public static List<(IntPtr Hwnd, string Title, int Pid)> EnumerateVisible()
    {
        var result = new List<(IntPtr, string, int)>();
        Win32.EnumWindows((hWnd, _) =>
        {
            if (!Win32.IsWindowVisible(hWnd)) return true;
            var buf = new char[512];
            var len = Win32.GetWindowTextW(hWnd, buf, buf.Length);
            if (len <= 0) return true;
            Win32.GetWindowThreadProcessId(hWnd, out var wpid);
            result.Add((hWnd, new string(buf, 0, len), (int)wpid));
            return true;
        }, IntPtr.Zero);
        return result;
    }

    public static (int X, int Y) ClientOriginScreen(IntPtr hWnd)
    {
        var p = new Win32.POINT();
        Win32.ClientToScreen(hWnd, ref p);
        return (p.X, p.Y);
    }

    public static (int W, int H) ClientSize(IntPtr hWnd)
    {
        Win32.GetClientRect(hWnd, out var r);
        return (r.Width, r.Height);
    }
}
