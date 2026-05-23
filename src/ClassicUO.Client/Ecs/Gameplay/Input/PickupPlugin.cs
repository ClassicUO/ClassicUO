using System;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct PickupPlugin : IPlugin
{
    public void Build(App app)
    {
        var pickupItemDelayedFn = PickupItem;
        var pickupItemFn = PickupItem;
        var dropItemFn = DropItem;

        app
            .AddResource(new GrabbedItem())
            .AddResource(new LeftPressLatch())

            // Latches whichever entity was the press target on the left-button
            // edge. UI items / windows under the cursor at click time win over
            // any stale world hit (SelectedEntity is 1 frame behind, so a
            // click over a container window can otherwise still see the world
            // tile the cursor was over a frame ago and latch to that). Items
            // inside camera bounds without a UI hit fall back to
            // SelectedEntity (the world hit-test result). Clicks fully outside
            // camera bounds with no UI under them latch to 0 so window-border
            // / chrome clicks can't accidentally pick up a world item.
            .AddSystem((
                Res<MouseContext> m,
                Res<SelectedEntity> sel,
                Res<Camera> camera,
                ResMut<LeftPressLatch> latch,
                Query<Data<ContainerItemUI, ComputedNode, Node, GlobalZIndex>> uiItemsQ,
                Query<Data<ContainerWindow, ComputedNode, GlobalZIndex>> windowsQ) =>
            {
                // Clear on release edge. NOTE: don't rely on `!IsPressed` to
                // detect "not held" — IsPressed returns false on the press-
                // once frame itself (oldState=Released), which would wipe the
                // latch before we ever set it.
                if (m.Value.IsReleased(Input.MouseButtonType.Left))
                {
                    latch.Value.Entity = 0;
                    return;
                }

                if (!m.Value.IsPressedOnce(Input.MouseButtonType.Left))
                    return;

                var pos = m.Value.Position;

                // Find the topmost container window under the cursor first;
                // its z bounds which item slots are eligible. Items inherit
                // their owning window's GlobalZIndex via PropagateZIndex, so
                // any item with z < topWindowZ sits behind the front window
                // and must NOT be hit even if its sprite peeks out.
                int topWindowZ = int.MinValue;
                uint topWindowClayId = 0;
                ulong topWindow = 0;
                foreach (var (ent, _, computed, z) in windowsQ)
                {
                    var bb = computed.Ref;
                    if (pos.X < bb.Position.X || pos.Y < bb.Position.Y) continue;
                    if (pos.X >= bb.Position.X + bb.Size.X) continue;
                    if (pos.Y >= bb.Position.Y + bb.Size.Y) continue;
                    if (z.Ref.Value > topWindowZ || (z.Ref.Value == topWindowZ && bb.ClayId >= topWindowClayId))
                    {
                        topWindowZ = z.Ref.Value;
                        topWindowClayId = bb.ClayId;
                        topWindow = ent.Ref;
                    }
                }

                ulong topUi = 0;
                uint topClayId = 0;
                int topItemZ = int.MinValue;

                foreach (var (ent, _, computed, node, z) in uiItemsQ)
                {
                    if (node.Ref.Display == Display.None) continue;
                    if (topWindow != 0 && z.Ref.Value < topWindowZ) continue;
                    var bb = computed.Ref;
                    if (pos.X < bb.Position.X || pos.Y < bb.Position.Y) continue;
                    if (pos.X >= bb.Position.X + bb.Size.X) continue;
                    if (pos.Y >= bb.Position.Y + bb.Size.Y) continue;
                    if (z.Ref.Value > topItemZ || (z.Ref.Value == topItemZ && bb.ClayId >= topClayId))
                    {
                        topItemZ = z.Ref.Value;
                        topClayId = bb.ClayId;
                        topUi = ent.Ref;
                    }
                }

                if (topUi == 0)
                    topUi = topWindow;

                if (topUi != 0)
                {
                    latch.Value.Entity = topUi;
                    Console.WriteLine("[LATCH] ui entity={0} clayId={1}", topUi, topClayId);
                    return;
                }

                // Outside game viewport AND no container UI hit -> nothing to
                // pick up. Prevents game-window border clicks from inheriting
                // a stale world selection.
                if (!camera.Value.Bounds.Contains((int)pos.X, (int)pos.Y))
                {
                    latch.Value.Entity = 0;
                    Console.WriteLine("[LATCH] cleared (outside camera, no UI)");
                    return;
                }

                latch.Value.Entity = sel.Value.Entity;
                Console.WriteLine("[LATCH] world fallback entity={0}", sel.Value.Entity);
            })
            .InStage(Stage.First)
            .Build()

            .AddSystem(pickupItemDelayedFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Commands cmds) => cmds.HasResource<SelectedEntity>() && cmds.HasResource<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0)
            .RunIf((Res<MouseContext> mouseCtx, Local<float?> delay, Res<Time> time) =>
            {
                // Mirror legacy ItemGump pickup gate: fire once the user has
                // either dragged past MIN_PICKUP_DRAG_DISTANCE_PIXELS from the
                // press origin, or held long enough that a double-click can no
                // longer be in progress. Drop the camera.Bounds gate — pickup
                // now works on container UI sitting outside the world view too;
                // the selection RunIf still rejects spurious presses.
                if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Left))
                    delay.Value = time.Value.Total + 1000f;
                else if (mouseCtx.Value.IsReleased(Input.MouseButtonType.Left))
                    delay.Value = null;

                if (!mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                    return false;

                var dragOffset = mouseCtx.Value.DraggingOffset;
                if (Math.Abs(dragOffset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                    || Math.Abs(dragOffset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                    return true;

                return delay.Value.HasValue && time.Value.Total > delay.Value;
            })
            .RunIf((
                Res<LeftPressLatch> latch,
                Res<NetworkEntitiesMap> entitiesMap,
                Query<Data<NetworkSerial>, Filter<With<Items>>> q,
                Query<Data<ContainerItemUI>> uiItemQ) =>
            {
                // Pickup eligibility is gated on the PRESS-ORIGIN entity, not
                // the currently hovered one — otherwise dragging the cursor
                // off the item before the drag-distance threshold trips would
                // cancel pickup. Mirrors legacy
                // UIManager.LastControlMouseDown(Left) == this.
                var ent = latch.Value.Entity;
                if (!ent.IsValid()) return false;
                // Items have serials in [0x40000000, 0x80000000). Reject
                // mobiles / multis / anything else so pickup never fires on
                // non-items (matches legacy GameActions.OpenCorpse-style
                // SerialHelper.IsItem guards).
                if (q.Contains(ent))
                {
                    var (_, ns) = q.Get(ent);
                    return SerialHelper.IsItem(ns.Ref.Value);
                }
                // Container item UI selections resolve to their backing game
                // entity via NetworkEntitiesMap. Pickup body re-resolves.
                if (uiItemQ.Contains(ent))
                {
                    var (_, link) = uiItemQ.Get(ent);
                    if (!SerialHelper.IsItem(link.Ref.Serial)) return false;
                    return entitiesMap.Value.TryGet(link.Ref.Serial, out var gameEnt)
                        && q.Contains(gameEnt);
                }
                return false;
            })
            .Build()

            .AddSystem(pickupItemFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Commands cmds) => cmds.HasResource<SelectedEntity>() && cmds.HasResource<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0)
            .RunIf((Res<MouseContext> mouseCtx, Local<float?> delay, Res<Time> time) =>
            {
                // Mirror legacy ItemGump pickup gate: fire once the user has
                // either dragged past MIN_PICKUP_DRAG_DISTANCE_PIXELS from the
                // press origin, or held long enough that a double-click can no
                // longer be in progress. Drop the camera.Bounds gate — pickup
                // now works on container UI sitting outside the world view too;
                // the selection RunIf still rejects spurious presses.
                if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Left))
                    delay.Value = time.Value.Total + 1000f;
                else if (mouseCtx.Value.IsReleased(Input.MouseButtonType.Left))
                    delay.Value = null;

                if (!mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                    return false;

                var dragOffset = mouseCtx.Value.DraggingOffset;
                if (Math.Abs(dragOffset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                    || Math.Abs(dragOffset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                    return true;

                return delay.Value.HasValue && time.Value.Total > delay.Value;
            })
            .RunIf((
                Res<LeftPressLatch> latch,
                Res<NetworkEntitiesMap> entitiesMap,
                Query<Data<NetworkSerial>, Filter<With<Items>>> q,
                Query<Data<ContainerItemUI>> uiItemQ) =>
            {
                // Pickup eligibility is gated on the PRESS-ORIGIN entity, not
                // the currently hovered one — otherwise dragging the cursor
                // off the item before the drag-distance threshold trips would
                // cancel pickup. Mirrors legacy
                // UIManager.LastControlMouseDown(Left) == this.
                var ent = latch.Value.Entity;
                if (!ent.IsValid()) return false;
                // Items have serials in [0x40000000, 0x80000000). Reject
                // mobiles / multis / anything else so pickup never fires on
                // non-items (matches legacy GameActions.OpenCorpse-style
                // SerialHelper.IsItem guards).
                if (q.Contains(ent))
                {
                    var (_, ns) = q.Get(ent);
                    return SerialHelper.IsItem(ns.Ref.Value);
                }
                // Container item UI selections resolve to their backing game
                // entity via NetworkEntitiesMap. Pickup body re-resolves.
                if (uiItemQ.Contains(ent))
                {
                    var (_, link) = uiItemQ.Get(ent);
                    if (!SerialHelper.IsItem(link.Ref.Serial)) return false;
                    return entitiesMap.Value.TryGet(link.Ref.Serial, out var gameEnt)
                        && q.Contains(gameEnt);
                }
                return false;
            })
            .Build()

            .AddSystem(dropItemFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Commands cmds) => cmds.HasResource<SelectedEntity>() && cmds.HasResource<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial != 0)
            .RunIf((Res<MouseContext> mouseCtx) => mouseCtx.Value.IsReleased(Input.MouseButtonType.Left))
            .Build();

        var handlePacketsFn = HandlePickupPackets;
        app
            .AddSystem(handlePacketsFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<IPacket> packets) => packets.HasEvents)
            .Build();
    }


    static void HandlePickupPackets(
        Commands commands,
        EventReader<IPacket> packets,
        Res<GrabbedItem> grabbedItem,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<Node>> nodeQ,
        Query<Data<Graphic, Hue, Amount>> itemPropsQ,
        Query<Data<WorldPosition>> worldPosQ,
        Query<Data<ContainerSlotPosition>> slotPosQ
    )
    {
        foreach (var packet in packets.Read())
        {
            switch (packet)
            {
                case OnDenyMoveItemPacket_0x27 deny:
                    Console.WriteLine("[PKT-0x27 DENY] code=0x{0:X2} heldSerial=0x{1:X8} sourceUi={2}",
                        deny.Code, grabbedItem.Value.Serial, grabbedItem.Value.SourceUiEntity);
                    RestoreSourceUi(grabbedItem.Value, nodeQ);
                    RestoreItemProps(grabbedItem.Value, entitiesMap.Value, itemPropsQ, worldPosQ, slotPosQ);
                    grabbedItem.Value.Clear();
                    grabbedItem.Value.SourceUiEntity = 0;
                    break;

                case OnEndDraggingItemPacket_0x28:
                    Console.WriteLine("[PKT-0x28 END-DRAG] heldSerial=0x{0:X8} sourceUi={1}",
                        grabbedItem.Value.Serial, grabbedItem.Value.SourceUiEntity);
                    RestoreSourceUi(grabbedItem.Value, nodeQ);
                    RestoreItemProps(grabbedItem.Value, entitiesMap.Value, itemPropsQ, worldPosQ, slotPosQ);
                    grabbedItem.Value.Clear();
                    grabbedItem.Value.SourceUiEntity = 0;
                    break;

                case OnDropItemOkPacket_0x29:
                    Console.WriteLine("[PKT-0x29 DROP-OK] heldSerial=0x{0:X8} sourceUi={1} despawning",
                        grabbedItem.Value.Serial, grabbedItem.Value.SourceUiEntity);
                    if (grabbedItem.Value.SourceUiEntity != 0)
                    {
                        commands.Entity(grabbedItem.Value.SourceUiEntity).Despawn();
                        grabbedItem.Value.SourceUiEntity = 0;
                    }
                    break;
            }
        }
    }

    private static void RestoreSourceUi(GrabbedItem grabbed, Query<Data<Node>> nodeQ)
    {
        var src = grabbed.SourceUiEntity;
        if (src == 0 || !nodeQ.Contains(src)) return;
        var (_, node) = nodeQ.Get(src);
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
        if (q.Contains(ent))
        {
            var (_, g, h, a) = q.Get(ent);
            g.Ref.Value = grabbed.OriginalGraphic;
            h.Ref.Value = grabbed.OriginalHue;
            a.Ref.Value = grabbed.OriginalAmount;
        }
        // Restore position into the same type slot the item originated from.
        // OriginalFromSlot was set at pickup based on which component the
        // game entity carried.
        if (grabbed.OriginalFromSlot && slotPosQ.Contains(ent))
        {
            var (_, sp) = slotPosQ.Get(ent);
            sp.Ref.X = grabbed.OriginalX;
            sp.Ref.Y = grabbed.OriginalY;
            sp.Ref.GridIndex = grabbed.OriginalGridIndex;
        }
        else if (!grabbed.OriginalFromSlot && worldPosQ.Contains(ent))
        {
            var (_, p) = worldPosQ.Get(ent);
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
        Res<LeftPressLatch> latch,
        Res<GrabbedItem> grabbedItem,
        Res<NetClient> network,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<NetworkSerial, Amount, Graphic, Hue>> q,
        Query<Data<WorldPosition>> worldPosQ,
        Query<Data<ContainerSlotPosition>> slotPosQ,
        Query<Data<ContainerItemUI>> uiItemQ,
        Query<Data<ContainerWindow>> windowQ,
        Query<Data<Node>> nodeQ
    )
    {
        // Source-of-truth is the press-origin entity captured at click time,
        // not whatever the cursor currently hovers (which may have changed
        // mid-drag).
        var sel = latch.Value.Entity;
        var target = sel;
        ulong sourceUi = 0;
        uint sourceContainer = 0;
        Console.WriteLine("[PICKUP-ENTRY] latch={0} target={1}", sel, target);
        // Container item UIs are not game entities — resolve to the backing
        // game entity (carrying NetworkSerial/Amount/Graphic/Hue) first.
        if (uiItemQ.Contains(sel))
        {
            var (_, link) = uiItemQ.Get(sel);
            if (!entitiesMap.Value.TryGet(link.Ref.Serial, out target))
                return;
            sourceUi = sel;
            // Walk ContainerItemUI -> UI window -> ContainerWindow.Serial to
            // recover the source container's UO serial. Snapshot below uses
            // it to mirror legacy ItemHold.Container.
            if (windowQ.Contains(link.Ref.Container))
            {
                var (_, win) = windowQ.Get(link.Ref.Container);
                sourceContainer = win.Ref.Serial;
            }
        }

        var (serial, amount, graphic, hue) = q.Get(target);

        // Snapshot origin from whichever position type the item carries.
        // Nested items use ContainerSlotPosition (X/Y/GridIndex); ground
        // items use WorldPosition (X/Y/Z). DenyMoveItem writes the
        // matching one back on revert.
        ushort origX = 0, origY = 0;
        sbyte origZ = 0;
        byte origGrid = 0;
        bool fromSlot = slotPosQ.Contains(target);
        if (fromSlot)
        {
            var (_, sp) = slotPosQ.Get(target);
            origX = sp.Ref.X; origY = sp.Ref.Y; origGrid = sp.Ref.GridIndex;
        }
        else if (worldPosQ.Contains(target))
        {
            var (_, wp) = worldPosQ.Get(target);
            origX = wp.Ref.X; origY = wp.Ref.Y; origZ = wp.Ref.Z;
        }

        Console.WriteLine("[PICKUP] serial=0x{0:X8} amount={1} graphic=0x{2:X4} sourceUi={3} sourceContainer=0x{4:X8} origin=({5},{6},{7}) fromSlot={8}",
            serial.Ref.Value, amount.Ref.Value, graphic.Ref.Value, sourceUi, sourceContainer,
            origX, origY, origZ, fromSlot);
        network.Value.Send_PickUpRequest(serial.Ref.Value, (ushort)amount.Ref.Value);

        grabbedItem.Value.Clear();
        grabbedItem.Value.IsActive = true;
        grabbedItem.Value.Serial = serial.Ref.Value;
        grabbedItem.Value.Graphic = graphic.Ref.Value;
        grabbedItem.Value.Hue = hue.Ref.Value;
        grabbedItem.Value.Amount = amount.Ref.Value;
        grabbedItem.Value.SourceUiEntity = sourceUi;
        grabbedItem.Value.OriginalGraphic = graphic.Ref.Value;
        grabbedItem.Value.OriginalHue = hue.Ref.Value;
        grabbedItem.Value.OriginalAmount = (ushort)amount.Ref.Value;
        grabbedItem.Value.OriginalX = origX;
        grabbedItem.Value.OriginalY = origY;
        grabbedItem.Value.OriginalZ = origZ;
        grabbedItem.Value.OriginalContainer = sourceContainer;
        grabbedItem.Value.OriginalGridIndex = origGrid;
        grabbedItem.Value.OriginalFromSlot = fromSlot;

        // Hide (don't despawn) the source slot so a deny/end response can
        // restore the item to its original position. Despawn happens on
        // OnDropItemOkPacket_0x29 in HandlePickupPackets.
        if (sourceUi != 0 && nodeQ.Contains(sourceUi))
        {
            var (_, node) = nodeQ.Get(sourceUi);
            node.Ref.Display = Display.None;
            Console.WriteLine("[PICKUP] hide sourceUi={0}", sourceUi);
        }
        else if (sourceUi != 0)
        {
            Console.WriteLine("[PICKUP-WARN] sourceUi={0} has no Node!", sourceUi);
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
    private static bool IsPileGraphic(ushort g) => g switch
    {
        0x0EFA or 0x2253 or 0x2252 or 0x238C or 0x23A0 or 0x2D50 => true,
        _ => false,
    };

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
        Query<Data<TinyEcs.Parent>> parentQuery
    )
    {
        var target = selectedEntity.Value.Entity;
        Console.WriteLine("[DROP-ENTER] target={0} heldSerial=0x{1:X8}", target, grabbedItem.Value.Serial);
        if (target == 0)
        {
            Console.WriteLine("[DROP-NULL-TARGET] clearing grabbed without sending packet");
            grabbedItem.Value.Clear();
            return;
        }

        var (_, playerPos) = playerQuery.Get();

        // Case 1: dropping directly onto a container window (UI entity).
        if (containerQuery.Contains(target))
        {
            var (_, tag, computed, window) = containerQuery.Get(target);
            if (!IsContainerInRange(window.Ref.Serial, playerPos.Ref, entitiesMap.Value, itemDataQuery, parentQuery, playerQuery))
            {
                Console.WriteLine("[DROP-DENY] container=0x{0:X8} out of range", window.Ref.Serial);
                grabbedItem.Value.Clear();
                return;
            }
            var (x, y) = ClampToContainer(
                mouse.Value.Position, computed.Ref, tag.Ref,
                assets.Value, grabbedItem.Value.Graphic, uiScale.Value);
            Console.WriteLine("[DROP] into-container heldSerial=0x{0:X8} container=0x{1:X8} pos=({2},{3})",
                grabbedItem.Value.Serial, window.Ref.Serial, x, y);
            network.Value.Send_DropRequest(grabbedItem.Value.Serial, x, y, 0, 0, window.Ref.Serial);
            grabbedItem.Value.Clear();
            return;
        }

        // Case 2: dropping onto an item that lives inside a container window.
        // Resolve the item's game entity through NetworkEntitiesMap to read
        // graphic / world-grid coords; resolve the parent container window via
        // ContainerItemUI.Container.
        if (containerItemQuery.Contains(target))
        {
            var (_, link) = containerItemQuery.Get(target);
            var targetSerial = link.Ref.Serial;

            ushort targetGraphic = 0;
            ushort targetItemX = 0, targetItemY = 0;
            // Target lives inside a container so its slot coords are on
            // ContainerSlotPosition (the post-split storage). Fall back to
            // WorldPosition for items that haven't been re-routed yet.
            if (entitiesMap.Value.TryGet(targetSerial, out var targetGameEnt))
            {
                if (slotItemQuery.Contains(targetGameEnt))
                {
                    var (_, g, sp) = slotItemQuery.Get(targetGameEnt);
                    targetGraphic = g.Ref.Value;
                    targetItemX = sp.Ref.X;
                    targetItemY = sp.Ref.Y;
                }
                else if (itemDataQuery.Contains(targetGameEnt))
                {
                    var (_, g, p) = itemDataQuery.Get(targetGameEnt);
                    targetGraphic = g.Ref.Value;
                    targetItemX = p.Ref.X;
                    targetItemY = p.Ref.Y;
                }
            }

            // Distance is measured against the owning container, not the item
            // (matches legacy: thisCont = World.Get(RootContainer)).
            uint ownerSerial = 0;
            if (containerQuery.Contains(link.Ref.Container))
            {
                var (_, _, _, pwindow) = containerQuery.Get(link.Ref.Container);
                ownerSerial = pwindow.Ref.Serial;
            }
            if (ownerSerial != 0 && !IsContainerInRange(ownerSerial, playerPos.Ref, entitiesMap.Value, itemDataQuery, parentQuery, playerQuery))
            {
                grabbedItem.Value.Clear();
                return;
            }

            var tileData = fileManager.Value.TileData.StaticData;
            ref readonly var td = ref tileData[targetGraphic];

            if (td.IsContainer)
            {
                Console.WriteLine("[DROP] onto-item-as-container heldSerial=0x{0:X8} target=0x{1:X8}",
                    grabbedItem.Value.Serial, targetSerial);
                network.Value.Send_DropRequest(
                    grabbedItem.Value.Serial, 0xFFFF, 0xFFFF, 0, 0, targetSerial);
            }
            else if ((td.IsStackable && targetGraphic == grabbedItem.Value.Graphic) || IsPileGraphic(targetGraphic))
            {
                Console.WriteLine("[DROP] onto-stackable heldSerial=0x{0:X8} target=0x{1:X8} pos=({2},{3})",
                    grabbedItem.Value.Serial, targetSerial, targetItemX, targetItemY);
                network.Value.Send_DropRequest(
                    grabbedItem.Value.Serial, targetItemX, targetItemY, 0, 0, targetSerial);
            }
            else if (containerQuery.Contains(link.Ref.Container))
            {
                var (_, ptag, pcomputed, pwindow) = containerQuery.Get(link.Ref.Container);
                var (x, y) = ClampToContainer(
                    mouse.Value.Position, pcomputed.Ref, ptag.Ref,
                    assets.Value, grabbedItem.Value.Graphic, uiScale.Value);
                Console.WriteLine("[DROP] into-parent-container heldSerial=0x{0:X8} container=0x{1:X8} pos=({2},{3})",
                    grabbedItem.Value.Serial, pwindow.Ref.Serial, x, y);
                network.Value.Send_DropRequest(grabbedItem.Value.Serial, x, y, 0, 0, pwindow.Ref.Serial);
            }
            grabbedItem.Value.Clear();
            return;
        }

        // Case 3: world drop (unchanged from prior behavior).
        if (worldQuery.Contains(target))
        {
            (var targetEntity, var targetSerial, var targetWorldPos) = worldQuery.Get(target);
            var serial = targetSerial.IsValid() ? targetSerial.Ref.Value : 0xFFFF_FFFF;
            (ushort tx, ushort ty, sbyte tz) = targetWorldPos.Ref;
            if (serial != 0xFFFF_FFFF) (tx, ty, tz) = (0, 0, 0);
            Console.WriteLine("[DROP] world heldSerial=0x{0:X8} target=0x{1:X8} pos=({2},{3},{4})",
                grabbedItem.Value.Serial, serial, tx, ty, tz);
            network.Value.Send_DropRequest(grabbedItem.Value.Serial, tx, ty, tz, 0, serial);
        }
        else
        {
            Console.WriteLine("[DROP-MISS] no matching target — heldSerial=0x{0:X8} selectedEntity={1}",
                grabbedItem.Value.Serial, target);
        }

        grabbedItem.Value.Clear();
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

        if (!itemDataQuery.Contains(root)) return true;
        var (_, _, pos) = itemDataQuery.Get(root);
        var dx = Math.Abs(playerPos.X - pos.Ref.X);
        var dy = Math.Abs(playerPos.Y - pos.Ref.Y);
        return Math.Max(dx, dy) <= Constants.DRAG_ITEMS_DISTANCE;
    }

    private static ulong ResolveRootHolder(ulong start, Query<Data<TinyEcs.Parent>> parentQuery)
    {
        var cur = start;
        for (int i = 0; i < 16; i++)
        {
            if (!parentQuery.Contains(cur)) return cur;
            var (_, parent) = parentQuery.Get(cur);
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

        // Chessboard rendering shifts items up by 20px (see
        // SpawnContainerItemUI: drawY subtracts 20 for graphic 0x091A). Reverse
        // that shift on drop so the server receives the unshifted Y.
        if (tag.Graphic == 0x091A) my += 20;

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

        // Scaled bounds (chessboard adds 20px to its drop height in legacy).
        var b = tag.Bounds;
        float bx = b.X * scale;
        float by = b.Y * scale;
        float bw = b.Width * scale;
        float bh = (b.Height + (tag.Graphic == 0x091A ? 20 : 0)) * scale;

        float x = mx - (spriteW / 2f);
        float y = my - (spriteH / 2f);

        if (x + spriteW > bw) x = bw - spriteW;
        if (y + spriteH > bh) y = bh - spriteH;
        if (x < bx) x = bx;
        if (y < by) y = by;

        // Reverse scale back to server-space coordinates.
        return ((ushort)(x / scale), (ushort)(y / scale));
    }
}

internal sealed class LeftPressLatch
{
    // Selected ECS entity on the most recent left-button press edge.
    // Cleared on release. Used by the pickup gate to ensure the drag
    // gesture began on the same entity it's now pointed at.
    public ulong Entity { get; set; }
}

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


    public void Clear()
    {
        IsActive = false;
        Serial = 0;
        Graphic = 0;
        Hue = 0;
        Amount = 0;
    }
}
