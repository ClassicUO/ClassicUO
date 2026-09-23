// SPDX-License-Identifier: BSD-2-Clause

using System.Runtime.CompilerServices;

using ClassicUO.PluginApi;

namespace HelloPlugin;

/// <summary>
/// Module-load registration for the sample plugin. The
/// <see cref="ModuleInitializerAttribute"/> ensures <see cref="Register"/>
/// runs as soon as the host triggers the module constructor — no reflection,
/// no <c>[Plugin]</c> attribute scan.
/// </summary>
internal static class HelloPluginRegistration
{
    // CA2255 warns that ModuleInitializer is unusual in libraries; for v2
    // plugins it is the documented contract — the host triggers the module
    // ctor explicitly so that registration runs at discovery without
    // reflection.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Register()
    {
        PluginRegistry.Register(new PluginRegistration(
            id: "HelloPlugin",
            factory: static () => new HelloPlugin(),
            name: "Hello Plugin",
            version: "1.0.0",
            description: "Sample plugin that logs every event for the smoke tests."));
    }
}

/// <summary>
/// Minimal sample plugin used by the smoke tests. Subscribes to every
/// lifecycle event the v2 contract exposes and appends a one-line entry to
/// the log path provided via the <c>CUO_PLUGIN_TEST_LOG</c> environment
/// variable. Real plugins would do something more interesting with the
/// events, but the surface exercised here mirrors any well-behaved plugin.
/// </summary>
public sealed class HelloPlugin : IPlugin
{
    private string? _logPath;
    private readonly object _logLock = new();

    public void OnInitialize(IPluginContext context)
    {
        _logPath = Environment.GetEnvironmentVariable("CUO_PLUGIN_TEST_LOG");
        Log("OnInitialize");

        context.Connected             += () => Log("Connected");
        context.Disconnected          += () => Log("Disconnected");
        context.FocusGained           += () => Log("FocusGained");
        context.FocusLost             += () => Log("FocusLost");
        context.PlayerPositionChanged += (x, y, z) => Log($"Pos:{x},{y},{z}");
        context.Tick                  += () => Log("Tick");
        context.Closing               += () => Log("Closing");

        context.Input.Mouse  += (button, wheel) => Log($"Mouse:{button}/{wheel}");
        context.Input.Hotkey += (key, mod, pressed) =>
        {
            Log($"Hotkey:{key}/{mod}/{pressed}");
            // Block the dedicated test key 999; allow everything else.
            return key != 999;
        };

        context.Packets.Incoming += (ReadOnlySpan<byte> p, ref bool block) =>
        {
            Log($"PacketIn:len={p.Length},id=0x{(p.Length > 0 ? p[0] : (byte)0):X2}");
            // Block packet id 0x99 as a test signal.
            if (p.Length > 0 && p[0] == 0x99) block = true;
        };
    }

    public void OnShutdown() => Log("OnShutdown");

    private void Log(string line)
    {
        if (_logPath is null) return;
        lock (_logLock)
            File.AppendAllText(_logPath, line + Environment.NewLine);
    }
}
