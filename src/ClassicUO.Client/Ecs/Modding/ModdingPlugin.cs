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
using ClassicUO.Network;
using Clay_cs;
using Extism.Sdk;
using Microsoft.Xna.Framework.Input;
using TinyEcs;

namespace ClassicUO.Ecs;

// https://github.com/bakcxoj/bevy_wasm
// https://github.com/mhmd-azeez/extism-space-commander/blob/main/scripts/mod_manager.cs#L161

internal readonly struct ModdingPlugins : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<HostMessage>();
        scheduler.AddEvent<(Mod, PluginMessage)>();

        scheduler.OnUpdate
        ((Query<Data<UINode, UIInteractionState, PluginEntity>, Changed<UIInteractionState>> query, Res<MouseContext> mouseCtx) =>
            {
                foreach ((var ent, var node, var interaction, var pluginEnt) in query)
                {
                    if (interaction.Ref == UIInteractionState.Released)
                    {
                        var ev = new UIMouseEvent(ent.Ref.ID, 0, mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y, interaction.Ref).ToJson();
                        pluginEnt.Ref.Mod.Plugin.Call("on_ui_mouse_event", ev);
                    }
                }
            }
        );
            //.RunIf((Query<Data<UINode, UIInteractionState, PluginEntity>, Changed<UIInteractionState>> query) => query.Count() > 0);

        scheduler.OnStartup((
            World world,
            EventWriter<(Mod, PluginMessage)> pluginWriter,
            EventWriter<HostMessage> hostWriter,
            Res<NetClient> network,
            Res<PacketsMap> packetMap,
            Res<Settings> settings,
            Res<NetworkEntitiesMap> networkEntities,
            Res<GameContext> gameCtx,
            Res<AssetsServer> assets,
            Res<UOFileManager> fileManager
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

                    HostFunction.FromMethod("cuo_set_packet_handler", null, (CurrentPlugin p, long offset) => {
                        var handlerInfo = p.ReadString(offset).FromJson<PacketHandlerInfo>();
                        if (plugin.FunctionExists(handlerInfo.FuncName))
                        {
                            packetMap.Value[handlerInfo.PacketId] = buffer
                                => plugin.Call(handlerInfo.FuncName, buffer);
                        }
                    }),

                    HostFunction.FromMethod("cuo_add_packet_handler", null, (CurrentPlugin p, long offset) => {
                        var handlerInfo = p.ReadString(offset).FromJson<PacketHandlerInfo>();
                        if (plugin.FunctionExists(handlerInfo.FuncName))
                        {
                            if (!packetMap.Value.TryGetValue(handlerInfo.PacketId, out var fn))
                            {
                                return;
                            }

                            packetMap.Value[handlerInfo.PacketId] = buffer => {
                                    plugin.Call(handlerInfo.FuncName, buffer);
                                    fn(buffer);
                                };
                        }
                    }),

                    HostFunction.FromMethod("cuo_set_sprite", null, (CurrentPlugin p, long offset) => {
                        var spriteDesc = p.ReadString(offset).FromJson<SpriteDescription>();
                        var data = Convert.FromBase64String(spriteDesc.Base64Data);

                        if (spriteDesc.Compression == CompressionType.Zlib)
                        {
                            data = Uncompress(data);
                        }

                        var pixels = MemoryMarshal.Cast<byte, uint>(data);

                        switch (spriteDesc.AssetType)
                        {
                            case AssetType.Gump:
                                assets.Value.Gumps.SetGump(spriteDesc.Idx, pixels, spriteDesc.Width, spriteDesc.Height);
                                break;

                            case AssetType.Arts:
                                assets.Value.Arts.SetArt(spriteDesc.Idx, pixels, spriteDesc.Width, spriteDesc.Height);
                                break;

                            default:
                                Console.WriteLine("'cuo_set_sprite' for {0} not implemented yet", spriteDesc.AssetType);
                                break;
                        }
                    }),

                    HostFunction.FromMethod("cuo_get_sprite", null, (CurrentPlugin p, long offset) => {
                        var spriteDesc = p.ReadString(offset).FromJson<SpriteDescription>();

                        switch (spriteDesc.AssetType)
                        {
                            case AssetType.Gump:
                                var gumpInfo = fileManager.Value.Gumps.GetGump(spriteDesc.Idx);
                                if (gumpInfo.Pixels.IsEmpty)
                                    return p.WriteBytes([]);

                                var json = createSpriteDesc(spriteDesc, gumpInfo.Pixels.AsBytes(), (gumpInfo.Width, gumpInfo.Height), CompressionType.Zlib)
                                    .ToJson();
                                return p.WriteString(json);

                            case AssetType.Arts:
                                var artInfo = fileManager.Value.Arts.GetArt(spriteDesc.Idx);
                                if (artInfo.Pixels.IsEmpty)
                                    return p.WriteBytes([]);

                                json = createSpriteDesc(spriteDesc, artInfo.Pixels.AsBytes(), (artInfo.Width, artInfo.Height), CompressionType.Zlib)
                                    .ToJson();
                                return p.WriteString(json);

                            default:
                                Console.WriteLine("'cuo_get_sprite' for {0} not implemented yet", spriteDesc.AssetType);
                                break;

                        }

                        return p.WriteBytes([]);

                        static SpriteDescription createSpriteDesc(SpriteDescription input, ReadOnlySpan<byte> data, (int w, int h) imgSize, CompressionType compression)
                        {
                            data = compression == CompressionType.Zlib ? Compress(data) : data;
                            var base64Data = Convert.ToBase64String(data);
                            return new SpriteDescription(input.AssetType, input.Idx, imgSize.w, imgSize.h, base64Data, compression);
                        }
                    }),



                    HostFunction.FromMethod("send_events", null, (CurrentPlugin p, long offset) => {
                        // var str = p.ReadString(offset);
                        // var events = str.FromJson<PluginMessages>();

                        // foreach (var ev in events.Messages)
                        //     pluginWriter.Enqueue((mod, ev));
                    }),


                    HostFunction.FromMethod("cuo_get_player_serial", null, p
                        => p.WriteBytes(gameCtx.Value.PlayerSerial.AsBytes())),


                    HostFunction.FromMethod("cuo_ecs_spawn_entity", null, (CurrentPlugin p) =>
                    {
                        var ent = world.Entity().Set(new PluginEntity(mod));
                        return ent.ID;
                    }),

                    HostFunction.FromMethod("cuo_ecs_delete_entity", null, (CurrentPlugin p, ulong id) =>
                    {
                        if (world.Exists(id))
                            world.Delete(id);
                    }),


                    HostFunction.FromMethod("cuo_ecs_set_component", null, (CurrentPlugin plugin, ulong id, long offset) =>
                    {
                        if (!world.Exists(id))
                            return;

                        // var ent = world.Entity(id);
                        // world.Set();
                    }),


                    HostFunction.FromMethod("cuo_ecs_query", null, (CurrentPlugin p, long offset) =>
                    {
                        var request = p.ReadString(offset).FromJson<QueryRequest>();

                        if (request.Terms.Count == 0)
                        {
                            Console.WriteLine("cuo_ecs_query: empty request");
                            return p.WriteString("{}");
                        }

                        var builder = world.QueryBuilder();
                        foreach (var (id, op) in request.Terms)
                        {
                            switch (op)
                            {
                                case TermOp.With:
                                    builder.With(id);
                                    break;
                                case TermOp.Without:
                                    builder.Without(id);
                                    break;
                                case TermOp.Optional:
                                    builder.Optional(id);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
                            }
                        }

                        var query = builder.Build();
                        var it = query.Iter();

                        var list = new List<ArchetypeProxy>();
                        while (it.Next())
                            list.Add(new ArchetypeProxy(it.Archetype.All.Select(s => new ComponentInfoProxy(s.ID, s.Size, world.Name(s.ID))), MemoryMarshal.ToEnumerable(it.EntitiesAsMemory()).Select(static s => s.ID)));

                        var response = new QueryResponse(list).ToJson();
                        return p.WriteString(response);
                    }),


                    HostFunction.FromMethod("cuo_ui_node", null, (CurrentPlugin p, long offset) => {
                        var nodes = p.ReadString(offset).FromJson<UINodes>();

                        foreach (var node in nodes.Nodes)
                        {
                            var ent = world.Entity(node.Id)
                                .Set(new PluginEntity(mod))
                                .Set(new UINode() {
                                    // TODO: missing some config
                                    Config = {
                                        layout = node.Config.Layout ?? default,
                                        backgroundColor = node.Config.BackgroundColor ?? default,
                                        cornerRadius = node.Config.CornerRadius ?? default,
                                        floating = node.Config.Floating ?? default,
                                        clip = node.Config.Clip ?? default,
                                        border = node.Config.Border ?? default,
                                        image = {
                                            // imageData = node.Config.Image.Base64Data
                                        }
                                    },
                                    UOConfig = node.UOConfig ?? default
                                });

                            if (node.TextConfig is {} textCfg)
                            {
                                ent.Set(new Text() {
                                    Value = textCfg.Value,
                                    TextConfig = {
                                        fontId = textCfg.TextConfig.FontId,
                                        fontSize = textCfg.TextConfig.FontSize,
                                        letterSpacing = textCfg.TextConfig.LetterSpacing,
                                        lineHeight = textCfg.TextConfig.LineHeight,
                                        textAlignment = textCfg.TextConfig.TextAlignment,
                                        textColor = textCfg.TextConfig.TextColor,
                                        wrapMode = textCfg.TextConfig.WrapMode
                                    }
                                });
                            }

                            if (node.Movable)
                                ent.Add<UIMovable>();

                            if (node.AcceptInputs)
                                ent.Set(UIInteractionState.None);

                            if (node.WidgetType == ClayWidgetType.TextInput)
                                ent.Add<TextInput>();
                            else if (node.WidgetType == ClayWidgetType.Button)
                            {
                                if (node.UOButton is {} button)
                                {
                                    ent.Set(UIInteractionState.None);
                                    ent.Set(new UOButton()
                                    {
                                        Normal = button.Normal,
                                        Over = button.Over,
                                        Pressed = button.Pressed
                                    });
                                }
                            }
                        }

                        foreach (var (child, parent) in nodes.Relations)
                        {
                            if (!world.Exists(child))
                                continue;

                            if (!world.Exists(parent))
                                continue;

                            world.Entity(parent).AddChild(child);
                        }
                    }),


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
                plugin = new Extism.Sdk.CompiledPlugin(manifest, functions, true).Instantiate();

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


            static IEnumerable<HostFunction> bind<T>(string postfix, NetworkEntitiesMap networkEntities)
                    where T : struct
            {
                var ctx = (JsonTypeInfo<T>)ModdingJsonContext.Default.GetTypeInfo(typeof(T));
                yield return serializeProps<T>("cuo_get_" + postfix, networkEntities, ctx);
                yield return deserializeProps<T>("cuo_set_" + postfix, networkEntities, ctx);
                yield break;


                static HostFunction serializeProps<T>(string name, NetworkEntitiesMap networkEntities, JsonTypeInfo<T> ctx)
                    where T : struct
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

                static HostFunction deserializeProps<T>(string name, NetworkEntitiesMap networkEntities, JsonTypeInfo<T> ctx)
                    where T : struct
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

        });




        scheduler.OnAfterUpdate((Res<MouseContext> mouseCtx, Res<KeyboardContext> keyboardCtx, EventWriter<HostMessage> writer) =>
        {
            // if (mouseCtx.Value.PositionOffset != Vector2.Zero)
            // {
            //     writer.Enqueue(new HostMessage.MouseMove(mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y));
            // }

            if (mouseCtx.Value.Wheel != 0)
            {
                writer.Enqueue(new HostMessage.MouseWheel(mouseCtx.Value.Wheel));
            }

            for (var button = Input.MouseButtonType.Left; button < Input.MouseButtonType.Size; button += 1)
            {
                if (mouseCtx.Value.IsPressedOnce(button))
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
                if (keyboardCtx.Value.IsPressedOnce(key))
                {
                    writer.Enqueue(new HostMessage.KeyPressed(key));
                }

                if (keyboardCtx.Value.IsReleased(key))
                {
                    writer.Enqueue(new HostMessage.KeyReleased(key));
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

    private static unsafe byte[] Uncompress(ReadOnlySpan<byte> data)
    {
        fixed (byte* dataPtr = data)
        {
            using var ms = new UnmanagedMemoryStream(dataPtr, data.Length);
            using var deflateStream = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress);
            using var msOut = new MemoryStream();
            deflateStream.CopyTo(msOut);
            return msOut.ToArray();
        }
    }

    private static byte[] Compress(ReadOnlySpan<byte> data)
    {
        using var ms = new MemoryStream();
        {
            using var deflateStream = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Compress, leaveOpen: true);
            deflateStream.Write(data);
        }
        return ms.ToArray();
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
        World world,
        Res<PacketsMap> packetMap,
        Res<AssetsServer> assets,
        EventReader<(Mod, PluginMessage)> reader
    )
    {
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


[JsonSourceGenerationOptions(IncludeFields = true, PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]

// components
[JsonSerializable(typeof(WorldPosition), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Graphic), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Hue), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Facing), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(EquipmentSlots), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Hits), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Mana), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Stamina), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(MobAnimation), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ServerFlags), GenerationMode = JsonSourceGenerationMode.Default)]

