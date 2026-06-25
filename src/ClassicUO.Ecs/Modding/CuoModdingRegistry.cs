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
[JsonSerializable(typeof(ModHovered))]
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
// Singleton resources exposed to mods (the "change resource" + "read input"
// capabilities).
[JsonSerializable(typeof(TinyEcs.Bevy.Time))]
[JsonSerializable(typeof(GameContextDto))]
[JsonSerializable(typeof(MouseInputDto))]
[JsonSerializable(typeof(KeyboardInputDto))]
// Tier 0: scene-state read/drive + text-field input infra.
[JsonSerializable(typeof(GameStateDto))]
[JsonSerializable(typeof(TextInput))]
[JsonSerializable(typeof(EditableText))]
[JsonSerializable(typeof(MaskedText))]
[JsonSerializable(typeof(FocusedInput))]
// Tier 1 scenes (zero-size markers) + the login-drive event.
[JsonSerializable(typeof(LoginScene))]
[JsonSerializable(typeof(ServerSelectionScene))]
[JsonSerializable(typeof(CharacterSelectionScene))]
[JsonSerializable(typeof(CharCreationScene))]
[JsonSerializable(typeof(LoginErrorScene))]
[JsonSerializable(typeof(GameScene))]
[JsonSerializable(typeof(OnLoginRequest))]
// Tier 2 gump window root markers (find / despawn / replace a window). Map/Vendor/
// Bulletin/Book deferred — their structs hold UOCustomRender/Dictionary/HashSet that
// STJ source-gen can't take raw (need a DTO follow-up).
[JsonSerializable(typeof(PaperdollWindow))]
[JsonSerializable(typeof(ContainerWindow))]
[JsonSerializable(typeof(BuffGumpUI))]
[JsonSerializable(typeof(JournalWindow))]
[JsonSerializable(typeof(SkillsWindow))]
[JsonSerializable(typeof(SpellbookWindow))]
[JsonSerializable(typeof(CombatBookWindow))]
[JsonSerializable(typeof(RacialBookWindow))]
[JsonSerializable(typeof(HealthBarWindow))]
[JsonSerializable(typeof(MiniMapWindow))]
[JsonSerializable(typeof(WorldMapWindow))]
[JsonSerializable(typeof(MenuGumpWindow))]
[JsonSerializable(typeof(MessageBoxWindow))]
[JsonSerializable(typeof(TipNoticeWindow))]
[JsonSerializable(typeof(LogoutGumpWindow))]
[JsonSerializable(typeof(PartyManifestWindow))]
[JsonSerializable(typeof(PartyInviteWindow))]
[JsonSerializable(typeof(ProfileWindow))]
[JsonSerializable(typeof(TradeWindow))]
[JsonSerializable(typeof(GridContainerWindow))]
[JsonSerializable(typeof(GridLootWindow))]
[JsonSerializable(typeof(SplitMenuWindow))]
[JsonSerializable(typeof(HouseDesignWindow))]
[JsonSerializable(typeof(ContainerOpenedEvent))]
[JsonSerializable(typeof(ContainerClosedEvent))]
// Tier 3 world-entity components. EquipmentSlots/MobileSteps deferred (InlineArray → DTO).
[JsonSerializable(typeof(NetworkSerial))]
[JsonSerializable(typeof(Graphic))]
[JsonSerializable(typeof(Hue))]
[JsonSerializable(typeof(WorldPosition))]
[JsonSerializable(typeof(Facing))]
[JsonSerializable(typeof(EntityName))]
[JsonSerializable(typeof(Notoriety))]
[JsonSerializable(typeof(Amount))]
[JsonSerializable(typeof(ContainerSlotPosition))]
[JsonSerializable(typeof(ContainedInto))]
[JsonSerializable(typeof(IsContainer))]
[JsonSerializable(typeof(Mobiles))]
[JsonSerializable(typeof(Items))]
[JsonSerializable(typeof(IsMulti))]
[JsonSerializable(typeof(ServerFlags))]
[JsonSerializable(typeof(MobAnimation))]
// Tier 4 action-state resources (read).
[JsonSerializable(typeof(TargetingState))]
[JsonSerializable(typeof(GrabbedItem))]
// DTOs for Tier 3/4 types whose raw struct can't cross STJ (InlineArray buffers):
// projected to flat arrays of the meaningful data.
[JsonSerializable(typeof(EquipmentSlotsDto))]
[JsonSerializable(typeof(MobStepDto))]
[JsonSerializable(typeof(MobileStepsDto))]
[JsonSerializable(typeof(PlayerStepDto))]
[JsonSerializable(typeof(PlayerStepsDto))]
// Scene info events (server/error serialize raw; character list via DTO).
[JsonSerializable(typeof(ServerSelectionInfoEvent))]
[JsonSerializable(typeof(LoginErrorsInfoEvent))]
[JsonSerializable(typeof(CharInfoDto))]
[JsonSerializable(typeof(TownInfoDto))]
[JsonSerializable(typeof(CharacterSelectionDto))]
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

