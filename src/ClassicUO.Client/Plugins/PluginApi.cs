using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using SDL2;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.Plugins
{
    enum PluginEventType
    {
        Mouse,
        Wheel,
        Keyboard,
        InputText,
        Window,
        Quit,
        Connect,
        Disconnect,
        FrameTick,
        OnPacket,
    }

    enum PluginWindowEventType
    {
        FocusGain,
        FocusLost,
        Maximize,
        Minimize,
        SizeChanged,
        PositionChanged
    }

    enum PluginAssetsType
    {
        Art,
        Gump,
        Cliloc,
        Tiledata
    }


    [StructLayout(LayoutKind.Explicit)]
    unsafe struct PluginEvent
    {
        [FieldOffset(0)] public PluginEventType EventType;
        [FieldOffset(0)] public PluginMouseEvent Mouse;
        [FieldOffset(0)] public PluginWheelEvent Wheel;
        [FieldOffset(0)] public PluginKeyboardEvent Keyboard;
        [FieldOffset(0)] public PluginInputTextEvent InputText;
        [FieldOffset(0)] public PluginWindowEvent Window;
        [FieldOffset(0)] public PluginQuitEvent Quit;
        [FieldOffset(0)] public PluginConnectEvent Connect;
        [FieldOffset(0)] public PluginDisconnectEvent Disconnect;
        [FieldOffset(0)] public PluginFrameTickEvent FrameTick;
        [FieldOffset(0)] public PluginOnPacketEvent OnPacket;
    }


    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginMouseEvent
    {
        private PluginEventType EventType;

        public MouseButtonType Button;
        public int X, Y;
        public bool IsPressed;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginWheelEvent
    {
        private PluginEventType EventType;

        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginKeyboardEvent
    {
        private PluginEventType EventType;

        public int Keycode;
        public int Mods;
        public bool IsDown;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginInputTextEvent
    {
        private PluginEventType EventType;

        public char InputChar;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginWindowEvent
    {
        private PluginEventType EventType;

        public PluginWindowEventType WindowEventType;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginQuitEvent
    {
        private PluginEventType EventType;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginConnectEvent
    {
        private PluginEventType EventType;
        public fixed char Server[64];
        public ushort Port;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginDisconnectEvent
    {
        private PluginEventType EventType;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginFrameTickEvent
    {
        private PluginEventType EventType;
        public uint Ticks;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginOnPacketEvent
    {
        private PluginEventType EventType;
        public nint PacketPtr;
        public int Size;
        public bool ClientToServer;
    }



    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginStruct
    {
        public int ApiVersion;
        public nint SdlWindow;
        public uint ClientVersion;
        public nint AssetsPath;
        public nint PluginPath;
        public delegate* unmanaged[Cdecl]<nint, int, int> PluginToClientPacket, PluginToServerPacket;
    }

    unsafe internal class PluginApi
    {
        private delegate* unmanaged[Cdecl]<nint, nint, int> _onEvent;

        public void Load
        (
            Microsoft.Xna.Framework.Game game,
            DirectoryInfo assetsPath,
            ClientVersion version, 
            FileInfo pluginFile, 
            string installFuncName
        )
        {
            var s = new PluginStruct();
            s.ApiVersion = 1;
            s.SdlWindow = game.Window.Handle;
            s.ClientVersion = (uint)version;
            s.AssetsPath = (nint)Unsafe.AsPointer(ref MemoryMarshal.AsRef<byte>(encodeToUtf8(assetsPath.FullName)));
            s.PluginPath = (nint)Unsafe.AsPointer(ref MemoryMarshal.AsRef<byte>(encodeToUtf8(Path.GetDirectoryName(pluginFile.FullName))));
            s.PluginToClientPacket = &pluginToClient;
            s.PluginToServerPacket = &pluginToServer;
            
            var libPtr = Native.LoadLibrary(pluginFile.FullName);
            if (libPtr == 0)
            {
                Console.WriteLine("plugi not found");
                return;
            }

            var installPtr = Native.GetProcessAddress(libPtr, installFuncName);
            if (installPtr == 0)
            {
                Console.WriteLine("function '{0}' not found", installFuncName);
                return;
            }

            _onEvent = (delegate* unmanaged [Cdecl] <nint, nint, int>) ((delegate* unmanaged[Cdecl]<PluginStruct*, nint>)installPtr)(&s);

            if (_onEvent == null)
            {
                Log.Warn("plugin didn't set the OnEvent function!");
            }

            [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
            static int pluginToClient(nint ptr, int size)
            {
                var span = new Span<byte>(ptr.ToPointer(), size);
                Console.WriteLine("plugin to client -> {0:X2} - {1}", span[0], span.Length);
                PacketHandlers.Handler.Append(span, true);
                return 1;
            }

            [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
            static int pluginToServer(nint ptr, int size)
            {
                var span = new Span<byte>(ptr.ToPointer(), size);
                Console.WriteLine("plugin to server -> {0:X2} - {1}", span[0], span.Length);
                NetClient.Socket.Send(span, true);
                return 1;
            }

            static Span<byte> encodeToUtf8(ReadOnlySpan<char> str)
            {
                var count = Encoding.UTF8.GetByteCount(str);
                Span<byte> span = new byte[count];
                fixed (char* ptr = str)
                fixed (byte* ptr2 = span)
                    Encoding.UTF8.GetBytes(ptr, str.Length, ptr2, count);
                return span;
            }
        }

        public int SendMouseEvent(MouseButtonType button, int x, int y, bool pressed)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Mouse;
            ev.Mouse.Button = button;
            ev.Mouse.X = x;
            ev.Mouse.Y = y;
            ev.Mouse.IsPressed = pressed;

            return SendEvent(&ev);
        }

        public int SendWheelEvent(int x, int y)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Wheel;
            ev.Wheel.X = x;
            ev.Wheel.Y = y;

            return SendEvent(&ev);
        }

        public int SendKeyboardEvent(int keycode, int mods, bool pressed)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Keyboard;
            ev.Keyboard.Keycode = keycode;
            ev.Keyboard.Mods = mods;
            ev.Keyboard.IsDown = pressed;

            return SendEvent(&ev);
        }

        public int SendInputTextEvent(char c)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.InputText;
            ev.InputText.InputChar = c;

            return SendEvent(&ev);
        }

        public int SendWindowEvent(PluginWindowEventType wndEvent)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Window;
            ev.Window.WindowEventType = wndEvent;

            return SendEvent(&ev);
        }

        public int SendQuitEvent()
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Quit;

            return SendEvent(&ev);
        }

        public int SendConnectEvent(string server, ushort port)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Connect;

            var count = Encoding.UTF8.GetByteCount(server);

            fixed (char* ptr = server)
                Encoding.UTF8.GetBytes(ptr, server.Length, (byte*)ev.Connect.Server, count);

            ev.Connect.Port = port;

            return SendEvent(&ev);
        }

        public int SendDisconnectEvent()
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Disconnect;

            return SendEvent(&ev);
        }
         
        public int SendFrameTick(uint ticks)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.FrameTick;
            ev.FrameTick.Ticks = ticks;

            return SendEvent(&ev);
        }

        public int SendClientToServerPacketEvent(ReadOnlySpan<byte> message)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.OnPacket;
            ev.OnPacket.ClientToServer = true;

            fixed (byte* ptr = message)
            {
                ev.OnPacket.PacketPtr = (nint)ptr;
                ev.OnPacket.Size = message.Length;

                return SendEvent(&ev);
            }
        }

        public int SendServerToClientPacketEvent(ReadOnlySpan<byte> message)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.OnPacket;
            ev.OnPacket.ClientToServer = false;

            fixed (byte* ptr = message)
            {
                ev.OnPacket.PacketPtr = (nint)ptr;
                ev.OnPacket.Size = message.Length;

                return SendEvent(&ev);
            }
        }

        private int SendEvent(PluginEvent* ev)
            => _onEvent == null ? 1 : _onEvent((nint)ev, 0);
    }
}
