#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;

using SDL2;

namespace ClassicUO.Utility.Platforms
{
    internal static class UoAssist
    {
        private static CustomWindow _customWindow;

        public static void Start()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Log.Warn( "This OS does not support the UOAssist API");

                return;
            }

            _customWindow = new CustomWindow("UOASSIST-TP-MSG-WND");
        }

        public static void SignalMapChanged(int newMap)
        {
            _customWindow?.SignalMapChanged(newMap);
        }

        public static void SignalMessage(string msg)
        {
            _customWindow?.SignalMessage(msg);
        }

        public static void SignalHits()
        {
            _customWindow?.SignalHitsUpdate();
        }

        public static void SignalStamina()
        {
            _customWindow?.SignalStaminaUpdate();
        }

        public static void SignalMana()
        {
            _customWindow?.SignalManaUpdate();
        }

        public static void SignalAddMulti(ushort graphic, ushort x, ushort y)
        {
            _customWindow?.SignalAddMulti(graphic, x, y);
        }

        private class CustomWindow : IDisposable
        {
            enum UOAMessage
            {
                First = REGISTER,

                //in comming:
                REGISTER = WM_USER + 200,
                COUNT_RESOURCES,
                GET_COORDS,
                GET_SKILL,
                GET_STAT,
                SET_MACRO,
                PLAY_MACRO,
                DISPLAY_TEXT,
                REQUEST_MULTIS,
                ADD_CMD,
                GET_UID,
                GET_SHARDNAME,
                ADD_USER_2_PARTY,
                GET_UO_HWND,
                GET_POISON,
                SET_SKILL_LOCK,
                GET_ACCT_ID,

                //out going:
                RES_COUNT_DONE = WM_USER + 301,
                CAST_SPELL,
                LOGIN,
                MAGERY_LEVEL,
                INT_STATUS,
                SKILL_LEVEL,
                MACRO_DONE,
                LOGOUT,
                STR_STATUS,
                DEX_STATUS,
                ADD_MULTI,
                REM_MULTI,
                MAP_INFO,
                POWERHOUR,

                Last = POWERHOUR
            }

            private const int ERROR_CLASS_ALREADY_EXISTS = 1410;
            public const int WM_USER = 0x400;

            private readonly Dictionary<int, WndRegEnt> _wndRegs = new Dictionary<int, WndRegEnt>();



            private readonly WndProc m_wnd_proc_delegate;

            private uint _cmdID = WM_USER + 401;

            private bool m_disposed;
            private IntPtr m_hwnd;

            public CustomWindow(string class_name)
            {
                SDL.SDL_SysWMinfo info = new SDL.SDL_SysWMinfo();
                SDL.SDL_VERSION(out info.version);
                SDL.SDL_GetWindowWMInfo(SDL.SDL_GL_GetCurrentWindow(), ref info);

                IntPtr hwnd = IntPtr.Zero;

                if (info.subsystem == SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS)
                    hwnd = info.info.win.window;

                if (class_name == null) throw new Exception("class_name is null");
                if (class_name == string.Empty) throw new Exception("class_name is empty");

                m_wnd_proc_delegate = CustomWndProc;

                // Create WNDCLASS
                WNDCLASS wind_class = new WNDCLASS
                {
                    hInstance = hwnd,
                    lpszClassName = class_name,
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(m_wnd_proc_delegate)
                };

                ushort class_atom = RegisterClassW(ref wind_class);

                int last_error = Marshal.GetLastWin32Error();

                if (class_atom == 0 && last_error != ERROR_CLASS_ALREADY_EXISTS) throw new Exception("Could not register window class");


                // Create window
                m_hwnd = CreateWindowExW(
                                         0,
                                         class_name,
                                         class_name,
                                         0,
                                         0,
                                         0,
                                         0,
                                         0,
                                         IntPtr.Zero,
                                         IntPtr.Zero,
                                         hwnd,
                                         IntPtr.Zero
                                        );

                if (m_hwnd != IntPtr.Zero)
                    ShowWindow(m_hwnd, 0);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            [DllImport("user32.dll")]
            internal static extern uint PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll")]
            internal static extern ushort GlobalAddAtom(string str);

            [DllImport("kernel32.dll")]
            internal static extern ushort GlobalDeleteAtom(ushort atom);

            [DllImport("kernel32.dll")]
            internal static extern uint GlobalGetAtomName(ushort atom, StringBuilder buff, int bufLen);

            [DllImport("user32.dll")]
            private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern ushort RegisterClassW(
                [In] ref WNDCLASS lpWndClass
            );

            [DllImport("user32.dll", SetLastError = true)]
            private static extern IntPtr CreateWindowExW(
                uint dwExStyle,
                [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
                [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
                uint dwStyle,
                int x,
                int y,
                int nWidth,
                int nHeight,
                IntPtr hWndParent,
                IntPtr hMenu,
                IntPtr hInstance,
                IntPtr lpParam
            );

            [DllImport("user32.dll", SetLastError = true)]
            private static extern IntPtr DefWindowProcW(
                IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam
            );

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool DestroyWindow(
                IntPtr hWnd
            );

            private void Dispose(bool disposing)
            {
                if (!m_disposed)
                {
                    if (disposing)
                    {
                        // Dispose managed resources
                    }

                    // Dispose unmanaged resources
                    if (m_hwnd != IntPtr.Zero)
                    {
                        DestroyWindow(m_hwnd);
                        m_hwnd = IntPtr.Zero;
                    }
                }
            }

            private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
            {
                if (msg >= WM_USER + 200 && msg < WM_USER + 315)
                    return (IntPtr) OnUOAssistMessage(msg, wParam.ToInt32(), lParam.ToInt32());

                return DefWindowProcW(hWnd, msg, wParam, lParam);
            }


            private int OnUOAssistMessage(uint msg, int wParam, int lParam)
            {
                switch ((UOAMessage) msg)
                {
                    case UOAMessage.REGISTER:

                        if (_wndRegs.ContainsKey(wParam))
                        {
                            _wndRegs.Remove(wParam);

                            return 2;
                        }

                        _wndRegs.Add(wParam, new WndRegEnt(wParam, lParam == 1 ? 1 : 0));

                        if (lParam == 1 && World.InGame)
                        {
                            foreach (Item item in World.Items.Where(s => s.IsMulti))
                                PostMessage((IntPtr) wParam, (uint) UOAMessage.ADD_MULTI, (IntPtr) ((item.X & 0xFFFF) | ((item.Y & 0xFFFF) << 16)), (IntPtr) item.Graphic);
                        }

                        return 1;
                    case UOAMessage.COUNT_RESOURCES: break;

                    case UOAMessage.GET_COORDS:

                        if (World.Player != null)
                            return (World.Player.X & 0xFFFF) | ((World.Player.Y & 0xFFFF) << 16);

                        break;
                    case UOAMessage.GET_SKILL: break;

                    case UOAMessage.GET_STAT:

                        if (World.Player == null || wParam < 0 || wParam > 5)
                            return 0;

                        switch (wParam)
                        {
                            case 0: return World.Player.Strength;
                            case 1: return World.Player.Intelligence;
                            case 2: return World.Player.Dexterity;
                            case 3: return World.Player.Weight;
                            case 4: return World.Player.HitsMax;
                            case 5: return (int) World.Player.TithingPoints;
                        }

                        return 0;
                    case UOAMessage.SET_MACRO: break;
                    case UOAMessage.PLAY_MACRO: break;

                    case UOAMessage.DISPLAY_TEXT:

                        if (World.Player != null)
                        {
                            ushort hue = (ushort) (wParam & 0xFFFF);
                            StringBuilder sb = new StringBuilder(256);

                            if (GlobalGetAtomName((ushort) lParam, sb, 256) == 0)
                                return 0;

                            if ((wParam & 0x00010000) != 0)
                                Chat.HandleMessage(null, sb.ToString(), "System", hue, MessageType.Regular, 3, true);
                            else
                                World.Player.AddMessage(MessageType.Regular, sb.ToString(), 3, hue, true);

                            return 1;
                        }

                        break;

                    case UOAMessage.REQUEST_MULTIS:

                        return World.Player != null ? 1 : 0;

                    case UOAMessage.ADD_CMD:

                    {
                        var sb = new StringBuilder(256);

                        if (GlobalGetAtomName((ushort) lParam, sb, 256) == 0)
                            return 0;

                        if (wParam == 0)
                        {
                            CommandManager.UnRegister(sb.ToString());

                            return 0;
                        }

                        new WndCmd(_cmdID, (IntPtr) wParam, sb.ToString());

                        return (int) _cmdID++;
                    }

                    case UOAMessage.GET_UID: return World.Player != null ? (int) World.Player.Serial : 0;
                    case UOAMessage.GET_SHARDNAME: break;
                    case UOAMessage.ADD_USER_2_PARTY: break;

                    case UOAMessage.GET_UO_HWND:
                        SDL.SDL_SysWMinfo info = new SDL.SDL_SysWMinfo();
                        SDL.SDL_VERSION(out info.version);
                        SDL.SDL_GetWindowWMInfo(SDL.SDL_GL_GetCurrentWindow(), ref info);

                        IntPtr hwnd = IntPtr.Zero;

                        if (info.subsystem == SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS)
                            hwnd = info.info.win.window;

                        return (int) hwnd;

                    case UOAMessage.GET_POISON:

                        return World.Player != null && World.Player.IsPoisoned ? 1 : 0;
                    case UOAMessage.SET_SKILL_LOCK: break;
                    case UOAMessage.GET_ACCT_ID: break;
                    case UOAMessage.RES_COUNT_DONE: break;
                    case UOAMessage.CAST_SPELL: break;
                    case UOAMessage.LOGIN: break;
                    case UOAMessage.MAGERY_LEVEL: break;
                    case UOAMessage.INT_STATUS: break;
                    case UOAMessage.SKILL_LEVEL: break;
                    case UOAMessage.MACRO_DONE: break;
                    case UOAMessage.LOGOUT: break;
                    case UOAMessage.STR_STATUS: break;
                    case UOAMessage.DEX_STATUS: break;
                    case UOAMessage.ADD_MULTI: break;
                    case UOAMessage.REM_MULTI: break;
                    case UOAMessage.MAP_INFO: break;
                    case UOAMessage.POWERHOUR: break;
                }


                return 0;
            }


            public void SignalMapChanged(int map)
            {
                PostMessage((uint) UOAMessage.MAP_INFO, (IntPtr) map, IntPtr.Zero);
            }

            public void SignalMessage(string str)
            {
                PostMessage(1425, (IntPtr) GlobalAddAtom(str), IntPtr.Zero);
            }

            public void SignalHitsUpdate()
            {
                if (World.Player != null)
                    PostMessage((uint) UOAMessage.STR_STATUS, (IntPtr) World.Player.HitsMax, (IntPtr) World.Player.Hits);
            }

            public void SignalStaminaUpdate()
            {
                if (World.Player != null)
                    PostMessage((uint) UOAMessage.DEX_STATUS, (IntPtr) World.Player.HitsMax, (IntPtr) World.Player.Hits);
            }

            public void SignalManaUpdate()
            {
                if (World.Player != null)
                    PostMessage((uint) UOAMessage.INT_STATUS, (IntPtr) World.Player.HitsMax, (IntPtr) World.Player.Hits);
            }

            public void SignalAddMulti(ushort graphic, ushort x, ushort y)
            {
                IntPtr pos = (IntPtr) ((x & 0xFFFF) | ((y & 0xFFFF) << 16));

                if (pos == IntPtr.Zero)
                    return;

                foreach (KeyValuePair<int, WndRegEnt> k in _wndRegs)
                {
                    if (k.Value.Type == 1)
                        PostMessage((IntPtr) k.Value.Handle, (uint) UOAMessage.ADD_MULTI, pos, (IntPtr) graphic);
                }
            }

            private void PostMessage(uint msg, IntPtr wParam, IntPtr lParam)
            {
                List<int> toremove = null;

                foreach (KeyValuePair<int, WndRegEnt> k in _wndRegs)
                {
                    if (PostMessage((IntPtr) k.Key, msg, wParam, lParam) == 0)
                    {
                        if (toremove == null)
                            toremove = new List<int>();
                        toremove.Add(k.Key);
                    }
                }

                if (toremove != null)
                {
                    foreach (int i in toremove)
                        _wndRegs.Remove(i);
                }
            }

            private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            [StructLayout(
                LayoutKind.Sequential,
                CharSet = CharSet.Unicode
            )]
            private struct WNDCLASS
            {
                public readonly uint style;
                public IntPtr lpfnWndProc;
                public readonly int cbClsExtra;
                public readonly int cbWndExtra;
                public IntPtr hInstance;
                public readonly IntPtr hIcon;
                public readonly IntPtr hCursor;
                public readonly IntPtr hbrBackground;
                [MarshalAs(UnmanagedType.LPWStr)] public readonly string lpszMenuName;
                [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
            }

            private class WndRegEnt
            {
                public WndRegEnt(int hWnd, int type)
                {
                    Handle = hWnd;
                    Type = type;
                }

                public int Handle { get; }
                public int Type { get; }
            }

            private class WndCmd
            {
                private readonly IntPtr hWnd;
                private readonly uint Msg;

                public WndCmd(uint msg, IntPtr handle, string cmd)
                {
                    Msg = msg;
                    hWnd = handle;
                    CommandManager.Register(cmd, MyCallback);
                }

                private void MyCallback(string[] args)
                {
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i != 0)
                            sb.Append(' ');
                        sb.Append(args[i]);
                    }

                    string str = sb.ToString();
                    ushort atom = 0;

                    if (str != null && str.Length > 0)
                        atom = GlobalAddAtom(str);
                    PostMessage(hWnd, Msg, (IntPtr) atom, IntPtr.Zero);
                }
            }
        }
    }
}