[JsonSerializable(typeof(UINodes), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(UIMouseEvent), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(QueryRequest), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(QueryResponse), GenerationMode = JsonSourceGenerationMode.Default)]


[JsonSerializable(typeof(HostMessages), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(PluginMessages), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(TimeProxy), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(WasmPluginVersion), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(PacketHandlerInfo), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(SpriteDescription), GenerationMode = JsonSourceGenerationMode.Default)]
internal partial class ModdingJsonContext : JsonSerializerContext { }

internal record struct WasmPluginVersion(uint Version = 1);
internal record struct TimeProxy(float Total, float Frame);
internal record struct PacketHandlerInfo(byte PacketId, string FuncName);
internal record struct SpriteDescription(AssetType AssetType, uint Idx, int Width, int Height, string Base64Data, CompressionType Compression);

enum CompressionType
{
    None,
    Zlib
}


internal record struct ComponentInfoProxy(ulong Id, int Size, string Name);
internal record struct QueryRequest(List<(ulong Ids, TermOp Op)> Terms);
internal record struct ArchetypeProxy(IEnumerable<ComponentInfoProxy> Components, IEnumerable<ulong> Entities);
internal record struct QueryResponse(List<ArchetypeProxy> Results);


internal record struct UINodes(List<UINodeProxy> Nodes, Dictionary<ulong, ulong> Relations);
internal record struct UINodeProxy(
    ulong Id,
    ClayElementDeclProxy Config,
    ClayUOCommandData? UOConfig = null,
    UITextProxy? TextConfig = null,
    UOButtonWidgetProxy? UOButton = null,
    ClayWidgetType WidgetType = ClayWidgetType.None,
    bool Movable = false,
    bool AcceptInputs = false
);

