// ClassicUO's set of mod-exposable components/resources for the tinyecs:modding
// modding API. The generic registry MECHANISM (IModComponent/IModResource/
// ModComponent<T>/ModResource<T>/ModComponentRegistry) lives in the reusable
// TinyEcs.Bevy.Modding library; this file is the cuo-specific CONTENT: which host
// components + resources are exposed, under which WIT type-paths, plus the
// hand-mapped DTOs for host types that can't cross STJ raw.
//
// AOT-safe: every entry is a closed generic (no reflection); JSON goes through
// System.Text.Json source-gen (ModJsonContext).

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.Modding;
using TinyEcs.Collections;

namespace ClassicUO.Ecs.Modding;

/// Demo component a mod can read/write across the WASM boundary. Proves the
/// round-trip; real exposable components get registered the same way.
internal struct ModCounter
{
    public int Value { get; set; }
}

// IncludeFields: Node/Val/UiRect/Text/TextFont expose public FIELDS, not props.
[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(ModCounter))]
[JsonSerializable(typeof(Node))]
[JsonSerializable(typeof(Text))]
[JsonSerializable(typeof(TextFont))]
[JsonSerializable(typeof(TextColor))]
[JsonSerializable(typeof(BackgroundColor))]
[JsonSerializable(typeof(Interaction))]
[JsonSerializable(typeof(UiName))]
[JsonSerializable(typeof(UiCustomDto))]
[JsonSerializable(typeof(TopBarFull))]
[JsonSerializable(typeof(IsTopBar))]
[JsonSerializable(typeof(TopBarDragHandle))]
[JsonSerializable(typeof(TopBarButton))]
[JsonSerializable(typeof(OptionsWindow))]
[JsonSerializable(typeof(ScrollPosition))]
[JsonSerializable(typeof(ModClicked))]
// Player stats + status-bar handle — let mods read the live player state and
// replace the host status gump (despawn StatusBarWindow, render their own).
[JsonSerializable(typeof(Hits))]
[JsonSerializable(typeof(Mana))]
[JsonSerializable(typeof(Stamina))]
[JsonSerializable(typeof(PlayerData))]
[JsonSerializable(typeof(StatLocks))]
[JsonSerializable(typeof(Player))]
[JsonSerializable(typeof(StatusBarWindow))]
// Status-gump parts a mod reuses so host systems (minimize, lock-graphic
// refresh) treat mod-built entities identically: the cliloc tooltip text, the
// buff-icon button marker (minimize-exempt), the stat-lock button marker.
[JsonSerializable(typeof(UiTooltip))]
[JsonSerializable(typeof(UOButton))]
[JsonSerializable(typeof(StatLockButton))]
// Window infra so a mod-built gump behaves like a host gump (drag / right-click
// close / z-stack) via the shared WindowDragPlugin — no host edit.
[JsonSerializable(typeof(UiMovable))]
[JsonSerializable(typeof(UiMovableNoDrag))]
[JsonSerializable(typeof(UiNoWindowDrag))]
[JsonSerializable(typeof(UiContainsByBounds))]
[JsonSerializable(typeof(GlobalZIndex))]
// One incoming packet (raw observe stream).
[JsonSerializable(typeof(ModIncomingPacket))]
// Singleton resources exposed to mods (the "change resource" + "read input"
// capabilities).
[JsonSerializable(typeof(TinyEcs.Bevy.Time))]
[JsonSerializable(typeof(GameContextDto))]
[JsonSerializable(typeof(MouseInputDto))]
[JsonSerializable(typeof(KeyboardInputDto))]
internal partial class ModJsonContext : JsonSerializerContext;

/// Mod-facing subset of GameContext (a big host struct holding a non-serializable
/// LightRenderData). Hand-mapped so mods can read/write the safe identity fields
/// (player name/serial, map, season) without dragging the render buffer across.
internal struct GameContextDto
{
    public uint PlayerSerial;
    public string PlayerName;
    public int Map;
    public byte Season;
    // Lets a mod pick the status-gump layout the same way the host does
    // (modern AOS vs classic): modern = ClientVersion >= CV_308Z (0x0300087A)
    // && !UseOldStatusGump. ClientVersion is the packed version int.
    public uint ClientVersion;
    public bool UseOldStatusGump;
}

