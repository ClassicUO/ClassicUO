// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.PluginApi;

/// <summary>
/// Implemented by every plugin. Register each implementation with
/// <see cref="PluginRegistry.Register"/> from a
/// <see cref="System.Runtime.CompilerServices.ModuleInitializerAttribute"/>-decorated
/// static method.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Called once after the plugin assembly loads. <paramref name="context"/>
    /// is the plugin's only handle to game events and client services and is
    /// valid until <see cref="OnShutdown"/> returns.
    /// </summary>
    void OnInitialize(IPluginContext context);

    /// <summary>
    /// Called once during a clean shutdown. After this returns the host may
    /// dispose the context and unload the plugin's assembly load context.
    /// </summary>
    void OnShutdown();
}
