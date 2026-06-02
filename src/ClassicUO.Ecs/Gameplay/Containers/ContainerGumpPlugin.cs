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
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
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

        var spawnUiFn = SpawnContainerWindow;
        var updateSlotsFn = UpdateContainerSlots;
        var animateEyeFn = AnimateCorpseEye;
        var handleMinimizeFn = HandleMinimizeClick;
        var tearDownFn = TearDownClosedUi;
        var updateSelectionFn = UpdateSelectedFromContainerUI;
        var disposeOnLogoutFn = DisposeOnLogout;

        app
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

            // Sync each item slot's GlobalZIndex to its parent window. Window
            // root z is bumped by WindowDragPlugin.Drag in Stage.Update (in-
            // place mutation, no observer fires), and Bevy.UI's z propagation
            // only covers descendants WITHOUT their own GlobalZIndex — items
            // carry theirs so they sort against world UI. Run in PostUpdate
            // (AFTER Drag's bump, BEFORE Last where layout/render snapshot z)
            // so the lift lands in the SAME frame as the click — PreUpdate
            // would leave items one frame behind and flicker the bg over them.
            .AddSystem((
                Query<Data<ContainerItemUI, GlobalZIndex>> items,
                Query<Data<GlobalZIndex>, Filter<With<ContainerWindow>>> windows) =>
            {
                foreach (var (_, link, itemZ) in items)
                {
                    if (!windows.Contains(link.Ref.Container)) continue;
                    var (_, winZ) = windows.Get(link.Ref.Container);
                    if (itemZ.Ref.Value != winZ.Ref.Value)
                        itemZ.Ref.Value = winZ.Ref.Value;
                }
            })
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
        app.AddObserver((
            On<UiDoubleClick> trig,
            Res<NetClient> net,
            Query<Data<ContainerItemUI>> itemQ) =>
        {
            if (!itemQ.Contains(trig.EntityId)) return;
            var (_, link) = itemQ.Get(trig.EntityId);
            net.Value.Send_DoubleClick(link.Ref.Serial);
        });
    }

    private static void DisposeOnLogout(
        Commands commands,
        Res<ContainerUiMap> uiMap,
        Res<GrabbedItem> grabbed,
        Query<Data<TinyEcs.Children>, Filter<With<ContainerWindow>>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ)
            DespawnSubtree(commands, ent.Ref, childrenQ);
        uiMap.Value.Clear();

        // Drop held-item state too — server side will reset on relog and we
        // don't want a stale SourceUiEntity id pointing at a despawned slot.
        grabbed.Value.Clear();
        grabbed.Value.SourceUiEntity = 0;
    }

    private static void DespawnSubtree(
        Commands commands,
        ulong entity,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.Contains(entity))
        {
            var (_, kids) = childrenQ.Get(entity);
            foreach (var cid in kids.Ref)
                DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(entity).Despawn();
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
        Query<Data<ComputedNode, Node, UiCustom>, Filter<Optional<UiCustom>>> rendered,
        Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>> movables,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<ContainerItemUI, ComputedNode, UiCustom, Node, GlobalZIndex>> itemQuery,
        Query<Data<ContainerWindow, ComputedNode, UiCustom, GlobalZIndex>> windowQuery)
    {
        var pos = mouse.Value.Position;

        // One shared pick: the topmost rendered element under the cursor. If it
        // is a container item -> that's the hover/selection target; otherwise
        // resolve its owning window and claim the window only if it's a
        // container. Occlusion (an item behind a front window, a container
        // behind a paperdoll) falls out of "topmost by paint order" for free —
        // no per-item z-gate needed.
        var hit = UiPick.Topmost(pos, assets.Value, rendered);
        ulong topItem = 0, topWindow = 0;
        if (hit.Found)
        {
            if (itemQuery.Contains(hit.Entity))
                topItem = hit.Entity;
            else
            {
                var owner = UiPick.MovableRoot(hit.Entity, movables, parents);
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
        if (candidate == 0x003C) return 0x003C;
        ref readonly var info = ref assets.Gumps.GetGump(candidate);
        return info.Texture != null ? candidate : (ushort)0x003C;
    }

    private static void SpawnContainerWindow(
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<AssetsServer> assets,
        Res<ContainerDataRegistry> registry,
        Res<ContainerUiMap> uiMap,
        Res<UIScale> uiScale,
        Res<UiZCounter> zCounter,
        EventReader<ContainerOpenedEvent> reader,
        Query<Data<Hue>> hueQuery)
    {
        foreach (var ev in reader.Read())
        {
            if (ev.Graphic == 0xFFFF || ev.Graphic == 0x0030)
                continue;

            // Skip if a UI window for this container is already up.
            if (uiMap.Value.TryGet(ev.Serial, out _))
                continue;

            var graphic = ResolveBackpackGraphic(assets.Value, ev.Graphic, uiScale.Value.BackpackStyle);
            var data = registry.Value.Get(graphic);

            bool isBoard = graphic == 0x091A || graphic == 0x092E;
            float scale = isBoard ? 1f : uiScale.Value.ContainerScale;

            ref readonly var gumpInfo = ref assets.Value.Gumps.GetGump(graphic);
            var width = (int)(gumpInfo.UV.Width * scale);
            var height = (int)(gumpInfo.UV.Height * scale);

            var posX = registry.Value.DefaultX;
            var posY = registry.Value.DefaultY;

            var hueVec = Vector3.UnitZ;
            if (uiScale.Value.HueContainerGumps
                && entitiesMap.Value.TryGet(ev.Serial, out var itemEntId)
                && hueQuery.Contains(itemEntId))
            {
                var (_, h) = hueQuery.Get(itemEntId);
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
                .Insert<UIMovable>();

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

            // TODO(audio): play data.OpenSound once AudioManager is ported to
            // an App resource.
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

            // Despawn any existing slot for this serial. For Remove that's the
            // whole job; for Add it's the dedup pass — a stale source slot can
            // linger after a same-container drop (server sends only 0x25, never
            // 0x29), and its old ComputedNode would still answer the hover hit-
            // test, so the next pickup grabs the wrong slot and the item looks
            // duplicated.
            foreach (var (oldEnt, oldLink) in existingItemsQ)
            {
                if (oldLink.Ref.Serial != itemSerial) continue;
                commands.Entity(oldEnt.Ref).Despawn();
                if (grabbed.Value.SourceUiEntity == oldEnt.Ref)
                    grabbed.Value.SourceUiEntity = 0;
            }

            if (ev.Action == ContainerSlotAction.Remove)
                continue;

            // Read window snapshot + item payload from the event itself —
            // querying the game entity here would race the deferred commands
            // that ContainersPlugin queued this same frame for Graphic/Hue/...
            if (!uiMap.Value.TryGet(ev.ContainerSerial, out var entry)) continue;

            ushort displayed = ev.Graphic;

            if (displayed < tileData.Length)
            {
                ref readonly var td = ref tileData[displayed];
                var slot = (GameLayer)td.Layer;
                bool isCorpse = entry.OriginalGraphic == 0x0009;
                if (isCorpse && td.Layer > 0 && (int)slot < Constants.BAD_CONTAINER_LAYERS.Length
                    && !Constants.BAD_CONTAINER_LAYERS[(int)slot])
                    continue;
                if (td.IsWearable && (slot == GameLayer.Face || slot == GameLayer.Beard || slot == GameLayer.Hair))
                    continue;
            }

            // Coins draw a pile sprite (base+1 / base+2) by amount; everything
            // else draws its own graphic. `displayed` stays the base graphic for
            // the tiledata/layer logic above.
            ushort artGraphic = ItemGraphics.Displayed(ev.Graphic, ev.Amount);
            ushort drawGraphic = entry.IsBoard
                ? (ushort)(artGraphic - Constants.ITEM_GUMP_TEXTURE_OFFSET)
                : artGraphic;

            float scale = entry.Scale;
            int spriteW, spriteH;
            if (entry.IsBoard)
            {
                ref readonly var gi = ref assets.Value.Gumps.GetGump(drawGraphic);
                spriteW = gi.UV.Width;
                spriteH = gi.UV.Height;
            }
            else
            {
                ref readonly var ai = ref assets.Value.Arts.GetArt(drawGraphic);
                spriteW = ai.UV.Width;
                spriteH = ai.UV.Height;
            }

            if (uiScale.Value.ScaleItemsInsideContainers && !entry.IsBoard)
            {
                spriteW = (int)(spriteW * scale);
                spriteH = (int)(spriteH * scale);
            }

            // Clamp the server-sent slot position into the container's content
            // bounds so an item dropped/added at an out-of-bounds coord renders
            // (and stays pickable) inside the gump. Mirrors legacy
            // ContainerGump bounds math + ClampToContainer: left/top edge is
            // Bounds.X/Y, right/bottom edge is Bounds.Width/Height (both scaled).
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

            var origHue = ShaderHueTranslator.GetHueVector(ev.Hue, partial: false, alpha: 1f, gump: entry.IsBoard);
            var hoverHue = ShaderHueTranslator.GetHueVector(0x0035, partial: false, alpha: 1f, gump: entry.IsBoard);

            // Mirror legacy ItemGump.Draw: stackable items with Amount > 1 get
            // drawn twice (+5/+5 offset) to fake a pile. Coins are excluded
            // because legacy `IsCoin` skips them too — coin stack visuals come
            // from a separate piled-graphic asset.
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
                // Items share the window's z. Clay's float-root sort is
                // stable on equal z, so items declared after the window in
                // tree order draw on top of it. PropagateZ keeps them in
                // sync on focus bumps, but the FIRST z we hand out has to
                // be the window's CURRENT z, not entry.ZBase (which was
                // cached at container-open time). Otherwise late spawns —
                // e.g. the slot respawned after a drop, after the user has
                // clicked the window several times — get a stale low z and
                // render behind the window background until the next focus
                // bump propagates a new value to them.
                .Insert(new GlobalZIndex(ResolveCurrentZ(entry, windowZQ)));
            commands.Entity(entry.ContentEntity).AddChild(itemUi);
        }
    }

    private static int ResolveCurrentZ(ContainerUiMap.Entry entry, Query<Data<GlobalZIndex>> q)
    {
        if (!q.Contains(entry.UiEntity)) return entry.ZBase;
        var (_, z) = q.Get(entry.UiEntity);
        return z.Ref.Value;
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
            if (!containerQuery.Contains(hb.Ref.Container)) continue;

            var (_, tag, render, node) = containerQuery.Get(hb.Ref.Container);
            bool willMinimize = !tag.Ref.IsMinimized;

            ushort newGraphic = willMinimize && tag.Ref.IconizedGraphic != 0
                ? tag.Ref.IconizedGraphic
                : tag.Ref.Graphic;

            render.Ref.Render().AssetId = newGraphic;
            tag.Ref.IsMinimized = willMinimize;

            ref readonly var gi = ref assets.Value.Gumps.GetGump(newGraphic);
            node.Ref.Width = Val.Px(gi.UV.Width * tag.Ref.Scale);
            node.Ref.Height = Val.Px(gi.UV.Height * tag.Ref.Scale);

            if (childrenQ.Contains(hb.Ref.Container))
            {
                var (_, kids) = childrenQ.Get(hb.Ref.Container);
                foreach (var cid in kids.Ref)
                {
                    if (!childNodes.Contains(cid)) continue;
                    var (_, childNode, _) = childNodes.Get(cid);
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
    private static void TearDownClosedUi(
        Commands commands,
        Res<ContainerUiMap> uiMap,
        EventReader<ContainerClosedEvent> reader,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var ev in reader.Read())
        {
            if (!uiMap.Value.TryGet(ev.Serial, out var entry))
                continue;

            if (childrenQ.Contains(entry.UiEntity))
            {
                var (_, kids) = childrenQ.Get(entry.UiEntity);
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