/// Mouse input snapshot mods read via cuo:input/mouse (read-only). Position is
/// UI-layout space (the one space every hit-test reasons in).
internal struct MouseInputDto
{
    public float X, Y;
    public bool Left, Right, Middle;
    public float Wheel;
}

/// Keyboard snapshot mods read via cuo:input/keyboard (read-only). Pressed = the
/// currently-down key codes (FNA Keys as ints).
internal struct KeyboardInputDto
{
    public int[] Pressed;
}

/// Mod-facing shape of UiCustom's UOCustomRender (which is a reference class
/// with non-serializable fields). Mods set a UO sprite via this; the registry
/// rebuilds the real UOCustomRender. Kind: 0=Gump (see UOCustomKind).
internal struct UiCustomDto
{
    public byte Kind;
    public uint AssetId;
    public float HueX, HueY, HueZ;
}

/// Hand-mapped component (UiCustom.Data is an object holding the engine's
/// UOCustomRender class — can't STJ it directly), so a mod can render a UO
/// sprite (e.g. the marble button gump) like native gumps do.
internal sealed class ModUiCustom : IModComponent
{
    public bool Has(World world, ulong entity) => world.Has<UiCustom>(entity);

    public void CollectEntities(World world, ref PooledList<ulong> into)
    {
        var q = world.QueryBuilder().With<UiCustom>().Build();
        var it = q.Iter();
        while (it.Next())
            foreach (var ev in it.Entities())
                into.Add(ev.ID);
    }

    public string GetJson(World world, ulong entity) => ToJson(world.Get<UiCustom>(entity));

    private static string ToJson(UiCustom c)
    {
        if (c.Data is not UOCustomRender d)
            return "null";
        var dto = new UiCustomDto { Kind = (byte)d.Kind, AssetId = d.AssetId, HueX = d.Hue.X, HueY = d.Hue.Y, HueZ = d.Hue.Z };
        return JsonSerializer.Serialize(dto, ModJsonContext.Default.UiCustomDto);
    }

    public void SetJson(World world, ulong entity, string json)
    {
        var dto = JsonSerializer.Deserialize(json, ModJsonContext.Default.UiCustomDto);
        world.Set(entity, new UiCustom
        {
            Data = new UOCustomRender
            {
                Kind = (UOCustomKind)dto.Kind,
                AssetId = dto.AssetId,
                Hue = new Vector3(dto.HueX, dto.HueY, dto.HueZ),
            }
        });
    }

    public void Remove(World world, ulong entity) => world.Entity(entity).Unset<UiCustom>();

    public void RegisterInsertObserver(App app, System.Action<ulong, string> onFire)
        => app.AddObserver<OnInsert<UiCustom>>(t => onFire(t.EntityId, ToJson(t.Component)));

    public void RegisterRemoveObserver(App app, System.Action<ulong, string> onFire)
        => app.AddObserver<OnRemove<UiCustom>>(t => onFire(t.EntityId, ToJson(t.Component)));
}

/// Hand-mapped GameContext ↔ GameContextDto (GameContext holds a non-serializable
/// LightRenderData, so it can't go through STJ raw).
internal sealed class ModGameContext : IModResource
{
    public string GetJson(App app)
    {
        if (!app.HasResource<GameContext>())
            return "null";
        var gc = app.GetResource<GameContext>();
        var dto = new GameContextDto
        {
            PlayerSerial = gc.PlayerSerial,
            PlayerName = gc.PlayerName ?? string.Empty,
            Map = gc.Map,
            Season = gc.Season,
            ClientVersion = (uint)gc.ClientVersion,
            UseOldStatusGump = app.HasResource<ClassicUO.Configuration.Profile>()
                && app.GetResource<ClassicUO.Configuration.Profile>().UseOldStatusGump,
        };
        return JsonSerializer.Serialize(dto, ModJsonContext.Default.GameContextDto);
    }

