// Bevy.UI port of the legacy Game/UI/Gumps/ContainerGump.cs control tree.
//
// IMPORTANT: container UI lives on a *separate* entity from the gameplay
// entity. The game entity (NetworkEntitiesMap lookup) is parented under the
// player / its owner via `AddChild` in ContainersPlugin, so it always carries
// `TinyEcs.Parent`. Bevy.UI's layout roots only from `Without<Parent>`, which
// means giving Node/UOCustomRender to the game entity never renders. The UI
// tree is therefore a fresh hierarchy rooted at a `ContainerWindow` entity
// that carries the UO serial — `ContainerUiMap` maps serial -> UI entity id
// so item updates / drop logic / close systems can cross-reference.

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

using GameLayer = ClassicUO.Game.Data.Layer;

namespace ClassicUO.Ecs;

internal readonly struct ContainerGumpPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new ContainerDataRegistry());
        app.AddResource(new ContainerUiMap());
        app.AddResource(new ContainerPositionMemory());
        app.AddResource(new ContainerBackendSwitch());

        var switchBackendFn = SwitchContainerBackend;
        var spawnUiFn = SpawnContainerWindow;
        var updateSlotsFn = UpdateContainerSlots;
        var animateEyeFn = AnimateCorpseEye;
        var handleMinimizeFn = HandleMinimizeClick;
        var tearDownFn = TearDownClosedUi;
        var cascadeCloseFn = CascadeCloseChildren;
        var updateSelectionFn = UpdateSelectedFromContainerUI;
        var disposeOnLogoutFn = DisposeOnLogout;
        var syncProfileFn = SyncProfileToUiScale;
        var dragEndFn = TrackContainerDragEnd;
        var syncItemZFn = SyncItemZToWindow;

        app
            // Profile -> UIScale bridge: the options gump writes Profile; the
            // container/pickup systems read UIScale. Stage.First so every
            // consumer this frame sees the current settings.
            .AddSystem(syncProfileFn)
                .InStage(Stage.First)
                .Build()

            // std<->grid backend toggle: rebuild every open container when the
            // user flips Profile.UseGridContainers mid-session (see
            // SwitchContainerBackend).
            .AddSystem(switchBackendFn)
                .InStage(Stage.First)
                .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
                .Build()

            // Legacy ContainerGump.OnDragEnd: dropping a dragged container
            // updates the shared "last dragged position" used by
            // OverrideContainerLocationSetting 2/3 for the next open.
            .AddSystem(dragEndFn)
                .InStage(Stage.PostUpdate)
                .Build()

            .AddSystem(spawnUiFn)
                .InStage(Stage.Update)
                .RunIf((EventReader<ContainerOpenedEvent> r) => r.HasEvents)
                .Build()

            // Item slot add (0x25/0x3C) and remove (0x1D delete / 0x2E equip)
            // share one system reading one ordered ContainerSlotEvent stream.
            // Collapsing per-serial to the last event lets a slot removed and
            // re-added in one frame settle to its final state (gone, or fresh)
            // without a spawn-then-despawn race on deferred commands.
            .AddSystem(updateSlotsFn)
                .InStage(Stage.Update)
                .RunIf((EventReader<ContainerSlotEvent> r) => r.HasEvents)
                .Build()

            .AddSystem(animateEyeFn)
                .InStage(Stage.Update)
                .RunIf((Query<Data<ContainerEyeTag>> q) => q.Count() > 0)
                .Build()

            .AddSystem(handleMinimizeFn)
                .InStage(Stage.Update)
                .Build()

            .AddSystem(tearDownFn)
                .InStage(Stage.Update)
                .RunIf((EventReader<ContainerClosedEvent> r) => r.HasEvents)
                .Build()

            // Closing a container closes the sub-containers opened from items
            // inside it (legacy: a bag's gump dies with its parent). Walks every
            // open container window's game-entity parent chain; any that descends
            // from the just-closed container is closed too. Works for normal and
            // grid windows alike (both carry ContainerWindow). The emitted events
            // are drained next frame, so grandchildren cascade in turn.
            .AddSystem(cascadeCloseFn)
                .InStage(Stage.Update)
                .RunIf((EventReader<ContainerClosedEvent> r) => r.HasEvents)
                .Build()

            // Container UI hit-test piggybacks on SelectedEntity (the same
            // resource the world hit-test feeds). Stage.Last runs after
            // PostUpdate (where world rendering does Clear+Set), so a UI hover
            // wins over a world hit on the same frame (high depth dominates,
            // committed at next frame's Clear). Pickup/drop systems then see
            // the container UI entity in selectedEntity.Value.Entity exactly
            // the same way they see world entities — no separate UI lane.
            .AddSystem(updateSelectionFn)
                .InStage(Stage.Last)
                .Build()

            // Item-slot z follows its window root every frame; see
            // SyncItemZToWindow for the timing rationale.
            .AddSystem(syncItemZFn)
                .InStage(Stage.PostUpdate)
                .Build()

            // Logout / scene exit: drop every container window UI, all child
            // item slots, and the serial->ui map. Game entities (held by
            // NetworkEntitiesMap) survive — they get cleaned up when the
            // network state resets.
            .AddSystem(disposeOnLogoutFn)
                .OnExit(GameState.GameScreen)
                .Build();

        // Double-click on a container slot -> Send_DoubleClick on the item's
        // serial. Server reacts according to the item type: opens it (sub-
        // container, book, scroll), uses it (food, potion), etc. UiDoubleClick
        // is synthesized by Bevy.UI from two UiClick events on the same entity
        // within UiClayContext.DoubleClickWindow. Pickup gate is drag-only so
        // a held-still click never grabs — both clicks land cleanly here.
        //
        // DoubleClickToLootInsideContainers reroutes the gesture to a grab
        // (legacy ItemGump.OnMouseDoubleClick -> GameActions.GrabItem): pick
        // the item up and drop it into the grab bag / player backpack, unless
        // the item is itself a container (still opens) or already sits in the
        // player's backpack window.
        var dclickFn = OnContainerItemDoubleClick;
        app.AddObserver(dclickFn);

        // Legacy ContainerGump ctor plays data.OpenSound on open. The spawn
        // system is at the system param cap, so play it off the tag insert
        // instead — OnInsert<ContainerGumpTag> fires once per window open
        // (minimize mutates the tag in place, never re-inserts it).
        app.AddObserver((
            OnInsert<ContainerGumpTag> trigger,
            ResMut<AudioState> audio,
            Res<AssetsServer> assets,
            Res<Profile> profile,
            Res<Time> time) =>
        {
            var openSound = trigger.Component.OpenSound;
            if (openSound != 0)
                audio.Value.PlaySound(assets.Value, profile.Value, time.Value.Total, openSound);
        });
    }

    private static void OnContainerItemDoubleClick(
        On<UiDoubleClick> trig,
        Res<NetClient> net,
        Res<Profile> profile,
        Res<UOFileManager> fileManager,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<ContainerItemUI>> itemQ,
        Query<Data<ContainerWindow>> windowQ,
        Query<Data<Graphic>> graphicQ,
        Query<Data<Amount>> amountQ,
        Query<Data<EquipmentSlots>, Filter<With<Player>>> playerQ,
        Query<Data<NetworkSerial>> serialQ)
    {
        if (!itemQ.TryGet(trig.EntityId, out var itemRow)) return;
        var (_, link) = itemRow;

        if (profile.Value.DoubleClickToLootInsideContainers)
        {
            uint containerSerial = 0;
            if (windowQ.TryGet(link.Ref.Container, out var winRow))
            {
                var (_, w) = winRow;
                containerSerial = w.Ref.Serial;
            }

            uint backpack = 0;
            foreach (var (_, slots) in playerQ)
            {
                var bp = slots.Ref[GameLayer.Backpack];
                if (bp != 0 && serialQ.TryGet(bp, out var bpRow))
                {
                    var (_, ns) = bpRow;
                    backpack = ns.Ref.Value;
                }
                break;
            }

            bool isContainerItem = false;
            ushort amount = 1;
            if (entitiesMap.Value.TryGet(link.Ref.Serial, out var gameEnt))
            {
                if (graphicQ.TryGet(gameEnt, out var gRow))
                {
                    var (_, g) = gRow;
                    var tileData = fileManager.Value.TileData.StaticData;
                    if (g.Ref.Value < tileData.Length)
                        isContainerItem = tileData[g.Ref.Value].IsContainer;
                }
                if (amountQ.TryGet(gameEnt, out var aRow))
                {
                    var (_, a) = aRow;
                    amount = (ushort)Math.Clamp(a.Ref.Value, 1, ushort.MaxValue);
                }
            }

            if (backpack != 0 && containerSerial != backpack && !isContainerItem)
            {
                uint bag = profile.Value.GrabBagSerial != 0 ? profile.Value.GrabBagSerial : backpack;
                net.Value.Send_PickUpRequest(link.Ref.Serial, amount);
                net.Value.Send_DropRequest(link.Ref.Serial, 0xFFFF, 0xFFFF, 0, 0, bag);
                return;
            }
        }

        net.Value.Send_DoubleClick(link.Ref.Serial);
    }

    private static void DisposeOnLogout(
        Commands commands,
        Res<ContainerUiMap> uiMap,
        Res<GrabbedItem> grabbed,
        Query<Data<TinyEcs.Children>, Filter<With<ContainerWindow>>> windowsQ)
    {
        foreach (var (ent, _) in windowsQ)
            commands.Entity(ent.Ref).Despawn();
        uiMap.Value.Clear();

        // Drop held-item state too — server side will reset on relog and we
        // don't want a stale SourceUiEntity id pointing at a despawned slot.
        grabbed.Value.Clear();
        grabbed.Value.SourceUiEntity = 0;
    }

    // std<->grid container backend toggle. The two backends only diverge at OPEN
    // time (ContainerOpenedEvent is consumed once), so flipping
    // Profile.UseGridContainers mid-session leaves already-open windows in the
    // stale backend. Rebuild them: close every open container, then once the
    // close + its child cascade have fully drained, re-send ContainerOpenedEvent
    // so the now-active backend respawns each window.
    //
    // Two-phase, gated on the close stream being empty: a stray ContainerClosedEvent
    // still in flight would wipe a freshly-rebuilt window's bookkeeping (grid
    // CloseWindows removes its view unconditionally on any matching close). Corpse
    // windows (0x0009) are left alone — the grid toggle doesn't govern them
    // (corpses are owned by GridLootGumpPlugin / std corpse view).
    private static void SwitchContainerBackend(
        ResMut<ContainerBackendSwitch> state,
        Res<Profile> profile,
        Res<NetworkEntitiesMap> entitiesMap,
        ResMut<ContainerPositionMemory> memory,
        EventReader<ContainerClosedEvent> closeReader,
        EventWriter<ContainerClosedEvent> closeWriter,
        EventWriter<ContainerOpenedEvent> openWriter,
        EventWriter<ContainerSlotEvent> slotWriter,
        Query<Data<ContainerWindow, ContainerGumpTag, Node>> windowsQ,
        Query<Data<TinyEcs.Parent, Graphic, Hue, Amount, ContainerSlotPosition, NetworkSerial>,
            Filter<With<ContainedInto>, Optional<Amount>>> itemsQ)
    {
        var sw = state.Value;
        bool useGrid = profile.Value.UseGridContainers;

        if (!sw.Initialized)
        {
            sw.Initialized = true;
            sw.LastUseGrid = useGrid;
            return;
        }

        // Phase 1: toggle just flipped — capture + close every open container.
        if (useGrid != sw.LastUseGrid)
        {
            sw.LastUseGrid = useGrid;
            sw.PendingReopen.Clear();
            foreach (var (_, win, tag, node) in windowsQ)
            {
                if (tag.Ref.OriginalGraphic == 0x0009) continue;
                sw.PendingReopen.Add((win.Ref.Serial, tag.Ref.OriginalGraphic));
                // Stash the live position so the reopen lands in place. Both
                // backends consume ContainerPositionMemory.Saved (consume-once) on
                // open, so the window keeps its spot across the switch.
                memory.Value.Saved[win.Ref.Serial] =
                    new Point((int)node.Ref.Left.Value, (int)node.Ref.Top.Value);
                closeWriter.Send(new ContainerClosedEvent(win.Ref.Serial));
            }
            return;
        }

        if (sw.PendingReopen.Count == 0)
            return;

        // Phase 2: wait until the close + cascade fully settle before reopening.
        if (closeReader.HasEvents)
            return;

        foreach (var (serial, graphic) in sw.PendingReopen)
        {
            openWriter.Send(new ContainerOpenedEvent(serial, graphic));

            // Replay the container's items as slot events. Grid repopulates from
            // child entities itself, but the std backend only fills from this
            // stream — without it a grid->std switch reopens empty windows.
            if (!entitiesMap.Value.TryGet(serial, out var containerEnt))
                continue;
            foreach (var (parent, g, hue, amount, slotPos, itemSerial) in itemsQ)
            {
                if ((ulong)parent.Ref.Id != containerEnt) continue;
                var amt = amount.IsValid() ? amount.Ref.Value : 1;
                slotWriter.Send(ContainerSlotEvent.Add(
                    serial, itemSerial.Ref.Value, g.Ref.Value, hue.Ref.Value,
                    slotPos.Ref.X, slotPos.Ref.Y, (ushort)Math.Clamp(amt, 1, ushort.MaxValue)));
            }
        }
        sw.PendingReopen.Clear();
    }

    // Mirrors the legacy ItemGump.OnMouseOver path: when the cursor is over a
    // container item or container window, register that UI entity as the
    // currently selected target. Downstream pickup (resolves item UI -> game
    // entity) and drop (uses UI entity directly for ClampToContainer math)
    // both branch on the entity type they find in SelectedEntity.
    private static void UpdateSelectedFromContainerUI(
        Res<MouseContext> mouse,
        Res<SelectedEntity> selected,
        Res<AssetsServer> assets,
        UiGesturePick pick,
        Query<Data<ContainerItemUI, ComputedNode, UiCustom, Node, GlobalZIndex>> itemQuery,
        // NOT Data<...UiCustom...>: grid container windows render as a plain
        // BackgroundColor rect (no UOCustomRender), so requiring UiCustom here
        // excluded them — drops onto an empty grid area resolved to no
        // SelectedEntity, DropItem took the target==0 early-out and cleared the
        // hold without sending a drop, leaving the item stuck on the server's
        // cursor. Match on the window marker + layout/z only.
        Query<Data<ContainerWindow, ComputedNode, GlobalZIndex>> windowQuery)
    {
        var pos = mouse.Value.Position;

        // One shared pick: the topmost rendered element under the cursor. If it
        // is a container item -> that's the hover/selection target; otherwise
        // resolve its owning window and claim the window only if it's a
        // container. Occlusion (an item behind a front window, a container
        // behind a paperdoll) falls out of "topmost by paint order" for free —
        // no per-item z-gate needed.
        var hit = pick.Topmost(pos, assets.Value);
        ulong topItem = 0, topWindow = 0;
        if (hit.Found)
        {
            if (itemQuery.Contains(hit.Entity))
                topItem = hit.Entity;
            else
            {
                var owner = pick.MovableRoot(hit.Entity);
                if (owner != 0 && windowQuery.Contains(owner))
                    topWindow = owner;
            }
        }

        // Apply hover hue (0x0035, legacy ItemGump.Draw) to the winner only —
        // every other item restores its cached Original.
        foreach (var (ent, link, _, render, _, _) in itemQuery)
        {
            render.Ref.Render().Hue = ent.Ref == topItem ? link.Ref.HoverHue : link.Ref.OriginalHue;
        }

        var topEnt = topItem != 0 ? topItem : topWindow;
        if (topEnt == 0) return;

        // float.MaxValue beats any world-tile/static/body depth so the UI hover
        // always wins over whatever world entity sits under the same pixel.
        // bypassViewport: a container window dragged into the side gutter / top
        // bar is outside Camera.Bounds where the world-pick gate is off; the
        // claim must still land or drop/pickup over it silently fail.
        selected.Value.Set(topEnt, float.MaxValue, bypassViewport: true);
    }

    private static ushort ResolveBackpackGraphic(AssetsServer assets, ushort requested, int backpackStyle)
    {
        if (requested != 0x003C) return requested;
        ushort candidate = backpackStyle switch
        {
            1 => 0x775E,
            2 => 0x7760,
            3 => 0x7762,
            _ => 0x003C,
        };
        return ResolveIfTextured(assets, candidate, 0x003C);
    }

    // Swap to `candidate` only when the dataset actually ships its art; else
    // keep `fallback`. Shared tail of the backpack / large-container art swaps.
    private static ushort ResolveIfTextured(AssetsServer assets, ushort candidate, ushort fallback)
    {
        if (candidate == fallback) return fallback;
        ref readonly var info = ref assets.Gumps.GetGump(candidate);
        return info.Texture != null ? candidate : fallback;
    }

    private static void SyncProfileToUiScale(Res<Profile> profile, ResMut<UIScale> uiScale)
    {
        var p = profile.Value;
        var s = uiScale.Value;
        s.ContainerScale = Math.Clamp(p.ContainersScale, (byte)50, (byte)200) / 100f;
        s.ScaleItemsInsideContainers = p.ScaleItemsInsideContainers;
        s.HueContainerGumps = p.HueContainerGumps;
        s.BackpackStyle = p.BackpackStyle;
        s.RelativeDragAndDropItems = p.RelativeDragAndDropItems;
        s.SkipEmptyCorpse = p.SkipEmptyCorpse;
        s.OverrideContainerLocation = p.OverrideContainerLocation;
    }

    // Legacy PacketHandlers.OpenContainer: UOP clients with the option on swap
    // the classic container art for the big 0x06Ex/0x9CDx variants when the
    // dataset actually has them.
    private static ushort ResolveLargeContainerGraphic(AssetsServer assets, ushort graphic)
    {
        ushort candidate = graphic switch
        {
            0x0048 => 0x06E8,
            0x0049 => 0x9CDF,
            0x0051 => 0x06E7,
            0x003E => 0x06E9,
            0x004D => 0x06EA,
            0x004E => 0x06E6,
            0x004F => 0x06E5,
            0x004A => 0x9CDD,
            0x0044 => 0x9CE3,
            _ => graphic,
        };
        return ResolveIfTextured(assets, candidate, graphic);
    }

    // World-anchored screen position of the container's root holder (chest on
    // ground / corpse / pack-animal mobile). Same camera math as
    // NameplatePlugin.UpdatePlates. Null when nothing in the chain has a
    // WorldPosition (e.g. a sub-container of the player's own backpack).
    private static Vector2? ScreenPosOf(
        uint serial,
        GameContext gameCtx,
        Camera camera,
        NetworkEntitiesMap entitiesMap,
        Query<Data<WorldPosition>> worldPosQ,
        Query<Data<TinyEcs.Parent>> parentsQ)
    {
        if (!entitiesMap.TryGet(serial, out var ent)) return null;

        var root = ent;
        for (int i = 0; i < 16 && !worldPosQ.Contains(root); i++)
        {
            if (!parentsQ.TryGet(root, out var parentRow)) break;
            var (_, p) = parentRow;
            var pid = (ulong)p.Ref.Id;
            if (pid == 0 || pid == root) break;
            root = pid;
        }
        if (!worldPosQ.TryGet(root, out var posRow)) return null;
        var (_, wp) = posRow;

        var center = Isometric.IsoToScreen(gameCtx.CenterX, gameCtx.CenterY, gameCtx.CenterZ);
        center -= new Vector2(camera.Bounds.Width, camera.Bounds.Height) / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.CenterOffset;

        var position = Isometric.IsoToScreen(wp.Ref.X, wp.Ref.Y, wp.Ref.Z);
        position -= center;
        return camera.WorldToScreen(position) + new Vector2(camera.Bounds.X, camera.Bounds.Y);
    }

    // Legacy ContainerManager.CalculateContainerPosition: a per-serial saved
    // position (setting 3, consumed once like UIManager.RemovePosition) wins;
    // otherwise OverrideContainerLocationSetting picks near-object / top-right
    // / the shared last-dragged point; otherwise the registry default.
    internal static (float X, float Y) ResolveSpawnPosition(
        uint serial,
        int width,
        int height,
        ContainerDataRegistry registry,
        Profile profile,
        GameContext gameCtx,
        Camera camera,
        UiSurface surface,
        NetworkEntitiesMap entitiesMap,
        ContainerPositionMemory memory,
        Query<Data<WorldPosition>> worldPosQ,
        Query<Data<TinyEcs.Parent>> parentsQ)
    {
        if (memory.Saved.TryGetValue(serial, out var saved))
        {
            memory.Saved.Remove(serial);
            return (saved.X, saved.Y);
        }

        if (!profile.OverrideContainerLocation)
            return (registry.DefaultX, registry.DefaultY);

        float screenW = surface.LogicalSize.X;
        float screenH = surface.LogicalSize.Y;
        float x = registry.DefaultX;
        float y = registry.DefaultY;

        switch (profile.OverrideContainerLocationSetting)
        {
            case 0: // near the container in the world (legacy +40, vertically centered)
                if (ScreenPosOf(serial, gameCtx, camera, entitiesMap, worldPosQ, parentsQ) is { } pos)
                {
                    x = pos.X + 40;
                    y = pos.Y - (height >> 1);
                }
                break;
            case 1: // top right
                x = screenW - width;
                y = 0;
                break;
            case 2:
            case 3: // centered on the last dragged point
                x = profile.OverrideContainerLocationPosition.X - (width >> 1);
                y = profile.OverrideContainerLocationPosition.Y - (height >> 1);
                break;
        }

        if (x + width > screenW) x -= width;
        if (y + height > screenH) y -= height;
        return (x, y);
    }

    // Legacy ContainerGump.OnDragEnd: when a container window was moved with
    // the mouse and setting >= 2, remember its center as the spawn point for
    // the next container. Snapshot positions on press, compare on release.
    private static void TrackContainerDragEnd(
        Res<MouseContext> mouse,
        Res<Profile> profile,
        Local<Dictionary<ulong, Vector2>> snapshot,
        Query<Data<ContainerWindow, Node>> windowsQ)
    {
        snapshot.Value ??= new Dictionary<ulong, Vector2>();

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            snapshot.Value.Clear();
            foreach (var (ent, _, node) in windowsQ)
                snapshot.Value[ent.Ref] = new Vector2(node.Ref.Left.Value, node.Ref.Top.Value);
            return;
        }

        if (!mouse.Value.IsReleased(MouseButtonType.Left) || snapshot.Value.Count == 0)
            return;

        if (profile.Value.OverrideContainerLocation
            && profile.Value.OverrideContainerLocationSetting >= 2)
        {
            foreach (var (ent, _, node) in windowsQ)
            {
                if (!snapshot.Value.TryGetValue(ent.Ref, out var pressPos)) continue;
                var cur = new Vector2(node.Ref.Left.Value, node.Ref.Top.Value);
                if (cur == pressPos) continue;
                profile.Value.OverrideContainerLocationPosition = new Point(
                    (int)(cur.X + node.Ref.Width.Value / 2f),
                    (int)(cur.Y + node.Ref.Height.Value / 2f));
            }
        }
        snapshot.Value.Clear();
    }

    private static void SpawnContainerWindow(
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        Res<ContainerDataRegistry> registry,
        Res<ContainerUiMap> uiMap,
        Res<UIScale> uiScale,
        Res<UiZCounter> zCounter,
        Res<Profile> profile,
        Res<GameContext> gameCtx,
        Res<UiSurface> surface,
        Res<Camera> camera,
        Res<ContainerPositionMemory> memory,
        EventReader<ContainerOpenedEvent> reader,
        Query<Data<Hue>> hueQuery,
        Query<Data<WorldPosition>> worldPosQ,
        Query<Data<TinyEcs.Parent>> parentsQ)
    {
        foreach (var ev in reader.Read())
        {
            if (ev.Graphic == 0xFFFF || ev.Graphic == 0x0030)
                continue;

            // Grid-loot type 1 ("grid only") replaces the normal corpse window
            // (gump 0x0009) with GridLootGumpPlugin's grid; suppress the default
            // window here. Type 2 ("both") falls through and opens this too.
            if (ev.Graphic == GridLootGumpPlugin.CorpseContainerGump && profile.Value.GridLootType == 1)
                continue;

            // Grid containers replace the normal gump for every non-corpse
            // container; GridContainerGumpPlugin owns those windows.
            if (ev.Graphic != GridLootGumpPlugin.CorpseContainerGump && profile.Value.UseGridContainers)
                continue;

            // Skip if a UI window for this container is already up.
            if (uiMap.Value.TryGet(ev.Serial, out _))
                continue;

            var graphic = ResolveBackpackGraphic(assets.Value, ev.Graphic, uiScale.Value.BackpackStyle);
            if (gameCtx.Value.ClientVersion >= ClientVersion.CV_706000 && profile.Value.UseLargeContainerGumps)
                graphic = ResolveLargeContainerGraphic(assets.Value, graphic);
            var data = registry.Value.Get(graphic);

            bool isBoard = graphic == 0x091A || graphic == 0x092E;
            float scale = isBoard ? 1f : uiScale.Value.ContainerScale;

            ref readonly var gumpInfo = ref assets.Value.Gumps.GetGump(graphic);
            var width = (int)(gumpInfo.UV.Width * scale);
            var height = (int)(gumpInfo.UV.Height * scale);

            var (posX, posY) = ResolveSpawnPosition(
                ev.Serial, width, height, registry.Value, profile.Value, gameCtx.Value,
                camera.Value, surface.Value, entitiesMap.Value, memory.Value, worldPosQ, parentsQ);

            var hueVec = Vector3.UnitZ;
            if (uiScale.Value.HueContainerGumps
                && entitiesMap.Value.TryGet(ev.Serial, out var itemEntId)
                && hueQuery.TryGet(itemEntId, out var hueRow))
            {
                var (_, h) = hueRow;
                if (h.Ref.Value != 0)
                    hueVec = new Vector3(h.Ref.Value, 1, 1);
            }

            // Container layout is two-layer:
            //   * `ui` is Absolute (draggable, hosts the gump background +
            //     interaction). Bevy.UI / Clay treats Absolute nodes as
            //     Floating, and Floating children of Floating do not nest in
            //     the layer they visually belong to — they default to z=0 and
            //     are drawn behind the parent's own gump fill. That's why
            //     items go missing when parented directly to `ui`.
            //   * `content` is Relative, sized to fill the window. Items live
            //     under `content`, so their PositionType.Absolute resolves to
            //     Floating.AttachTo=Parent against a non-Floating parent,
            //     which is the pattern LoginScreenPlugin uses for its menu.
            var ui = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(posX),
                    Top = Val.Px(posY),
                    Width = Val.Px(width),
                    Height = Val.Px(height),
                })
                .Insert(new UiCustom
                {
                    Data = new UOCustomRender
                    {
                        Kind = UOCustomKind.Gump,
                        AssetId = graphic,
                        Hue = hueVec,
                    }
                })
                .Insert(Interaction.None)
                .Insert(new FloatingWindowState
                {
                    InitialX = posX,
                    InitialY = posY,
                    InitialWidth = width,
                    InitialHeight = height,
                })
                .Insert(new ContainerGumpTag
                {
                    Graphic = graphic,
                    OriginalGraphic = ev.Graphic,
                    IconizedGraphic = data.IconizedGraphic,
                    OpenSound = data.OpenSound,
                    ClosedSound = data.ClosedSound,
                    Scale = scale,
                    IsBoard = isBoard,
                    Bounds = data.Bounds,
                })
                .Insert(new ContainerWindow { Serial = ev.Serial })
                .Insert<UiMovable>();

            int zBase = zCounter.Value.Bump();
            commands.Entity(ui.Id).Insert(new GlobalZIndex(zBase));

            // Relative content area parents the item children.
            var content = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Relative,
                    Width = Val.Percent(100),
                    Height = Val.Percent(100),
                });
            ui.AddChild(content);

            uiMap.Value.Set(ev.Serial, new ContainerUiMap.Entry
            {
                UiEntity = ui.Id,
                ContentEntity = content.Id,
                Graphic = graphic,
                OriginalGraphic = ev.Graphic,
                Scale = scale,
                IsBoard = isBoard,
                ZBase = zBase,
                Bounds = data.Bounds,
            });

            if (data.MinimizerArea.Width > 0 && data.MinimizerArea.Height > 0)
            {
                var btn = commands.Spawn()
                    .Insert(new Node
                    {
                        PositionType = PositionType.Absolute,
                        Left = Val.Px(data.MinimizerArea.X * scale),
                        Top = Val.Px(data.MinimizerArea.Y * scale),
                        Width = Val.Px(data.MinimizerArea.Width * scale),
                        Height = Val.Px(data.MinimizerArea.Height * scale),
                    })
                    .Insert(Interaction.None)
                    .Insert(new MinimizeHitbox { Container = ui.Id });
                ui.AddChild(btn);
            }

            if (graphic == 0x0009)
            {
                var eye = commands.Spawn()
                    .Insert(new Node
                    {
                        PositionType = PositionType.Absolute,
                        Left = Val.Px(45f * scale),
                        Top = Val.Px(30f * scale),
                    })
                    .Insert(new UiCustom
                    {
                        Data = new UOCustomRender
                        {
                            Kind = UOCustomKind.Gump,
                            AssetId = 0x0045,
                            Hue = Vector3.UnitZ,
                        }
                    })
                    .Insert(new ContainerEyeTag { NextTickMs = 0 });
                ui.AddChild(eye);
            }

        }
    }

    // Add (0x25/0x3C) and remove (0x1D/0x2E) of container slots, driven by one
    // ordered ContainerSlotEvent stream. A serial can be both added and removed
    // in one packet read; order decides the final state and the two are NOT
    // symmetric:
    //   mount    = add(0x3EAA) then equip/delete -> Remove last -> no slot
    //   dismount = delete      then add(statuette) -> Add last   -> slot
    // We collapse the stream to the LAST event per serial and act on that one
    // intent. Resolving net intent in memory first sidesteps the deferred-
    // command race that two separate systems hit: a slot spawned and removed in
    // the same frame is still queued in Commands, invisible to a despawn query.
    private static void UpdateContainerSlots(
        Commands commands,
        Res<ContainerUiMap> uiMap,
        Res<AssetsServer> assets,
        Res<UIScale> uiScale,
        Res<UOFileManager> fileManager,
        Res<GrabbedItem> grabbed,
        EventReader<ContainerSlotEvent> reader,
        Query<Data<ContainerItemUI>> existingItemsQ,
        Query<Data<GlobalZIndex>> windowZQ)
    {
        var tileData = fileManager.Value.TileData.StaticData;

        // Buffer order == server emit order, so the last event for a serial is
        // its final intent. `order` preserves first-seen sequence for
        // deterministic command emission.
        var finalBySerial = new Dictionary<uint, ContainerSlotEvent>();
        var order = new List<uint>();
        foreach (var raw in reader.Read())
        {
            if (!finalBySerial.ContainsKey(raw.ItemSerial))
                order.Add(raw.ItemSerial);
            finalBySerial[raw.ItemSerial] = raw;
        }

        foreach (var itemSerial in order)
        {
            var ev = finalBySerial[itemSerial];

            DespawnExistingSlot(commands, existingItemsQ, grabbed.Value, itemSerial);

            if (ev.Action == ContainerSlotAction.Remove)
                continue;

            // Read window snapshot + item payload from the event itself —
            // querying the game entity here would race the deferred commands
            // that ContainersPlugin queued this same frame for Graphic/Hue/...
            if (!uiMap.Value.TryGet(ev.ContainerSerial, out var entry)) continue;

            if (ShouldSkipSlot(ev.Graphic, entry, tileData))
                continue;

            SpawnItemSlot(commands, assets.Value, uiScale.Value, tileData, ev, entry, windowZQ);
        }
    }

    // Despawn any existing slot for this serial. For Remove that's the whole
    // job; for Add it's the dedup pass — a stale source slot can linger after a
    // same-container drop (server sends only 0x25, never 0x29), and its old
    // ComputedNode would still answer the hover hit-test, so the next pickup
    // grabs the wrong slot and the item looks duplicated.
    private static void DespawnExistingSlot(
        Commands commands,
        Query<Data<ContainerItemUI>> existingItemsQ,
        GrabbedItem grabbed,
        uint itemSerial)
    {
        foreach (var (oldEnt, oldLink) in existingItemsQ)
        {
            if (oldLink.Ref.Serial != itemSerial) continue;
            commands.Entity(oldEnt.Ref).Despawn();
            if (grabbed.SourceUiEntity == oldEnt.Ref)
                grabbed.SourceUiEntity = 0;
        }
    }

    // Corpse layers the dataset flags as un-lootable, plus hair/beard/face on
    // any container, never get a visible slot (legacy ContainerGump filter).
    internal static bool ShouldSkipSlot(ushort graphic, ContainerUiMap.Entry entry, StaticTiles[] tileData)
    {
        if (graphic >= tileData.Length) return false;
        ref readonly var td = ref tileData[graphic];
        var slot = (GameLayer)td.Layer;
        bool isCorpse = entry.OriginalGraphic == 0x0009;
        if (isCorpse && td.Layer > 0 && (int)slot < Constants.BAD_CONTAINER_LAYERS.Length
            && !Constants.BAD_CONTAINER_LAYERS[(int)slot])
            return true;
        if (td.IsWearable && (slot == GameLayer.Face || slot == GameLayer.Beard || slot == GameLayer.Hair))
            return true;
        return false;
    }

    private static void SpawnItemSlot(
        Commands commands,
        AssetsServer assets,
        UIScale uiScale,
        StaticTiles[] tileData,
        ContainerSlotEvent ev,
        ContainerUiMap.Entry entry,
        Query<Data<GlobalZIndex>> windowZQ)
    {
        ushort displayed = ev.Graphic;

        // Coins draw a pile sprite (base+1 / base+2) by amount; everything else
        // draws its own graphic. `displayed` stays the base graphic for the
        // tiledata/layer logic above.
        ushort artGraphic = ItemGraphics.Displayed(ev.Graphic, ev.Amount);
        ushort drawGraphic = entry.IsBoard
            ? (ushort)(artGraphic - Constants.ITEM_GUMP_TEXTURE_OFFSET)
            : artGraphic;

        var (spriteW, spriteH) = ResolveSpriteSize(assets, uiScale, entry, drawGraphic);
        var (drawX, drawY) = ClampSlotPosition(ev, entry, spriteW, spriteH);

        var origHue = ShaderHueTranslator.GetHueVector(ev.Hue, partial: false, alpha: 1f, gump: entry.IsBoard);
        var hoverHue = ShaderHueTranslator.GetHueVector(0x0035, partial: false, alpha: 1f, gump: entry.IsBoard);

        // Mirror legacy ItemGump.Draw: stackable items with Amount > 1 get drawn
        // twice (+5/+5 offset) to fake a pile. Coins are excluded because legacy
        // `IsCoin` skips them too — coin stack visuals come from a separate
        // piled-graphic asset.
        bool stacked = false;
        if (!entry.IsBoard && displayed < tileData.Length)
        {
            ref readonly var td = ref tileData[displayed];
            stacked = ItemGraphics.DrawStacked(ev.Graphic, ev.Amount, td.IsStackable);
        }

        var itemUi = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(drawX),
                Top = Val.Px(drawY),
                Width = Val.Px(spriteW),
                Height = Val.Px(spriteH),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = entry.IsBoard ? UOCustomKind.Gump : UOCustomKind.Art,
                    AssetId = drawGraphic,
                    Hue = origHue,
                    Stacked = stacked,
                }
            })
            .Insert(Interaction.None)
            .Insert(new ContainerItemUI
            {
                Container = entry.UiEntity,
                Serial = ev.ItemSerial,
                OriginalHue = origHue,
                HoverHue = hoverHue,
            })
            // Items share the window's z. Clay's float-root sort is stable on
            // equal z, so items declared after the window in tree order draw on
            // top of it. PropagateZ keeps them in sync on focus bumps, but the
            // FIRST z we hand out has to be the window's CURRENT z, not
            // entry.ZBase (which was cached at container-open time). Otherwise
            // late spawns — e.g. the slot respawned after a drop, after the user
            // has clicked the window several times — get a stale low z and
            // render behind the window background until the next focus bump
            // propagates a new value to them.
            .Insert(new GlobalZIndex(ResolveCurrentZ(entry, windowZQ)));
        commands.Entity(entry.ContentEntity).AddChild(itemUi);
    }

    private static (int W, int H) ResolveSpriteSize(
        AssetsServer assets, UIScale uiScale, ContainerUiMap.Entry entry, ushort drawGraphic)
    {
        int spriteW, spriteH;
        if (entry.IsBoard)
        {
            ref readonly var gi = ref assets.Gumps.GetGump(drawGraphic);
            spriteW = gi.UV.Width;
            spriteH = gi.UV.Height;
        }
        else
        {
            ref readonly var ai = ref assets.Arts.GetArt(drawGraphic);
            spriteW = ai.UV.Width;
            spriteH = ai.UV.Height;
        }

        if (uiScale.ScaleItemsInsideContainers && !entry.IsBoard)
        {
            spriteW = (int)(spriteW * entry.Scale);
            spriteH = (int)(spriteH * entry.Scale);
        }
        return (spriteW, spriteH);
    }

    // Clamp the server-sent slot position into the container's content bounds so
    // an item dropped/added at an out-of-bounds coord renders (and stays
    // pickable) inside the gump. Mirrors legacy ContainerGump bounds math +
    // ClampToContainer: left/top edge is Bounds.X/Y, right/bottom edge is
    // Bounds.Width/Height (both scaled).
    internal static (float X, float Y) ClampSlotPosition(
        ContainerSlotEvent ev, ContainerUiMap.Entry entry, int spriteW, int spriteH)
    {
        float scale = entry.Scale;
        float bx = entry.Bounds.X * scale;
        float by = entry.Bounds.Y * scale;
        float bw = entry.Bounds.Width * scale;
        float bh = entry.Bounds.Height * scale;

        float drawX = ev.X * scale;
        float drawY = (short)ev.Y * scale;
        if (drawX + spriteW > bw) drawX = bw - spriteW;
        if (drawY + spriteH > bh) drawY = bh - spriteH;
        if (drawX < bx) drawX = bx;
        if (drawY < by) drawY = by;

        // Chessboard (0x091A) renders items shifted up 20px; apply after the
        // clamp so the bounds check runs in unshifted space.
        if (entry.Graphic == 0x091A) drawY -= 20 * scale;
        return (drawX, drawY);
    }

    internal static int ResolveCurrentZ(ContainerUiMap.Entry entry, Query<Data<GlobalZIndex>> q)
    {
        if (!q.TryGet(entry.UiEntity, out var zRow)) return entry.ZBase;
        var (_, z) = zRow;
        return z.Ref.Value;
    }

    // Sync each item slot's GlobalZIndex to its parent window. Window root z is
    // bumped by WindowDragPlugin.Drag in Stage.Update (in-place mutation, no
    // observer fires), and Bevy.UI's z propagation only covers descendants
    // WITHOUT their own GlobalZIndex — items carry theirs so they sort against
    // world UI. Run in PostUpdate (AFTER Drag's bump, BEFORE Last where
    // layout/render snapshot z) so the lift lands in the SAME frame as the
    // click — PreUpdate would leave items one frame behind and flicker the bg
    // over them.
    private static void SyncItemZToWindow(
        Query<Data<ContainerItemUI, GlobalZIndex>> items,
        Query<Data<GlobalZIndex>, Filter<With<ContainerWindow>>> windows)
    {
        foreach (var (_, link, itemZ) in items)
        {
            if (!windows.TryGet(link.Ref.Container, out var windowRow)) continue;
            var (_, winZ) = windowRow;
            if (itemZ.Ref.Value != winZ.Ref.Value)
                itemZ.Ref.Value = winZ.Ref.Value;
        }
    }

    private static void AnimateCorpseEye(
        Res<Time> time,
        Query<Data<ContainerEyeTag, UiCustom>> q)
    {
        foreach (var (tag, render) in q)
        {
            if (time.Value.Total < tag.Ref.NextTickMs) continue;
            var r = render.Ref.Render();
            r.AssetId = r.AssetId == 0x0045 ? 0x0046u : 0x0045u;
            tag.Ref.NextTickMs = time.Value.Total + 750f;
        }
    }

    private static void HandleMinimizeClick(
        Res<MouseContext> mouse,
        Res<AssetsServer> assets,
        Query<Data<MinimizeHitbox, Interaction>> hitboxQuery,
        Query<Data<ContainerGumpTag, UiCustom, Node>> containerQuery,
        Query<Data<Node, UiCustom>> childNodes,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (!mouse.Value.IsPressedOnce(MouseButtonType.Left)) return;

        foreach (var (ent, hb, interaction) in hitboxQuery)
        {
            if (interaction.Ref != Interaction.Pressed) continue;
            if (!containerQuery.TryGet(hb.Ref.Container, out var containerRow)) continue;

            var (_, tag, render, node) = containerRow;
            bool willMinimize = !tag.Ref.IsMinimized;

            ushort newGraphic = willMinimize && tag.Ref.IconizedGraphic != 0
                ? tag.Ref.IconizedGraphic
                : tag.Ref.Graphic;

            render.Ref.Render().AssetId = newGraphic;
            tag.Ref.IsMinimized = willMinimize;

            ref readonly var gi = ref assets.Value.Gumps.GetGump(newGraphic);
            node.Ref.Width = Val.Px(gi.UV.Width * tag.Ref.Scale);
            node.Ref.Height = Val.Px(gi.UV.Height * tag.Ref.Scale);

            if (childrenQ.TryGet(hb.Ref.Container, out var childrenRow))
            {
                var (_, kids) = childrenRow;
                foreach (var cid in kids.Ref)
                {
                    if (!childNodes.TryGet(cid, out var childRow)) continue;
                    var (_, childNode, _) = childRow;
                    childNode.Ref.Display = willMinimize ? Display.None : Display.Flex;
                }
            }
            break;
        }
    }

    // Despawn the UI subtree for a closed container. The game entity stays —
    // it still exists in the world / inventory, only the visible window goes
    // away. Item UI children are picked up via the Children component and
    // despawned alongside their parent.
    // Close any open container window whose game entity descends from a
    // just-closed container (sub-bags die with their parent). Emits more
    // ContainerClosedEvents; they drain next frame so the chain cascades.
    private static void CascadeCloseChildren(
        Res<NetworkEntitiesMap> entitiesMap,
        EventReader<ContainerClosedEvent> reader,
        EventWriter<ContainerClosedEvent> writer,
        Query<Data<ContainerWindow>> windowsQ,
        Query<Data<TinyEcs.Parent>> parentsQ)
    {
        foreach (var ev in reader.Read())
        {
            if (!entitiesMap.Value.TryGet(ev.Serial, out var closedEnt))
                continue;

            foreach (var (_, win) in windowsQ)
            {
                if (win.Ref.Serial == ev.Serial)
                    continue;
                if (!entitiesMap.Value.TryGet(win.Ref.Serial, out var winEnt))
                    continue;
                if (DescendsFrom(winEnt, closedEnt, parentsQ))
                    writer.Send(new ContainerClosedEvent(win.Ref.Serial));
            }
        }
    }

    private static bool DescendsFrom(ulong entity, ulong ancestor, Query<Data<TinyEcs.Parent>> parentsQ)
    {
        var cur = entity;
        for (int i = 0; i < 16; i++)
        {
            if (!parentsQ.TryGet(cur, out var row))
                return false;
            var (_, p) = row;
            var pid = (ulong)p.Ref.Id;
            if (pid == 0 || pid == cur)
                return false;
            if (pid == ancestor)
                return true;
            cur = pid;
        }
        return false;
    }

    private static void TearDownClosedUi(
        Commands commands,
        Res<ContainerUiMap> uiMap,
        Res<Profile> profile,
        Res<ContainerPositionMemory> memory,
        ResMut<AudioState> audio,
        Res<AssetsServer> assets,
        Res<Time> time,
        EventReader<ContainerClosedEvent> reader,
        Query<Data<Node>> nodeQ,
        Query<Data<ContainerGumpTag>> tagQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var ev in reader.Read())
        {
            if (!uiMap.Value.TryGet(ev.Serial, out var entry))
                continue;

            // Legacy CloseWithRightClick plays ClosedSound; server/logout
            // teardowns (UserInitiated == false) stay silent.
            if (ev.UserInitiated && tagQ.TryGet(entry.UiEntity, out var tagRow))
            {
                var (_, tag) = tagRow;
                if (tag.Ref.ClosedSound != 0)
                    audio.Value.PlaySound(assets.Value, profile.Value, time.Value.Total, tag.Ref.ClosedSound);
            }

            // Setting 3 remembers each container's position across closes
            // (legacy ContainerGump.Dispose -> UIManager.SavePosition).
            if (profile.Value.OverrideContainerLocationSetting == 3
                && nodeQ.TryGet(entry.UiEntity, out var nodeRow))
            {
                var (_, n) = nodeRow;
                memory.Value.Saved[ev.Serial] = new Point((int)n.Ref.Left.Value, (int)n.Ref.Top.Value);
            }

            if (childrenQ.TryGet(entry.UiEntity, out var childrenRow))
            {
                var (_, kids) = childrenRow;
                foreach (var cid in kids.Ref)
                    commands.Entity(cid).Despawn();
            }
            commands.Entity(entry.UiEntity).Despawn();
            uiMap.Value.Remove(ev.Serial);
        }
    }
}

