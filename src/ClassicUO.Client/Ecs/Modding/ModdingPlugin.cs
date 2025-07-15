using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Input;
using ClassicUO.Network;
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

        var sendGeneralEventsFn = SendGeneralEvents;
        scheduler.OnAfterUpdate(sendGeneralEventsFn);

        var sendUIEventsFn = SendUIEvents;
        scheduler.OnUpdate(sendUIEventsFn);

        var propagateTextConfigFn = PropagateTextConfigToChildTextFragments;
        scheduler.OnUpdate(propagateTextConfigFn);
    }


    private static void SetupMods(
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


                HostFunction.FromMethod("cuo_get_player_serial", null, p =>
                {
                    var span = gameCtx.Value.PlayerSerial.AsBytes();
                    // BinaryPrimitives.WriteUInt32BigEndian(span, gameCtx.Value.PlayerSerial);
                    var addr = p.WriteBytes(span);
                    return addr;
                }),


                HostFunction.FromMethod("cuo_ecs_spawn_entity", null, (CurrentPlugin p) =>
                {
                    var ent = world.Entity().Set(new PluginEntity(mod));
                    return (long)ent.ID;
                }),

                HostFunction.FromMethod("cuo_ecs_delete_entity", null, (CurrentPlugin p, long id) =>
                {
                    if (world.Exists((ulong)id))
                        world.Delete((ulong)id);
                }),


                HostFunction.FromMethod("cuo_ecs_set_component", null, (CurrentPlugin plugin, long id, long offset) =>
                {
                    if (!world.Exists((ulong)id))
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
                            .SetUINode(new UINode() {
                                // TODO: missing some config
                                Config = {
                                    // id = Clay.Id(node.Id.ToString()),
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
                            var config = new Text() {
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
                            };

                            ent.Set(config);
                        }

                        if (node.Movable)
                            ent.Add<UIMovable>();

                        // if (node.AcceptInputs)
                        // ent.Set(new UIMouseAction());

                        if (node.WidgetType == ClayWidgetType.TextInput)
                            ent.Add<TextInput>();
                        else if (node.WidgetType == ClayWidgetType.TextFragment)
                            ent.Add<TextFragment>();
                        else if (node.WidgetType == ClayWidgetType.Button)
                        {
                            if (node.UOButton is {} button)
                            {
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

                HostFunction.FromMethod("cuo_add_entity_to_parent", null, (CurrentPlugin p, long entityId, long parentId, long index) =>
                {
                    if (!world.Exists((ulong)entityId) || !world.Exists((ulong)parentId))
                        return;

                    Console.WriteLine("cuo_add_entity_to_parent {0} {1} {2}", entityId, parentId, index);

                    var parent = world.Entity((ulong)parentId);
                    var entity = world.Entity((ulong)entityId);
                    parent.AddChild(entity, (int)index);
                }),


                HostFunction.FromMethod("cuo_ui_add_event_listener", null, (CurrentPlugin p, long offset) => {
                    var addEvent = p.ReadString(offset).FromJson<UIEvent>();

                    if (!world.Exists(addEvent.EntityId)) {
                        return 0ul;
                    }

                    var entity = world.Entity(addEvent.EntityId);
                    var ev = world.Entity().Set(addEvent);

                    // when the entity will be deleted, the event will be deleted too.
                    // this needs to be handled by the plugin too (?)
                    entity.AddChild(ev);

                    return ev.ID;
                }),

                HostFunction.FromMethod("cuo_ui_remove_event_listener", null, (CurrentPlugin p, long offset) => {
                    var removeEvent = p.ReadString(offset).FromJson<UIEvent>();

                    if (!world.Exists(removeEvent.EntityId))
                        return 0ul;

                    if (removeEvent.EventId is null)
                        return 0ul;

                    if (!world.Exists(removeEvent.EventId.Value))
                        return 0ul;

                    // var entity = world.Entity(removeEvent.EntityId);
                    var ev = world.Entity(removeEvent.EventId.Value);
                    ev.Delete();
                    // entity.RemoveChild(ev);

                    return ev.ID;
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

    private static void SendGeneralEvents(
        Res<MouseContext> mouseCtx,
        Res<KeyboardContext> keyboardCtx,
        EventWriter<HostMessage> writer
    )
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
    }

    private static void SendUIEvents(
        Query<Data<UINode, UIMouseAction, PluginEntity>, Changed<UIMouseAction>> queryChanged,
        Query<Data<UINode, UIMouseAction, PluginEntity>> query,
        Query<Data<UIEvent>, With<Parent>> queryEvents,
        Query<Data<Children>> children,
        Query<Data<Parent>, With<UINode>> queryUIParents,
        Res<MouseContext> mouseCtx,
        Res<KeyboardContext> keyboardCtx
    )
    {
        var isDragging = mouseCtx.Value.PositionOffset.Length() > 1;
        var mousePos = mouseCtx.Value.Position;

        static bool sendEventForId(
            ulong id,
            Query<Data<UIEvent>, With<Parent>> queryEvents,
            Query<Data<Children>> queryChildren,
            MouseContext mouseCtx,
            EventType eventType,
            MouseButtonType button,
            Mod mod
        )
        {
            // check if there is any child event
            if (!queryChildren.Contains(id))
                return true;

            (_, var children) = queryChildren.Get(id);

            foreach (var child in children.Ref)
            {
                // check if child is an event
                if (!queryEvents.Contains(child))
                    return true;

                (var eventId, var uiEv) = queryEvents.Get(child);

                if (uiEv.Ref.EventType != eventType)
                {
                    continue;
                }

                // push the event
                var json = (uiEv.Ref with
                {
                    EntityId = id,
                    EventId = eventId.Ref.ID,
                    X = mouseCtx.Position.X,
                    Y = mouseCtx.Position.Y,
                    Wheel = mouseCtx.Wheel,
                    MouseButton = button,
                }).ToJson();

                var result = mod.Plugin.Call("on_ui_event", json);
                if (result == "0")
                {
                    Console.WriteLine("on_ui_event returned 0, stopping event propagation");
                    return false;
                }

                Console.WriteLine("event {0} sent to {1}", eventId.Ref.ID, id);
            }

            return true;
        }

        static List<ulong> getHierarchy(ulong parentId, Query<Data<Parent>, With<UINode>> queryUIParents)
        {
            var list = new List<ulong>();
            while (queryUIParents.Contains(parentId))
            {
                (_, var parent) = queryUIParents.Get(parentId);

                list.Add(parentId);
                parentId = parent.Ref.Id;
            }

            return list;
        }

        foreach ((var ent, var node, var mouseAction, var pluginEnt) in queryChanged)
        {
            EventType? eventType = mouseAction.Ref switch
            {
                { IsPressed: true, WasPressed: false, IsHovered: true } => EventType.OnMousePressed,
                { IsPressed: false, WasPressed: true } => EventType.OnMouseReleased,
                { IsHovered: true, WasHovered: false } => EventType.OnMouseEnter,
                { IsHovered: false, WasHovered: true } => EventType.OnMouseLeave,
                { IsHovered: true } => EventType.OnMouseOver,
                _ => null
            };

            if (mouseCtx.Value.IsPressedDouble(mouseAction.Ref.Button))
            {
                eventType = EventType.OnMouseDoubleClick;
            }

            if (eventType == null)
                continue;


            // var hierarchy = getHierarchy(ent.Ref.ID, queryUIParents);

            // for (var i = hierarchy.Count - 1; i >= 0; i--)
            // {
            //     var result = sendEventForId(
            //         hierarchy[i],
            //         queryEvents,
            //         children,
            //         mouseCtx,
            //         eventType.Value,
            //         mouseAction.Ref.Button,
            //         pluginEnt.Ref.Mod
            //     );
            // }

            // for (var i = 0; i < hierarchy.Count; i++)
            // {
            //     var result = sendEventForId(
            //         hierarchy[i],
            //         queryEvents,
            //         children,
            //         mouseCtx,
            //         eventType.Value,
            //         mouseAction.Ref.Button,
            //         pluginEnt.Ref.Mod
            //     );
            // }

            var result = sendEventForId(
                ent.Ref.ID,
                queryEvents,
                children,
                mouseCtx,
                eventType.Value,
                mouseAction.Ref.Button,
                pluginEnt.Ref.Mod
            );

            if (!result)
                continue;

            var parentId = ent.Ref.ID;
            while (queryUIParents.Contains(parentId))
            {
                (_, var parent) = queryUIParents.Get(parentId);
                result = sendEventForId(
                    parent.Ref.Id,
                    queryEvents,
                    children,
                    mouseCtx,
                    eventType.Value,
                    mouseAction.Ref.Button,
                    pluginEnt.Ref.Mod
                );

                // block the events propagation
                if (!result)
                    break;

                parentId = parent.Ref.Id;
            }
        }
    }


    private static void PropagateTextConfigToChildTextFragments(
        Query<Data<Text, Children>, Filter<With<PluginEntity>>> query,
        Query<Data<Text>, With<Parent>> queryChildren
    )
    {
        foreach (var (text, children) in query)
        {
            foreach (var childId in children.Ref)
            {
                if (!queryChildren.Contains(childId))
                    continue;

                var (_, textChild) = queryChildren.Get(childId);

                // assign the parent text config to the child text config
                textChild.Ref.TextConfig = text.Ref.TextConfig;
            }
        }
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
[JsonSerializable(typeof(UIEvent), GenerationMode = JsonSourceGenerationMode.Default)]

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
    bool Movable = false
);

internal record struct UOButtonWidgetProxy(ushort Normal, ushort Pressed, ushort Over);
internal record struct UIMouseEvent(ulong Id, int Button, float X, float Y, UIInteractionState State);

internal record struct UIEvent(
    EventType EventType,
    ulong EntityId,

    ulong? EventId = null,
    float? X = null,
    float? Y = null,
    float? Wheel = null,
    MouseButtonType? MouseButton = null,
    Keys? Key = null
);

enum EventType
{
    OnMouseMove, // this is the same as MouseOver I guess
    OnMouseWheel,
    OnMouseOver,
    OnMousePressed,
    OnMouseReleased,
    OnMouseDoubleClick,
    OnMouseEnter,
    OnMouseLeave,
    OnDragging,

    OnKeyPressed,
    OnKeyReleased,
}

enum ClayWidgetType
{
    None,
    Button,
    TextInput,
    TextFragment
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
