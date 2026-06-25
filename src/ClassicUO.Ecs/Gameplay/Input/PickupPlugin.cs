using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using GameLayer = ClassicUO.Game.Data.Layer;

namespace ClassicUO.Ecs;

internal readonly struct PickupPlugin : IPlugin
{
    public void Build(App app)
    {
        var pickupItemFn = PickupItem;
        var dropItemFn = DropItem;
        var failedDropSoundFn = PlayFailedDropSound;
        var latchFn = LatchPressTarget;
        var shouldFirePickupFn = ShouldFirePickup;
        var pickupEligibleFn = PickupTargetEligible;
        var sallosFn = ResolveSallosEasyGrab;

        app
            .AddResource(new GrabbedItem())
            .AddResource(new LeftPressLatch())

            // Left-press-edge latch; see LatchPressTarget for the rationale.
            .AddSystem(latchFn)
            .InStage(Stage.First)
            // A press while targeting is the target click (TargetingPlugin owns
            // it) — don't latch a pickup candidate from it.
            .RunIf((Res<TargetingState> targeting) => !targeting.Value.IsTargeting)
            .Build()

            // SallosEasyGrab: with the press NOT on a pickable item, substitute
            // the currently-hovered item into the latch so the pickup below grabs
            // it (legacy GameSceneInputHandler easy-grab). Runs before PickupItem
            // (registration order = run order) so its gate sees the substitution.
            // Off by default → press-origin pickup only.
            .AddSystem(sallosFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<TargetingState> targeting) => !targeting.Value.IsTargeting)
            .RunIf((Res<SplitPrompt> split) => !split.Value.Open)
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0)
            .RunIf((Res<LeftPressLatch> latch) => !latch.Value.Consumed)
            .RunIf((Res<Profile> p) => p.Value.SallosEasyGrab)
            .Build()

            .AddSystem(pickupItemFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<TargetingState> targeting) => !targeting.Value.IsTargeting)
            // A split menu is up (or pending) — don't re-pick while the user
            // chooses an amount.
            .RunIf((Res<SplitPrompt> split) => !split.Value.Open)
            .RunIf((Commands cmds) => cmds.HasResource<SelectedEntity>() && cmds.HasResource<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0)
            // One pickup per left-hold. A 0x27 deny clears GrabbedItem.Serial
            // mid-drag, which would otherwise re-open the Serial==0 gate and
            // re-fire PickupItem every frame the button stays held -> infinite
            // PickUpRequest/deny loop, item stuck flickering on the cursor.
            // Consumed latches the gesture; cleared on release in LatchPressTarget.
            .RunIf((Res<LeftPressLatch> latch) => !latch.Value.Consumed)
            .RunIf(shouldFirePickupFn)
            .RunIf(pickupEligibleFn)
            .Build()

            .AddSystem(dropItemFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<TargetingState> targeting) => !targeting.Value.IsTargeting)
            .RunIf((Commands cmds) => cmds.HasResource<SelectedEntity>() && cmds.HasResource<GrabbedItem>())
            // Skip while awaiting a server response to a previous drop — the
            // user can't initiate another drop on the same held item until the
            // server acknowledges (0x27 deny / 0x28 end / 0x29 ok). Prevents
            // phantom drop packets while the network round-trips.
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial != 0 && !grabbedItem.Value.PendingDrop)
            .RunIf((Res<MouseContext> mouseCtx) => mouseCtx.Value.IsReleased(Input.MouseButtonType.Left))
            .Build()

            // Bonk (0x0051) when DropItem rejected a drop as out of reach.
            // Registered after DropItem so the flag it set this frame is
            // consumed the same frame (registration order = run order in stage).
            .AddSystem(failedDropSoundFn)
            .InStage(Stage.Update)
            .Build();

        // Per-packet observers replace the boxed EventReader<IPacket> scan.
        // 0x27/0x28 restore source UI + revert item props (server rejected
        // the move). 0x29 accepts the drop. 0x25/0x2E/0x1A/0xF3 are treated
        // as implicit drop-acks when they readdress the held serial (most
        // server flavors skip the explicit 0x29).
        app.AddObserver((
            On<PacketReceived<OnDenyMoveItemPacket_0x27>> trig,
            Res<GrabbedItem> grabbedItem,
            Res<NetworkEntitiesMap> entitiesMap,
            Query<Data<Node>> nodeQ,
            Query<Data<Graphic, Hue, Amount>> itemPropsQ,
            Query<Data<WorldPosition>> worldPosQ,
            Query<Data<ContainerSlotPosition>> slotPosQ) =>
        {
            RestoreSourceUi(grabbedItem.Value, nodeQ);
            RestoreItemProps(grabbedItem.Value, entitiesMap.Value, itemPropsQ, worldPosQ, slotPosQ);
            grabbedItem.Value.Clear();
            grabbedItem.Value.SourceUiEntity = 0;
        });

        app.AddObserver((
            On<PacketReceived<OnEndDraggingItemPacket_0x28>> _,
            Res<GrabbedItem> grabbedItem,
            Res<NetworkEntitiesMap> entitiesMap,
            Query<Data<Node>> nodeQ,
            Query<Data<Graphic, Hue, Amount>> itemPropsQ,
            Query<Data<WorldPosition>> worldPosQ,
            Query<Data<ContainerSlotPosition>> slotPosQ) =>
        {
            RestoreSourceUi(grabbedItem.Value, nodeQ);
            RestoreItemProps(grabbedItem.Value, entitiesMap.Value, itemPropsQ, worldPosQ, slotPosQ);
            grabbedItem.Value.Clear();
            grabbedItem.Value.SourceUiEntity = 0;
        });

        app.AddObserver((
            On<PacketReceived<OnDropItemOkPacket_0x29>> _,
            Commands commands,
            Res<GrabbedItem> grabbedItem) => FinalizeDrop(commands, grabbedItem.Value, "0x29 DROP-OK"));

        // Implicit-ACK observers — only commit drop completion when the
        // readdressed serial matches the pending-drop serial.
        app.AddObserver((
            On<PacketReceived<OnUpdateContainerPacket_0x25_Pre6017>> trig,
            Commands commands,
            Res<GrabbedItem> grabbedItem) =>
        {
            if (IsDropAck(grabbedItem.Value, trig.Event.Packet.Serial))
                FinalizeDrop(commands, grabbedItem.Value, "0x25");
        });

        app.AddObserver((
            On<PacketReceived<OnUpdateContainerPacket_0x25_Post6017>> trig,
            Commands commands,
            Res<GrabbedItem> grabbedItem) =>
        {
            if (IsDropAck(grabbedItem.Value, trig.Event.Packet.Serial))
                FinalizeDrop(commands, grabbedItem.Value, "0x25");
        });

        app.AddObserver((
            On<PacketReceived<OnEquipItemPacket_0x2E>> trig,
            Commands commands,
            Res<GrabbedItem> grabbedItem) =>
        {
            if (IsDropAck(grabbedItem.Value, trig.Event.Packet.Serial))
                FinalizeDrop(commands, grabbedItem.Value, "0x2E");
        });

        app.AddObserver((
            On<PacketReceived<OnUpdateItemPacket_0x1A>> trig,
            Commands commands,
            Res<GrabbedItem> grabbedItem) =>
        {
            if (IsDropAck(grabbedItem.Value, trig.Event.Packet.Serial))
                FinalizeDrop(commands, grabbedItem.Value, "0x1A");
        });

        app.AddObserver((
            On<PacketReceived<OnUpdateItemSAPacket_0xF3>> trig,
            Commands commands,
            Res<GrabbedItem> grabbedItem) =>
        {
            if (IsDropAck(grabbedItem.Value, trig.Event.Packet.Serial))
                FinalizeDrop(commands, grabbedItem.Value, "0xF3");
        });

        // Merging a held stack onto a same-graphic stack deletes the held item
        // server-side instead of readdressing it, so none of the readdress acks
        // above fire — the held item + source slot would stay stuck. Treat a
        // delete of the pending-drop serial as the drop ack.
        app.AddObserver((
            On<PacketReceived<OnDeleteObjectPacket_0x1D>> trig,
            Commands commands,
            Res<GrabbedItem> grabbedItem) =>
        {
            if (IsDropAck(grabbedItem.Value, trig.Event.Packet.Serial))
                FinalizeDrop(commands, grabbedItem.Value, "0x1D");
        });
    }

    // Latches whichever entity was the press target on the left-button edge.
    // UI items / windows under the cursor at click time win over any stale
    // world hit (SelectedEntity is 1 frame behind, so a click over a container
    // window can otherwise still see the world tile the cursor was over a frame
    // ago and latch to that). Items inside camera bounds without a UI hit fall
    // back to SelectedEntity (the world hit-test result). Clicks fully outside
    // camera bounds with no UI under them latch to 0 so window-border / chrome
    // clicks can't accidentally pick up a world item.
    private static void LatchPressTarget(
        Res<MouseContext> m,
        Res<SelectedEntity> sel,
        Res<Camera> camera,
        Res<AssetsServer> assets,
        ResMut<LeftPressLatch> latch,
        UiGesturePick pick,
        Query<Data<ContainerItemUI>> itemsQ,
        Query<Data<PaperdollEquipUI>> equipQ,
        Query<Data<ContainerWindow>> containerWinQ,
        Query<Data<PaperdollWindow>> pdWinQ)
    {
        // Clear on release edge. NOTE: don't rely on `!IsPressed` to detect
        // "not held" — IsPressed returns false on the press-once frame itself
        // (oldState=Released), which would wipe the latch before we ever set it.
        if (m.Value.IsReleased(Input.MouseButtonType.Left))
        {
            latch.Value.Entity = 0;
            latch.Value.Consumed = false;
            return;
        }

        if (!m.Value.IsPressedOnce(Input.MouseButtonType.Left))
            return;

        var pos = m.Value.Position;

        // One shared topmost pick (UiPick). A container item or paperdoll
        // equipment directly under the cursor IS the pickup target; otherwise
        // resolve the owning window and latch it only if it's a container/
        // paperdoll. Occlusion falls out of topmost-by-paint-order.
        var hit = pick.Topmost(pos, assets.Value);
        ulong topUi = 0;
        if (hit.Found)
        {
            if (itemsQ.Contains(hit.Entity) || equipQ.Contains(hit.Entity))
                topUi = hit.Entity;
            else
            {
                var owner = pick.MovableRoot(hit.Entity);
                if (owner != 0 && (containerWinQ.Contains(owner) || pdWinQ.Contains(owner)))
                    topUi = owner;
            }
        }

        if (topUi != 0)
        {
            latch.Value.Entity = topUi;
            return;
        }

        // Outside game viewport AND no container UI hit -> nothing to pick up.
        // Prevents game-window border clicks from inheriting a stale selection.
        if (!camera.Value.Bounds.Contains((int)pos.X, (int)pos.Y))
        {
            latch.Value.Entity = 0;
            return;
        }

        latch.Value.Entity = sel.Value.Entity;
    }

    // Pickup fires on EITHER trigger: left held + mouse dragged past
    // MIN_PICKUP_DRAG_DISTANCE_PIXELS from the press origin, or left held
    // continuously for >= 1000ms (held-still pickup). Hold deadline armed on
    // press-once, cleared on release. Time.Total accumulates in milliseconds.
    private static bool ShouldFirePickup(Res<MouseContext> mouseCtx, Local<float?> holdDeadline, Res<Time> time)
    {
        if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Left))
            holdDeadline.Value = time.Value.Total + 1000f;
        else if (mouseCtx.Value.IsReleased(Input.MouseButtonType.Left))
            holdDeadline.Value = null;

        if (!mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
            return false;

        var dragOffset = mouseCtx.Value.DraggingOffset;
        if (Math.Abs(dragOffset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
            || Math.Abs(dragOffset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
            return true;

        return holdDeadline.Value.HasValue && time.Value.Total >= holdDeadline.Value;
    }

    // Pickup eligibility is gated on the PRESS-ORIGIN entity, not the currently
    // hovered one — otherwise dragging the cursor off the item before the
    // drag-distance threshold trips would cancel pickup. Mirrors legacy
    // UIManager.LastControlMouseDown(Left) == this. Items have serials in
    // [0x40000000, 0x80000000); reject mobiles / multis / anything else.
    private static bool PickupTargetEligible(
        Res<LeftPressLatch> latch,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<NetworkSerial>, Filter<With<Items>>> q,
        Query<Data<ContainerItemUI>> uiItemQ,
        Query<Data<PaperdollEquipUI>> equipUiQ)
        => IsPickable(latch.Value.Entity, entitiesMap.Value, q, uiItemQ, equipUiQ);

    // True when `ent` is a liftable item: a world/ground item (NetworkSerial in
    // the item range), or a container-slot / paperdoll-equipment UI resolving to
    // one. Shared by the pickup gate and the SallosEasyGrab substitution.
    private static bool IsPickable(
        ulong ent,
        NetworkEntitiesMap entitiesMap,
        Query<Data<NetworkSerial>, Filter<With<Items>>> q,
        Query<Data<ContainerItemUI>> uiItemQ,
        Query<Data<PaperdollEquipUI>> equipUiQ)
    {
        if (!ent.IsValid()) return false;
        if (q.TryGet(ent, out var serialRow))
        {
            var (_, ns) = serialRow;
            return SerialHelper.IsItem(ns.Ref.Value);
        }
        if (uiItemQ.TryGet(ent, out var uiItemRow))
        {
            var (_, link) = uiItemRow;
            if (!SerialHelper.IsItem(link.Ref.Serial)) return false;
            return entitiesMap.TryGet(link.Ref.Serial, out var gameEnt) && q.Contains(gameEnt);
        }
        if (equipUiQ.TryGet(ent, out var equipUiRow))
        {
            var (_, link) = equipUiRow;
            if (!SerialHelper.IsItem(link.Ref.ItemSerial)) return false;
            return entitiesMap.TryGet(link.Ref.ItemSerial, out var gameEnt) && q.Contains(gameEnt);
        }
        return false;
    }

    // SallosEasyGrab (legacy GameSceneInputHandler): when the left press did NOT
    // start on a pickable item, grab whatever pickable item the cursor is hovering
    // now — substitute it into the latch so the normal pickup path lifts it. A
    // press that DID land on an item keeps that origin (press-origin wins).
    private static void ResolveSallosEasyGrab(
        Res<MouseContext> m,
        Res<SelectedEntity> sel,
        Res<NetworkEntitiesMap> entitiesMap,
        ResMut<LeftPressLatch> latch,
        Query<Data<NetworkSerial>, Filter<With<Items>>> q,
        Query<Data<ContainerItemUI>> uiItemQ,
        Query<Data<PaperdollEquipUI>> equipUiQ)
    {
        if (!m.Value.IsPressed(Input.MouseButtonType.Left))
            return;
        if (IsPickable(latch.Value.Entity, entitiesMap.Value, q, uiItemQ, equipUiQ))
            return;

        var hover = sel.Value.Entity;
        if (hover != 0 && IsPickable(hover, entitiesMap.Value, q, uiItemQ, equipUiQ))
            latch.Value.Entity = hover;
    }

    // A readdress packet acks the pending drop if it touches the held item OR
    // the drop target. The target match is what finalizes a same-graphic merge,
    // where the held serial is consumed and only the target stack updates.
    internal static bool IsDropAck(GrabbedItem grabbed, uint serial)
        => grabbed.PendingDrop
        && (serial == grabbed.Serial
            || (grabbed.DropTargetSerial != 0 && serial == grabbed.DropTargetSerial));

    private static void FinalizeDrop(Commands commands, GrabbedItem grabbed, string tag)
    {
        if (grabbed.SourceUiEntity != 0)
        {
            commands.Entity(grabbed.SourceUiEntity).Despawn();
            grabbed.SourceUiEntity = 0;
        }
        grabbed.Clear();
    }

    private static bool ShiftDown(KeyboardContext kb)
        => kb.IsPressed(Microsoft.Xna.Framework.Input.Keys.LeftShift)
        || kb.IsPressed(Microsoft.Xna.Framework.Input.Keys.RightShift)
        || kb.IsPressedOnce(Microsoft.Xna.Framework.Input.Keys.LeftShift)
        || kb.IsPressedOnce(Microsoft.Xna.Framework.Input.Keys.RightShift);

    private static int GetZ(Query<Data<GlobalZIndex>> q, ulong ent)
    {
        var (_, z) = q.Get(ent);
        return z.Ref.Value;
    }

    private static void RestoreSourceUi(GrabbedItem grabbed, Query<Data<Node>> nodeQ)
    {
        var src = grabbed.SourceUiEntity;
        if (src == 0 || !nodeQ.TryGet(src, out var nodeRow)) return;
        var (_, node) = nodeRow;
        node.Ref.Display = Display.Flex;
    }

    private static void RestoreItemProps(
        GrabbedItem grabbed,
        NetworkEntitiesMap entitiesMap,
        Query<Data<Graphic, Hue, Amount>> q,
        Query<Data<WorldPosition>> worldPosQ,
        Query<Data<ContainerSlotPosition>> slotPosQ)
    {
        if (grabbed.Serial == 0) return;
        if (!entitiesMap.TryGet(grabbed.Serial, out var ent)) return;
        if (q.TryGet(ent, out var propsRow))
        {
            var (_, g, h, a) = propsRow;
            g.Ref.Value = grabbed.OriginalGraphic;
            h.Ref.Value = grabbed.OriginalHue;
            a.Ref.Value = grabbed.OriginalAmount;
        }
        // Restore position into the same type slot the item originated from.
        // OriginalFromSlot was set at pickup based on which component the
        // game entity carried.
        if (grabbed.OriginalFromSlot && slotPosQ.TryGet(ent, out var slotRow))
        {
            var (_, sp) = slotRow;
            sp.Ref.X = grabbed.OriginalX;
            sp.Ref.Y = grabbed.OriginalY;
            sp.Ref.GridIndex = grabbed.OriginalGridIndex;
        }
        else if (!grabbed.OriginalFromSlot && worldPosQ.TryGet(ent, out var worldRow))
        {
            var (_, p) = worldRow;
            p.Ref.X = grabbed.OriginalX;
            p.Ref.Y = grabbed.OriginalY;
            p.Ref.Z = grabbed.OriginalZ;
        }
        // Container relationship is left to the server's follow-up 0x25
        // update — TinyEcs.Parent rewrites need a Commands ChildOf which
        // races our restore here; the server's add packet rebuilds it
        // through the normal pipeline.
    }

    static void PickupItem(
        Commands commands,
        ResMut<LeftPressLatch> latch,
        Res<GrabbedItem> grabbedItem,
        Res<NetClient> network,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<UOFileManager> fileManager,
        Res<KeyboardContext> keyboard,
        Res<Profile> profile,
        ResMut<SplitPrompt> splitPrompt,
        // ContainerSlotPosition rides along as an Optional (instead of its own
        // query) — the system is at the 16-param cap.
        Query<Data<NetworkSerial, Amount, Graphic, Hue, ContainerSlotPosition>, Filter<Optional<Amount>, Optional<ContainerSlotPosition>>> q,
        Query<Data<WorldPosition>> worldPosQ,
        Query<Data<ContainerItemUI>> uiItemQ,
        Query<Data<ContainerWindow>> windowQ,
        Query<Data<PaperdollEquipUI>> equipUiQ,
        Query<Data<EquipmentSlots>> equipmentSlotsQ,
        Query<Data<Node>> nodeQ
    )
    {
        // Consume the gesture immediately: the system only runs once all gates
        // passed, so reaching here means "fire pickup now". A server 0x27 deny
        // clears GrabbedItem.Serial mid-hold; without this the Serial==0 gate
        // re-opens and pickup re-fires every frame the button stays held.
        latch.Value.Consumed = true;

        // Source-of-truth is the press-origin entity captured at click time,
        // not whatever the cursor currently hovers (which may have changed
        // mid-drag).
        var sel = latch.Value.Entity;
        var target = sel;
        ulong sourceUi = 0;
        uint sourceContainer = 0;
        // Container item UIs are not game entities — resolve to the backing
        // game entity (carrying NetworkSerial/Amount/Graphic/Hue) first.
        if (uiItemQ.TryGet(sel, out var uiItemRow))
        {
            var (_, link) = uiItemRow;
            if (!entitiesMap.Value.TryGet(link.Ref.Serial, out target))
                return;
            sourceUi = sel;
            // Walk ContainerItemUI -> UI window -> ContainerWindow.Serial to
            // recover the source container's UO serial. Snapshot below uses
            // it to mirror legacy ItemHold.Container.
            if (windowQ.TryGet(link.Ref.Container, out var windowRow))
            {
                var (_, win) = windowRow;
                sourceContainer = win.Ref.Serial;
            }
        }
        // Paperdoll equipment overlay: resolve item serial -> game entity.
        // sourceContainer = mobile serial (matches main's PickUpRequest
        // semantics where the equipped item's "container" is the wearer).
        // Also clear the mobile's EquipmentSlots[layer] in-place so the
        // refresh system fires its Changed<EquipmentSlots> path and
        // rebuilds the paperdoll body without the picked-up overlay.
        // Server's drop-ok ack doesn't re-broadcast equipment state, so
        // without this the body stays stale until the user re-equips
        // something or relogs.
        else if (equipUiQ.TryGet(sel, out var equipRow))
        {
            var (_, link) = equipRow;
            if (!entitiesMap.Value.TryGet(link.Ref.ItemSerial, out target))
                return;
            sourceUi = sel;
            sourceContainer = link.Ref.MobileSerial;

            if (entitiesMap.Value.TryGet(link.Ref.MobileSerial, out var mobileEnt)
                && equipmentSlotsQ.TryGet(mobileEnt, out var slotsRow))
            {
                var (_, slots) = slotsRow;
                slots.Ref[link.Ref.Layer] = 0;
                // Re-Insert to bump the Changed tick (in-place mutation of
                // an InlineArray field doesn't trigger TinyEcs's tick).
                commands.Entity(mobileEnt).Insert(slots.Ref);
            }
        }

        var (serial, amount, graphic, hue, slotPos) = q.Get(target);

        // Snapshot origin from whichever position type the item carries.
        // Nested items use ContainerSlotPosition (X/Y/GridIndex); ground
        // items use WorldPosition (X/Y/Z). DenyMoveItem writes the
        // matching one back on revert.
        ushort origX = 0, origY = 0;
        sbyte origZ = 0;
        byte origGrid = 0;
        bool fromSlot = slotPos.IsValid();
        if (fromSlot)
        {
            origX = slotPos.Ref.X; origY = slotPos.Ref.Y; origGrid = slotPos.Ref.GridIndex;
        }
        else if (worldPosQ.TryGet(target, out var worldPosRow))
        {
            var (_, wp) = worldPosRow;
            origX = wp.Ref.X; origY = wp.Ref.Y; origZ = wp.Ref.Z;
        }

        // Equipped items often lack an Amount component (server doesn't
        // send a stack count for worn gear). Default to 1 in that case;
        // ground/stackable items always carry it.
        int amountValue = amount.IsValid() ? amount.Ref.Value : 1;

        // Stackable + amount>1 -> prompt for a split instead of lifting the
        // whole pile when the Shift state matches HoldShiftToSplitStack
        // (legacy GameActions.PickUp: `HoldShiftToSplitStack == Keyboard.Shift`;
        // default false, so the menu shows when Shift is up).
        // Snapshot the full pickup context into SplitPrompt; SplitMenuPlugin's
        // OK/Enter commit replays it as a normal pickup. The pickup system's
        // RunIf gate (SplitPrompt.Open) keeps this from re-firing while up.
        var graphicId = graphic.Ref.Value;
        bool stackable = graphicId < fileManager.Value.TileData.StaticData.Length
            && fileManager.Value.TileData.StaticData[graphicId].IsStackable;
        if (!splitPrompt.Value.Open && amountValue > 1 && stackable
            && ShiftDown(keyboard.Value) == profile.Value.HoldShiftToSplitStack)
        {
            splitPrompt.Value.Open = true;
            splitPrompt.Value.Built = false;
            splitPrompt.Value.PendingSerial = serial.Ref.Value;
            splitPrompt.Value.Graphic = graphicId;
            splitPrompt.Value.Hue = hue.Ref.Value;
            splitPrompt.Value.MaxAmount = amountValue;
            splitPrompt.Value.SourceUiEntity = sourceUi;
            splitPrompt.Value.SourceContainer = sourceContainer;
            splitPrompt.Value.OriginalGraphic = graphicId;
            splitPrompt.Value.OriginalHue = hue.Ref.Value;
            splitPrompt.Value.OriginalAmount = (ushort)amountValue;
            splitPrompt.Value.OriginalX = origX;
            splitPrompt.Value.OriginalY = origY;
            splitPrompt.Value.OriginalZ = origZ;
            splitPrompt.Value.OriginalContainer = sourceContainer;
            splitPrompt.Value.OriginalGridIndex = origGrid;
            splitPrompt.Value.OriginalFromSlot = fromSlot;
            return;
        }

        network.Value.Send_PickUpRequest(serial.Ref.Value, (ushort)amountValue);

        grabbedItem.Value.Clear();
        grabbedItem.Value.IsActive = true;
        grabbedItem.Value.Serial = serial.Ref.Value;
        grabbedItem.Value.Graphic = graphic.Ref.Value;
        grabbedItem.Value.Hue = hue.Ref.Value;
        grabbedItem.Value.Amount = amountValue;
        grabbedItem.Value.SourceUiEntity = sourceUi;
        grabbedItem.Value.OriginalGraphic = graphic.Ref.Value;
        grabbedItem.Value.OriginalHue = hue.Ref.Value;
        grabbedItem.Value.OriginalAmount = (ushort)amountValue;
        grabbedItem.Value.OriginalX = origX;
        grabbedItem.Value.OriginalY = origY;
        grabbedItem.Value.OriginalZ = origZ;
        grabbedItem.Value.OriginalContainer = sourceContainer;
        grabbedItem.Value.OriginalGridIndex = origGrid;
        grabbedItem.Value.OriginalFromSlot = fromSlot;

        // Hide (don't despawn) the source slot so a deny/end response can
        // restore the item to its original position. Despawn happens on
        // OnDropItemOkPacket_0x29 in HandlePickupPackets.
        if (sourceUi != 0 && nodeQ.TryGet(sourceUi, out var nodeRow))
        {
            var (_, node) = nodeRow;
            node.Ref.Display = Display.None;
        }
    }

    // Route the held item to the right drop target. Three cases ported from
    // ContainerGump.OnMouseUp:
    //   1. Selected entity is a container window UI (has ContainerGumpTag):
    //      drop into that container at the mouse-relative position, clamped
    //      to ContainerData.Bounds minus the held sprite's footprint.
    //   2. Selected entity is an item inside a container (ContainerItemUI):
    //        - target itself is a container (tiledata IsContainer): nest with
    //          x = y = 0xFFFF (server auto-slots)
    //        - target is stackable + same graphic: drop onto its slot to merge
    //        - otherwise: drop at the target's coordinates inside the parent
    //          container (legacy "place adjacent" behavior is approximated as
    //          "same slot as target" since we lack relative mouse offset here)
    //   3. Else: original world-drop path (tile or ground object).
    // Legacy ContainerGump.OnMouseUp drops onto a "pile-like" graphic (sand
    // pile, coin pile, etc) by snapping to the target's slot just like a
    // stackable. Same set of graphics here.
    internal static bool IsPileGraphic(ushort g) => g switch
    {
        0x0EFA or 0x2253 or 0x2252 or 0x238C or 0x23A0 or 0x2D50 => true,
        _ => false,
    };

    // Legacy GameScene/ContainerGump play 0x0051 when a held item is released
    // over an out-of-reach target. DropItem flags it on GrabbedItem; play here.
    static void PlayFailedDropSound(
        ResMut<GrabbedItem> grabbedItem,
        ResMut<AudioState> audio,
        Res<AssetsServer> assets,
        Res<Profile> profile,
        Res<Time> time)
    {
        if (!grabbedItem.Value.FailedDrop)
            return;

        grabbedItem.Value.FailedDrop = false;
        audio.Value.PlaySound(assets.Value, profile.Value, time.Value.Total, 0x0051);
    }

    static void DropItem(
        Res<SelectedEntity> selectedEntity,
        Res<GrabbedItem> grabbedItem,
        Res<NetClient> network,
        Res<MouseContext> mouse,
        Res<AssetsServer> assets,
        Res<UOFileManager> fileManager,
        Res<UIScale> uiScale,
        Res<NetworkEntitiesMap> entitiesMap,
        Single<Data<WorldPosition>, With<Player>> playerQuery,
        Query<Data<NetworkSerial, WorldPosition>, Optional<NetworkSerial>> worldQuery,
        Query<Data<ContainerGumpTag, ComputedNode, ContainerWindow>> containerQuery,
        Query<Data<ContainerItemUI>> containerItemQuery,
        Query<Data<Graphic, WorldPosition>> itemDataQuery,
        Query<Data<Graphic, ContainerSlotPosition>> slotItemQuery,
        Query<Data<TinyEcs.Parent>> parentQuery,
        PaperdollDropParams paperdoll
    )
    {
        var paperdollWindowQ = paperdoll.WindowQ;
        var paperdollEquipQ = paperdoll.EquipQ;
        var equipmentSlotsQ = paperdoll.EquipmentSlotsQ;
        var target = selectedEntity.Value.Entity;
        if (target == 0)
        {
            grabbedItem.Value.Clear();
            return;
        }

        var (_, playerPos) = playerQuery.Get();

        // Drop on paperdoll body (PaperdollWindow root or any equip overlay
        // child) -> equip the held item on that mobile. Mirrors main's
        // PaperDollGump.OnMouseUp Equip path. Two pre-conditions match
        // GameActions.Equip: held item must be wearable, and the target
        // layer must be empty on the mobile (otherwise legacy aborts).
        if (paperdollWindowQ.Contains(target) || paperdollEquipQ.Contains(target))
        {
            uint mobileSerial = 0;
            if (paperdollWindowQ.TryGet(target, out var pdWinRow))
            {
                var (_, pw) = pdWinRow;
                mobileSerial = pw.Ref.Serial;
            }
            else
            {
                var (_, pe) = paperdollEquipQ.Get(target);
                mobileSerial = pe.Ref.MobileSerial;
            }

            var tileData = fileManager.Value.TileData.StaticData;
            var heldGraphic = grabbedItem.Value.Graphic;
            if (heldGraphic == 0 || heldGraphic >= tileData.Length)
            {
                grabbedItem.Value.Clear();
                return;
            }
            ref readonly var td = ref tileData[heldGraphic];
            if (!td.IsWearable)
            {
                grabbedItem.Value.Clear();
                return;
            }
            var heldLayer = (GameLayer)td.Layer;

            // Check target layer slot is empty (legacy behavior — main's
            // PaperDollGump.OnMouseUp guards via FindItemByLayer).
            if (entitiesMap.Value.TryGet(mobileSerial, out var mobileEnt)
                && equipmentSlotsQ.TryGet(mobileEnt, out var slotsRow))
            {
                var (_, slots) = slotsRow;
                if (slots.Ref[heldLayer] != 0)
                {
                    grabbedItem.Value.Clear();
                    return;
                }
            }

            network.Value.Send_EquipRequest(grabbedItem.Value.Serial, heldLayer, mobileSerial);
            grabbedItem.Value.PendingDrop = true;
            return;
        }

        // Case 1: dropping directly onto a container window (UI entity).
        if (containerQuery.TryGet(target, out var containerRow))
        {
            var (_, tag, computed, window) = containerRow;
            if (!IsContainerInRange(window.Ref.Serial, playerPos.Ref, entitiesMap.Value, itemDataQuery, parentQuery, playerQuery))
            {
                grabbedItem.Value.FailedDrop = true;
                grabbedItem.Value.Clear();
                return;
            }
            var (x, y) = ClampToContainer(
                mouse.Value.Position, computed.Ref, tag.Ref,
                assets.Value, grabbedItem.Value.Graphic, uiScale.Value);
            network.Value.Send_DropRequest(grabbedItem.Value.Serial, x, y, 0, 0, window.Ref.Serial);
            grabbedItem.Value.DropTargetSerial = window.Ref.Serial;
            grabbedItem.Value.PendingDrop = true;
            return;
        }

        // Case 2: dropping onto an item that lives inside a container window.
        // Resolve the item's game entity through NetworkEntitiesMap to read
        // graphic / world-grid coords; resolve the parent container window via
        // ContainerItemUI.Container.
        if (containerItemQuery.TryGet(target, out var itemLinkRow))
        {
            var (_, link) = itemLinkRow;
            var targetSerial = link.Ref.Serial;

            ushort targetGraphic = 0;
            ushort targetItemX = 0, targetItemY = 0;
            // Target lives inside a container so its slot coords are on
            // ContainerSlotPosition (the post-split storage). Fall back to
            // WorldPosition for items that haven't been re-routed yet.
            if (entitiesMap.Value.TryGet(targetSerial, out var targetGameEnt))
            {
                if (slotItemQuery.TryGet(targetGameEnt, out var slotItemRow))
                {
                    var (_, g, sp) = slotItemRow;
                    targetGraphic = g.Ref.Value;
                    targetItemX = sp.Ref.X;
                    targetItemY = sp.Ref.Y;
                }
                else if (itemDataQuery.TryGet(targetGameEnt, out var itemDataRow))
                {
                    var (_, g, p) = itemDataRow;
                    targetGraphic = g.Ref.Value;
                    targetItemX = p.Ref.X;
                    targetItemY = p.Ref.Y;
                }
            }

            // Distance is measured against the owning container, not the item
            // (matches legacy: thisCont = World.Get(RootContainer)).
            uint ownerSerial = 0;
            if (containerQuery.TryGet(link.Ref.Container, out var ownerWinRow))
            {
                var (_, _, _, pwindow) = ownerWinRow;
                ownerSerial = pwindow.Ref.Serial;
            }
            if (ownerSerial != 0 && !IsContainerInRange(ownerSerial, playerPos.Ref, entitiesMap.Value, itemDataQuery, parentQuery, playerQuery))
            {
                grabbedItem.Value.FailedDrop = true;
                grabbedItem.Value.Clear();
                return;
            }

            var tileData = fileManager.Value.TileData.StaticData;
            ref readonly var td = ref tileData[targetGraphic];

            bool itemSent = false;
            if (td.IsContainer)
            {
                network.Value.Send_DropRequest(
                    grabbedItem.Value.Serial, 0xFFFF, 0xFFFF, 0, 0, targetSerial);
                grabbedItem.Value.DropTargetSerial = targetSerial;
                itemSent = true;
            }
            else if ((td.IsStackable && targetGraphic == grabbedItem.Value.Graphic) || IsPileGraphic(targetGraphic))
            {
                network.Value.Send_DropRequest(
                    grabbedItem.Value.Serial, targetItemX, targetItemY, 0, 0, targetSerial);
                grabbedItem.Value.DropTargetSerial = targetSerial;
                itemSent = true;
            }
            else if (containerQuery.TryGet(link.Ref.Container, out var parentWinRow))
            {
                var (_, ptag, pcomputed, pwindow) = parentWinRow;
                var (x, y) = ClampToContainer(
                    mouse.Value.Position, pcomputed.Ref, ptag.Ref,
                    assets.Value, grabbedItem.Value.Graphic, uiScale.Value);
                network.Value.Send_DropRequest(grabbedItem.Value.Serial, x, y, 0, 0, pwindow.Ref.Serial);
                grabbedItem.Value.DropTargetSerial = pwindow.Ref.Serial;
                itemSent = true;
            }
            if (itemSent)
                grabbedItem.Value.PendingDrop = true;
            else
                grabbedItem.Value.Clear();
            return;
        }

        // Case 3: world drop (unchanged from prior behavior).
        if (worldQuery.TryGet(target, out var worldHitRow))
        {
            (var targetEntity, var targetSerial, var targetWorldPos) = worldHitRow;
            var serial = targetSerial.IsValid() ? targetSerial.Ref.Value : 0xFFFF_FFFF;
            (ushort tx, ushort ty, sbyte tz) = targetWorldPos.Ref;
            if (serial != 0xFFFF_FFFF) (tx, ty, tz) = (0, 0, 0);
            network.Value.Send_DropRequest(grabbedItem.Value.Serial, tx, ty, tz, 0, serial);
            grabbedItem.Value.DropTargetSerial = serial;
            grabbedItem.Value.PendingDrop = true;
        }
        else
        {
            grabbedItem.Value.Clear();
        }
    }

    // Chebyshev distance between player and the container's world tile. Legacy
    // uses Item.Distance == max(|dx|, |dy|). Containers on the player (held in
    // backpack) won't be in itemDataQuery (no WorldPosition) and are considered
    // in-range by default. DRAG_ITEMS_DISTANCE = 3.
    private static bool IsContainerInRange(
        uint containerSerial,
        in WorldPosition playerPos,
        NetworkEntitiesMap entitiesMap,
        Query<Data<Graphic, WorldPosition>> itemDataQuery,
        Query<Data<TinyEcs.Parent>> parentQuery,
        Single<Data<WorldPosition>, With<Player>> playerQuery)
    {
        if (!entitiesMap.TryGet(containerSerial, out var gameEnt)) return true;

        // Walk to the root holder so nested containers (pouch -> backpack ->
        // player) measure distance from the actual ground tile or owner,
        // not from grid coords inside the parent. Matches legacy
        // RootContainer logic.
        var root = ResolveRootHolder(gameEnt, parentQuery);
        var (playerEnt, _) = playerQuery.Get();
        if (root == playerEnt.Ref) return true;

        if (!itemDataQuery.TryGet(root, out var rootRow)) return true;
        var (_, _, pos) = rootRow;
        var dx = Math.Abs(playerPos.X - pos.Ref.X);
        var dy = Math.Abs(playerPos.Y - pos.Ref.Y);
        return Math.Max(dx, dy) <= Constants.DRAG_ITEMS_DISTANCE;
    }

    internal static ulong ResolveRootHolder(ulong start, Query<Data<TinyEcs.Parent>> parentQuery)
    {
        var cur = start;
        for (int i = 0; i < 16; i++)
        {
            if (!parentQuery.TryGet(cur, out var parentRow)) return cur;
            var (_, parent) = parentRow;
            var pid = (ulong)parent.Ref.Id;
            if (pid == 0 || pid == cur) return cur;
            cur = pid;
        }
        return cur;
    }

    // Convert the absolute mouse position into a clamped (x, y) suitable for
    // Send_DropRequest. Mirrors ContainerGump.OnMouseUp bounds math: subtract
    // half the sprite footprint to centre on the cursor, clamp to data.Bounds
    // minus the sprite size, then reverse the container scale so values land
    // in unscaled server space.
    private static (ushort, ushort) ClampToContainer(
        Vector2 mouseAbs,
        in ComputedNode computed,
        in ContainerGumpTag tag,
        AssetsServer assets,
        ushort grabbedGraphic,
        UIScale uiScale)
    {
        float scale = tag.Scale;

        // Container-local position (mouse relative to window's screen origin).
        float mx = mouseAbs.X - computed.Position.X;
        float my = mouseAbs.Y - computed.Position.Y;

        int spriteW, spriteH;
        if (tag.IsBoard)
        {
            var g = (ushort)(grabbedGraphic - Constants.ITEM_GUMP_TEXTURE_OFFSET);
            ref readonly var gi = ref assets.Gumps.GetGump(g);
            spriteW = gi.UV.Width;
            spriteH = gi.UV.Height;
        }
        else
        {
            ref readonly var ai = ref assets.Arts.GetArt(grabbedGraphic);
            spriteW = ai.UV.Width;
            spriteH = ai.UV.Height;
            if (uiScale.ScaleItemsInsideContainers)
            {
                spriteW = (int)(spriteW * scale);
                spriteH = (int)(spriteH * scale);
            }
        }

        // Chessboard (0x091A) renders items shifted up 20px; ClampDropPosition
        // reverses that so the server receives the unshifted Y.
        return ClampDropPosition(mx, my, spriteW, spriteH, tag.Bounds, scale, tag.Graphic == 0x091A);
    }

    // Pure drop-position math, split out of ClampToContainer so it is
    // unit-testable without an AssetsServer: centre the held sprite on the
    // cursor's container-local position, clamp into the scaled content bounds
    // minus the sprite footprint, then reverse the scale back to server space.
    // `chessboard` shifts the item up 20px and adds 20px to the drop height.
    internal static (ushort, ushort) ClampDropPosition(
        float mx, float my, int spriteW, int spriteH, in Rectangle bounds, float scale, bool chessboard)
    {
        if (chessboard) my += 20;

        float bx = bounds.X * scale;
        float by = bounds.Y * scale;
        float bw = bounds.Width * scale;
        float bh = (bounds.Height + (chessboard ? 20 : 0)) * scale;

        float x = mx - (spriteW / 2f);
        float y = my - (spriteH / 2f);

        if (x + spriteW > bw) x = bw - spriteW;
        if (y + spriteH > bh) y = bh - spriteH;
        if (x < bx) x = bx;
        if (y < by) y = by;

        return ((ushort)(x / scale), (ushort)(y / scale));
    }
}

// Composite system param: bundles paperdoll-related drop queries so
// DropItem stays under TinyEcs's delegate arity ceiling. Each inner
// query advertises its access independently so the scheduler can still
// parallelize correctly.
internal sealed class PaperdollDropParams : ISystemParam
{
    public Query<Data<PaperdollWindow>> WindowQ { get; } = new();
    public Query<Data<PaperdollEquipUI>> EquipQ { get; } = new();
    public Query<Data<EquipmentSlots>> EquipmentSlotsQ { get; } = new();

    public void Initialize(App app)
    {
        WindowQ.Initialize(app);
        EquipQ.Initialize(app);
        EquipmentSlotsQ.Initialize(app);
    }

    public void Fetch(App app)
    {
        WindowQ.Fetch(app);
        EquipQ.Fetch(app);
        EquipmentSlotsQ.Fetch(app);
    }

    public SystemParamAccess GetAccess()
    {
        var access = new SystemParamAccess();
        foreach (var src in new SystemParamAccess[]
        {
            WindowQ.GetAccess(),
            EquipQ.GetAccess(),
            EquipmentSlotsQ.GetAccess(),
        })
        {
            foreach (var r in src.ReadResources) access.ReadResources.Add(r);
            foreach (var w in src.WriteResources) access.WriteResources.Add(w);
        }
        return access;
    }
}

internal sealed class LeftPressLatch
{
    // Selected ECS entity on the most recent left-button press edge.
    // Cleared on release. Used by the pickup gate to ensure the drag
    // gesture began on the same entity it's now pointed at.
    public ulong Entity { get; set; }

    // Set once PickupItem fires for the current left-hold; cleared on release.
    // Stops a 0x27 deny (which clears GrabbedItem.Serial mid-drag) from
    // re-opening the pickup gate and looping PickUpRequest/deny while held.
    public bool Consumed { get; set; }
}

// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal sealed class GrabbedItem
{
    public bool IsActive { get; set; }
    public uint Serial { get; set; }
    public ushort Graphic { get; set; }
    public ushort Hue { get; set; }
    public int Amount { get; set; }
    // Source ContainerItemUI entity. Hidden (Display.None) on pickup so the
    // pile doesn't visually duplicate while held. Persists across Clear() so
    // the server's deny/end/ok response can restore or despawn it. Cleared
    // explicitly by the packet handlers.
    public ulong SourceUiEntity { get; set; }

    // Snapshot of the game entity's components at pickup time. Mirrors
    // legacy ItemHold (Graphic/Hue/TotalAmount/X/Y/Z/Container). DenyMoveItem
    // / EndDraggingItem write these back onto the game entity so the item
    // reverts to its source state on server rejection. Persist across
    // Clear(); zeroed only by the packet handlers.
    public ushort OriginalGraphic { get; set; }
    public ushort OriginalHue { get; set; }
    public ushort OriginalAmount { get; set; }
    public ushort OriginalX { get; set; }
    public ushort OriginalY { get; set; }
    public sbyte OriginalZ { get; set; }
    public uint OriginalContainer { get; set; }
    public byte OriginalGridIndex { get; set; }
    // Selects which position component the deny/end restore writes back:
    // true -> ContainerSlotPosition (X/Y/GridIndex), false -> WorldPosition.
    public bool OriginalFromSlot { get; set; }

    // True between sending a drop request and receiving the server's
    // accept/deny/end response. While set, further drop / pickup actions are
    // blocked so the user can't fire phantom drops on the same held item
    // before the server has acknowledged the first one. Cleared by the packet
    // handlers (0x27/0x28/0x29).
    public bool PendingDrop { get; set; }

    // Serial the held item was dropped onto/into (target stack for a merge,
    // container for a plain drop). A merge CONSUMES the held serial, so the
    // server only readdresses the TARGET (a 0x25/0x1A amount update) — none of
    // the held-serial acks fire and the item would stay stuck on the cursor.
    // Matching this serial in the implicit-ack observers finalizes the merge.
    public uint DropTargetSerial { get; set; }

    // Set by DropItem when a drop is rejected client-side for being out of
    // reach (legacy GameScene's !can_drop -> PlaySound(0x0051) bonk). Consumed
    // by PlayFailedDropSound the same frame; deliberately NOT reset by Clear()
    // so the reject branches can set it immediately before clearing the hold.
    public bool FailedDrop { get; set; }


    public void Clear()
    {
        IsActive = false;
        Serial = 0;
        Graphic = 0;
        Hue = 0;
        Amount = 0;
        PendingDrop = false;
        DropTargetSerial = 0;
    }
}
