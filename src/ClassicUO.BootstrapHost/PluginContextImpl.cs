// SPDX-License-Identifier: BSD-2-Clause

using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.PluginApi;

namespace ClassicUO.BootstrapHost;

/// <summary>One <see cref="IPluginContext"/> per loaded plugin.</summary>
internal sealed class PluginContextImpl : IPluginContext
{
    private IPlugin? _plugin;

    private readonly PacketPipelineImpl _packets;
    private readonly InputRouterImpl _input;
    private readonly GameActionsImpl _actions;
    private readonly ClientImpl _client;
    private readonly DispatcherImpl _dispatcher;

    public PluginContextImpl(HostBridge bridge, PluginRegistration registration, string folderName, string pluginsRoot)
    {
        Id = registration.Id;
        UODataPath = ResolveUODataPath();
        PluginDataPath = EnsurePluginDataPath(pluginsRoot, folderName);

        _packets    = new PacketPipelineImpl(bridge);
        _input      = new InputRouterImpl();
        _actions    = new GameActionsImpl(bridge);
        _client     = new ClientImpl(bridge);
        _dispatcher = new DispatcherImpl(bridge);
    }

    public string Id { get; }
    public string UODataPath { get; }
    public string PluginDataPath { get; }
    public uint ClientVersion { get; private set; }

    public IPacketPipeline Packets => _packets;
    public IInputRouter Input => _input;
    public IGameActions Actions => _actions;
    public IClient Client => _client;
    public IDispatcher Game => _dispatcher;

    public event Action? Connected;
    public event Action? Disconnected;
    public event Action? FocusGained;
    public event Action? FocusLost;
    public event Action<int, int, int>? PlayerPositionChanged;
    public event Action? Tick;
    public event Action? Closing;

    public void AttachPlugin(IPlugin plugin)
    {
        _plugin = plugin;
    }

    public void AttachHost()
    {
        // Invoked after HostBridge.OnInitialize so the bridge has its game
        // thread and ClientBindings populated when the plugin first runs.
        _plugin?.OnInitialize(this);
    }

    public void RaiseShutdown()
    {
        _plugin?.OnShutdown();
    }

    public void RaiseConnected()             => Connected?.Invoke();
    public void RaiseDisconnected()          => Disconnected?.Invoke();
    public void RaiseFocusGained()           => FocusGained?.Invoke();
    public void RaiseFocusLost()             => FocusLost?.Invoke();
    public void RaisePlayerPositionChanged(int x, int y, int z) => PlayerPositionChanged?.Invoke(x, y, z);
    public void RaiseTick()                  => Tick?.Invoke();
    public void RaiseClosing()               => Closing?.Invoke();
    public void RaiseMouse(int button, int wheel) => _input.RaiseMouse(button, wheel);
    public bool RaiseHotkey(int key, int mod, bool pressed) => _input.RaiseHotkey(key, mod, pressed);
    public void RaisePacket(ReadOnlySpan<byte> data, ref bool block, bool incoming) => _packets.Raise(data, ref block, incoming);

    private static string ResolveUODataPath() => AppContext.BaseDirectory;

    private static string EnsurePluginDataPath(string pluginsRoot, string folderName)
    {
        var path = Path.Combine(pluginsRoot, folderName);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Test-only: lets the smoke test inspect the loaded plugin instance.</summary>
    internal IPlugin? PluginInstance => _plugin;
}

internal sealed class PacketPipelineImpl : IPacketPipeline
{
    private readonly HostBridge _bridge;

    public PacketPipelineImpl(HostBridge bridge) { _bridge = bridge; }

    public event PacketHandler? Incoming;
    public event PacketHandler? Outgoing;

    public void SendToClient(ReadOnlySpan<byte> data) => Inject(data, _bridge.ClientBindings.PluginRecvFn);
    public void SendToServer(ReadOnlySpan<byte> data) => Inject(data, _bridge.ClientBindings.PluginSendFn);

    public unsafe short GetPacketLength(byte packetId)
    {
        var fn = _bridge.ClientBindings.PacketLengthFn;
        if (fn == 0) return 0;
        return ((delegate* unmanaged[Cdecl]<int, short>)fn)(packetId);
    }

    public void Raise(ReadOnlySpan<byte> data, ref bool block, bool incoming)
    {
        // Direct multicast invoke: a ref-struct (Span<T>) can't cross the
        // GetInvocationList iterator boundary. The PacketHandler contract
        // makes block sticky-true so multicast aggregation is safe here.
        var handler = incoming ? Incoming : Outgoing;
        handler?.Invoke(data, ref block);
    }

