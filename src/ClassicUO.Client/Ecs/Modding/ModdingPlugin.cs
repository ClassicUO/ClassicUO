using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using Extism.Sdk;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serde;
using Serde.Json;
using TinyEcs;

namespace ClassicUO.Ecs;


internal readonly struct ModdingPlugins : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<HostMessage>();
        scheduler.AddEvent<(Mod, PluginMessage)>();

        scheduler.OnStartup((
            World world,
            EventWriter<(Mod, PluginMessage)> writer,
            Res<NetClient> network,
            Res<PacketsMap> packetMap,
            Res<Settings> settings,
            Res<NetworkEntitiesMap> networkEntities,
            Res<GameContext> gameCtx,
            Res<GraphicsDevice> device
        ) =>
        {
            Extism.Sdk.Plugin.ConfigureFileLogging("stdout", LogLevel.Info);
            Console.WriteLine("extism version: {0}", Extism.Sdk.Plugin.ExtismVersion());
            var requiredFunctions = new[] { "on_init", "on_update", "on_event" };

            foreach (var path in settings.Value.Plugins)
            {
                var isUrl = Uri.TryCreate(path, UriKind.Absolute, out var uri);

                if (!isUrl && !File.Exists(path))
                {
                    Console.WriteLine("{0} not found", path);
                    continue;
                }

                Mod mod = null;
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

                    // HostFunction.FromMethod("cuo_add_packet_handler", null, (CurrentPlugin p, long offset) => {
                    //     var handlerInfo = JsonSerializer.Deserialize(p.ReadBytes(offset), PluginJsonContext.Default.PacketHandlerInfo);
                    //     packetMap.Value[handlerInfo.PacketId] = buffer => {
                    //         plugin.Call(handlerInfo.FuncName, buffer);
                    //     };
                    // }),


                    HostFunction.FromMethod("send_events", null, (CurrentPlugin p, long offset) => {
                        var events = p.ReadString(offset)
                            .FromJson<PluginMessages>();

                        foreach (var ev in events.Messages)
                            writer.Enqueue((mod, ev));
                    }),


                    HostFunction.FromMethod("cuo_get_player_serial", null, p => {
                        return p.WriteBytes(gameCtx.Value.PlayerSerial.AsBytes());
                    }),

                    archAsJson("cuo_ecs_get_components", networkEntities),


                    // ..bind<Graphic>("entity_graphic", networkEntities),
                    // ..bind<Hue>("entity_hue", networkEntities),
                    // ..bind<Facing>("entity_direction", networkEntities),
                    // ..bind<WorldPosition>("entity_position", networkEntities),
                ];



                // static IEnumerable<HostFunction> bind<T>(string postfix, NetworkEntitiesMap networkEntities)
                //     where T : struct
                // {
                //     var ctx = (JsonTypeInfo<T>)PluginJsonContext.Default.GetTypeInfo(typeof(T));
                //     yield return serializeProps<T>("cuo_get_" + postfix, networkEntities, ctx);
                //     yield return deserializeProps<T>("cuo_set_" + postfix, networkEntities, ctx);
                // }

                // static HostFunction serializeProps<T>(string name, NetworkEntitiesMap networkEntities, JsonTypeInfo<T> ctx)
                //     where T : struct
                // {
                //     ArgumentNullException.ThrowIfNull(ctx);

                //     return HostFunction.FromMethod(name, null,
                //     (CurrentPlugin p, long offset) =>
                //         {
                //             var serial = p.ReadBytes(offset).As<uint>();
                //             var ent = networkEntities.Get(serial);
                //             if (ent == 0 || !ent.Has<T>())
                //                 return p.WriteString("{}");

                //             var json = JsonSerializer.Serialize(ent.Get<T>(), ctx);
                //             return p.WriteString(json);
                //         });
                // }

                // static HostFunction deserializeProps<T>(string name, NetworkEntitiesMap networkEntities, JsonTypeInfo<T> ctx)
                //     where T : struct
                // {
                //     ArgumentNullException.ThrowIfNull(ctx);

                //     return HostFunction.FromMethod(name, null,
                //     (CurrentPlugin p, long keyOffset, long valueOffset) =>
                //         {
                //             var serial = p.ReadBytes(keyOffset).As<uint>();
                //             var ent = networkEntities.Get(serial);
                //             if (ent == 0)
                //                 return;

                //             var value = p.ReadBytes(valueOffset);
                //             var val = JsonSerializer.Deserialize(value, ctx);
                //             ent.Set(val);
                //         });
                // }

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

                            // var json = JsonSerializer.Serialize(ent.Archetype.All, PluginJsonContext.Default.ComponentInfoArray);
                            return p.WriteString("{}");
                        });
                }

                var manifest = new Manifest(uri?.IsFile ?? true ? new PathWasmSource(path) : new UrlWasmSource(uri));
                plugin = new Extism.Sdk.Plugin(manifest, functions, true);


                var ok = true;
                foreach (var fnName in requiredFunctions)
                {
                    if (!plugin.FunctionExists(fnName))
                    {
                        ok = false;
                        Console.WriteLine("{0} is not a valid wasm plugin. Missing {1} function", path, fnName);
                        break;
                    }
                }

                if (!ok)
                {
                    plugin.Dispose();
                    continue;
                }

                mod = new Mod(plugin);
                world.Entity()
                    .Set(new WasmMod() { Mod = mod });
            }

        });


        scheduler.OnAfterUpdate((Res<MouseContext> mouseCtx, Res<KeyboardContext> keyboardCtx, EventWriter<HostMessage> writer) =>
        {
            if (mouseCtx.Value.PositionOffset != Vector2.Zero)
            {
                writer.Enqueue(new HostMessage.MouseMove(mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y));
            }

            if (mouseCtx.Value.Wheel != 0)
            {
                writer.Enqueue(new HostMessage.MouseWheel(mouseCtx.Value.Wheel));
            }

            for (var button = Input.MouseButtonType.Left; button < Input.MouseButtonType.Size; button += 1)
            {
                if (mouseCtx.Value.IsPressed(button))
                {
                    writer.Enqueue(new HostMessage.MousePressed((int)button, mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y));
                }

                if (mouseCtx.Value.IsReleased(button))
                {
                    writer.Enqueue(new HostMessage.MouseReleased((int)button, mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y));
                }

                if (mouseCtx.Value.IsPressedDouble(button))
                {
                    writer.Enqueue(new HostMessage.MouseDoubleClick((int)button, mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y));
                }
            }

            for (var key = Keys.None + 1; key <= Keys.OemEnlW; key += 1)
            {
                if (keyboardCtx.Value.IsPressed(key))
                {
                    writer.Enqueue(new HostMessage.KeyPressed((int)key));
                }

                if (keyboardCtx.Value.IsReleased(key))
                {
                    writer.Enqueue(new HostMessage.KeyReleased((int)key));
                }
            }
        });

        var modInitFn = ModInitialize;
        scheduler.OnUpdate(modInitFn);

        var modUpdateFn = ModUpdate;
        scheduler.OnUpdate(modUpdateFn);

        var modEventsFn = ModEvents;
        scheduler.OnUpdate(modEventsFn)
            .RunIf((EventReader<HostMessage> reader) => !reader.IsEmpty);

        var modReadEventsFn = ReadModEvents;
        scheduler.OnUpdate(modReadEventsFn)
            .RunIf((EventReader<(Mod, PluginMessage)> reader) => !reader.IsEmpty);
    }

    private static void ModInitialize(Query<Data<WasmMod>, Without<WasmInitialized>> query)
    {
        var pluginVersion = new HostMessage.WasmPluginVersion().ToJson<HostMessage>();

        foreach ((var ent, var mod) in query)
        {
            var result = mod.Ref.Mod.Plugin.Call("on_init", pluginVersion);
            ent.Ref.Add<WasmInitialized>();
        }
    }

    private static void ModUpdate(Query<Data<WasmMod>> query, Time time)
    {
        var timeProxy = new TimeProxy(time.Total, time.Frame).ToJson();
        foreach ((_, var mod) in query)
        {
            var result = mod.Ref.Mod.Plugin.Call("on_update", timeProxy);
        }
    }

    private static void ModEvents(
        Query<Data<WasmMod>> query,
        EventReader<HostMessage> reader,
        EventWriter<(Mod, PluginMessage)> writer
    )
    {
        var jsonEvents = new HostMessages(reader.Values).ToJson();

        foreach ((_, var mod) in query)
        {
            var result = mod.Ref.Mod.Plugin.Call("on_event", jsonEvents);
            // if (!string.IsNullOrEmpty(result))
            // {
            //     var eventsOut = JsonSerializer.Deserialize<PluginMessages>(result);
            //     foreach (var ev in eventsOut.Messages)
            //         writer.Enqueue((mod.Ref.Mod, ev));
            // }
        }
    }

    private static void ReadModEvents(
        Res<PacketsMap> packetMap,
        Res<AssetsServer> assets,
        EventReader<(Mod, PluginMessage)> reader
    )
    {
        foreach ((var mod, var ev) in reader)
        {
            switch (ev)
            {
                case PluginMessage.SetPacketHandler setHandler:
                    if (mod.Plugin.FunctionExists(setHandler.FuncName))
                    {
                        packetMap.Value[setHandler.PacketId] = buffer =>
                            mod.Plugin.Call(setHandler.FuncName, buffer);
                    }
                    else
                    {
                        Console.WriteLine("trying to assing the handler {0:X2} but function name {1} doesn't exists in the following plugin {2}",
                            setHandler.PacketId, setHandler.FuncName, mod.Plugin.Id);
                    }

                    break;


                case PluginMessage.OverrideAsset overrideAsset:
                    if (overrideAsset.AssetType == AssetType.Gump)
                    {
                        var data = MemoryMarshal.Cast<byte, uint>(Convert.FromBase64String(overrideAsset.DataBase64));
                        assets.Value.Gumps.SetGump(overrideAsset.Idx, data, overrideAsset.Width, overrideAsset.Height);
                    }
                    break;
            }
        }
    }
}