// Marker on the UI window root. Carries the UO serial so drop systems and
// close systems can find the right game-side container.
// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct ContainerWindow
{
    public uint Serial;
}

internal struct ContainerGumpTag
{
    public ushort Graphic;
    public ushort OriginalGraphic;
    public ushort IconizedGraphic;
    public ushort OpenSound;
    public ushort ClosedSound;
    public float Scale;
    public bool IsBoard;
    public bool IsMinimized;
    public Rectangle Bounds;
}

internal struct ContainerItemUI
{
    public ulong Container;   // UI entity id (NOT the game entity)
    public uint Serial;       // UO serial — used by drop / pickup
    // Cached so the selection hue toggle can flip Hover<->Original without
    // recomputing ShaderHueTranslator output each frame. Hover mirrors legacy
    // ItemGump.Draw which forces 0x0035 when MouseIsOver.
    public Vector3 OriginalHue;
    public Vector3 HoverHue;
}

internal struct ContainerEyeTag
{
    public float NextTickMs;
}

internal struct MinimizeHitbox
{
    public ulong Container;   // UI entity id
}

// Tracks Profile.UseGridContainers across frames so SwitchContainerBackend can
// rebuild every open container when the std<->grid toggle flips mid-session.
internal sealed class ContainerBackendSwitch
{
    public bool Initialized;
    public bool LastUseGrid;
    // (serial, server-sent graphic) captured at close time, reopened once the
    // close + child cascade have drained.
    public readonly List<(uint Serial, ushort Graphic)> PendingReopen = new();
}

