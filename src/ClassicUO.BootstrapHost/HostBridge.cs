// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.BootstrapHost;

/// <summary>
/// Owns the HostBindings table cuo calls into and the ClientBindings table
/// cuo populates. Fans cuo events out to every loaded plugin and aggregates
/// their block flags / return values.
/// </summary>
internal sealed unsafe class HostBridge
{
    private static HostBridge? _instance;

    private nint _cuoHandle;
    private ClientBindings _clientBindings;
    private readonly PluginLoader _loader;
    private Thread? _gameThread;
    private readonly ConcurrentQueue<Action> _gameThreadQueue = new();

    public HostBridge()
    {
        _loader = new PluginLoader(this);
        _instance = this;
    }

    public IReadOnlyList<PluginContextImpl> Plugins => _loader.Plugins;
    public ClientBindings ClientBindings => _clientBindings;
    public bool IsGameThread => Thread.CurrentThread == _gameThread;

    /// <summary>Test-only: discover plugins from a custom root, skipping the cuo.dll load and Initialize call.</summary>
    internal void LoadPluginsForTest(string pluginsRoot)
    {
        _gameThread = Thread.CurrentThread;
        _loader.DiscoverAndLoad(pluginsRoot);
        foreach (var p in _loader.Plugins)
            p.AttachHost();
    }

    /// <summary>Test-only: drive lifecycle events without going through native callbacks.</summary>
    internal void TestRaiseConnected()    { foreach (var p in _loader.Plugins) p.RaiseConnected(); }
    internal void TestRaiseDisconnected() { foreach (var p in _loader.Plugins) p.RaiseDisconnected(); }
    internal void TestRaiseTick()         { DrainGameThreadQueue(); foreach (var p in _loader.Plugins) p.RaiseTick(); }
    internal void TestRaiseClosing()      { foreach (var p in _loader.Plugins) p.RaiseClosing(); }
    internal void TestRaisePlayerPositionChanged(int x, int y, int z) { foreach (var p in _loader.Plugins) p.RaisePlayerPositionChanged(x, y, z); }
    internal void TestRaiseMouse(int button, int wheel) { foreach (var p in _loader.Plugins) p.RaiseMouse(button, wheel); }
    internal bool TestRaiseHotkey(int key, int mod, bool pressed)
    {
        bool allow = true;
        foreach (var p in _loader.Plugins)
            if (!p.RaiseHotkey(key, mod, pressed)) allow = false;
        return allow;
    }
    internal bool TestRaisePacketIn(ReadOnlySpan<byte> data)
    {
        bool block = false;
        foreach (var p in _loader.Plugins)
            p.RaisePacket(data, ref block, incoming: true);
        return !block;
    }

    public int Run(string[] args)
    {
        if (!TryResolveCuoLib(out var libPath))
        {
            Console.Error.WriteLine($"[BootstrapHost] cuo native binary not found in {AppContext.BaseDirectory}. Looked for: {string.Join(", ", CuoLibCandidates())}");
            return 2;
        }

        Console.WriteLine($"[BootstrapHost] loading cuo from {libPath}");
        _cuoHandle = NativeLibrary.Load(libPath);

        if (!NativeLibrary.TryGetExport(_cuoHandle, "Initialize", out var initPtr))
        {
            Console.Error.WriteLine($"[BootstrapHost] '{Path.GetFileName(libPath)}' does not export 'Initialize'. Was it built with NativeAOT?");
            return 3;
        }

        var pluginsRoot = Path.Combine(AppContext.BaseDirectory, "Data", "Plugins");
        _loader.DiscoverAndLoad(pluginsRoot);
        Console.WriteLine($"[BootstrapHost] loaded {_loader.Plugins.Count} plugin(s)");

        var argvManaged = new IntPtr[args.Length];
        for (int i = 0; i < args.Length; i++)
            argvManaged[i] = Marshal.StringToHGlobalAnsi(args[i]);

        var hostBindings = BuildHostBindings();

        try
        {
            fixed (IntPtr* argvPtr = argvManaged)
            {
                var init = (delegate* unmanaged[Cdecl]<IntPtr*, int, HostBindings*, void>)initPtr;
                init(argvPtr, args.Length, &hostBindings);
            }
        }
        finally
        {
            for (int i = 0; i < argvManaged.Length; i++)
                if (argvManaged[i] != IntPtr.Zero)
                    Marshal.FreeHGlobal(argvManaged[i]);
        }

        return 0;
    }