/// Read the current GameState + DRIVE scene transitions. Read = the current enum
/// byte; write = QUEUE a transition (app.QueueState → applied at the frame boundary
/// so OnEnter/OnExit fire). Never SetState, which applies mid-frame and skips the
/// transition systems a mod-driven scene swap relies on.
internal sealed class ModGameState : IModResource
{
    public string GetJson(App app)
        => app.HasState<GameState>()
            ? JsonSerializer.Serialize(new GameStateDto { Current = (byte)app.GetState<GameState>() }, ModJsonContext.Default.GameStateDto)
            : "null";

    public void SetJson(App app, string json)
    {
        if (!app.HasState<GameState>())
            return;
        var dto = JsonSerializer.Deserialize(json, ModJsonContext.Default.GameStateDto);
        app.QueueState((GameState)dto.Current);
    }
}

/// Mod-facing GameState shape (the enum byte). Boot.cs GameState: 0=Loading,
/// 1=LoginScreen, 2=ServerSelection, 3=CharacterSelection, 4=CharacterCreation,
/// 5=LoginError, 6=GameScreen.
internal struct GameStateDto { public byte Current; }

// ── Deferred-type DTOs (Tier 3/4): host structs whose raw shape can't cross STJ
// (InlineArray fixed buffers). Each mapper projects the meaningful data — equipped
// item serials, queued steps — into a flat DTO; writes are no-ops (a mod changes
// these by sending packets, not by poking the buffer). ──────────────────────────

/// Equipped items by layer: Serials[layer] = the item's UO serial (0 = empty).
internal struct EquipmentSlotsDto { public uint[] Serials; }

internal sealed class ModEquipmentSlots : IModComponent
{
    private Query? _query;
    public bool Has(World world, ulong entity) => world.Has<EquipmentSlots>(entity);
    public void CollectEntities(World world, ref PooledList<ulong> into)
    {
        var q = _query ??= world.QueryBuilder().With<EquipmentSlots>().Build();
        var it = q.Iter();
        while (it.Next()) foreach (var ev in it.Entities()) into.Add(ev.ID);
    }
    public string GetJson(World world, ulong entity)
    {
        var eq = world.Get<EquipmentSlots>(entity);
        var serials = new uint[EquipmentSlots.LayerCount];
        for (var i = 0; i < EquipmentSlots.LayerCount; i++)
        {
            var id = eq[(ClassicUO.Game.Data.Layer)i];
            serials[i] = id != 0 && world.Has<NetworkSerial>(id) ? world.Get<NetworkSerial>(id).Value : 0u;
        }
        return JsonSerializer.Serialize(new EquipmentSlotsDto { Serials = serials }, ModJsonContext.Default.EquipmentSlotsDto);
    }
    public void SetJson(World world, ulong entity, string json) { }
    public void Remove(World world, ulong entity) => world.Entity(entity).Unset<EquipmentSlots>();
    public void RegisterInsertObserver(App app, System.Action<ulong, string> onFire)
        => app.AddObserver<OnInsert<EquipmentSlots>>(t => onFire(t.EntityId, GetJson(app.GetWorld(), t.EntityId)));
    public void RegisterRemoveObserver(App app, System.Action<ulong, string> onFire)
        => app.AddObserver<OnRemove<EquipmentSlots>>(t => onFire(t.EntityId, "{}"));
}

