// SPDX-License-Identifier: BSD-2-Clause

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

using ClassicUO.PluginApi;

namespace ClassicUO.BootstrapHost;

/// <summary>
/// Discovers plugins under <c>Data/Plugins/&lt;name&gt;/&lt;name&gt;.dll</c>,
/// loads each into its own non-collectible <see cref="AssemblyLoadContext"/>,
/// forces the module constructor to run, and creates a
/// <see cref="PluginContextImpl"/> for each entry plugins pushed onto
/// <see cref="PluginRegistry"/>.
/// </summary>
internal sealed class PluginLoader
{
    private readonly HostBridge _bridge;
    private readonly List<PluginContextImpl> _plugins = [];

    public PluginLoader(HostBridge bridge)
    {
        _bridge = bridge;
    }

    public IReadOnlyList<PluginContextImpl> Plugins => _plugins;

    public void DiscoverAndLoad(string pluginsRoot)
    {
        if (!Directory.Exists(pluginsRoot))
            return;

        foreach (var dir in Directory.EnumerateDirectories(pluginsRoot))
        {
            var name = Path.GetFileName(dir);
            var dllPath = Path.Combine(dir, $"{name}.dll");
            if (!File.Exists(dllPath))
                continue;

            try
            {
                LoadPlugin(pluginsRoot, name, dllPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[BootstrapHost] failed to load plugin '{name}': {ex.Message}");
            }
        }
    }

    private void LoadPlugin(string pluginsRoot, string folderName, string dllPath)
    {
        var alc = new PluginAssemblyLoadContext(folderName, dllPath);
        var asm = alc.LoadFromAssemblyPath(dllPath);

        // The host never touches plugin types directly, so the runtime would
        // never run the module ctor on its own. Force it now so
        // [ModuleInitializer] registrations land before DrainPending.
        RuntimeHelpers.RunModuleConstructor(asm.ManifestModule.ModuleHandle);

        var registrations = PluginRegistry.DrainPending();
        if (registrations.Length == 0)
        {
            Console.Error.WriteLine(
                $"[BootstrapHost] '{folderName}': no plugins registered. Add a [ModuleInitializer] that calls PluginRegistry.Register.");
            return;
        }

        foreach (var registration in registrations)
        {
            var instance = registration.Factory();
            var ctx = new PluginContextImpl(_bridge, registration, folderName, pluginsRoot);
            ctx.AttachPlugin(instance);
            _plugins.Add(ctx);
        }
    }

    public void ShutdownAll()
    {
        foreach (var p in _plugins)
        {
            try { p.RaiseShutdown(); }
            catch (Exception ex) { Console.Error.WriteLine($"[BootstrapHost] shutdown of '{p.Id}' threw: {ex}"); }
        }
    }
}

/// <summary>
/// Per-plugin <see cref="AssemblyLoadContext"/> rooted at the plugin's
/// folder. Resolves dependencies against the plugin's local files first, then
/// falls back to the default ALC for shared contracts (<c>ClassicUO.PluginApi</c>).
/// </summary>
internal sealed class PluginAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginAssemblyLoadContext(string name, string mainAssemblyPath)
        : base(name, isCollectible: false)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // The contract DLL must resolve to a single Assembly instance across
        // every plugin, otherwise interface identity and PluginRegistry's
        // static state break. Defer to the default ALC.
        if (assemblyName.Name == "ClassicUO.PluginApi")
            return null;

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is not null ? LoadUnmanagedDllFromPath(path) : 0;
    }
}
