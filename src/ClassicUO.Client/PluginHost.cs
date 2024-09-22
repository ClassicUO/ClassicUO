using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Network;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace ClassicUO
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HostBindings
    {
        public IntPtr InitializeFn;
        public IntPtr LoadPluginFn;
        public IntPtr TickFn;
        public IntPtr ClosingFn;
        public IntPtr FocusGainedFn;
        public IntPtr FocusLostFn;
        public IntPtr ConnectedFn;
        public IntPtr DisconnectedFn;
        public IntPtr /*delegate*<int, int, bool, bool>*/ HotkeyFn;
        public IntPtr /*delegate*<int, int, void>*/ MouseFn;
        public IntPtr /*delegate*<IntPtr, ref int, void>*/ CmdListFn;
        public IntPtr /*delegate*<IntPtr, int>*/ SdlEventFn;
        public IntPtr /*delegate*<int, int, int, void>*/ UpdatePlayerPosFn;
        public IntPtr PacketInFn;
        public IntPtr PacketOutFn;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct ClientBindings
    {
        public IntPtr /*delegate*<IntPtr, ref int, bool>*/ PluginRecvFn;
        public IntPtr /*delegate*<IntPtr, ref int, bool>*/ PluginSendFn;
        public IntPtr /*delegate*<int, short>*/ PacketLengthFn;
        public IntPtr /*delegate*<int, void>*/ CastSpellFn;
        public IntPtr /*delegate*<IntPtr>*/ SetWindowTitleFn;
        public IntPtr /*delegate*<int, IntPtr, IntPtr, bool, bool>*/ GetClilocFn;
        public IntPtr /*delegate*<int, bool, bool>*/ RequestMoveFn;
        public IntPtr /*delegate*<ref int, ref int, ref int, bool>*/ GetPlayerPositionFn;
        public IntPtr ReflectionCmdFn;
    }

    internal unsafe sealed class UnmanagedAssistantHost : IPluginHost
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void dOnPluginBindCuoFunctions(IntPtr exportedFuncs);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnPluginBindCuoFunctions _initialize;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void dOnPluginLoad(IntPtr pluginPathPtr, uint clientVersion, IntPtr assetsPathPtr, IntPtr sdlWindow);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnPluginLoad _loadPlugin;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void dOnPluginTick();
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnPluginTick _tick;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void dOnPluginClose();
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnPluginClose _close;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void dOnPluginConnection();
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnPluginConnection _connected, _disconnected;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool dOnPluginPacketInOut(IntPtr data, ref int length);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnPluginPacketInOut _packetIn, _packetOut;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool dOnHotkey(int key, int mod, bool pressed);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnHotkey _hotkey;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void dOnMouse(int button, int wheel);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnMouse _mouse;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool dOnUpdatePlayerPosition(int x, int y, int z);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnUpdatePlayerPosition _updatePlayerPos;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void dOnPluginFocusWindow();
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnPluginFocusWindow _focusGained, _focusLost;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int dOnPluginSdlEvent(IntPtr ev);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnPluginSdlEvent _sdlEvent;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void dOnPluginCommandList(out IntPtr list, out int len);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnPluginCommandList _cmdList;


        public UnmanagedAssistantHost(HostBindings* setup)
        {
            _initialize = Marshal.GetDelegateForFunctionPointer<dOnPluginBindCuoFunctions>(setup->InitializeFn);
            _loadPlugin = Marshal.GetDelegateForFunctionPointer<dOnPluginLoad>(setup->LoadPluginFn);
            _tick = Marshal.GetDelegateForFunctionPointer<dOnPluginTick>(setup->TickFn);
            _close = Marshal.GetDelegateForFunctionPointer<dOnPluginClose>(setup->ClosingFn);
            _packetIn = Marshal.GetDelegateForFunctionPointer<dOnPluginPacketInOut>(setup->PacketInFn);
            _packetOut = Marshal.GetDelegateForFunctionPointer<dOnPluginPacketInOut>(setup->PacketOutFn);
            _hotkey = Marshal.GetDelegateForFunctionPointer<dOnHotkey>(setup->HotkeyFn);
            _mouse = Marshal.GetDelegateForFunctionPointer<dOnMouse>(setup->MouseFn);
            _updatePlayerPos = Marshal.GetDelegateForFunctionPointer<dOnUpdatePlayerPosition>(setup->UpdatePlayerPosFn);
            _focusGained = Marshal.GetDelegateForFunctionPointer<dOnPluginFocusWindow>(setup->FocusGainedFn);
            _focusLost = Marshal.GetDelegateForFunctionPointer<dOnPluginFocusWindow>(setup->FocusLostFn);
            _sdlEvent = Marshal.GetDelegateForFunctionPointer<dOnPluginSdlEvent>(setup->SdlEventFn);
            _connected = Marshal.GetDelegateForFunctionPointer<dOnPluginConnection>(setup->ConnectedFn);
            _disconnected = Marshal.GetDelegateForFunctionPointer<dOnPluginConnection>(setup->DisconnectedFn);
            _cmdList = Marshal.GetDelegateForFunctionPointer<dOnPluginCommandList>(setup->CmdListFn);
        }

        public Dictionary<IntPtr, GraphicsResource> GfxResources { get; } = new Dictionary<nint, GraphicsResource>();

        public void Closing()
        {
            _close?.Invoke();
        }

        public void GetCommandList(out IntPtr listPtr, out int listCount)
        {
            listPtr = IntPtr.Zero;
            listCount = 0;
            _cmdList?.Invoke(out listPtr, out listCount);
        }

        public void Connected()
        {
            _connected?.Invoke();
        }

        public void Disconnected()
        {
            _disconnected?.Invoke();
        }

        public void FocusGained()
        {
            _focusGained?.Invoke();
        }

        public void FocusLost()
        {
            _focusLost?.Invoke();
        }

        public bool Hotkey(int key, int mod, bool pressed)
        {
            return _hotkey == null || _hotkey(key, mod, pressed);
        }

        public void Mouse(int button, int wheel)
        {
            _mouse?.Invoke(button, wheel);
        }


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void dCastSpell(int index);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dCastSpell _castSpell = GameActions.CastSpell;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr dGetCliloc(int cliloc, IntPtr args, bool capitalize);
        [MarshalAs(UnmanagedType.FunctionPtr)]
#pragma warning disable CS0169
        private readonly dGetCliloc _getCliloc;
#pragma warning restore CS0169

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate short dGetPacketLength(int id);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dGetPacketLength _packetLength = getPacketLength;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool dGetPlayerPosition(out int x, out int y, out int z);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dGetPlayerPosition _getPlayerPosition = Plugin.GetPlayerPosition;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool dRequestMove(int dir, bool run);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dRequestMove _requestMove = Plugin.RequestMove;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool dPacketRecvSend(IntPtr data, ref int length);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dPacketRecvSend _sendToClient = Plugin.OnPluginRecv_new;
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dPacketRecvSend _sendToServer = Plugin.OnPluginSend_new;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void dSetWindowTitle(IntPtr textPtr);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dSetWindowTitle _setWindowTitle = setWindowTitle;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr dOnPluginReflectionCommand(IntPtr cmdPtr);
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly dOnPluginReflectionCommand _reflectionCmd = reflectionCmd;


        static short getPacketLength(int id)
        {
            return NetClient.Socket.PacketsTable.GetPacketLength(id);
        }

        static void setWindowTitle(IntPtr ptr)
        {
            var title = SDL2.SDL.UTF8_ToManaged(ptr);
            Client.Game.SetWindowTitle(title);
        }

        static IntPtr reflectionCmd(IntPtr cmd)
        {
            Console.WriteLine("called reflection cmd {0}", cmd);

            switch (Unsafe.AsRef<int>(cmd.ToPointer()))
            {
#pragma warning disable CS0618
                case 1:
                    GameActions.UsePrimaryAbility();
                    break;
                case 2:
                    GameActions.UseSecondaryAbility();
#pragma warning restore CS0618
                    break;
                case 3:
                    var subCmd = Unsafe.AsRef<(int, sbyte)>(cmd.ToPointer());
                    var res = Client.Game.UO?.World?.Player?.Pathfinder?.AutoWalking ?? false;

                    switch (subCmd.Item2)
                    {
                        case -1: return (IntPtr)Unsafe.AsPointer(ref res);
                        case 0:
                            Client.Game.UO.World.Player.Pathfinder.AutoWalking = false;
                            break;
                        default:
                            Client.Game.UO.World.Player.Pathfinder.AutoWalking = true;
                            break;
                    }

                    break;

            }

            return IntPtr.Zero;
        }

        public void Initialize()
        {
            if (_initialize == null)
                return;

            var mem = NativeMemory.AllocZeroed((nuint)sizeof(ClientBindings));
            ref var cuoHost = ref Unsafe.AsRef<ClientBindings>(mem);
            cuoHost.PacketLengthFn = Marshal.GetFunctionPointerForDelegate(_packetLength);
            cuoHost.CastSpellFn = Marshal.GetFunctionPointerForDelegate(_castSpell);
            cuoHost.SetWindowTitleFn = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);
            cuoHost.PluginRecvFn = Marshal.GetFunctionPointerForDelegate(_sendToClient);
            cuoHost.PluginSendFn = Marshal.GetFunctionPointerForDelegate(_sendToServer);
            cuoHost.RequestMoveFn = Marshal.GetFunctionPointerForDelegate(_requestMove);
            cuoHost.GetPlayerPositionFn = Marshal.GetFunctionPointerForDelegate(_getPlayerPosition);
            cuoHost.ReflectionCmdFn = Marshal.GetFunctionPointerForDelegate(_reflectionCmd);

            _initialize((IntPtr)mem);
        }

        public void LoadPlugin(string pluginPath)
        {
            if (_loadPlugin == null)
                return;

            var pluginPathPtr = Marshal.StringToHGlobalAnsi(pluginPath);
            var uoAssetsPtr = Marshal.StringToHGlobalAnsi(Settings.GlobalSettings.UltimaOnlineDirectory);

            _loadPlugin
            (
                pluginPathPtr,
                (uint)Client.Game.UO.Version,
                uoAssetsPtr,
                Client.Game.Window.Handle
            );

            if (pluginPathPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(pluginPathPtr);

            if (uoAssetsPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(uoAssetsPtr);
        }

        public bool PacketIn(ArraySegment<byte> buffer)
        {
            if (_packetIn == null || buffer.Array == null || buffer.Count <= 0)
                return true;

            var len = buffer.Count;
            fixed (byte* ptr = buffer.Array)
                return _packetIn((IntPtr)ptr, ref len);
        }

        public bool PacketOut(Span<byte> buffer)
        {
            if (_packetOut == null || buffer.IsEmpty)
                return true;

            var len = buffer.Length;
            fixed (byte* ptr = buffer)
                return _packetOut((IntPtr)ptr, ref len);
        }

        public unsafe int SdlEvent(SDL.SDL_Event* ev)
        {
            return _sdlEvent != null ? _sdlEvent((IntPtr)ev) : 0;
        }

        public void Tick()
        {
            _tick?.Invoke();
        }

        public void UpdatePlayerPosition(int x, int y, int z)
        {
            _updatePlayerPos?.Invoke(x, y, z);
        }
    }

    interface IPluginHost
    {
        public Dictionary<IntPtr, GraphicsResource> GfxResources { get; }

        public void Initialize();
        public void LoadPlugin(string pluginPath);
        public void Tick();
        public void Closing();
        public void FocusGained();
        public void FocusLost();
        public void Connected();
        public void Disconnected();
        public bool Hotkey(int key, int mod, bool pressed);
        public void Mouse(int button, int wheel);
        public void GetCommandList(out IntPtr listPtr, out int listCount);
        public unsafe int SdlEvent(SDL2.SDL.SDL_Event* ev);
        public void UpdatePlayerPosition(int x, int y, int z);
        public bool PacketIn(ArraySegment<byte> buffer);
        public bool PacketOut(Span<byte> buffer);
    }
}