    public void SetJson(App app, string json)
    {
        if (!app.HasResource<GameContext>())
            return;
        var dto = JsonSerializer.Deserialize(json, ModJsonContext.Default.GameContextDto);
        ref var gc = ref app.GetResourceRef<GameContext>();
        gc.PlayerSerial = dto.PlayerSerial;
        gc.PlayerName = dto.PlayerName;
        gc.Map = dto.Map;
        gc.Season = dto.Season;
    }
}

/// Read-only mouse input (cuo:input/mouse). Set is a no-op — input state is fed
/// by the device each frame; mods influence it via the consume capability, not
/// by overwriting the snapshot.
internal sealed class ModMouseInput : IModResource
{
    public string GetJson(App app)
    {
        if (!app.HasResource<MouseContext>())
            return "null";
        var m = app.GetResource<MouseContext>();
        var dto = new MouseInputDto
        {
            X = m.Position.X,
            Y = m.Position.Y,
            Left = m.IsPressed(MouseButtonType.Left),
            Right = m.IsPressed(MouseButtonType.Right),
            Middle = m.IsPressed(MouseButtonType.Middle),
            Wheel = m.Wheel,
        };
        return JsonSerializer.Serialize(dto, ModJsonContext.Default.MouseInputDto);
    }

    public void SetJson(App app, string json) { }
}

/// Read-only keyboard input (cuo:input/keyboard).
internal sealed class ModKeyboardInput : IModResource
{
    public string GetJson(App app)
    {
        if (!app.HasResource<KeyboardContext>())
            return "null";
        var keys = app.GetResource<KeyboardContext>().GetPressedKeys();
        var dto = new KeyboardInputDto { Pressed = new int[keys.Length] };
        for (var i = 0; i < keys.Length; i++)
            dto.Pressed[i] = (int)keys[i];
        return JsonSerializer.Serialize(dto, ModJsonContext.Default.KeyboardInputDto);
    }

    public void SetJson(App app, string json) { }
}

