using System;
using Extism.Sdk;

namespace ClassicUO.Ecs;

class Mod : IDisposable
{
    private readonly Plugin _plugin;

    public Mod(Plugin plugin)
    {
        _plugin = plugin;
    }

    public Plugin Plugin => _plugin;


    public void Dispose()
    {
        _plugin.Dispose();
    }
}