internal record struct UOButtonWidgetProxy(ushort Normal, ushort Pressed, ushort Over);
internal record struct UIMouseEvent(ulong Id, int Button, float X, float Y, UIInteractionState State);


enum ClayWidgetType
{
    None,
    Button,
    TextInput
}

internal record struct UITextProxy(string Value, char ReplacedChar = '\0', ClayTextProxy TextConfig = default);
internal record struct ClayTextProxy(Clay_Color TextColor, ushort FontId, ushort FontSize, ushort LetterSpacing, ushort LineHeight, Clay_TextElementConfigWrapMode WrapMode, Clay_TextAlignment TextAlignment);
internal record struct ClayElementIdProxy(uint Id, uint Offset, uint BaseId, string StringId);
internal record struct ClayImageProxy(string Base64Data);
internal struct ClayElementDeclProxy
{
    public ClayElementIdProxy? Id;
    public Clay_LayoutConfig? Layout;
    public Clay_Color? BackgroundColor;
    public Clay_CornerRadius? CornerRadius;
    public ClayImageProxy? Image;
    public Clay_FloatingElementConfig? Floating;
    public Clay_ClipElementConfig? Clip;
    public Clay_BorderElementConfig? Border;
}

internal record struct HostMessages(IEnumerable<HostMessage> Messages);
internal record struct PluginMessages(List<PluginMessage> Messages);


