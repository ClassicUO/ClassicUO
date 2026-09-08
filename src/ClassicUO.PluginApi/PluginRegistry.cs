// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.PluginApi;

/// <summary>
/// Static registration target for v2 plugins. A plugin assembly registers
/// itself from a <c>[ModuleInitializer]</c> static method; the host runs the
/// module ctor and then drains the pending entries. Reflection-free and
/// trim-/AOT-friendly.
/// </summary>
public static class PluginRegistry
{
    private static readonly List<PluginRegistration> _entries = new();
    private static int _consumed;
    private static readonly Lock _gate = new();

    /// <summary>
    /// Registers a plugin. Call from a <c>[ModuleInitializer]</c> so
    /// registration happens during assembly load.
    /// </summary>
    public static void Register(PluginRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        lock (_gate)
            _entries.Add(registration);
    }

    /// <summary>
    /// Host-only: returns and consumes any registrations added since the last
    /// drain. Called between plugin module ctors so each batch can be
    /// attributed to the originating assembly.
    /// </summary>
    public static PluginRegistration[] DrainPending()
    {
        lock (_gate)
        {
            var pending = _entries.Count - _consumed;
            if (pending == 0) return Array.Empty<PluginRegistration>();
            var slice = new PluginRegistration[pending];
            _entries.CopyTo(_consumed, slice, 0, pending);
            _consumed = _entries.Count;
            return slice;
        }
    }
}

/// <summary>
/// Identity metadata plus the factory the host calls once at discovery to
/// construct the <see cref="IPlugin"/> instance.
/// </summary>
public sealed class PluginRegistration
{
    public PluginRegistration(string id, Func<IPlugin> factory, string? name = null, string? version = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Plugin id must be non-empty.", nameof(id));
        Id = id;
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Name = name;
        Version = version;
        Description = description;
    }

    /// <summary>Stable identifier; used for log lines and per-plugin data dirs.</summary>
    public string Id { get; }

    /// <summary>Human-readable name. Defaults to <see cref="Id"/> when null.</summary>
    public string? Name { get; }

    /// <summary>Semver version string. Informational; not enforced by the host.</summary>
    public string? Version { get; }

    /// <summary>One-line description shown in any plugin listing UI.</summary>
    public string? Description { get; }

    /// <summary>Constructs the plugin instance. Invoked once at discovery.</summary>
    public Func<IPlugin> Factory { get; }
}
