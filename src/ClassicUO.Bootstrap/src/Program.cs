using ClassicUO;
using CUO_API;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;


#if !DEBUG
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    var dt = DateTime.Now;
    var version = Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "0.0.0.0";
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("######################## [START LOG] ########################");

#if DEV_BUILD
    sb.AppendLine($"ClassicUO [DEV_BUILD] - {version} - {dt}");
#else
    sb.AppendLine($"ClassicUO [STANDARD_BUILD] - {version} - {dt}");
#endif

    sb.AppendLine($"OS: {Environment.OSVersion.Platform} {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}");
    sb.AppendLine($"Thread: {Thread.CurrentThread.Name}");
    sb.AppendLine();
    sb.AppendFormat("Exception:\n{0}\n", e.ExceptionObject);
    sb.AppendLine("######################## [END LOG] ########################");
    sb.AppendLine();
    sb.AppendLine();

    Console.WriteLine(e.ExceptionObject.ToString());
    var path = Path.Combine(AppContext.BaseDirectory, "Logs");

    if (!Directory.Exists(path))
        Directory.CreateDirectory(path);

    File.WriteAllText(Path.Combine(path, $"{dt:yyyy-MM-dd_hh-mm-ss}_crash.txt"), s.ToString());
};
#endif

Global.Host.Run(args);
Console.WriteLine("finished");


static class Global
{
    // NOTE: this must be static otherwise GC does some weird stuff on delegates ¯\_(ツ)_/¯
    public static readonly ClassicUOHost Host = new ClassicUOHost();
}

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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginTick();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginClose();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void dOnPluginConnection();

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
    

    public ClassicUOHost()
    {
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
        _updatePlayerPosDel = new FuncPointer<OnUpdatePlayerPosition>(UpdatePlayerPosition);
        _focusGainedDel = new FuncPointer<dOnPluginFocusWindow>(FocusGained);
        _focusLostDel = new FuncPointer<dOnPluginFocusWindow>(FocusLost);
        _sdlEventDel = new FuncPointer<dOnPluginSdlEvent>(SdlEvent);
        _cmdListDel = new FuncPointer<dOnPluginCommandList>(GetCommandList);
    }

    public void Run(string[] args)
    {
        var libName = "./cuo";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            libName += ".dylib";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            libName += ".so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            libName += ".dll";
        }
        else
        {
            Console.WriteLine("OS not supported");
            throw new NotSupportedException("OS not suported");
        }

        Console.WriteLine("ClassicUO lib loaded: {0}", libName);
        
        var libPtr = Native.LoadLibrary(libName);
        if (libPtr == IntPtr.Zero)
        {
            Console.WriteLine("Failed to load {0}. Maybe it doesn't exists.", libName);
            throw new DllNotFoundException($"Failed to load {libName}. Maybe it doesn't exists.");
        }

        unsafe
        {
            var initializePtr = Native.GetProcessAddress(libPtr, "Initialize");
            if (initializePtr == IntPtr.Zero)
            {
                Console.WriteLine("'Initialize' entry point not found");
                throw new EntryPointNotFoundException("'Initialize' entry point not found");
            }

            var initializeMethod = Marshal.GetDelegateForFunctionPointer<dOnInitializeCuo>(initializePtr);

            var argv = new IntPtr[args.Length];
            for (int i = 0; i < args.Length; i++)
                argv[i] = Marshal.StringToHGlobalAnsi(args[i]);

            var mem = Marshal.AllocHGlobal(sizeof(HostBindings));
            for (var i = 0; i < sizeof(HostBindings); i++)
                ((byte*)mem)[i] = 0;

            ref var hostSetup = ref Unsafe.AsRef<HostBindings>(mem.ToPointer());
            hostSetup.InitializeFn = _initCuoFunctionsDel.Pointer;
            hostSetup.LoadPluginFn = _loadPluginDel.Pointer;
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
            hostSetup.ConnectedFn = _connectedDel.Pointer;
            hostSetup.DisconnectedFn = _disconnectedDel.Pointer;
            hostSetup.CmdListFn = _cmdListDel.Pointer;

            fixed (IntPtr* argvPtr = argv)
                initializeMethod(argvPtr, args.Length, mem);

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
        _plugins.Add(plugin);

        var pluginPath = Marshal.PtrToStringAnsi(pluginPathPtr);
        var assetsPath = Marshal.PtrToStringAnsi(assetsPathPtr);
        plugin.Load(sdlWindow, pluginPath, clientVersion, assetsPath);
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

    bool HotkeyPlugin(int key, int mod, bool pressed)
    {
        var ok = true;

        foreach (var plugin in _plugins)
            ok &= plugin.ProcessHotkeys(key, mod, pressed);

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

                ok &= plugin.ProcessRecvPacket(ref rentBuf, ref length);

                fixed (byte* ptr = rentBuf)
                    Buffer.MemoryCopy(ptr, data.ToPointer(), sizeof(byte) * length, sizeof(byte) * length);
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

                ok &= plugin.ProcessSendPacket(ref rentBuf, ref length);

                fixed (byte* ptr = rentBuf)
                    Buffer.MemoryCopy(ptr, data.ToPointer(), sizeof(byte) * length, sizeof(byte) * length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBuf);
            }
        }

        return ok;
    }

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
    }

    public void CastSpell(Guid id, int index)
    {
        _castSpell?.Delegate?.Invoke(index);
    }

    public unsafe string GetCliloc(Guid id, int cliloc, string args, bool capitalize)
    {
        var output = string.Empty;
        fixed (char* ptr = args)
            output = Marshal.PtrToStringAnsi(_getCliloc?.Delegate?.Invoke(cliloc, (IntPtr)ptr, capitalize) ?? IntPtr.Zero);

        return output;
    }

    public short GetPacketLen(Guid id, byte packetId)
    {
        return _packetLength?.Delegate?.Invoke(packetId) ?? -1;
    }

    public bool GetPlayerPosition(Guid id, out int x, out int y, out int z)
    {
        x = y = z = 0;
        return _getPlayerPosition?.Delegate?.Invoke(out x, out y, out z) ?? true;
    }

    public bool RequestMove(Guid id, int dir, bool run)
    {
        return _requestMove?.Delegate?.Invoke(dir, run) ?? true;
    }

    public unsafe bool SendToClient(Guid id, ref byte[] data, ref int length)
    {
        fixed (byte* ptr = data)
            return SendToClient(id, (IntPtr)ptr, ref length);
    }

    public bool SendToClient(Guid id, IntPtr data, ref int length)
    {
        return _sendToClient?.Delegate?.Invoke(data, ref length) ?? true;
    }

    public unsafe bool SendToServer(Guid id, ref byte[] data, ref int length)
    {
        fixed (byte* ptr = data)
            return SendToServer(id, (IntPtr)ptr, ref length);
    }

    public bool SendToServer(Guid id, IntPtr data, ref int length)
    {
        return _sendToServer?.Delegate?.Invoke(data, ref length) ?? true;
    }

    public unsafe void SetWindowTitle(Guid id, string title)
    {
        if (string.IsNullOrEmpty(title) || _setWindowTitle == null || _setWindowTitle.Delegate == null)
            return;

        var count = Encoding.UTF8.GetByteCount(title);

        var ptr = stackalloc byte[count + 1];
        ptr[count] = 0;

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