    private static string[] CuoLibCandidates()
    {
        // NativeAOT shared-lib publishes emit cuo.dll/.so/.dylib; the bare
        // names cover NativeAOT exe publishes that still expose Initialize.
        if (OperatingSystem.IsWindows()) return ["cuo.dll", "cuo.exe"];
        if (OperatingSystem.IsMacOS())   return ["cuo.dylib", "cuo"];
        return ["cuo.so", "cuo"];
    }

    private static bool TryResolveCuoLib(out string path)
    {
        foreach (var name in CuoLibCandidates())
        {
            var candidate = Path.Combine(AppContext.BaseDirectory, name);
            if (File.Exists(candidate)) { path = candidate; return true; }
        }
        path = string.Empty;
        return false;
    }

    private static HostBindings BuildHostBindings() => new()
    {
        InitializeFn      = (nint)(delegate* unmanaged[Cdecl]<nint, void>)              &OnInitialize,
        LoadPluginFn      = (nint)(delegate* unmanaged[Cdecl]<nint, uint, nint, nint, void>) &OnLoadPlugin,
        TickFn            = (nint)(delegate* unmanaged[Cdecl]<void>)                    &OnTick,
        ClosingFn         = (nint)(delegate* unmanaged[Cdecl]<void>)                    &OnClosing,
        FocusGainedFn     = (nint)(delegate* unmanaged[Cdecl]<void>)                    &OnFocusGained,
        FocusLostFn       = (nint)(delegate* unmanaged[Cdecl]<void>)                    &OnFocusLost,
        ConnectedFn       = (nint)(delegate* unmanaged[Cdecl]<void>)                    &OnConnected,
        DisconnectedFn    = (nint)(delegate* unmanaged[Cdecl]<void>)                    &OnDisconnected,
        HotkeyFn          = (nint)(delegate* unmanaged[Cdecl]<int, int, byte, byte>)    &OnHotkey,
        MouseFn           = (nint)(delegate* unmanaged[Cdecl]<int, int, void>)          &OnMouse,
        CmdListFn         = (nint)(delegate* unmanaged[Cdecl]<nint*, int*, void>)       &OnCmdList,
        SdlEventFn        = (nint)(delegate* unmanaged[Cdecl]<nint, int>)               &OnSdlEvent,
        UpdatePlayerPosFn = (nint)(delegate* unmanaged[Cdecl]<int, int, int, void>)     &OnUpdatePlayerPos,
        PacketInFn        = (nint)(delegate* unmanaged[Cdecl]<nint, int*, byte>)        &OnPacketIn,
        PacketOutFn       = (nint)(delegate* unmanaged[Cdecl]<nint, int*, byte>)        &OnPacketOut,
    };