/// One queued smooth-walk step of a mobile.
internal struct MobStepDto { public int X, Y; public sbyte Z; public byte Direction; public bool Run; }
/// A mobile's pending movement-step queue (Index = current slot, -1 = empty).
internal struct MobileStepsDto { public int Index; public float Time; public MobStepDto[] Steps; }

internal sealed class ModMobileSteps : IModComponent
{
    private Query? _query;
    public bool Has(World world, ulong entity) => world.Has<MobileSteps>(entity);
    public void CollectEntities(World world, ref PooledList<ulong> into)
    {
        var q = _query ??= world.QueryBuilder().With<MobileSteps>().Build();
        var it = q.Iter();
        while (it.Next()) foreach (var ev in it.Entities()) into.Add(ev.ID);
    }
    public string GetJson(World world, ulong entity)
    {
        var ms = world.Get<MobileSteps>(entity);
        var n = ms.Index < 0 ? 0 : System.Math.Min(MobileSteps.COUNT, ms.Index + 1);
        var steps = new MobStepDto[n];
        for (var i = 0; i < n; i++)
        {
            var s = ms[i];
            steps[i] = new MobStepDto { X = s.X, Y = s.Y, Z = s.Z, Direction = s.Direction, Run = s.Run };
        }
        return JsonSerializer.Serialize(new MobileStepsDto { Index = ms.Index, Time = ms.Time, Steps = steps }, ModJsonContext.Default.MobileStepsDto);
    }
    public void SetJson(World world, ulong entity, string json) { }
    public void Remove(World world, ulong entity) => world.Entity(entity).Unset<MobileSteps>();
    public void RegisterInsertObserver(App app, System.Action<ulong, string> onFire)
        => app.AddObserver<OnInsert<MobileSteps>>(t => onFire(t.EntityId, GetJson(app.GetWorld(), t.EntityId)));
    public void RegisterRemoveObserver(App app, System.Action<ulong, string> onFire)
        => app.AddObserver<OnRemove<MobileSteps>>(t => onFire(t.EntityId, "{}"));
}

/// The local player's pending walk-request steps.
internal struct PlayerStepDto { public byte Sequence; public byte Direction; public ushort X, Y; public sbyte Z; }
internal struct PlayerStepsDto { public float LastStep; public int Count; public byte Sequence; public bool ResyncSent; public PlayerStepDto[] Steps; }

internal sealed class ModPlayerSteps : IModResource
{
    public string GetJson(App app)
    {
        if (!app.HasResource<PlayerStepsContext>()) return "null";
        var c = app.GetResource<PlayerStepsContext>();
        var n = System.Math.Clamp(c.Count, 0, 5);
        var steps = new PlayerStepDto[n];
        for (var i = 0; i < n; i++)
        {
            var s = c.Steps[i];
            steps[i] = new PlayerStepDto { Sequence = s.Sequence, Direction = (byte)s.Direction, X = s.X, Y = s.Y, Z = s.Z };
        }
        return JsonSerializer.Serialize(new PlayerStepsDto { LastStep = c.LastStep, Count = c.Count, Sequence = c.Sequence, ResyncSent = c.ResyncSent, Steps = steps }, ModJsonContext.Default.PlayerStepsDto);
    }
    public void SetJson(App app, string json) { }
}

// ── Scene info events (server/character/error lists). The host fires these via
// EventWriter; ModdingPlugin re-emits them as triggers (the forwarders) so a mod's
// On<T> observer — wired by ModEvent / the custom mapper below — receives them.
// Server + login-error serialize raw; the character list needs a DTO because
// TownInfo.Position is a ValueTuple STJ can't take. ──────────────────────────────

internal struct CharInfoDto { public string Name; public uint Index; }
internal struct TownInfoDto { public byte Index; public string Name; public string Building; public ushort X, Y; public sbyte Z; public uint Map; public uint ClilocDescription; }
internal struct CharacterSelectionDto { public CharInfoDto[] Characters; public TownInfoDto[] Towns; }