[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(MouseMove), nameof(MouseMove))]
[JsonDerivedType(typeof(MouseWheel), nameof(MouseWheel))]
[JsonDerivedType(typeof(MousePressed), nameof(MousePressed))]
[JsonDerivedType(typeof(MouseReleased), nameof(MouseReleased))]
[JsonDerivedType(typeof(MouseDoubleClick), nameof(MouseDoubleClick))]
[JsonDerivedType(typeof(KeyPressed), nameof(KeyPressed))]
[JsonDerivedType(typeof(KeyReleased), nameof(KeyReleased))]

internal interface HostMessage
{
    internal record struct MouseMove(float X, float Y) : HostMessage;
    internal record struct MouseWheel(float Delta) : HostMessage;
    internal record struct MousePressed(int Button, float X, float Y) : HostMessage;
    internal record struct MouseReleased(int Button, float X, float Y) : HostMessage;
    internal record struct MouseDoubleClick(int Button, float X, float Y) : HostMessage;
    internal record struct KeyPressed(Keys Key) : HostMessage;
    internal record struct KeyReleased(Keys Key) : HostMessage;

}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
internal interface PluginMessage
{

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

public static class JsonEx
{
    public static string ToJson<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj, (JsonTypeInfo<T>)ModdingJsonContext.Default.GetTypeInfo(typeof(T)));
    }

    public static T FromJson<T>(this string json)
    {
        return JsonSerializer.Deserialize(json, (JsonTypeInfo<T>)ModdingJsonContext.Default.GetTypeInfo(typeof(T)));
    }
}