    // ─── cuo → host callbacks ────────────────────────────────────────────────
    // [UnmanagedCallersOnly] methods must be static; they all forward to the
    // singleton. cuo's calling thread is treated as the game thread.

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnInitialize(nint clientBindingsPtr) => _instance?.HandleInitialize(clientBindingsPtr);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnLoadPlugin(nint pathPtr, uint clientVer, nint assetsPtr, nint sdlWindow)
        => _instance?.HandleLoadPlugin(pathPtr, clientVer, assetsPtr, sdlWindow);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnTick() => _instance?.HandleTick();

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnClosing() => _instance?.HandleClosing();

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnFocusGained() => _instance?.RaiseEachPlugin(p => p.RaiseFocusGained());

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnFocusLost() => _instance?.RaiseEachPlugin(p => p.RaiseFocusLost());

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnConnected() => _instance?.RaiseEachPlugin(p => p.RaiseConnected());

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnDisconnected() => _instance?.RaiseEachPlugin(p => p.RaiseDisconnected());

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte OnHotkey(int key, int mod, byte pressed)
        => _instance?.HandleHotkey(key, mod, pressed != 0) ?? 1;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnMouse(int button, int wheel) => _instance?.HandleMouse(button, wheel);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnCmdList(nint* listOut, int* lenOut)
    {
        if (listOut != null) *listOut = 0;
        if (lenOut  != null) *lenOut  = 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OnSdlEvent(nint ev) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnUpdatePlayerPos(int x, int y, int z)
        => _instance?.RaiseEachPlugin(p => p.RaisePlayerPositionChanged(x, y, z));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte OnPacketIn(nint data, int* lengthRef)
        => _instance?.HandlePacket(data, lengthRef, incoming: true) ?? 1;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte OnPacketOut(nint data, int* lengthRef)
        => _instance?.HandlePacket(data, lengthRef, incoming: false) ?? 1;

    // ─── instance handlers ───────────────────────────────────────────────────

    private void HandleInitialize(nint clientBindingsPtr)
    {
        _gameThread = Thread.CurrentThread;
        _clientBindings = Unsafe.AsRef<ClientBindings>((void*)clientBindingsPtr);

        foreach (var p in _loader.Plugins)
            p.AttachHost();
    }

    private void HandleLoadPlugin(nint pathPtr, uint clientVer, nint assetsPtr, nint sdlWindow)
    {
        // v2 plugins are discovered from Data/Plugins/ in DiscoverAndLoad;
        // cuo's per-path -plugins callback is a v1 path we ignore.
        _ = pathPtr; _ = clientVer; _ = assetsPtr; _ = sdlWindow;
    }

    private void HandleTick()
    {
        DrainGameThreadQueue();
        foreach (var p in _loader.Plugins)
            p.RaiseTick();
    }

    private void HandleClosing()
    {
        foreach (var p in _loader.Plugins)
            p.RaiseClosing();

        _loader.ShutdownAll();
    }

    private void HandleMouse(int button, int wheel)
    {
        foreach (var p in _loader.Plugins)
            p.RaiseMouse(button, wheel);
    }

    private byte HandleHotkey(int key, int mod, bool pressed)
    {
        bool allow = true;
        foreach (var p in _loader.Plugins)
            if (!p.RaiseHotkey(key, mod, pressed))
                allow = false;
        return allow ? (byte)1 : (byte)0;
    }

    private byte HandlePacket(nint data, int* lengthRef, bool incoming)
    {
        var length = *lengthRef;
        var span = new ReadOnlySpan<byte>((byte*)data, length);

        bool block = false;
        foreach (var p in _loader.Plugins)
            p.RaisePacket(span, ref block, incoming);

        // Plugins observe-and-block only; the span aliases cuo's network
        // buffer and is not rewritten back.
        return block ? (byte)0 : (byte)1;
    }

    public void PostToGameThread(Action action)
    {
        if (IsGameThread)
        {
            try { action(); } catch (Exception ex) { Console.Error.WriteLine($"[BootstrapHost] queued action threw: {ex}"); }
            return;
        }
        _gameThreadQueue.Enqueue(action);
    }

    private void DrainGameThreadQueue()
    {
        while (_gameThreadQueue.TryDequeue(out var next))
        {
            try { next(); } catch (Exception ex) { Console.Error.WriteLine($"[BootstrapHost] queued action threw: {ex}"); }
        }
    }

    private void RaiseEachPlugin(Action<PluginContextImpl> action)
    {
        foreach (var p in _loader.Plugins)
            action(p);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct HostBindings
{
    public nint InitializeFn;
    public nint LoadPluginFn;
    public nint TickFn;
    public nint ClosingFn;
    public nint FocusGainedFn;
    public nint FocusLostFn;
    public nint ConnectedFn;
    public nint DisconnectedFn;
    public nint HotkeyFn;
    public nint MouseFn;
    public nint CmdListFn;
    public nint SdlEventFn;
    public nint UpdatePlayerPosFn;
    public nint PacketInFn;
    public nint PacketOutFn;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ClientBindings
{
    public nint PluginRecvFn;          // bool(nint data, ref int length) — inject to client
    public nint PluginSendFn;          // bool(nint data, ref int length) — inject to server
    public nint PacketLengthFn;        // short(int id)
    public nint CastSpellFn;           // void(int index)
    public nint SetWindowTitleFn;      // void(nint utf8Ptr)
    public nint GetClilocFn;           // nint(int id, nint utf8ArgsPtr, bool capitalize)
    public nint RequestMoveFn;         // bool(int dir, bool run)
    public nint GetPlayerPositionFn;   // bool(out int x, out int y, out int z)
    public nint ReflectionCmdFn;       // legacy reflection commands; unused by v2
}
