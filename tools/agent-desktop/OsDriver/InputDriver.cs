// SPDX-License-Identifier: BSD-2-Clause
//
// OS-level input synthesis via SendInput. Coordinates are window-client
// space; converted to virtual-screen absolute coords for SendInput's
// MOUSEEVENTF_ABSOLUTE path.
//
// SendInput delivers events to whichever window has foreground focus, so
// every Drive() call first foregrounds the target HWND. The brief moment
// between SetForegroundWindow and SendInput is the only window in which a
// human moving the real mouse can derail the run; acceptable for v1.

using System;
using System.Runtime.Versioning;
using System.Threading;

namespace ClassicUO.Agent.Desktop.OsDriver;

internal enum OsMouseButton
{
    Left,
    Right,
    Middle,
}

[SupportedOSPlatform("windows5.0")]
internal static class InputDriver
{
    private const int FocusSettleMs = 50;
    private const int BetweenEventsMs = 20;

    public static void Focus(IntPtr hWnd)
    {
        Win32.ShowWindow(hWnd, Win32.SW_RESTORE);
        Win32.SetForegroundWindow(hWnd);
        Thread.Sleep(FocusSettleMs);
    }

    public static void MouseMove(IntPtr hWnd, int clientX, int clientY)
    {
        Focus(hWnd);
        SendMouseAbsolute(hWnd, clientX, clientY, Win32.MOUSEEVENTF_MOVE);
    }

    public static void Click(IntPtr hWnd, int clientX, int clientY, OsMouseButton button)
    {
        Focus(hWnd);
        SendMouseAbsolute(hWnd, clientX, clientY, Win32.MOUSEEVENTF_MOVE);
        Thread.Sleep(BetweenEventsMs);
        SendMouseAbsolute(hWnd, clientX, clientY, DownFlag(button));
        Thread.Sleep(BetweenEventsMs);
        SendMouseAbsolute(hWnd, clientX, clientY, UpFlag(button));
    }

    public static void DoubleClick(IntPtr hWnd, int clientX, int clientY, OsMouseButton button)
    {
        Focus(hWnd);
        SendMouseAbsolute(hWnd, clientX, clientY, Win32.MOUSEEVENTF_MOVE);
        Thread.Sleep(BetweenEventsMs);
        SendMouseAbsolute(hWnd, clientX, clientY, DownFlag(button));
        Thread.Sleep(BetweenEventsMs);
        SendMouseAbsolute(hWnd, clientX, clientY, UpFlag(button));
        Thread.Sleep(BetweenEventsMs);
        SendMouseAbsolute(hWnd, clientX, clientY, DownFlag(button));
        Thread.Sleep(BetweenEventsMs);
        SendMouseAbsolute(hWnd, clientX, clientY, UpFlag(button));
    }

    public static void Hold(IntPtr hWnd, int clientX, int clientY, OsMouseButton button)
    {
        Focus(hWnd);
        SendMouseAbsolute(hWnd, clientX, clientY, Win32.MOUSEEVENTF_MOVE);
        Thread.Sleep(BetweenEventsMs);
        SendMouseAbsolute(hWnd, clientX, clientY, DownFlag(button));
    }

    public static void Release(IntPtr hWnd, int clientX, int clientY, OsMouseButton button)
    {
        Focus(hWnd);
        SendMouseAbsolute(hWnd, clientX, clientY, Win32.MOUSEEVENTF_MOVE);
        Thread.Sleep(BetweenEventsMs);
        SendMouseAbsolute(hWnd, clientX, clientY, UpFlag(button));
    }

    public static void TypeText(IntPtr hWnd, string text)
    {
        Focus(hWnd);
        foreach (var ch in text)
        {
            SendUnicodeChar(ch, down: true);
            SendUnicodeChar(ch, down: false);
            Thread.Sleep(BetweenEventsMs);
        }
    }

    private static uint DownFlag(OsMouseButton b) => b switch
    {
        OsMouseButton.Left => Win32.MOUSEEVENTF_LEFTDOWN,
        OsMouseButton.Right => Win32.MOUSEEVENTF_RIGHTDOWN,
        OsMouseButton.Middle => Win32.MOUSEEVENTF_MIDDLEDOWN,
        _ => throw new ArgumentOutOfRangeException(nameof(b)),
    };

    private static uint UpFlag(OsMouseButton b) => b switch
    {
        OsMouseButton.Left => Win32.MOUSEEVENTF_LEFTUP,
        OsMouseButton.Right => Win32.MOUSEEVENTF_RIGHTUP,
        OsMouseButton.Middle => Win32.MOUSEEVENTF_MIDDLEUP,
        _ => throw new ArgumentOutOfRangeException(nameof(b)),
    };

    private static void SendMouseAbsolute(IntPtr hWnd, int clientX, int clientY, uint flags)
    {
        var pt = new Win32.POINT { X = clientX, Y = clientY };
        Win32.ClientToScreen(hWnd, ref pt);
        var (vx, vy) = ToVirtualDeskCoords(pt.X, pt.Y);
        var input = new Win32.INPUT
        {
            type = Win32.INPUT_MOUSE,
            u = new Win32.INPUTUNION
            {
                mi = new Win32.MOUSEINPUT
                {
                    dx = vx,
                    dy = vy,
                    mouseData = 0,
                    dwFlags = flags | Win32.MOUSEEVENTF_ABSOLUTE | Win32.MOUSEEVENTF_VIRTUALDESK,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                },
            },
        };
        Win32.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<Win32.INPUT>());
    }

    private static (int Vx, int Vy) ToVirtualDeskCoords(int screenX, int screenY)
    {
        var vsX = Win32.GetSystemMetrics(Win32.SM_XVIRTUALSCREEN);
        var vsY = Win32.GetSystemMetrics(Win32.SM_YVIRTUALSCREEN);
        var vsW = Win32.GetSystemMetrics(Win32.SM_CXVIRTUALSCREEN);
        var vsH = Win32.GetSystemMetrics(Win32.SM_CYVIRTUALSCREEN);
        if (vsW <= 0 || vsH <= 0) return (0, 0);
        var nx = (int)Math.Round((double)(screenX - vsX) * 65535.0 / (vsW - 1));
        var ny = (int)Math.Round((double)(screenY - vsY) * 65535.0 / (vsH - 1));
        return (nx, ny);
    }

    private static void SendUnicodeChar(char ch, bool down)
    {
        var input = new Win32.INPUT
        {
            type = Win32.INPUT_KEYBOARD,
            u = new Win32.INPUTUNION
            {
                ki = new Win32.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = Win32.KEYEVENTF_UNICODE | (down ? 0u : Win32.KEYEVENTF_KEYUP),
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                },
            },
        };
        Win32.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<Win32.INPUT>());
    }
}
