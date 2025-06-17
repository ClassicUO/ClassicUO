using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
            Res<Settings> settings,
            Res<NetworkEntitiesMap> networkEntities,
            Res<GameContext> gameCtx
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
                    HostFunction.FromMethod("cuo_get_packet_size", null, (CurrentPlugin p, long offset) => {
                        var span = p.ReadBytes(offset);
                        var size = network.Value.PacketsTable.GetPacketLength(span[0]);
                        return p.WriteBytes(size.AsBytes());
                    }),

                    HostFunction.FromMethod("cuo_send_to_server", null, (CurrentPlugin p, long offset) => {
                        var packet = p.ReadBytes(offset);
                        network.Value.Send(packet, true);
                    }),

                    HostFunction.FromMethod("cuo_add_packet_handler", null, (CurrentPlugin p, long offset) => {
                        var handlerInfo = JsonSerializer.Deserialize(p.ReadBytes(offset), PluginJsonContext.Default.PacketHandlerInfo);
                        packetMap.Value[handlerInfo.PacketId] = buffer => {
                            plugin.Call(handlerInfo.FuncName, buffer);
                        };
                    }),


                    HostFunction.FromMethod("cuo_get_player_serial", null, p => {
                        return p.WriteBytes(gameCtx.Value.PlayerSerial.AsBytes());
                    }),

                    archAsJson("cuo_ecs_get_components", networkEntities),


                    ..bind<Graphic>("entity_graphic", networkEntities),
                    ..bind<Hue>("entity_hue", networkEntities),
                    ..bind<Facing>("entity_direction", networkEntities),
                    ..bind<WorldPosition>("entity_position", networkEntities),
                ];



                static IEnumerable<HostFunction> bind<T>(string postfix, NetworkEntitiesMap networkEntities)
                    where T : struct
                {
                    var ctx = (JsonTypeInfo<T>)PluginJsonContext.Default.GetTypeInfo(typeof(T));
                    yield return serializeProps<T>("cuo_get_" + postfix, networkEntities, ctx);
                    yield return deserializeProps<T>("cuo_set_" + postfix, networkEntities, ctx);
                }

                static HostFunction serializeProps<T>(string name, NetworkEntitiesMap networkEntities, JsonTypeInfo<T> ctx)
                    where T : struct
                {
                    ArgumentNullException.ThrowIfNull(ctx);

                    return HostFunction.FromMethod(name, null,
                    (CurrentPlugin p, long offset) =>
                        {
                            var serial = p.ReadBytes(offset).As<uint>();
                            var ent = networkEntities.Get(serial);
                            if (ent == 0 || !ent.Has<T>())
                                return p.WriteString("{}");

                            var json = JsonSerializer.Serialize(ent.Get<T>(), ctx);
                            return p.WriteString(json);
                        });
                }

                static HostFunction deserializeProps<T>(string name, NetworkEntitiesMap networkEntities, JsonTypeInfo<T> ctx)
                    where T : struct
                {
                    ArgumentNullException.ThrowIfNull(ctx);

                    return HostFunction.FromMethod(name, null,
                    (CurrentPlugin p, long keyOffset, long valueOffset) =>
                        {
                            var serial = p.ReadBytes(keyOffset).As<uint>();
                            var ent = networkEntities.Get(serial);
                            if (ent == 0)
                                return;

                            var value = p.ReadBytes(valueOffset);
                            var val = JsonSerializer.Deserialize(value, ctx);
                            ent.Set(val);
                        });
                }

                static HostFunction archAsJson(string name, NetworkEntitiesMap networkEntities)
                {
                    return HostFunction.FromMethod(name, null,
                    (CurrentPlugin p, long offset) =>
                        {
                            var serial = p.ReadBytes(offset).As<uint>();
                            var ent = networkEntities.Get(serial);
                            if (ent == 0)
                                return p.WriteString("{}");

                            var arch = ent.Archetype;
                            if (arch == null)
                                return p.WriteString("{}");

                            var json = JsonSerializer.Serialize(ent.Archetype.All, PluginJsonContext.Default.ComponentInfoArray);
                            return p.WriteString(json);
                        });
                }

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


[JsonSourceGenerationOptions(IncludeFields = true, PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ComponentInfo[]), GenerationMode = JsonSourceGenerationMode.Serialization)]

// internals
[JsonSerializable(typeof(PacketHandlerInfo), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(MousePosProxy), GenerationMode = JsonSourceGenerationMode.Default)]


// components
[JsonSerializable(typeof(WorldPosition), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Graphic), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Hue), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Facing), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(EquipmentSlots), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Hitpoints), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Mana), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Stamina), GenerationMode = JsonSourceGenerationMode.Default)]
internal partial class PluginJsonContext : JsonSerializerContext { }



internal record struct PacketHandlerInfo(byte PacketId, string FuncName);
internal record struct MousePosProxy(string Button, string Pressed);