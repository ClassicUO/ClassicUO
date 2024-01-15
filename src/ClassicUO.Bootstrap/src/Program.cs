using ClassicUO;
using CUO_API;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


Global.Host.Run(args);
Console.WriteLine("finished");


static class Global
{
    // NOTE: this must be static otherwise GC does some weird stuff on delegates ¯\_(ツ)_/¯
    public static readonly ClassicUOHost Host = new ClassicUOHost();

}

sealed class ClassicUOHost
{
    private readonly List<Plugin> _plugins = new List<Plugin>();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void dOnInitializeCuo(IntPtr* argv, int argc, IntPtr hostSetupPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginInitialize(IntPtr exportedFuncs, uint clientVersion, IntPtr pluginPathPtr, IntPtr assetsPathPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginTick();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginClose();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool dOnPluginPacketInOut(IntPtr data, ref int length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool dOnHotkey(int key, int mod, bool pressed);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnMouse(int button, int wheel);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool dOnUpdatePlayerPosition(int x, int y, int z);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginFocusWindow();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int dOnPluginSdlEvent(IntPtr ev);


    private readonly FuncPointer<dOnPluginInitialize> _initPluginDel;
    private readonly FuncPointer<dOnPluginTick> _tickPluginDel;
    private readonly FuncPointer<dOnPluginClose> _closingPluginDel;
    private readonly FuncPointer<dOnPluginPacketInOut> _packetInPluginDel;
    private readonly FuncPointer<dOnPluginPacketInOut> _packetOutPluginDel;
    private readonly FuncPointer<dOnHotkey> _hotkeyPluginDel;
    private readonly FuncPointer<dOnMouse> _mousePluginDel;
    private readonly FuncPointer<OnUpdatePlayerPosition> _updatePlayerPosDel;
    private readonly FuncPointer<dOnPluginFocusWindow> _focusGainedDel, _focusLostDel;
    private readonly FuncPointer<dOnPluginSdlEvent> _sdlEventDel;

    

    public ClassicUOHost()
    {
        _initPluginDel = new FuncPointer<dOnPluginInitialize>(InitializePlugin);
        _tickPluginDel = new FuncPointer<dOnPluginTick>(TickPlugin);
        _closingPluginDel = new FuncPointer<dOnPluginClose>(ClosingPlugin);
        _packetInPluginDel = new FuncPointer<dOnPluginPacketInOut>(PacketInPlugin);
        _packetOutPluginDel = new FuncPointer<dOnPluginPacketInOut>(PacketOutPlugin);
        _hotkeyPluginDel = new FuncPointer<dOnHotkey>(HotkeyPlugin);
        _mousePluginDel = new FuncPointer<dOnMouse>(MousePlugin);
        _updatePlayerPosDel = new FuncPointer<OnUpdatePlayerPosition>(UpdatePlayerPosition);
        _focusGainedDel = new FuncPointer<dOnPluginFocusWindow>(FocusGained);
        _focusLostDel = new FuncPointer<dOnPluginFocusWindow>(FocusLost);
        _sdlEventDel = new FuncPointer<dOnPluginSdlEvent>(SdlEvent);
    }

    public void Run(string[] args)
    {
        var libName = "./cuo";
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.MacOSX:
                libName += ".dylib";
                break;
            case PlatformID.Unix:
                libName += ".so";
                break;
            default:
                libName += ".dll";
                break;
        }

        var libPtr = Native.LoadLibrary(libName);

        unsafe
        {
            var initializePtr = Native.GetProcessAddress(libPtr, "Initialize");
            var initializeMethod = Marshal.GetDelegateForFunctionPointer<dOnInitializeCuo>(initializePtr);

            var argv = stackalloc IntPtr[args.Length];
            for (int i = 0; i < args.Length; i++)
                argv[i] = Marshal.StringToHGlobalAnsi(args[i]);

            var mem = Marshal.AllocHGlobal(sizeof(HostSetup));
            for (var i = 0; i < sizeof(HostSetup); i++)
                ((byte*)mem)[i] = 0;

            ref var hostSetup = ref Unsafe.AsRef<HostSetup>(mem.ToPointer());
            hostSetup.InitializeFn = _initPluginDel.Pointer;
            hostSetup.TickFn = _tickPluginDel.Pointer;
            hostSetup.ClosingFn = _closingPluginDel.Pointer;
            hostSetup.PacketInFn = _packetInPluginDel.Pointer;
            hostSetup.PacketOutFn = _packetOutPluginDel.Pointer;
            hostSetup.HotkeyFn = _hotkeyPluginDel.Pointer;
            hostSetup.MouseFn = _mousePluginDel.Pointer;
            hostSetup.UpdatePlayerPosFn = _updatePlayerPosDel.Pointer;
            hostSetup.FocusGainedFn = _focusGainedDel.Pointer;
            hostSetup.FocusLostFn = _focusLostDel.Pointer;
            hostSetup.SdlEventFn = _sdlEventDel.Pointer;

            initializeMethod(argv, args.Length, mem);

            if (mem != null)
                Marshal.FreeHGlobal(mem);
        }
    }

    unsafe void InitializePlugin(IntPtr exportedFuncs, uint clientVersion, IntPtr pluginPathPtr, IntPtr assetsPathPtr)
    {
        ref var cuoHost = ref Unsafe.AsRef<CuoHostSetup>(exportedFuncs.ToPointer());
        var cuoHandler = new ClassicUOHandler((CuoHostSetup*)exportedFuncs);

        var plugin = new Plugin(cuoHandler, Guid.Empty);
        _plugins.Add(plugin);

        var pluginPath = Marshal.PtrToStringAnsi(pluginPathPtr);
        var assetsPath = Marshal.PtrToStringAnsi(assetsPathPtr);
        plugin.Load(cuoHost.SdlWindow, pluginPath, clientVersion, assetsPath);
    }

    void TickPlugin()
    {
        foreach (var plugin in _plugins)
            plugin.Tick();
    }

    void ClosingPlugin()
    {
        foreach (var plugin in _plugins)
            plugin.Close();
    }

    bool HotkeyPlugin(int key, int mod, bool pressed)
    {
        var ok = true;

        foreach (var plugin in _plugins)
            ok |= plugin.ProcessHotkeys(key, mod, pressed);

        return ok;
    }

    void MousePlugin(int button, int wheel)
    {
        foreach (var plugin in _plugins)
            plugin.ProcessMouse(button, wheel);
    }

    void UpdatePlayerPosition(int x, int y, int z)
    {
        foreach (var plugin in _plugins)
            plugin.UpdatePlayerPosition(x, y, z);
    }

    void FocusGained()
    {
        foreach (var plugin in _plugins)
            plugin.FocusGained();
    }

    void FocusLost()
    {
        foreach (var plugin in _plugins)
            plugin.FocusLost();
    }

    unsafe int SdlEvent(IntPtr ev)
    {
        var res = 0;

        foreach (var plugin in _plugins)
            res |= plugin.ProcessWndProc(ev.ToPointer());

        return res;
    }

    unsafe bool PacketInPlugin(IntPtr data, ref int length)
    {
        var ok = true;

        foreach (var plugin in _plugins)
        {
            var rentBuf = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                fixed (byte* ptr = rentBuf)
                    Buffer.MemoryCopy(data.ToPointer(), ptr, sizeof(byte) * length, sizeof(byte) * length);

                ok |= plugin.ProcessRecvPacket(ref rentBuf, ref length);

                fixed (byte* ptr = rentBuf)
                    Buffer.MemoryCopy(ptr, data.ToPointer(), sizeof(byte) * length, sizeof(byte) * length);

                //if (!ok)
                //{
                //    length = 0;
                //}
                //else
                //{
                //    fixed (byte* ptr = rentBuf)
                //        Buffer.MemoryCopy(ptr, data.ToPointer(), sizeof(byte) * length, sizeof(byte) * length);
                //}
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBuf);
            }
        }

        return ok;
    }