/// Maps CharacterSelectionInfoEvent → a flat DTO (flattens TownInfo's ValueTuple).
/// Observe-only: the host consumes the event via EventReader, so a mod re-emitting
/// it wouldn't reach the host scene — a mod drives selection via net.send.
internal sealed class ModCharacterSelectionEvent : IModEvent
{
    public void RegisterObserver(App app, System.Action<ulong, string> onFire)
        => app.AddObserver<On<CharacterSelectionInfoEvent>>(t => onFire(t.EntityId, ToJson(t.Event)));

    public void Emit(World world, ulong entity, string json) { }

    private static string ToJson(CharacterSelectionInfoEvent ev)
    {
        var chars = ev.Characters ?? new();
        var towns = ev.Towns ?? new();
        var cdto = new CharInfoDto[chars.Count];
        for (var i = 0; i < chars.Count; i++)
            cdto[i] = new CharInfoDto { Name = chars[i].Name, Index = chars[i].Index };
        var tdto = new TownInfoDto[towns.Count];
        for (var i = 0; i < towns.Count; i++)
        {
            var t = towns[i];
            tdto[i] = new TownInfoDto { Index = t.Index, Name = t.Name, Building = t.Building, X = t.Position.X, Y = t.Position.Y, Z = t.Position.Z, Map = t.Map, ClilocDescription = t.ClilocDescription };
        }
        return JsonSerializer.Serialize(new CharacterSelectionDto { Characters = cdto, Towns = tdto }, ModJsonContext.Default.CharacterSelectionDto);
    }
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
        // Tier 0 text-field input infra: a mod tags its glyph TextInput (focusable
        // I-beam) + EditableText (the global GuiPlugin.EditFocusedTextField appends/
        // backspaces typed chars into the focused glyph's Text/MaskedText), reads/
        // writes MaskedText for password-style fields, and sets cuo:ui/focused-input
        // to focus a field. (Full host-wired fields — caret/selection/geom — come via
        // the spawn-text-field capability; these expose the raw pieces.)
        reg.Register("cuo:ui/text-input", new ModComponent<TextInput>(ModJsonContext.Default.TextInput));
        reg.Register("cuo:ui/editable-text", new ModComponent<EditableText>(ModJsonContext.Default.EditableText));
        reg.Register("cuo:ui/masked-text", new ModComponent<MaskedText>(ModJsonContext.Default.MaskedText));
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
        // Sparse hover marker (host hover bridge keeps it on the topmost hovered
        // mod entity) — mods poll this instead of every element's Interaction byte.
        reg.Register("cuo:ui/hovered", new ModComponent<ModHovered>(ModJsonContext.Default.ModHovered));
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
        // Incoming-packet filtering is NOT an event: a mod exports
        // `on-incoming-packet(id, data) -> bool` (see cuo-modding.wit / ModNetTap)
        // and NetworkPlugin.PacketReader calls it synchronously before dispatch.
        // Singleton resources — read/write host state by type-path.
        reg.RegisterResource("cuo:engine/time", new ModResource<TinyEcs.Bevy.Time>(ModJsonContext.Default.Time));
        reg.RegisterResource("cuo:game/context", new ModGameContext());
        // Tier 0: read current scene + drive transitions; focus a text field.
        reg.RegisterResource("cuo:game/state", new ModGameState());
        reg.RegisterResource("cuo:ui/focused-input", new ModResource<FocusedInput>(ModJsonContext.Default.FocusedInput));
        reg.RegisterResource("cuo:input/mouse", new ModMouseInput());
        reg.RegisterResource("cuo:input/keyboard", new ModKeyboardInput());