/// Builds the cuo registry: the default set of mod-exposable components +
/// resources, keyed by WIT type-path. Handed to the modding plugin via
/// ModdingConfig.Registry.
internal static class CuoModdingRegistry
{
    public static ModComponentRegistry Build()
    {
        var reg = new ModComponentRegistry();
        reg.Register("cuo:test/counter", new ModComponent<ModCounter>(ModJsonContext.Default.ModCounter));
        // UI components — let mods build/attach gump nodes.
        reg.Register("cuo:ui/node", new ModComponent<Node>(ModJsonContext.Default.Node));
        reg.Register("cuo:ui/text", new ModComponent<Text>(ModJsonContext.Default.Text));
        reg.Register("cuo:ui/text-font", new ModComponent<TextFont>(ModJsonContext.Default.TextFont));
        reg.Register("cuo:ui/text-color", new ModComponent<TextColor>(ModJsonContext.Default.TextColor));
        reg.Register("cuo:ui/bg-color", new ModComponent<BackgroundColor>(ModJsonContext.Default.BackgroundColor));
        reg.Register("cuo:ui/custom", new ModUiCustom());
        reg.Register("cuo:ui/interaction", new ModComponent<Interaction>(ModJsonContext.Default.Interaction));
        // Generic stable identity — a mod can name its own nodes for bookkeeping.
        reg.Register("cuo:ui/name", new ModComponent<UiName>(ModJsonContext.Default.UiName));
        // Existing host UI markers exposed for mods to find/navigate gumps
        // (NOT added to the gump files — these already exist on the entities).
        reg.Register("cuo:ui/options-window", new ModComponent<OptionsWindow>(ModJsonContext.Default.OptionsWindow));
        reg.Register("cuo:ui/scroll", new ModComponent<ScrollPosition>(ModJsonContext.Default.ScrollPosition));
        // Markers — query-only handles into existing gumps + click feedback.
        reg.Register("cuo:ui/topbar-full", new ModComponent<TopBarFull>(ModJsonContext.Default.TopBarFull));
        reg.Register("cuo:ui/topbar-root", new ModComponent<IsTopBar>(ModJsonContext.Default.IsTopBar));
        reg.Register("cuo:ui/topbar-bg", new ModComponent<TopBarDragHandle>(ModJsonContext.Default.TopBarDragHandle));
        reg.Register("cuo:ui/topbar-button", new ModComponent<TopBarButton>(ModJsonContext.Default.TopBarButton));
        reg.Register("cuo:ui/clicked", new ModComponent<ModClicked>(ModJsonContext.Default.ModClicked));
        // Player stats (packet-updated in place by the host) — a mod reads these
        // to render its own status gump. cuo:ui/statusbar-window is the host bar's
        // root marker, so a mod can despawn it and show its own instead.
        reg.Register("cuo:player/player", new ModComponent<Player>(ModJsonContext.Default.Player));
        reg.Register("cuo:player/hits", new ModComponent<Hits>(ModJsonContext.Default.Hits));
        reg.Register("cuo:player/mana", new ModComponent<Mana>(ModJsonContext.Default.Mana));
        reg.Register("cuo:player/stamina", new ModComponent<Stamina>(ModJsonContext.Default.Stamina));
        reg.Register("cuo:player/data", new ModComponent<PlayerData>(ModJsonContext.Default.PlayerData));
        reg.Register("cuo:player/stat-locks", new ModComponent<StatLocks>(ModJsonContext.Default.StatLocks));
        reg.Register("cuo:ui/statusbar-window", new ModComponent<StatusBarWindow>(ModJsonContext.Default.StatusBarWindow));
        // Cliloc tooltip text (host TooltipPlugin renders it generically).
        reg.Register("cuo:ui/tooltip", new ModComponent<UiTooltip>(ModJsonContext.Default.UiTooltip));
        // Button markers a mod tags onto its status-gump controls so the host's
        // generic systems treat them like native ones: UOButton (press-state
        // graphic swap + minimize-latch exempt), StatLockButton (minimize-latch
        // exempt + host Refresh maintains the lock graphic from the player's
        // StatLocks). Both are minimize-exempt in HealthBarPlugin.HbInteractParams.
        reg.Register("cuo:ui/button", new ModComponent<UOButton>(ModJsonContext.Default.UOButton));
        reg.Register("cuo:ui/stat-lock-button", new ModComponent<StatLockButton>(ModJsonContext.Default.StatLockButton));
        // Movable-window infra (shared WindowDragPlugin honours these generically).
        reg.Register("cuo:ui/movable", new ModComponent<UiMovable>(ModJsonContext.Default.UiMovable));
        // Whole-window nomove (on the root). NOT a per-control opt-out.
        reg.Register("cuo:ui/movable-no-drag", new ModComponent<UiMovableNoDrag>(ModJsonContext.Default.UiMovableNoDrag));
        // Per-control drag opt-out (host WindowDragPlugin scans this BY BOUNDS): a
        // press on a tagged child reaches its own UiClick instead of latching the
        // window drag. Interactive children of a movable mod window need this.
        reg.Register("cuo:ui/no-window-drag", new ModComponent<UiNoWindowDrag>(ModJsonContext.Default.UiNoWindowDrag));
        reg.Register("cuo:ui/contains-by-bounds", new ModComponent<UiContainsByBounds>(ModJsonContext.Default.UiContainsByBounds));
        reg.Register("cuo:ui/global-z", new ModComponent<GlobalZIndex>(ModJsonContext.Default.GlobalZIndex));
        // Incoming-packet observe stream: a custom event a mod observes via
        // add_observer(Custom("cuo:net/incoming")). Host emits it per tapped packet
        // (NetworkPlugin.PacketReader) — no entity, no per-frame spawn/despawn.
        reg.RegisterEvent("cuo:net/incoming", new ModEvent<ModIncomingPacket>(ModJsonContext.Default.ModIncomingPacket));
        // Singleton resources — read/write host state by type-path.
        reg.RegisterResource("cuo:engine/time", new ModResource<TinyEcs.Bevy.Time>(ModJsonContext.Default.Time));
        reg.RegisterResource("cuo:game/context", new ModGameContext());
        reg.RegisterResource("cuo:input/mouse", new ModMouseInput());
        reg.RegisterResource("cuo:input/keyboard", new ModKeyboardInput());
        return reg;
    }
}