    private static unsafe void Inject(ReadOnlySpan<byte> data, nint fnPtr)
    {
        if (fnPtr == 0 || data.IsEmpty) return;
        var fn = (delegate* unmanaged[Cdecl]<nint, int*, byte>)fnPtr;
        fixed (byte* p = data)
        {
            int len = data.Length;
            fn((nint)p, &len);
        }
    }
}

internal sealed class InputRouterImpl : IInputRouter
{
    public event HotkeyHandler? Hotkey;
    public event MouseHandler? Mouse;

    public bool RaiseHotkey(int key, int mod, bool pressed)
    {
        var h = Hotkey;
        if (h is null) return true;
        bool allow = true;
        foreach (HotkeyHandler d in h.GetInvocationList())
            if (!d(key, mod, pressed)) allow = false;
        return allow;
    }

    public void RaiseMouse(int button, int wheel) => Mouse?.Invoke(button, wheel);
}

internal sealed class GameActionsImpl : IGameActions
{
    private readonly HostBridge _bridge;
    public GameActionsImpl(HostBridge bridge) { _bridge = bridge; }

    public unsafe void CastSpell(int spellIndex)
    {
        var fn = _bridge.ClientBindings.CastSpellFn;
        if (fn == 0) return;
        if (_bridge.IsGameThread)
            ((delegate* unmanaged[Cdecl]<int, void>)fn)(spellIndex);
        else
            _bridge.PostToGameThread(() => ((delegate* unmanaged[Cdecl]<int, void>)fn)(spellIndex));
    }

    public unsafe bool RequestMove(int direction, bool run)
    {
        var fn = _bridge.ClientBindings.RequestMoveFn;
        if (fn == 0) return false;
        if (!_bridge.IsGameThread)
            throw new InvalidOperationException("RequestMove must be called from the game thread; use IDispatcher.Post.");
        return ((delegate* unmanaged[Cdecl]<int, byte, byte>)fn)(direction, run ? (byte)1 : (byte)0) != 0;
    }

    public unsafe bool TryGetPlayerPosition(out int x, out int y, out int z)
    {
        var fn = _bridge.ClientBindings.GetPlayerPositionFn;
        x = y = z = 0;
        if (fn == 0) return false;
        int xx = 0, yy = 0, zz = 0;
        bool ok = ((delegate* unmanaged[Cdecl]<int*, int*, int*, byte>)fn)(&xx, &yy, &zz) != 0;
        x = xx; y = yy; z = zz;
        return ok;
    }
}

internal sealed class ClientImpl : IClient
{
    private readonly HostBridge _bridge;
    public ClientImpl(HostBridge bridge) { _bridge = bridge; }

    public unsafe void SetWindowTitle(string title)
    {
        var fn = _bridge.ClientBindings.SetWindowTitleFn;
        if (fn == 0 || title is null) return;

        var byteCount = Encoding.UTF8.GetByteCount(title);
        Span<byte> buf = byteCount + 1 <= 1024
            ? stackalloc byte[byteCount + 1]
            : new byte[byteCount + 1];
        Encoding.UTF8.GetBytes(title, buf);
        buf[byteCount] = 0;
        fixed (byte* p = buf)
            ((delegate* unmanaged[Cdecl]<nint, void>)fn)((nint)p);
    }

    public unsafe string? GetCliloc(int id, string args = "", bool capitalize = false)
    {
        var fn = _bridge.ClientBindings.GetClilocFn;
        if (fn == 0) return null;

        var argsPtr = string.IsNullOrEmpty(args)
            ? nint.Zero
            : Marshal.StringToHGlobalAnsi(args);
        try
        {
            var resultPtr = ((delegate* unmanaged[Cdecl]<int, nint, byte, nint>)fn)(id, argsPtr, capitalize ? (byte)1 : (byte)0);
            return resultPtr == 0 ? null : Marshal.PtrToStringAnsi(resultPtr);
        }
        finally
        {
            if (argsPtr != 0) Marshal.FreeHGlobal(argsPtr);
        }
    }
}

internal sealed class DispatcherImpl : IDispatcher
{
    private readonly HostBridge _bridge;
    public DispatcherImpl(HostBridge bridge) { _bridge = bridge; }

    public bool IsGameThread => _bridge.IsGameThread;

    public void Post(Action action) => _bridge.PostToGameThread(action);

    public Task PostAsync(Action action)
    {
        if (_bridge.IsGameThread)
        {
            try { action(); return Task.CompletedTask; }
            catch (Exception ex) { return Task.FromException(ex); }
        }
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _bridge.PostToGameThread(() =>
        {
            try { action(); tcs.SetResult(); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }
}
