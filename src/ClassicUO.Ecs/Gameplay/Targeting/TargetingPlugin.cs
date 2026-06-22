// Port of legacy Game/Managers/TargetManager.cs to the ECS branch.
//
// Flow: the server pushes 0x6C (OnTargetCursorPacket) to ask the client to
// pick a target. We flip TargetingState.IsTargeting on; the targeting cursor
// reticle renders at the mouse, normal world interaction (pickup / use) is
// gated off (RunIf(!IsTargeting) on those plugins), and the NEXT left-click on
// a world object/tile sends the 0x6C response (Send_TargetObject for an entity,
// Send_TargetXYZ for ground/static) echoing the server's cursorID + cursorType.
// Right-click or Escape cancels (Send_TargetCancel).
//
// World selection reuses Res<SelectedEntity> — the same one-frame-lagged hit
// the pickup/use systems read — so targeting picks exactly what the highlight
// shows. Object vs position is decided by whether the hit entity carries a
// NetworkSerial (mobile/ground item) or is a land/static tile.

using System;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct TargetingPlugin : IPlugin
{
    // The targeting reticle cursor (legacy _cursorData[war, 12]) is drawn by
    // GameCursorPlugin — it owns the whole cursor (border-clip, hotspot, felucca
    // hue) so the reticle gets the same treatment as every other cursor state.
    // This plugin only owns the targeting STATE + click/cancel resolution.

    // QuestionGump art (legacy Game/UI/Gumps/QuestionGump.cs) — reused for the
    // client-side criminal-action confirm, same as LogoutGumpPlugin.
    private const ushort QuestionBg = 0x0816;
    private const ushort CancelN = 0x0817, CancelP = 0x0818, CancelO = 0x0819;
    private const ushort OkN = 0x081A, OkP = 0x081B, OkO = 0x081C;

    public void Build(App app)
    {
        app.AddResource(new TargetingState());
        app.AddResource(new CriminalConfirmState());
        app.AddResource(new TargetQueueState());

        // Server -> client target request. Cancel (type 3) just clears; any
        // other type arms targeting with the server's echoed cursor id/type.
        app.AddObserver((
            On<PacketReceived<OnTargetCursorPacket_0x6C>> trig,
            ResMut<TargetingState> targeting,
            ResMut<TargetQueueState> queue,
            ResMut<LastTargetState> lastTarget,
            Res<Time> time,
            Res<NetClient> net) =>
        {
            var packet = trig.Event.Packet;
            if (packet.CursorType >= (byte)TargetType.Cancel)
            {
                targeting.Value.Clear();
                return;
            }

            targeting.Value.IsTargeting = true;
            targeting.Value.Mode = packet.CursorTarget;
            targeting.Value.CursorId = packet.CursorId;
            targeting.Value.CursorType = packet.CursorType;

            // UOSteam target queue: a target queued while no cursor was up
            // auto-applies to this fresh OBJECT cursor, within the 5s timeout.
            // Expired → drop it. A positional/multi cursor leaves the queue armed
            // for a later object cursor.
            if (queue.Value.Armed)
            {
                if (time.Value.Total > queue.Value.ExpireAt)
                {
                    queue.Value.Clear();
                }
                else if (targeting.Value.Mode == 0)
                {
                    net.Value.Send_TargetObject(
                        queue.Value.Serial, queue.Value.Graphic,
                        queue.Value.X, queue.Value.Y, queue.Value.Z,
                        targeting.Value.CursorId, targeting.Value.CursorType);
                    lastTarget.Value.Set(queue.Value.Serial, queue.Value.Graphic,
                        queue.Value.X, queue.Value.Y, queue.Value.Z);
                    targeting.Value.Clear();
                    queue.Value.Clear();
                }
            }
        });

        // 0x99 is self-arming (legacy SetTargetingMulti): the server does NOT send
        // a 0x6C for house-deed placement — 0x99 alone puts the cursor in multi
        // placement mode AND carries the multi graphic + drop offset. The deed
        // serial is the cursor id echoed in the click response (Send_TargetXYZ).
        app.AddObserver((
            On<PacketReceived<OnPlaceMultiPacket_0x99>> trig,
            ResMut<TargetingState> targeting) =>
        {
            var packet = trig.Event.Packet;
            targeting.Value.IsTargeting = true;
            targeting.Value.Mode = MultiMode;
            targeting.Value.CursorId = packet.TargetSerial;
            targeting.Value.CursorType = 0; // neutral
            targeting.Value.MultiId = packet.MultiId;
            targeting.Value.MultiOffsetX = packet.OffsetX;
            targeting.Value.MultiOffsetY = packet.OffsetY;
            targeting.Value.MultiOffsetZ = packet.OffsetZ;
            targeting.Value.MultiHue = packet.Hue;
        });

        var clickFn = ResolveTargetClick;
        var cancelFn = CancelTarget;
        var previewFn = SyncMultiPreview;
        var previewExitFn = DespawnMultiPreviewOnExit;
        var resetConfirmFn = ResetConfirmIfClosed;
        var selectUiFn = SelectUiItemWhileTargeting;

        // Stage.First so the target/cancel consume their mouse button before the
        // pickup latch (also Stage.First, gated off while targeting) and before
        // PreUpdate's right-click gump-close / world movement see it.
        // Both suspend while a criminal-action confirm gump is open so its
        // Cancel/OK buttons own the click and the cursor stays armed underneath.
        app
            .AddSystem(cancelFn)
                .InStage(Stage.First)
                .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
                .RunIf((Res<TargetingState> t) => t.Value.IsTargeting)
                .RunIf((Res<CriminalConfirmState> c) => !c.Value.Open)
                .Build()
            .AddSystem(clickFn)
                .InStage(Stage.First)
                .Label("cuo:targeting:click")
                // WorldMap positional-target (added earlier in Boot) must resolve a
                // map click before this generic resolver consumes it.
                .After("cuo:worldmap:positional")
                .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
                .RunIf((Res<TargetingState> t) => t.Value.IsTargeting)
                .RunIf((Res<CriminalConfirmState> c) => !c.Value.Open)
                .RunIf((Res<MouseContext> m) => m.Value.IsPressedOnce(Input.MouseButtonType.Left))
                .Build()
            // Not gated on IsTargeting — the !show branch despawns the ghost when
            // targeting ends. Runs in Update like HouseCustomization.SyncPreview.
            .AddSystem(previewFn)
                .InStage(Stage.Update)
                .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
                .Build()
            // Safety net: if the confirm gump vanished by any path (right-click,
            // scene exit) without resetting the flag, un-stick targeting.
            .AddSystem(resetConfirmFn)
                .InStage(Stage.First)
                .RunIf((Res<CriminalConfirmState> c) => c.Value.Open)
                .Build()
            // While targeting, claim the hovered container/paperdoll UI item as the
            // SelectedEntity so ResolveTargetClick can object-target it. Stage.Last,
            // built after WindowDragPlugin, so it overrides that plugin's window
            // claim (paperdoll items would otherwise resolve to the window root).
            .AddSystem(selectUiFn)
                .InStage(Stage.Last)
                .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
                .RunIf((Res<TargetingState> t) => t.Value.IsTargeting)
                .Build();

        app.AddSystem(previewExitFn).OnExit(GameState.GameScreen).Build();
    }

    // Legacy GameScene multi-placement preview: a translucent ghost of the multi
    // follows the cursor tile while a 0x99 multi target is armed. Blocks are
    // spawned once per graphic and repositioned each frame (no per-frame
    // spawn/despawn churn), mirroring legacy's reuse of _multi + House.Components.
    private static void SyncMultiPreview(
        Commands commands,
        ResMut<TargetingState> targeting,
        Res<SelectedEntity> selected,
        Res<MultiCache> multis,
        Query<Data<WorldPosition>> worldQ,
        Query<Data<WorldPosition, ScreenPosition, AlphaFade, MultiPreviewOffset>, With<MultiPlacementPreview>> previewQ)
    {
        var st = targeting.Value;
        bool show = st.IsTargeting && st.Mode == MultiMode && st.MultiId != 0;

        // Targeting ended -> tear down the ghost.
        if (!show)
        {
            if (st.PreviewBuiltId != 0)
                ClearMultiPreview(commands, selected, previewQ, st);
            return;
        }

        // Hovered tile = the render's pixel pick (preview blocks excluded via
        // IgnorePickEntities), so the ghost lands where the deed will drop. On a
        // stray no-hit frame (cursor over a transparent gap / world edge / a gump)
        // KEEP the ghost where it is — despawn+rebuild every such frame is what
        // made it flicker. Legacy only repositions on a hit, removes only on end.
        var hit = selected.Value.Entity;
        if (hit == 0 || !worldQ.TryGet(hit, out var hitRow))
            return;

        var (_, hitPos) = hitRow;
        // base = hovered tile - deed offset (legacy). We use the hovered tile's
        // own Z for ground; legacy re-derives via GetMapZ — equivalent for the
        // terrain tiles a deed targets.
        int baseX = hitPos.Ref.X - st.MultiOffsetX;
        int baseY = hitPos.Ref.Y - st.MultiOffsetY;
        int baseZ = hitPos.Ref.Z - st.MultiOffsetZ;

        // (Re)build when the multi graphic changes; blocks become queryable next
        // sync point, so reposition + ignore-set are filled on the following frame.
        if (st.PreviewBuiltId != st.MultiId)
        {
            ClearMultiPreview(commands, selected, previewQ, st);
            foreach (ref readonly var block in CollectionsMarshal.AsSpan(multis.Value.GetMulti(st.MultiId).Blocks))
            {
                if (!block.IsVisible)
                    continue;

                var wp = new WorldPosition
                {
                    X = (ushort)(baseX + block.X),
                    Y = (ushort)(baseY + block.Y),
                    Z = (sbyte)(baseZ + block.Z),
                };
                commands.Spawn()
                    .Insert(new Graphic { Value = block.ID })
                    .Insert(new Hue { Value = st.MultiHue })
                    .Insert(wp)
                    .Insert(new ScreenPosition { Value = wp.WorldToScreen() })
                    .Insert(new AlphaFade { Value = PreviewAlpha })
                    // Mode 1 = translucent: RenderStatics' ProcessFade settles the
                    // alpha at 178 and HOLDS it. Without this it treats the block as
                    // a normal visible static and ramps alpha to 0xFF each fade tick,
                    // so re-stamping a low alpha per frame pulsed it (the flicker).
                    .Insert(new HouseVisionFade { Mode = 1 })
                    .Insert(new MultiPreviewOffset { X = block.X, Y = block.Y, Z = block.Z })
                    .Insert<MultiPlacementPreview>()
                    .Insert<NormalMulti>();
            }
            st.PreviewBuiltId = st.MultiId;
            return;
        }

        // Reposition existing blocks and rebuild the pick-ignore set (so the
        // preview never selects itself). Alpha is owned by ProcessFade now (held
        // translucent via HouseVisionFade) — do NOT re-stamp it here.
        selected.Value.IgnorePickEntities.Clear();
        foreach (var (e, wp, sp, _, off) in previewQ)
        {
            wp.Ref.X = (ushort)(baseX + off.Ref.X);
            wp.Ref.Y = (ushort)(baseY + off.Ref.Y);
            wp.Ref.Z = (sbyte)(baseZ + off.Ref.Z);
            sp.Ref.Value = wp.Ref.WorldToScreen();
            selected.Value.IgnorePickEntities.Add(e.Ref);
        }
    }

    private static void ClearMultiPreview(
        Commands commands,
        Res<SelectedEntity> selected,
        Query<Data<WorldPosition, ScreenPosition, AlphaFade, MultiPreviewOffset>, With<MultiPlacementPreview>> previewQ,
        TargetingState st)
    {
        foreach (var (e, _, _, _, _) in previewQ)
            commands.Entity(e.Ref).Despawn();
        selected.Value.IgnorePickEntities.Clear();
        st.PreviewBuiltId = 0;
    }

    private static void DespawnMultiPreviewOnExit(
        Commands commands,
        Res<SelectedEntity> selected,
        ResMut<TargetingState> targeting,
        Query<Data<WorldPosition>, With<MultiPlacementPreview>> previewQ)
    {
        foreach (var (e, _) in previewQ)
            commands.Entity(e.Ref).Despawn();
        selected.Value.IgnorePickEntities.Clear();
        targeting.Value.PreviewBuiltId = 0;
    }

    // 0x6C CursorTarget byte: 2 = multi placement.
    private const byte MultiMode = 2;
    // Spawn at the translucent settle value (ProcessFade's mode-1 target, 178) so
    // the ghost is half-transparent from frame one with no fade-in ramp. Legacy
    // MultiView.IsHousePreview shows it at ~0.5; 178 (~0.7) is the closest the
    // shared fade path holds steady.
    private const byte PreviewAlpha = 178;

    private static void ResolveTargetClick(
        Commands commands,
        Res<MouseContext> mouse,
        ResMut<TargetingState> targeting,
        Res<SelectedEntity> selected,
        Res<NetClient> net,
        Res<Profile> profile,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiSurface> surface,
        Res<UiZCounter> zCounter,
        ResMut<CriminalConfirmState> confirm,
        ResMut<LastTargetState> lastTarget,
        Res<NetworkEntitiesMap> entitiesMap,
        TargetClickQueries q)
    {
        // The click belongs to targeting whether or not it lands on something —
        // swallow it so a miss doesn't fall through to movement/pickup. Mark the
        // gesture so the matching left-release can't arm a context-menu popup
        // (targeting resolves on the press and clears IsTargeting before the
        // release-time popup gate sees it).
        mouse.Value.Consume(Input.MouseButtonType.Left);
        targeting.Value.JustTargeted = true;

        var ent = selected.Value.Entity;
        if (ent == 0)
            return;

        var cursorId = targeting.Value.CursorId;
        var cursorType = targeting.Value.CursorType;

        // Container / paperdoll UI item: it has no WorldPosition, so resolve its
        // serial to the backing game entity for the graphic and object-target it
        // (serial-keyed; x/y/z are the item's world pos when it has one, else 0).
        uint uiSerial = 0;
        if (q.ContainerItems.TryGet(ent, out var ciRow)) { var (_, ci) = ciRow; uiSerial = ci.Ref.Serial; }
        else if (q.EquipItems.TryGet(ent, out var peRow)) { var (_, pe) = peRow; uiSerial = pe.Ref.ItemSerial; }
        if (uiSerial != 0)
        {
            if (entitiesMap.Value.TryGet(uiSerial, out var gameEnt) && q.ItemGraphic.TryGet(gameEnt, out var giRow))
            {
                var (_, gfx, wpos) = giRow;
                ushort gx = wpos.IsValid() ? wpos.Ref.X : (ushort)0;
                ushort gy = wpos.IsValid() ? wpos.Ref.Y : (ushort)0;
                sbyte gz = wpos.IsValid() ? wpos.Ref.Z : (sbyte)0;
                net.Value.Send_TargetObject(uiSerial, gfx.Ref.Value, gx, gy, gz, cursorId, cursorType);
                lastTarget.Value.Set(uiSerial, gfx.Ref.Value, gx, gy, gz);
            }
            targeting.Value.Clear();
            return;
        }

        if (q.Objects.TryGet(ent, out var objectRow))
        {
            var (_, serial, graphic, pos, noto) = objectRow;
            var targetNoto = noto.IsValid() ? noto.Ref.Value : NotorietyFlag.Unknown;

            // Legacy TargetManager client-side query: when the player is innocent
            // or ally, a harmful target on an innocent — or a beneficial target on
            // a criminal/murderer/gray — pops a confirm before sending. The cursor
            // stays armed under the gump; OK sends, Cancel sends TargetCancel.
            if (ShouldConfirmCriminal(profile.Value, cursorType, targetNoto, q.PlayerNoto))
            {
                ShowCriminalConfirm(
                    commands, builder.Value, assets.Value, surface.Value, zCounter.Value, confirm.Value,
                    serial.Ref.Value, graphic.Ref.Value, pos.Ref.X, pos.Ref.Y, pos.Ref.Z);
                return; // wait for the user's answer (click already consumed)
            }

            net.Value.Send_TargetObject(
                serial.Ref.Value, graphic.Ref.Value,
                pos.Ref.X, pos.Ref.Y, pos.Ref.Z, cursorId, cursorType);
            // Remember it for the LastTarget hotkey.
            lastTarget.Value.Has = true;
            lastTarget.Value.Serial = serial.Ref.Value;
            lastTarget.Value.Graphic = graphic.Ref.Value;
            lastTarget.Value.X = pos.Ref.X;
            lastTarget.Value.Y = pos.Ref.Y;
            lastTarget.Value.Z = pos.Ref.Z;
            targeting.Value.Clear();
            return;
        }

        // Land sends graphic 0; a static sends its own graphic (legacy parity).
        if (q.Lands.TryGet(ent, out var landRow))
        {
            var (_, _, pos) = landRow;
            net.Value.Send_TargetXYZ(0, pos.Ref.X, pos.Ref.Y, pos.Ref.Z, cursorId, cursorType);
            targeting.Value.Clear();
            return;
        }

        if (q.Statics.TryGet(ent, out var staticRow))
        {
            var (_, graphic, pos) = staticRow;
            net.Value.Send_TargetXYZ(graphic.Ref.Value, pos.Ref.X, pos.Ref.Y, pos.Ref.Z, cursorId, cursorType);
            targeting.Value.Clear();
        }
    }

    // Topmost container/paperdoll UI item under the cursor becomes the selection
    // while a cursor is up, so a target click resolves to it (float.MaxValue beats
    // the world pick; runs after the movable-window claim so it wins for paperdoll).
    private static void SelectUiItemWhileTargeting(
        Res<MouseContext> mouse,
        Res<SelectedEntity> selected,
        Res<AssetsServer> assets,
        UiGesturePick pick,
        Query<Data<ContainerItemUI>> containerItems,
        Query<Data<PaperdollEquipUI>> equipItems)
    {
        var hit = pick.Topmost(mouse.Value.Position, assets.Value);
        if (!hit.Found)
            return;
        if (containerItems.Contains(hit.Entity) || equipItems.Contains(hit.Entity))
            selected.Value.Set(hit.Entity, float.MaxValue, bypassViewport: true);
    }

    private static void CancelTarget(
        Res<MouseContext> mouse,
        Res<KeyboardContext> keyboard,
        ResMut<TargetingState> targeting,
        Res<NetClient> net)
    {
        bool rightClick = mouse.Value.IsPressedOnce(Input.MouseButtonType.Right);
        bool escape = keyboard.Value.IsPressedOnce(Keys.Escape);
        if (!rightClick && !escape)
            return;

        if (rightClick)
            mouse.Value.Consume(Input.MouseButtonType.Right);

        net.Value.Send_TargetCancel(targeting.Value.Mode, targeting.Value.CursorId, targeting.Value.CursorType);
        targeting.Value.Clear();
    }

    // Legacy TargetManager: only query when the player is innocent/ally (a
    // criminal/red has nothing to lose). Harmful-on-innocent or beneficial-on-
    // criminal both risk flagging the player.
    private static bool ShouldConfirmCriminal(
        Profile profile, byte cursorType, NotorietyFlag target,
        Query<Data<Notoriety>, Filter<With<Player>>> playerNotoQ)
    {
        var player = NotorietyFlag.Unknown;
        foreach (var (_, pn) in playerNotoQ) { player = pn.Ref.Value; break; }
        if (player != NotorietyFlag.Innocent && player != NotorietyFlag.Ally)
            return false;

        if (cursorType == (byte)TargetType.Harmful)
            return profile.EnabledCriminalActionQuery && target == NotorietyFlag.Innocent;
        if (cursorType == (byte)TargetType.Beneficial)
            return profile.EnabledBeneficialCriminalActionQuery
                && (target == NotorietyFlag.Criminal || target == NotorietyFlag.Murderer || target == NotorietyFlag.Gray);
        return false;
    }

    // Client-side criminal-action confirm (legacy QuestionGump). The pending
    // target (serial/graphic/coords) is captured immutably; the cursor id/type
    // are read live from TargetingState when a button fires (the cursor stays
    // armed under the gump). OK sends the target; Cancel sends TargetCancel.
    internal static void ShowCriminalConfirm(
        Commands commands, GumpBuilder builder, AssetsServer assets, UiSurface surface,
        UiZCounter zCounter, CriminalConfirmState state,
        uint serial, ushort graphic, ushort x, ushort y, sbyte z)
    {
        state.Open = true;

        ref readonly var bg = ref assets.Gumps.GetGump(QuestionBg);
        var size = new Vector2(bg.UV.Width, bg.UV.Height);
        var pos = new Vector2(
            MathF.Max(0, (surface.LogicalSize.X - size.X) * 0.5f),
            MathF.Max(0, (surface.LogicalSize.Y - size.Y) * 0.5f));

        var root = builder.SpawnUOGump(commands, QuestionBg, Vector3.UnitZ, pos, zCounter)
            .Insert<CriminalConfirmGump>()
            .Insert<UiNoRightClickClose>();
        var rootId = root.Id;

        var label = builder.AddLabel(commands, "This may flag\nyou criminal!", new Vector2(33, 30), new Vector2(165, 40))
            .Insert(new TextFont { FontId = (ushort)(1 | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(0x0386)));
        commands.AddChild(rootId, label.Id);

        var cancel = builder.AddButton(commands, (CancelN, CancelP, CancelO), Vector3.UnitZ, new Vector2(37, 75));
        cancel.Observe((On<UiClick> _, Commands cmd, Res<NetClient> net2, ResMut<TargetingState> t, ResMut<CriminalConfirmState> cc) =>
        {
            net2.Value.Send_TargetCancel(t.Value.Mode, t.Value.CursorId, t.Value.CursorType);
            t.Value.Clear();
            cc.Value.Open = false;
            cmd.Entity(rootId).Despawn();
        });
        commands.AddChild(rootId, cancel.Id);

        var ok = builder.AddButton(commands, (OkN, OkP, OkO), Vector3.UnitZ, new Vector2(100, 75));
        ok.Observe((On<UiClick> _, Commands cmd, Res<NetClient> net2, ResMut<TargetingState> t, ResMut<CriminalConfirmState> cc) =>
        {
            net2.Value.Send_TargetObject(serial, graphic, x, y, z, t.Value.CursorId, t.Value.CursorType);
            t.Value.Clear();
            cc.Value.Open = false;
            cmd.Entity(rootId).Despawn();
        });
        commands.AddChild(rootId, ok.Id);
    }

    // Un-stick the confirm flag if the gump went away by any path that didn't
    // reset it (right-click consumed elsewhere, scene exit despawn).
    private static void ResetConfirmIfClosed(
        ResMut<CriminalConfirmState> confirm,
        Query<Data<CriminalConfirmGump>> gumps)
    {
        foreach (var _ in gumps)
            return;
        confirm.Value.Open = false;
    }

    // Legacy TargetType (0x6C cursorType byte). Cancel is the "not targeting"
    // sentinel the server uses to clear an active cursor.
    private enum TargetType : byte
    {
        Neutral = 0,
        Harmful = 1,
        Beneficial = 2,
        Cancel = 3,
    }
}

// Singleton mirroring the live state of legacy TargetManager: whether a target
// cursor is up and the server-echoed cursor id/type/mode needed to build the
// 0x6C response.
internal sealed class TargetingState
{
    public bool IsTargeting { get; set; }
    public byte Mode { get; set; }        // 0 object, 1 position, 2 multi
    public uint CursorId { get; set; }
    public byte CursorType { get; set; }  // 0 neutral, 1 harmful, 2 beneficial

    // Set when ResolveTargetClick consumes a target click on the PRESS; consumed
    // by PopupMenuTrack on the matching left-release so that release can't arm a
    // context-menu popup for the just-targeted object. NOT reset by Clear() —
    // Clear runs on the press frame, the release it must suppress arrives later.
    public bool JustTargeted { get; set; }

    // 0x99 multi-placement preview (legacy MultiTargetInfo). The deed's multi
    // graphic + drop offset; consumed by SyncMultiPreview while Mode==2 is live.
    public ushort MultiId { get; set; }
    public short MultiOffsetX { get; set; }
    public short MultiOffsetY { get; set; }
    public short MultiOffsetZ { get; set; }
    public ushort MultiHue { get; set; }
    // Which multi the ghost blocks are currently spawned for (0 = none) so
    // SyncMultiPreview rebuilds only on graphic change. Reset by the preview
    // system when it despawns the ghost, not by Clear (Clear has no Commands).
    public ushort PreviewBuiltId { get; set; }

    public void Clear()
    {
        IsTargeting = false;
        Mode = 0;
        CursorId = 0;
        CursorType = 0;
        MultiId = 0;
        MultiOffsetX = MultiOffsetY = MultiOffsetZ = 0;
        MultiHue = 0;
    }
}

// Open while a client-side criminal-action confirm gump is up. Suspends the
// targeting click/cancel resolvers so the gump's own buttons own the click.
internal sealed class CriminalConfirmState { public bool Open; }

// UOSteam-style target queue: a target issued while no cursor is up is held here
// and auto-applied to the next object cursor within the timeout (default 5s, the
// UOS default). Stores the resolved target so apply needs no entity lookup.
internal sealed class TargetQueueState
{
    public const float TimeoutMs = 5000f;

    public bool Armed;
    public float ExpireAt;
    public uint Serial;
    public ushort Graphic;
    public ushort X, Y;
    public sbyte Z;

    public void Set(uint serial, ushort graphic, ushort x, ushort y, sbyte z, float now)
    {
        Armed = true;
        Serial = serial; Graphic = graphic; X = x; Y = y; Z = z;
        ExpireAt = now + TimeoutMs;
    }

    public void Clear() => Armed = false;
}

// Query bundle for ResolveTargetClick (keeps it under the 16-param cap). Adds the
// container/paperdoll UI-item queries + a by-id graphic lookup so UI items (no
// WorldPosition) can be object-targeted.
internal sealed class TargetClickQueries : CompositeSystemParam
{
    public readonly Query<Data<NetworkSerial, Graphic, WorldPosition, Notoriety>, Filter<Optional<Notoriety>>> Objects;
    public readonly Query<Data<Graphic, WorldPosition>, Filter<With<IsTile>>> Lands;
    public readonly Query<Data<Graphic, WorldPosition>, Filter<With<IsStatic>>> Statics;
    public readonly Query<Data<Notoriety>, Filter<With<Player>>> PlayerNoto;
    public readonly Query<Data<ContainerItemUI>> ContainerItems;
    public readonly Query<Data<PaperdollEquipUI>> EquipItems;
    public readonly Query<Data<Graphic, WorldPosition>, Filter<Optional<WorldPosition>>> ItemGraphic;

    public TargetClickQueries()
    {
        Objects = Add(new Query<Data<NetworkSerial, Graphic, WorldPosition, Notoriety>, Filter<Optional<Notoriety>>>());
        Lands = Add(new Query<Data<Graphic, WorldPosition>, Filter<With<IsTile>>>());
        Statics = Add(new Query<Data<Graphic, WorldPosition>, Filter<With<IsStatic>>>());
        PlayerNoto = Add(new Query<Data<Notoriety>, Filter<With<Player>>>());
        ContainerItems = Add(new Query<Data<ContainerItemUI>>());
        EquipItems = Add(new Query<Data<PaperdollEquipUI>>());
        ItemGraphic = Add(new Query<Data<Graphic, WorldPosition>, Filter<Optional<WorldPosition>>>());
    }
}

// Marker on the confirm gump root (existence check for the un-stick safety net).
internal struct CriminalConfirmGump;
