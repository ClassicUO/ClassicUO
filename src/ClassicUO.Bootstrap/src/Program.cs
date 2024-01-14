using ClassicUO;
using CUO_API;
using System;
using System.Buffers;
using System.Collections.Generic;
<<<<<<< HEAD
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


Global.Host.Run(args);
=======
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

var address = "127.0.0.1";
var port = 7777;

if (args.Length >= 1)
{
    Console.WriteLine(args[0]);
    address = IPAddress.Parse(args[0]).ToString();
}

if (args.Length >= 2)
{
    Console.WriteLine(args[1]);
    port = int.Parse(args[1]);
}


//var cuoServer = new ClassicUORpcServer();
//cuoServer.Start(address, port);

Global.Host.Run(args);

>>>>>>> + classicuo.bootstrap app
Console.WriteLine("finished");


static class Global
{
    // NOTE: this must be static otherwise GC does some weird stuff on delegates ¯\_(ツ)_/¯
    public static readonly ClassicUOHost Host = new ClassicUOHost();

}

<<<<<<< HEAD
sealed class ClassicUOHost : IPluginHandler
{
    private readonly List<Plugin> _plugins = new List<Plugin>();

    // Plugin -> Client
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dCastSpell(int index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr dGetCliloc(int cliloc, IntPtr args, bool capitalize);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate short dGetPacketLength(int id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool dGetPlayerPosition(out int x, out int y, out int z);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool dRequestMove(int dir, bool run);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool dPacketRecvSend(IntPtr data, ref int length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dSetWindowTitle(IntPtr textPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr dOnPluginReflectionCommand(IntPtr cmdPtr);

    private FuncPointer<dCastSpell> _castSpell;
    private FuncPointer<dGetCliloc> _getCliloc;
    private FuncPointer<dGetPacketLength> _packetLength;
    private FuncPointer<dGetPlayerPosition> _getPlayerPosition;
    private FuncPointer<dRequestMove> _requestMove;
    private FuncPointer<dPacketRecvSend> _sendToClient, _sendToServer;
    private FuncPointer<dSetWindowTitle> _setWindowTitle;
    private FuncPointer<dOnPluginReflectionCommand> _reflectionCmd;


    // Client -> Plugin
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void dOnInitializeCuo(IntPtr* argv, int argc, IntPtr hostSetupPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginBindCuoFunctions(IntPtr exportedFuncs);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginLoad(IntPtr pluginPathPtr, uint clientVersion, IntPtr assetsPathPtr, IntPtr sdlWindow);
=======
sealed class ClassicUOHost
{
    private readonly List<Plugin> _plugins = new List<Plugin>();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void dOnInitializeCuo(IntPtr* argv, int argc, IntPtr hostSetupPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginInitialize(IntPtr exportedFuncs, uint clientVersion, IntPtr pluginPathPtr, IntPtr assetsPathPtr);
>>>>>>> + classicuo.bootstrap app

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginTick();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginClose();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
<<<<<<< HEAD
    delegate void dOnPluginConnection();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
=======
>>>>>>> + classicuo.bootstrap app
    delegate bool dOnPluginPacketInOut(IntPtr data, ref int length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool dOnHotkey(int key, int mod, bool pressed);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
<<<<<<< HEAD
    delegate void dOnMouse(int button, int wheel);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
=======
>>>>>>> + classicuo.bootstrap app
    delegate bool dOnUpdatePlayerPosition(int x, int y, int z);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginFocusWindow();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int dOnPluginSdlEvent(IntPtr ev);

<<<<<<< HEAD
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginCommandList(out IntPtr list, out int len);


    private readonly FuncPointer<dOnPluginLoad> _loadPluginDel;
    private readonly FuncPointer<dOnPluginBindCuoFunctions> _initCuoFunctionsDel;
    private readonly FuncPointer<dOnPluginTick> _tickPluginDel;
    private readonly FuncPointer<dOnPluginClose> _closingPluginDel;
    private readonly FuncPointer<dOnPluginConnection> _connectedDel, _disconnectedDel;
    private readonly FuncPointer<dOnPluginPacketInOut> _packetInPluginDel, _packetOutPluginDel;
    private readonly FuncPointer<dOnHotkey> _hotkeyPluginDel;
    private readonly FuncPointer<dOnMouse> _mousePluginDel;
    private readonly FuncPointer<OnUpdatePlayerPosition> _updatePlayerPosDel;
    private readonly FuncPointer<dOnPluginFocusWindow> _focusGainedDel, _focusLostDel;
    private readonly FuncPointer<dOnPluginSdlEvent> _sdlEventDel;
    private readonly FuncPointer<dOnPluginCommandList> _cmdListDel;
=======

    private readonly FuncPointer<dOnPluginInitialize> _initPluginDel;
    private readonly FuncPointer<dOnPluginTick> _tickPluginDel;
    private readonly FuncPointer<dOnPluginClose> _closingPluginDel;
    private readonly FuncPointer<dOnPluginPacketInOut> _packetInPluginDel;
    private readonly FuncPointer<dOnPluginPacketInOut> _packetOutPluginDel;
    private readonly FuncPointer<dOnHotkey> _hotkeyPluginDel;
    private readonly FuncPointer<OnUpdatePlayerPosition> _updatePlayerPosDel;
    private readonly FuncPointer<dOnPluginFocusWindow> _focusGainedDel, _focusLostDel;
    private readonly FuncPointer<dOnPluginSdlEvent> _sdlEventDel;

>>>>>>> + classicuo.bootstrap app
    

    public ClassicUOHost()
    {
<<<<<<< HEAD
        _initCuoFunctionsDel = new FuncPointer<dOnPluginBindCuoFunctions>(Initialize);
        _loadPluginDel = new FuncPointer<dOnPluginLoad>(LoadPlugin);
        _tickPluginDel = new FuncPointer<dOnPluginTick>(TickPlugin);
        _closingPluginDel = new FuncPointer<dOnPluginClose>(ClosingPlugin);
        _connectedDel = new FuncPointer<dOnPluginConnection>(Connected);
        _disconnectedDel = new FuncPointer<dOnPluginConnection>(Disconnected);
        _packetInPluginDel = new FuncPointer<dOnPluginPacketInOut>(PacketInPlugin);
        _packetOutPluginDel = new FuncPointer<dOnPluginPacketInOut>(PacketOutPlugin);
        _hotkeyPluginDel = new FuncPointer<dOnHotkey>(HotkeyPlugin);
        _mousePluginDel = new FuncPointer<dOnMouse>(MousePlugin);
=======
        _initPluginDel = new FuncPointer<dOnPluginInitialize>(InitializePlugin);
        _tickPluginDel = new FuncPointer<dOnPluginTick>(TickPlugin);
        _closingPluginDel = new FuncPointer<dOnPluginClose>(ClosingPlugin);
        _packetInPluginDel = new FuncPointer<dOnPluginPacketInOut>(PacketInPlugin);
        _packetOutPluginDel = new FuncPointer<dOnPluginPacketInOut>(PacketOutPlugin);
        _hotkeyPluginDel = new FuncPointer<dOnHotkey>(HotkeyPlugin);
>>>>>>> + classicuo.bootstrap app
        _updatePlayerPosDel = new FuncPointer<OnUpdatePlayerPosition>(UpdatePlayerPosition);
        _focusGainedDel = new FuncPointer<dOnPluginFocusWindow>(FocusGained);
        _focusLostDel = new FuncPointer<dOnPluginFocusWindow>(FocusLost);
        _sdlEventDel = new FuncPointer<dOnPluginSdlEvent>(SdlEvent);
<<<<<<< HEAD
        _cmdListDel = new FuncPointer<dOnPluginCommandList>(GetCommandList);
=======
>>>>>>> + classicuo.bootstrap app
    }

    public void Run(string[] args)
    {
<<<<<<< HEAD
<<<<<<< HEAD
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
=======
        var libName = "";
=======
        var libName = "./cuo";
>>>>>>> fixed a weird bug tht causes access mem violation
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.MacOSX:
                libName += ".dylib";
                break;
            case PlatformID.Unix:
                libName += ".so";
                break;
            default:
<<<<<<< HEAD
                libName = "./ClassicUO.dll";
>>>>>>> + classicuo.bootstrap app
=======
                libName += ".dll";
>>>>>>> fixed a weird bug tht causes access mem violation
                break;
        }

        var libPtr = Native.LoadLibrary(libName);

        unsafe
        {
<<<<<<< HEAD
<<<<<<< HEAD
            var initializePtr = Native.GetProcessAddress(libPtr, "Initialize");
            var initializeMethod = Marshal.GetDelegateForFunctionPointer<dOnInitializeCuo>(initializePtr);
=======
            var initializeMethod = (delegate*<IntPtr*, int, HostSetup*, void>)Native.GetProcessAddress(libPtr, "Initialize");
>>>>>>> + classicuo.bootstrap app
=======
            var initializePtr = Native.GetProcessAddress(libPtr, "Initialize");
            var initializeMethod = Marshal.GetDelegateForFunctionPointer<dOnInitializeCuo>(initializePtr);
>>>>>>> delegate * --> delegate [lol]

            var argv = stackalloc IntPtr[args.Length];
            for (int i = 0; i < args.Length; i++)
                argv[i] = Marshal.StringToHGlobalAnsi(args[i]);

<<<<<<< HEAD
            var mem = Marshal.AllocHGlobal(sizeof(HostBindings));
            for (var i = 0; i < sizeof(HostBindings); i++)
                ((byte*)mem)[i] = 0;

            ref var hostSetup = ref Unsafe.AsRef<HostBindings>(mem.ToPointer());
            hostSetup.InitializeFn = _initCuoFunctionsDel.Pointer;
            hostSetup.LoadPluginFn = _loadPluginDel.Pointer;
=======
            var mem = Marshal.AllocHGlobal(sizeof(HostSetup));
            for (var i = 0; i < sizeof(HostSetup); i++)
                ((byte*)mem)[i] = 0;

            ref var hostSetup = ref Unsafe.AsRef<HostSetup>(mem.ToPointer());
            hostSetup.InitializeFn = _initPluginDel.Pointer;
>>>>>>> + classicuo.bootstrap app
            hostSetup.TickFn = _tickPluginDel.Pointer;
            hostSetup.ClosingFn = _closingPluginDel.Pointer;
            hostSetup.PacketInFn = _packetInPluginDel.Pointer;
            hostSetup.PacketOutFn = _packetOutPluginDel.Pointer;
            hostSetup.HotkeyFn = _hotkeyPluginDel.Pointer;
<<<<<<< HEAD
            hostSetup.MouseFn = _mousePluginDel.Pointer;
=======
>>>>>>> + classicuo.bootstrap app
            hostSetup.UpdatePlayerPosFn = _updatePlayerPosDel.Pointer;
            hostSetup.FocusGainedFn = _focusGainedDel.Pointer;
            hostSetup.FocusLostFn = _focusLostDel.Pointer;
            hostSetup.SdlEventFn = _sdlEventDel.Pointer;
<<<<<<< HEAD
            hostSetup.ConnectedFn = _connectedDel.Pointer;
            hostSetup.DisconnectedFn = _disconnectedDel.Pointer;
            hostSetup.CmdListFn = _cmdListDel.Pointer;

            initializeMethod(argv, args.Length, mem);

            if (mem != null)
                Marshal.FreeHGlobal(mem);
        }
    }

    unsafe void Initialize(IntPtr exportedFuncs)
    {
        ref var cuoHost = ref Unsafe.AsRef<ClientBindings>(exportedFuncs.ToPointer());

        _castSpell = new FuncPointer<dCastSpell>(cuoHost.CastSpellFn);
        _getCliloc = new FuncPointer<dGetCliloc>(cuoHost.GetClilocFn);
        _packetLength = new FuncPointer<dGetPacketLength>(cuoHost.PacketLengthFn);
        _getPlayerPosition = new FuncPointer<dGetPlayerPosition>(cuoHost.GetPlayerPositionFn);
        _requestMove = new FuncPointer<dRequestMove>(cuoHost.RequestMoveFn);
        _sendToClient = new FuncPointer<dPacketRecvSend>(cuoHost.PluginRecvFn);
        _sendToServer = new FuncPointer<dPacketRecvSend>(cuoHost.PluginSendFn);
        _setWindowTitle = new FuncPointer<dSetWindowTitle>(cuoHost.SetWindowTitleFn);
        _reflectionCmd = new FuncPointer<dOnPluginReflectionCommand>(cuoHost.ReflectionCmdFn);
    }

    unsafe void LoadPlugin(IntPtr pluginPathPtr, uint clientVersion, IntPtr assetsPathPtr, IntPtr sdlWindow)
    {
        var plugin = new Plugin(this, Guid.Empty);
=======

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
>>>>>>> + classicuo.bootstrap app
        _plugins.Add(plugin);

        var pluginPath = Marshal.PtrToStringAnsi(pluginPathPtr);
        var assetsPath = Marshal.PtrToStringAnsi(assetsPathPtr);
<<<<<<< HEAD
        plugin.Load(sdlWindow, pluginPath, clientVersion, assetsPath);
=======
        plugin.Load(cuoHost.SdlWindow, pluginPath, clientVersion, assetsPath);
>>>>>>> + classicuo.bootstrap app
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

<<<<<<< HEAD
    void Connected()
    {
        foreach (var plugin in _plugins)
            plugin.Connected();
    }

    void Disconnected()
    {
        foreach (var plugin in _plugins)
            plugin.Disconnected();
    }

=======
>>>>>>> + classicuo.bootstrap app
    bool HotkeyPlugin(int key, int mod, bool pressed)
    {
        var ok = true;

        foreach (var plugin in _plugins)
            ok |= plugin.ProcessHotkeys(key, mod, pressed);

        return ok;
    }

<<<<<<< HEAD
    void MousePlugin(int button, int wheel)
    {
        foreach (var plugin in _plugins)
            plugin.ProcessMouse(button, wheel);
    }

=======
>>>>>>> + classicuo.bootstrap app
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

<<<<<<< HEAD
    void GetCommandList(out IntPtr data, out int len)
    {
        data = IntPtr.Zero;
        len = 0;

        foreach (var plugin in _plugins)
            plugin.GetCommandList(out data, out len);
    }


    public unsafe void ReflectionUsePrimaryAbility()
    {
        var f = 1;
        SendReflectionCmd((IntPtr)(&f));
    }

    public unsafe void ReflectionUseSecondaryAbility()
    {
        var f = 2;
        SendReflectionCmd((IntPtr)(&f));
    }

    public unsafe bool ReflectionAutowalking(sbyte walking)
    {
        var f = (3, walking);
        var result = SendReflectionCmd((IntPtr)(&f));
        var toBool = Unsafe.AsRef<bool>(result.ToPointer());
        Console.WriteLine("bool: {0} [{1}]", toBool, result);
        return toBool;
    }

    IntPtr SendReflectionCmd(IntPtr ptr)
    {
        return _reflectionCmd?.Delegate?.Invoke(ptr) ?? IntPtr.Zero;
=======
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
>>>>>>> + classicuo.bootstrap app
    }

    public void CastSpell(Guid id, int index)
    {
<<<<<<< HEAD
        _castSpell?.Delegate?.Invoke(index);
    }

    public unsafe string GetCliloc(Guid id, int cliloc, string args, bool capitalize)
    {
        var output = string.Empty;
        fixed (char* ptr = args)
            output = Marshal.PtrToStringAnsi(_getCliloc?.Delegate?.Invoke(cliloc, (IntPtr)ptr, capitalize) ?? IntPtr.Zero);
=======
        _castSpell?.Invoke(index);
    }

    public string GetCliloc(Guid id, int cliloc, string args, bool capitalize)
    {
        var output = string.Empty;
        fixed (char* ptr = args)
            output = Marshal.PtrToStringAnsi(_getCliloc?.Invoke(cliloc, (IntPtr)ptr, capitalize) ?? IntPtr.Zero);
>>>>>>> + classicuo.bootstrap app

        return output;
    }

    public short GetPacketLen(Guid id, byte packetId)
    {
<<<<<<< HEAD
        return _packetLength?.Delegate?.Invoke(packetId) ?? -1;
=======
        return _packetLength?.Invoke(packetId) ?? -1;
>>>>>>> + classicuo.bootstrap app
    }

    public bool GetPlayerPosition(Guid id, out int x, out int y, out int z)
    {
        x = y = z = 0;
<<<<<<< HEAD
        return _getPlayerPosition?.Delegate?.Invoke(out x, out y, out z) ?? true;
=======
        return _getPlayerPosition?.Invoke(out x, out y, out z) ?? true;
>>>>>>> + classicuo.bootstrap app
    }

    public bool RequestMove(Guid id, int dir, bool run)
    {
<<<<<<< HEAD
        return _requestMove?.Delegate?.Invoke(dir, run) ?? true;
    }

    public unsafe bool SendToClient(Guid id, ref byte[] data, ref int length)
=======
        return _requestMove?.Invoke(dir, run) ?? true;
    }

    public bool SendToClient(Guid id, ref byte[] data, ref int length)
>>>>>>> + classicuo.bootstrap app
    {
        fixed (byte* ptr = data)
            return SendToClient(id, (IntPtr)ptr, ref length);
    }

    public bool SendToClient(Guid id, IntPtr data, ref int length)
    {
<<<<<<< HEAD
        return _sendToClient?.Delegate?.Invoke(data, ref length) ?? true;
    }

    public unsafe bool SendToServer(Guid id, ref byte[] data, ref int length)
=======
        return _sendToClient?.Invoke(data, ref length) ?? true;
    }

    public bool SendToServer(Guid id, ref byte[] data, ref int length)
>>>>>>> + classicuo.bootstrap app
    {
        fixed (byte* ptr = data)
            return SendToServer(id, (IntPtr)ptr, ref length);
    }

    public bool SendToServer(Guid id, IntPtr data, ref int length)
    {
<<<<<<< HEAD
        return _sendToServer?.Delegate?.Invoke(data, ref length) ?? true;
    }

    public unsafe void SetWindowTitle(Guid id, string title)
    {
        if (string.IsNullOrEmpty(title) || _setWindowTitle == null || _setWindowTitle.Delegate == null)
            return;

        var count = Encoding.UTF8.GetByteCount(title);

        var ptr = stackalloc byte[count + 1];

        fixed (char* titlePtr = title)
        //fixed (byte* ptr = &buf[0])
        {
            Encoding.UTF8.GetBytes(titlePtr, title.Length, ptr, count);

            ptr[count] = 0;
            _setWindowTitle.Delegate((IntPtr)ptr);
        }
    }

    sealed class FuncPointer<T> where T : Delegate
    {
        [MarshalAs(UnmanagedType.FunctionPtr)]
        private readonly T _delegate;
        private readonly IntPtr _ptr;

        public FuncPointer(T @delegate)
        {
            _delegate = @delegate;
            _ptr = _delegate == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(_delegate);
        }

        public FuncPointer(IntPtr ptr)
        {
            _delegate = ptr == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<T>(ptr);
            _ptr = ptr;
        }

        public T Delegate => _delegate;
        public IntPtr Pointer => _ptr;
    }
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct HostBindings
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
    public IntPtr /*delegate*<int, int, bool>*/ MouseFn;
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
=======
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
>>>>>>> + classicuo.bootstrap app