    unsafe bool PacketOutPlugin(IntPtr data, ref int length)
    {
        var ok = true;

        foreach (var plugin in _plugins)
        {
            var rentBuf = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                fixed (byte* ptr = rentBuf)
                    Buffer.MemoryCopy(data.ToPointer(), ptr, sizeof(byte) * length, sizeof(byte) * length);

                ok |= plugin.ProcessSendPacket(ref rentBuf, ref length);

                fixed (byte* ptr = rentBuf)
                    Buffer.MemoryCopy(ptr, data.ToPointer(), sizeof(byte) * length, sizeof(byte) * length);

                //if (!ok)
                //{
                //    length = 0;
                //}
                //else
                //{
                //    fixed (byte* ptr = rentBuf)
                //        Buffer.MemoryCopy(ptr, data.ToPointer(), sizeof(byte) * length, sizeof(byte) * length);
                //}
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBuf);
            }
        }

        return ok;
    }

    sealed class FuncPointer<T> where T : Delegate
    {
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly T _delegate;
        private readonly IntPtr _ptr;

        public FuncPointer(T @delegate)
        {
            _delegate = @delegate;
            _ptr = Marshal.GetFunctionPointerForDelegate(_delegate);
        }

        public IntPtr Pointer => _ptr;
    }
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct HostSetup
{
    public IntPtr InitializeFn;
    public IntPtr TickFn;
    public IntPtr ClosingFn;
    public IntPtr FocusGainedFn;
    public IntPtr FocusLostFn;
    public IntPtr ConnectedFn;
    public IntPtr DisconnectedFn;
    public IntPtr /*delegate*<int, int, bool, bool>*/ HotkeyFn;
    public IntPtr /*delegate*<int, int, bool>*/ MouseFn;
    public IntPtr /*delegate*<IntPtr, ref int, void>*/ CmdListFn;
    public IntPtr /*delegate*<IntPtr, int>*/ SdlEventFn;
    public IntPtr /*delegate*<int, int, int, void>*/ UpdatePlayerPosFn;
    public IntPtr PacketInFn;
    public IntPtr PacketOutFn;
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct CuoHostSetup
{
    public IntPtr SdlWindow;
    public IntPtr /*delegate*<IntPtr, ref int, bool>*/ PluginRecvFn;
    public IntPtr /*delegate*<IntPtr, ref int, bool>*/ PluginSendFn;
    public IntPtr /*delegate*<int, short>*/ PacketLengthFn;
    public IntPtr /*delegate*<int, void>*/ CastSpellFn;
    public IntPtr /*delegate*<IntPtr>*/ SetWindowTitleFn;
    public IntPtr /*delegate*<int, IntPtr, IntPtr, bool, bool>*/ GetClilocFn;
    public IntPtr /*delegate*<int, bool, bool>*/ RequestMoveFn;
    public IntPtr /*delegate*<ref int, ref int, ref int, bool>*/ GetPlayerPositionFn;
}

sealed unsafe class ClassicUOHandler : IPluginHandler
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dCastSpell(int index);
    [MarshalAs(UnmanagedType.FunctionPtr)]
    private readonly dCastSpell _castSpell;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr dGetCliloc(int cliloc, IntPtr args, bool capitalize);
    [MarshalAs(UnmanagedType.FunctionPtr)]
    private readonly dGetCliloc _getCliloc;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate short dGetPacketLength(int id);
    [MarshalAs(UnmanagedType.FunctionPtr)]
    private readonly dGetPacketLength _packetLength;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool dGetPlayerPosition(out int x, out int y, out int z);
    [MarshalAs(UnmanagedType.FunctionPtr)]
    private readonly dGetPlayerPosition _getPlayerPosition;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool dRequestMove(int dir, bool run);
    [MarshalAs(UnmanagedType.FunctionPtr)]
    private readonly dRequestMove _requestMove;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool dPacketRecvSend(IntPtr data, ref int length);
    [MarshalAs(UnmanagedType.FunctionPtr)]
    private readonly dPacketRecvSend _sendToClient, _sendToServer;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dSetWindowTitle(IntPtr textPtr);
    [MarshalAs(UnmanagedType.FunctionPtr)]
    private readonly dSetWindowTitle _setWindowTitle;


    public ClassicUOHandler(CuoHostSetup* setup)
    {
        _castSpell = SetFunction<dCastSpell>(setup->CastSpellFn);
        _getCliloc = SetFunction<dGetCliloc>(setup->GetClilocFn);
        _packetLength = SetFunction<dGetPacketLength>(setup->PacketLengthFn);
        _getPlayerPosition = SetFunction<dGetPlayerPosition>(setup->GetPlayerPositionFn);
        _requestMove = SetFunction<dRequestMove>(setup->RequestMoveFn);
        _sendToClient = SetFunction<dPacketRecvSend>(setup->PluginRecvFn);
        _sendToServer = SetFunction<dPacketRecvSend>(setup->PluginSendFn);
        _setWindowTitle = SetFunction<dSetWindowTitle>(setup->SetWindowTitleFn);

        static T SetFunction<T>(IntPtr ptr) where T : Delegate => ptr == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    public void CastSpell(Guid id, int index)
    {
        _castSpell?.Invoke(index);
    }

    public string GetCliloc(Guid id, int cliloc, string args, bool capitalize)
    {
        var output = string.Empty;
        fixed (char* ptr = args)
            output = Marshal.PtrToStringAnsi(_getCliloc?.Invoke(cliloc, (IntPtr)ptr, capitalize) ?? IntPtr.Zero);

        return output;
    }

    public short GetPacketLen(Guid id, byte packetId)
    {
        return _packetLength?.Invoke(packetId) ?? -1;
    }

    public bool GetPlayerPosition(Guid id, out int x, out int y, out int z)
    {
        x = y = z = 0;
        return _getPlayerPosition?.Invoke(out x, out y, out z) ?? true;
    }

    public bool RequestMove(Guid id, int dir, bool run)
    {
        return _requestMove?.Invoke(dir, run) ?? true;
    }

    public bool SendToClient(Guid id, ref byte[] data, ref int length)
    {
        fixed (byte* ptr = data)
            return SendToClient(id, (IntPtr)ptr, ref length);
    }

    public bool SendToClient(Guid id, IntPtr data, ref int length)
    {
        return _sendToClient?.Invoke(data, ref length) ?? true;
    }

    public bool SendToServer(Guid id, ref byte[] data, ref int length)
    {
        fixed (byte* ptr = data)
            return SendToServer(id, (IntPtr)ptr, ref length);
    }

    public bool SendToServer(Guid id, IntPtr data, ref int length)
    {
        return _sendToServer?.Invoke(data, ref length) ?? true;
    }

    public void SetWindowTitle(Guid id, string title)
    {
        if (string.IsNullOrEmpty(title) || _setWindowTitle == null)
            return;

        var count = Encoding.UTF8.GetByteCount(title);

        var ptr = stackalloc byte[count + 1];

        fixed (char* titlePtr = title)
        //fixed (byte* ptr = &buf[0])
        {
            Encoding.UTF8.GetBytes(titlePtr, title.Length, ptr, count);

            ptr[count] = 0;
            _setWindowTitle((IntPtr)ptr);
        }
    }
}