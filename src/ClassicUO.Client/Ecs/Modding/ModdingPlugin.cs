using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Network;
using Extism.Sdk;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct ModdingPlugins : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new List<Mod>());

        scheduler.OnStartup((
            Res<List<Mod>> mods,
            Res<NetClient> network,
            Res<PacketsMap> packetMap,
            Res<Settings> settings
        ) =>
        {
            Extism.Sdk.Plugin.ConfigureFileLogging("stdout", LogLevel.Info);
            Console.WriteLine("extism version: {0}", Extism.Sdk.Plugin.ExtismVersion());

            foreach (var path in settings.Value.Plugins)
            {
                var isUrl = Uri.TryCreate(path, UriKind.Absolute, out var uri);

                if (!isUrl && !File.Exists(path))
                {
                    Console.WriteLine("{0} not found", path);
                    continue;
                }

                Extism.Sdk.Plugin plugin = null;

                HostFunction[] functions = [
                    HostFunction.FromMethod("cuo_get_packet_size", null, (CurrentPlugin p, long offset) =>
                    {
                        var span = p.ReadBytes(offset);
                        var size = network.Value.PacketsTable.GetPacketLength(span[0]);
                        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref size, 1));
                        return p.WriteBytes(bytes);
                    }),

                    HostFunction.FromMethod("cuo_send_to_server", null, (CurrentPlugin p, long offset) => {
                        var packet = p.ReadBytes(offset);
                        network.Value.Send(packet, true);
                    }),

                    HostFunction.FromMethod("cuo_add_packet_handler", null, (CurrentPlugin p, long offset) => {
                        var handlerDescriptionSpan = p.ReadBytes(offset);
                        var packetId = handlerDescriptionSpan[0];
                        var funcName = Encoding.UTF8.GetString(handlerDescriptionSpan.Slice(1));

                        packetMap.Value[packetId] = buffer => {
                            plugin.Call(funcName, buffer);
                        };
                    })
                ];

                var manifest = new Manifest(uri?.IsFile ?? true ? new PathWasmSource(path) : new UrlWasmSource(uri));
                plugin = new Extism.Sdk.Plugin(manifest, functions, true);

                if (!plugin.FunctionExists("register"))
                {
                    Console.WriteLine("{0} is not a valid wasm plugin", path);
                    plugin.Dispose();
                    continue;
                }

                var mod = new Mod(plugin);
                mods.Value.Add(mod);
                var result = mod.Plugin.Call("register", []);
            }

        });
    }
}