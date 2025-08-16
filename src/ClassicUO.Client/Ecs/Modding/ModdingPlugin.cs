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

        var sendGeneralEventsFn = SendGeneralEvents;
        scheduler.OnAfterUpdate(sendGeneralEventsFn);

        var sendUIEventsFn = SendUIEvents;
        scheduler.OnUpdate(sendUIEventsFn);

        var propagateTextConfigFn = PropagateTextConfigToChildTextFragments;
        scheduler.OnUpdate(propagateTextConfigFn);
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
        Res<KeyboardContext> keyboardCtx,
        Local<Vector2> lastMousePos
    )
    {
        var isDragging = mouseCtx.Value.PositionOffset.Length() > 1;
        var isMouseWheel = mouseCtx.Value.Wheel != 0;
        var mousePos = mouseCtx.Value.Position;
        var isMousePosChanged = mousePos != lastMousePos.Value;
        lastMousePos.Value = mousePos;


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
                    continue;

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

            }

            return true;
        }


        foreach ((var ent, var node, var mouseAction, var pluginEnt) in queryChanged)
        {
            EventType? eventType = mouseAction.Ref switch
            {
                { IsPressed: true, WasPressed: false, IsHovered: true } => EventType.OnMousePressed,
                { IsPressed: false, WasPressed: true } => EventType.OnMouseReleased,
                { IsHovered: true, WasHovered: false } => EventType.OnMouseEnter,
                { IsHovered: false, WasHovered: true } => EventType.OnMouseLeave,
                _ => null
            };

            if (mouseCtx.Value.IsPressedDouble(mouseAction.Ref.Button))
            {
                eventType = EventType.OnMouseDoubleClick;
            }

            if (eventType == null)
                continue;


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

        if (isMousePosChanged || isMouseWheel)
        {
            foreach ((var ent, var node, var mouseAction, var pluginEnt) in query)
            {
                EventType? eventType = mouseAction.Ref switch
                {
                    { IsPressed: true, WasPressed: true, Button: MouseButtonType.Left } when isMousePosChanged => EventType.OnDragging,
                    { IsHovered: true } when isMouseWheel => EventType.OnMouseWheel,
                    { IsHovered: true } when isMousePosChanged => EventType.OnMouseOver,
                    _ => null
                };

                if (eventType == null)
                    continue;


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

[JsonDerivedType(typeof(LoginResponse), nameof(LoginResponse))]
[JsonDerivedType(typeof(ServerLoginResponse), nameof(ServerLoginResponse))]
internal interface HostMessage
{
    internal record struct MouseMove(float X, float Y) : HostMessage;
    internal record struct MouseWheel(float Delta) : HostMessage;
    internal record struct MousePressed(int Button, float X, float Y) : HostMessage;
    internal record struct MouseReleased(int Button, float X, float Y) : HostMessage;
    internal record struct MouseDoubleClick(int Button, float X, float Y) : HostMessage;
    internal record struct KeyPressed(Keys Key) : HostMessage;
    internal record struct KeyReleased(Keys Key) : HostMessage;



    internal record struct LoginResponse(CharacterListFlags Flags, IEnumerable<CharacterInfo> Characters, IEnumerable<TownInfo> Cities) : HostMessage;
    internal record struct ServerLoginResponse(byte Flags, IEnumerable<ServerInfo> Servers) : HostMessage;
}


[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(LoginRequest), nameof(LoginRequest))]
[JsonDerivedType(typeof(ServerLoginRequest), nameof(ServerLoginRequest))]
internal interface PluginMessage
{
    internal record struct LoginRequest(string Username, string Password) : PluginMessage;
    internal record struct ServerLoginRequest(byte Index) : PluginMessage;
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

public static class CurrentPluginExtismExt
{
    public static TempBuffer<byte> Buffer(this CurrentPlugin p, long offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be non-negative.");
        }

        var span = p.ReadBytes(offset);

        var temp = new TempBuffer<byte>(span.Length);
        span.CopyTo(temp.Span);

        return temp;
    }
}