// Per-serial remembered window positions for OverrideContainerLocationSetting
// == 3 ("remember each container"). Written on close (legacy Dispose ->
// UIManager.SavePosition), consumed once on the next open (legacy
// GetGumpCachePosition + RemovePosition).
internal sealed class ContainerPositionMemory
{
    public readonly Dictionary<uint, Point> Saved = new();
}

// Serial -> UI window entry. Caches enough of ContainerGumpTag inline so item
// spawns that arrive in the *same frame* as the window open packet don't have
// to query the tag (commands are deferred — the UI entity is only materialized
// after this system's batch flushes).
internal sealed class ContainerUiMap
{
    public struct Entry
    {
        public ulong UiEntity;         // Absolute window root (draggable, gump bg)
        public ulong ContentEntity;    // Relative inner content area; item parent
        public ushort Graphic;
        public ushort OriginalGraphic;
        public float Scale;
        public bool IsBoard;
        public int ZBase;              // Window's GlobalZIndex; items use ZBase+1
        public Rectangle Bounds;       // Container content rect (unscaled); item draw pos is clamped into it
    }

    private readonly Dictionary<uint, Entry> _map = new();

    public void Set(uint serial, Entry entry) => _map[serial] = entry;
    public bool TryGet(uint serial, out Entry entry) => _map.TryGetValue(serial, out entry);
    public bool Remove(uint serial) => _map.Remove(serial);
    public void Clear() => _map.Clear();
}