        // ── Tier 1: scene roots (query `with` to find, despawn to replace) + the
        // login driver (a mod emits cuo:scene/login-request to log in). ────────────
        reg.Register("cuo:scene/login", new ModComponent<LoginScene>(ModJsonContext.Default.LoginScene));
        reg.Register("cuo:scene/server-selection", new ModComponent<ServerSelectionScene>(ModJsonContext.Default.ServerSelectionScene));
        reg.Register("cuo:scene/character-selection", new ModComponent<CharacterSelectionScene>(ModJsonContext.Default.CharacterSelectionScene));
        reg.Register("cuo:scene/character-creation", new ModComponent<CharCreationScene>(ModJsonContext.Default.CharCreationScene));
        reg.Register("cuo:scene/login-error", new ModComponent<LoginErrorScene>(ModJsonContext.Default.LoginErrorScene));
        reg.Register("cuo:scene/game", new ModComponent<GameScene>(ModJsonContext.Default.GameScene));
        reg.RegisterEvent("cuo:scene/login-request", new ModEvent<OnLoginRequest>(ModJsonContext.Default.OnLoginRequest));
        // Scene info events (host EventWriter → re-emitted as triggers by the
        // ModdingPlugin forwarders, so these On<T>-based observers receive them).
        reg.RegisterEvent("cuo:scene/server-list", new ModEvent<ServerSelectionInfoEvent>(ModJsonContext.Default.ServerSelectionInfoEvent));
        reg.RegisterEvent("cuo:scene/login-error-info", new ModEvent<LoginErrorsInfoEvent>(ModJsonContext.Default.LoginErrorsInfoEvent));
        reg.RegisterEvent("cuo:scene/character-list", new ModCharacterSelectionEvent());

        // ── Tier 2: gump window root markers. Query `with cuo:gump/<x>` to find a
        // window, despawn its root to remove it, or read the marker for its serial. ─
        reg.Register("cuo:gump/paperdoll", new ModComponent<PaperdollWindow>(ModJsonContext.Default.PaperdollWindow));
        reg.Register("cuo:gump/container", new ModComponent<ContainerWindow>(ModJsonContext.Default.ContainerWindow));
        reg.Register("cuo:gump/buff", new ModComponent<BuffGumpUI>(ModJsonContext.Default.BuffGumpUI));
        reg.Register("cuo:gump/journal", new ModComponent<JournalWindow>(ModJsonContext.Default.JournalWindow));
        reg.Register("cuo:gump/skills", new ModComponent<SkillsWindow>(ModJsonContext.Default.SkillsWindow));
        reg.Register("cuo:gump/spellbook", new ModComponent<SpellbookWindow>(ModJsonContext.Default.SpellbookWindow));
        reg.Register("cuo:gump/combat-book", new ModComponent<CombatBookWindow>(ModJsonContext.Default.CombatBookWindow));
        reg.Register("cuo:gump/racial-book", new ModComponent<RacialBookWindow>(ModJsonContext.Default.RacialBookWindow));
        reg.Register("cuo:gump/health-bar", new ModComponent<HealthBarWindow>(ModJsonContext.Default.HealthBarWindow));
        reg.Register("cuo:gump/minimap", new ModComponent<MiniMapWindow>(ModJsonContext.Default.MiniMapWindow));
        reg.Register("cuo:gump/worldmap", new ModComponent<WorldMapWindow>(ModJsonContext.Default.WorldMapWindow));
        reg.Register("cuo:gump/menu", new ModComponent<MenuGumpWindow>(ModJsonContext.Default.MenuGumpWindow));
        reg.Register("cuo:gump/message-box", new ModComponent<MessageBoxWindow>(ModJsonContext.Default.MessageBoxWindow));
        reg.Register("cuo:gump/tip-notice", new ModComponent<TipNoticeWindow>(ModJsonContext.Default.TipNoticeWindow));
        reg.Register("cuo:gump/logout", new ModComponent<LogoutGumpWindow>(ModJsonContext.Default.LogoutGumpWindow));
        reg.Register("cuo:gump/party-manifest", new ModComponent<PartyManifestWindow>(ModJsonContext.Default.PartyManifestWindow));
        reg.Register("cuo:gump/party-invite", new ModComponent<PartyInviteWindow>(ModJsonContext.Default.PartyInviteWindow));
        reg.Register("cuo:gump/profile", new ModComponent<ProfileWindow>(ModJsonContext.Default.ProfileWindow));
        reg.Register("cuo:gump/trade", new ModComponent<TradeWindow>(ModJsonContext.Default.TradeWindow));
        reg.Register("cuo:gump/grid-container", new ModComponent<GridContainerWindow>(ModJsonContext.Default.GridContainerWindow));
        reg.Register("cuo:gump/grid-loot", new ModComponent<GridLootWindow>(ModJsonContext.Default.GridLootWindow));
        reg.Register("cuo:gump/split-menu", new ModComponent<SplitMenuWindow>(ModJsonContext.Default.SplitMenuWindow));
        reg.Register("cuo:gump/house-design", new ModComponent<HouseDesignWindow>(ModJsonContext.Default.HouseDesignWindow));
        reg.RegisterEvent("cuo:gump/container-opened", new ModEvent<ContainerOpenedEvent>(ModJsonContext.Default.ContainerOpenedEvent));
        reg.RegisterEvent("cuo:gump/container-closed", new ModEvent<ContainerClosedEvent>(ModJsonContext.Default.ContainerClosedEvent));
        // Presence-only (their structs hold UOCustomRender/Dictionary/HashSet that
        // STJ can't take raw): a mod finds + despawns them via `with`, get() = "{}".
        reg.Register("cuo:gump/map", new ModPresence<MapWindow>());
        reg.Register("cuo:gump/vendor", new ModPresence<VendorWindow>());
        reg.Register("cuo:gump/bulletin-board", new ModPresence<BulletinBoardWindow>());
        reg.Register("cuo:gump/book", new ModPresence<BookWindow>());