internal struct WasmMod
{
    public Mod Mod;
}

internal struct WasmInitialized;


// [JsonSourceGenerationOptions(IncludeFields = true, PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
// [JsonSerializable(typeof(ComponentInfo[]), GenerationMode = JsonSourceGenerationMode.Serialization)]

// // internals
// [JsonSerializable(typeof(PacketHandlerInfo), GenerationMode = JsonSourceGenerationMode.Default)]


// // components
// [JsonSerializable(typeof(WorldPosition), GenerationMode = JsonSourceGenerationMode.Default)]
// [JsonSerializable(typeof(Graphic), GenerationMode = JsonSourceGenerationMode.Default)]
// [JsonSerializable(typeof(Hue), GenerationMode = JsonSourceGenerationMode.Default)]
// [JsonSerializable(typeof(Facing), GenerationMode = JsonSourceGenerationMode.Default)]
// [JsonSerializable(typeof(EquipmentSlots), GenerationMode = JsonSourceGenerationMode.Default)]
// [JsonSerializable(typeof(Hitpoints), GenerationMode = JsonSourceGenerationMode.Default)]
// [JsonSerializable(typeof(Mana), GenerationMode = JsonSourceGenerationMode.Default)]
// [JsonSerializable(typeof(Stamina), GenerationMode = JsonSourceGenerationMode.Default)]


