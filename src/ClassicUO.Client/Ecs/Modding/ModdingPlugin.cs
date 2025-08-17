using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Ecs.Modding;
using ClassicUO.Ecs.Modding.Guest;
using ClassicUO.Ecs.Modding.Host;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using Clay_cs;
using Extism.Sdk;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyEcs;

namespace ClassicUO.Ecs;

// https://github.com/bakcxoj/bevy_wasm
// https://github.com/mhmd-azeez/extism-space-commander/blob/main/scripts/mod_manager.cs#L161

internal readonly struct ModdingPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<HostMessage>();
        scheduler.AddEvent<(Mod, PluginMessage)>();


        var setupModsFn = SetupMods;
        scheduler.OnStartup(setupModsFn);

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


    private static void SetupMods(
        World world,
        Res<NetClient> network,
        Res<Settings> settings,
        Res<NetworkEntitiesMap> networkEntities,
        SchedulerState schedState
    )
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
            var modRef = new WeakReference<Mod>(mod);

            HostFunction[] functions =
            [
                ..Api.Functions(modRef, schedState),
                ..bind<Graphic>("entity_graphic", networkEntities),
                ..bind<Hue>("entity_hue", networkEntities),
                ..bind<Facing>("entity_direction", networkEntities),
                ..bind<WorldPosition>("entity_position", networkEntities),
                ..bind<MobAnimation>("entity_animation", networkEntities),
                ..bind<ServerFlags>("entity_flags", networkEntities),
                ..bind<Hits>("entity_hp", networkEntities),
                ..bind<Stamina>("entity_stamina", networkEntities),
                ..bind<Mana>("entity_mana", networkEntities),
            ];

            var manifest = new Manifest(uri?.IsFile ?? true ? new PathWasmSource(path) : new UrlWasmSource(uri));
            using var compiled = new Extism.Sdk.CompiledPlugin(manifest, functions, true);
            var plugin = compiled.Instantiate();

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

            modRef.SetTarget(mod);
        }

        static IEnumerable<HostFunction> bind<T>(string postfix, NetworkEntitiesMap networkEntities)
                where T : struct
        {
            var ctx = (JsonTypeInfo<T>)ModdingJsonContext.Default.GetTypeInfo(typeof(T));
            yield return serializeProps("cuo_get_" + postfix, networkEntities, ctx);
            yield return deserializeProps("cuo_set_" + postfix, networkEntities, ctx);
            yield break;


            static HostFunction serializeProps(string name, NetworkEntitiesMap networkEntities, JsonTypeInfo<T> ctx)
            {
                ArgumentNullException.ThrowIfNull(ctx);

                return HostFunction.FromMethod(name, null, (CurrentPlugin p, long offset) =>
                {
                    var serial = p.ReadBytes(offset).As<uint>();
                    var ent = networkEntities.Get(serial);
                    if (ent == 0 || !ent.Has<T>())
                        return p.WriteString("{}");

                    var json = JsonSerializer.Serialize(ent.Get<T>(), ctx);
                    return p.WriteString(json);
                });
            }

            static HostFunction deserializeProps(string name, NetworkEntitiesMap networkEntities, JsonTypeInfo<T> ctx)
            {
                ArgumentNullException.ThrowIfNull(ctx);

                return HostFunction.FromMethod(name, null, (CurrentPlugin p, long keyOffset, long valueOffset) =>
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
        }
    }

    private static void ModInitialize(Query<Data<WasmMod>, Without<WasmInitialized>> query)
    {
        var pluginVersion = new WasmPluginVersion().ToJson();

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
            try
            {
                var result = mod.Ref.Mod.Plugin.Call("on_update", timeProxy);
            }
            catch (Exception e)
            {
                Console.WriteLine("on_update failed: {0}", e);
            }
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
        World world,
        Res<PacketsMap> packetMap,
        Res<AssetsServer> assets,
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        Res<Settings> settings,
        EventReader<(Mod, PluginMessage)> reader
    )
    {
        foreach ((var mod, var ev) in reader)
        {
            switch (ev)
            {
                case PluginMessage.LoginRequest loginRequest:
                    network.Value.Connect(settings.Value.IP, settings.Value.Port);

                    if (!network.Value.IsConnected)
                        continue;

                    network.Value.Encryption?.Initialize(true, network.Value.LocalIP);

                    if (gameCtx.Value.ClientVersion >= ClientVersion.CV_6040)
                    {
                        // NOTE: im forcing the use of latest client just for convenience rn
                        var major = (byte)((uint)gameCtx.Value.ClientVersion >> 24);
                        var minor = (byte)((uint)gameCtx.Value.ClientVersion >> 16);
                        var build = (byte)((uint)gameCtx.Value.ClientVersion >> 8);
                        var extra = (byte)gameCtx.Value.ClientVersion;

                        network.Value.Send_Seed(network.Value.LocalIP, major, minor, build, extra);
                    }
                    else
                    {
                        network.Value.Send_Seed_Old(network.Value.LocalIP);
                    }

                    network.Value.Send_FirstLogin(loginRequest.Username, loginRequest.Password);

                    break;

                case PluginMessage.ServerLoginRequest serverLoginRequest:
                    if (network.Value.IsConnected)
                    {
                        network.Value.Send_SelectServer(serverLoginRequest.Index);
                    }

                    break;
            }
        }

        // foreach ((var mod, var ev) in reader)
        // {
        //     switch (ev)
        //     {
        //         case PluginMessage.SetPacketHandler setHandler:
        //             if (mod.Plugin.FunctionExists(setHandler.FuncName))
        //             {
        //                 packetMap.Value[setHandler.PacketId] = buffer =>
        //                     mod.Plugin.Call(setHandler.FuncName, buffer);
        //             }
        //             else
        //             {
        //                 Console.WriteLine("trying to assing the handler {0:X2} but function name {1} doesn't exists in the following plugin {2}",
        //                     setHandler.PacketId, setHandler.FuncName, mod.Plugin.Id);
        //             }

        //             break;


        //         case PluginMessage.OverrideAsset overrideAsset:
        //             if (overrideAsset.AssetType == AssetType.Gump)
        //             {
        //                 var data = MemoryMarshal.Cast<byte, uint>(Convert.FromBase64String(overrideAsset.DataBase64));
        //                 assets.Value.Gumps.SetGump(overrideAsset.Idx, data, overrideAsset.Width, overrideAsset.Height);
        //             }
        //             break;

        //         case PluginMessage.SetComponent<Graphic> setter:
        //             setComponent(world, setter);
        //             break;
        //         case PluginMessage.SetComponent<Hue> setter:
        //             setComponent(world, setter);
        //             break;
        //     }


        //     static void setComponent<T>(World world, PluginMessage.SetComponent<T> setter) where T : struct
        //     {
        //         if (!world.Exists(setter.Entity))
        //             return;

        //         var cmpEnt = world.Entity<T>();
        //         ref var cmp = ref cmpEnt.Get<ComponentInfo>();

        //         if (cmp.Size > 0)
        //             world.Set(setter.Entity, setter.Value);
        //         else
        //             world.Add<T>(setter.Entity);
        //     }
        // }
    }
}



internal struct WasmMod
{
    public Mod Mod;
}

internal struct WasmInitialized;




internal record struct WasmPluginVersion(uint Version = 1);
internal record struct TimeProxy(float Total, float Frame);
internal record struct PacketHandlerInfo(byte PacketId, string FuncName);
internal record struct SpriteDescription(AssetType AssetType, uint Idx, int Width, int Height, string Base64Data, CompressionType Compression);

enum CompressionType
{
    None,
    Zlib
}



internal enum AssetType
{
    Gump,
    Arts,
    Animation,
}


internal readonly struct PluginEntity(Mod mod)
{
    public Mod Mod { get; } = mod;
}