        // ── Tier 3: world-entity components (mobiles + items). Query by serial,
        // read graphic/hue/position/name/notoriety, mutate where it makes sense. ────
        reg.Register("cuo:ent/serial", new ModComponent<NetworkSerial>(ModJsonContext.Default.NetworkSerial));
        reg.Register("cuo:ent/graphic", new ModComponent<Graphic>(ModJsonContext.Default.Graphic));
        reg.Register("cuo:ent/hue", new ModComponent<Hue>(ModJsonContext.Default.Hue));
        reg.Register("cuo:ent/world-position", new ModComponent<WorldPosition>(ModJsonContext.Default.WorldPosition));
        reg.Register("cuo:ent/facing", new ModComponent<Facing>(ModJsonContext.Default.Facing));
        reg.Register("cuo:ent/name", new ModComponent<EntityName>(ModJsonContext.Default.EntityName));
        reg.Register("cuo:ent/notoriety", new ModComponent<Notoriety>(ModJsonContext.Default.Notoriety));
        reg.Register("cuo:ent/amount", new ModComponent<Amount>(ModJsonContext.Default.Amount));
        reg.Register("cuo:ent/slot-position", new ModComponent<ContainerSlotPosition>(ModJsonContext.Default.ContainerSlotPosition));
        reg.Register("cuo:ent/contained-into", new ModComponent<ContainedInto>(ModJsonContext.Default.ContainedInto));
        reg.Register("cuo:ent/is-container", new ModComponent<IsContainer>(ModJsonContext.Default.IsContainer));
        reg.Register("cuo:ent/is-mobile", new ModComponent<Mobiles>(ModJsonContext.Default.Mobiles));
        reg.Register("cuo:ent/is-item", new ModComponent<Items>(ModJsonContext.Default.Items));
        reg.Register("cuo:ent/is-multi", new ModComponent<IsMulti>(ModJsonContext.Default.IsMulti));
        reg.Register("cuo:ent/server-flags", new ModComponent<ServerFlags>(ModJsonContext.Default.ServerFlags));
        reg.Register("cuo:ent/animation", new ModComponent<MobAnimation>(ModJsonContext.Default.MobAnimation));
        // InlineArray-backed → projected to flat DTOs (equipped item serials by layer; queued steps).
        reg.Register("cuo:ent/equipment", new ModEquipmentSlots());
        reg.Register("cuo:ent/mob-steps", new ModMobileSteps());

        // ── Tier 4: action-state resources (read; mods drive actions via net.send). ─
        reg.RegisterResource("cuo:target/state", new ModResource<TargetingState>(ModJsonContext.Default.TargetingState));
        reg.RegisterResource("cuo:player/grabbed-item", new ModResource<GrabbedItem>(ModJsonContext.Default.GrabbedItem));
        reg.RegisterResource("cuo:player/steps", new ModPlayerSteps());
        return reg;
    }
}