// [JsonSerializable(typeof(WasmPluginVersion), GenerationMode = JsonSourceGenerationMode.Default)]
// // [JsonSerializable(typeof(List<IHostMessage>), GenerationMode = JsonSourceGenerationMode.Default)]
// [JsonSerializable(typeof(IPluginMessage[]), GenerationMode = JsonSourceGenerationMode.Default)]
// internal partial class PluginJsonContext : JsonSerializerContext { }



// [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
// [JsonDerivedType(typeof(MouseMove), nameof(MouseMove))]
// [JsonDerivedType(typeof(MousePressed), nameof(MousePressed))]
// [JsonDerivedType(typeof(MouseReleased), nameof(MouseReleased))]
// [JsonDerivedType(typeof(KeyPressed), nameof(KeyPressed))]
// [JsonDerivedType(typeof(KeyReleased), nameof(KeyReleased))]
// internal interface IHostMessage
// {
//     internal record struct MouseMove(float X, float Y) : IHostMessage;
//     internal record struct MousePressed(int Button, float X, float Y) : IHostMessage;
//     internal record struct MouseReleased(int Button, float X, float Y) : IHostMessage;
//     internal record struct KeyPressed(Keys Key) : IHostMessage;
//     internal record struct KeyReleased(Keys Key) : IHostMessage;
// }

// [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
// [JsonDerivedType(typeof(SetPacketHandler), nameof(SetPacketHandler))]
// [JsonDerivedType(typeof(PlayerInfo), nameof(PlayerInfo))]
// [JsonDerivedType(typeof(OverrideAsset), nameof(OverrideAsset))]
// internal interface IPluginMessage
// {
//     internal record struct SetPacketHandler(byte PacketId, string FuncName) : IPluginMessage;

//     internal record struct PlayerInfo(uint Serial, ushort Graphic, ushort X, ushort Y, sbyte Z) : IPluginMessage;

//     internal record struct OverrideAsset(AssetType AssetType, uint Idx, string DataBase64, int Width, int Height) : IPluginMessage;
// }


[GenerateSerde]
internal enum AssetType
{
    Gump,
    Arts,
    Animation,
}

[GenerateSerde]
partial record Hello(int A);



[GenerateSerde]
internal partial record struct HostMessages(List<HostMessage> Messages);
[GenerateSerde]
internal partial record struct PluginMessages(List<PluginMessage> Messages);

[GenerateSerde]
abstract partial record HostMessage
{
    private HostMessage() { }

    public partial record WasmPluginVersion(uint Version = 1) : HostMessage;

    public partial record MouseMove(float X, float Y) : HostMessage;

    public partial record MouseWheel(float Delta) : HostMessage;

    public partial record MousePressed(int Button, float X, float Y) : HostMessage;

    public partial record MouseReleased(int Button, float X, float Y) : HostMessage;

    public partial record MouseDoubleClick(int Button, float X, float Y) : HostMessage;

    public partial record KeyPressed(int Key) : HostMessage;

    public partial record KeyReleased(int Key) : HostMessage;
}

[GenerateSerde]
abstract partial record PluginMessage
{
    private PluginMessage() { }


    public partial record SetPacketHandler(byte PacketId, string FuncName) : PluginMessage;

    public partial record PlayerInfo(uint Serial, ushort Graphic, ushort X, ushort Y, sbyte Z) : PluginMessage;

    public partial record OverrideAsset(AssetType AssetType, uint Idx, string DataBase64, int Width, int Height) : PluginMessage;
}


[GenerateSerde]
partial record struct TimeProxy(float Total, float Frame);
