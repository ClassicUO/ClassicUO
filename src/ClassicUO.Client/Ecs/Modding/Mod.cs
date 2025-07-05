using System;
using Extism.Sdk;

namespace ClassicUO.Ecs;

internal class Mod(Plugin plugin) : IDisposable
{
    public Plugin Plugin { get; } = plugin;

    public void Dispose() => Plugin.Dispose();
}
