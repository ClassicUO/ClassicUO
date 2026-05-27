// Generic drag system for floating UI windows.
//
// Any entity tagged with `UIMovable` + carrying `Node` + `Interaction` becomes
// draggable. Modeled after `GameScreenPlugin.DragWindow`:
//   * Single `Drag` system runs every frame the left mouse button is held.
//   * On first held frame: scan UIMovable+Interaction queries, latch the
//     entity whose Interaction is Pressed, snapshot mouse anchor + Node
//     origin (Local<DragAnchor>).
//   * Subsequent held frames: write `originX + delta`, `originY + delta`
//     into Node.Left / Node.Top.
//   * Release clears the anchor.
//
// Why one system, not Begin/Motion split: Bevy.UI updates `Interaction` in
// UiPostLayoutStage, which runs *after* Stage.Update. A `RunIf IsPressedOnce`
// gate on a separate BeginDrag would fire the same frame as the press edge,
// when Interaction is still None from the previous frame. The continuous-held
// pattern dodges that race without needing observers.

using ClassicUO.Ecs.Modding.Host;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct WindowDragPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new UiZCounter());
        var dragFn = Drag;
        var closeOnRightClickFn = CloseOnRightClick;
        var claimSelectionFn = ClaimSelectedFromMovable;
        app.AddSystem(dragFn).InStage(Stage.Update).Build();
        // No z-propagation: only the window root carries a GlobalZIndex.
        // LayoutSystem threads that z down to every descendant float at layout
        // time (an element with no z of its own inherits its ancestor's), so
        // the whole window rides one layer without per-child bookkeeping.
        // Right-click on any UIMovable closes it. Runs in PreUpdate so it
        // can consume the click before PlayerMovementPlugin (Stage.Update)
        // sees it and starts walking the player.
        app.AddSystem(closeOnRightClickFn)
            .InStage(Stage.PreUpdate)
            .RunIf((Res<MouseContext> m) => m.Value.IsPressedOnce(MouseButtonType.Right))
            .Build();
        // Click-capture: any UIMovable under the cursor claims SelectedEntity
        // at float.MaxValue so world / pickup / use systems see the window
        // entity (which carries no NetworkSerial / Items / ContainerItemUI)
        // and bail. Container windows have their own item-aware claim in
        // ContainerGumpPlugin.UpdateSelectedFromContainerUI; filter them out
        // here so the two systems don't race on the same entity.
        app.AddSystem(claimSelectionFn).InStage(Stage.Last).Build();
    }

    private static void ClaimSelectedFromMovable(
        Res<MouseContext> mouse,
        Res<SelectedEntity> selected,
        Res<AssetsServer> assets,
        Query<Data<ComputedNode, GlobalZIndex, UOCustomRender>, Filter<With<UIMovable>, Without<ContainerWindow>>> q)
    {
        var pos = mouse.Value.Position;
        ulong topEnt = 0;
        int topZ = int.MinValue;
        int topOrder = int.MinValue;

        foreach (var (ent, computed, z, custom) in q)
        {
            var bb = computed.Ref;
            // Pixel-perfect: a click on a transparent pixel of the window's bg
            // gump passes through (doesn't claim selection).
            if (!UiHitTest.PixelHit(assets.Value, custom.Ref, bb, pos)) continue;
            if (z.Ref.Value > topZ || (z.Ref.Value == topZ && bb.PaintOrder >= topOrder))
            {
                topZ = z.Ref.Value;
                topOrder = bb.PaintOrder;
                topEnt = ent.Ref;
            }
        }

        if (topEnt == 0) return;
        selected.Value.Set(topEnt, float.MaxValue);
    }

    // Find the topmost UIMovable under the cursor and close it.
    //   * Container windows route through ContainerClosedEvent +
    //     HostMessage.ContainerClosed so the server / mods learn about it;
    //     ContainerGumpPlugin.TearDownClosedUi does the actual despawn.
    //   * Any other UIMovable is despawned in-place along with its subtree.
    // In all cases the right-click is consumed so it doesn't leak into the
    // movement / world-interaction systems that also poll mouse state.
    private static void CloseOnRightClick(
        Commands commands,
        Res<MouseContext> mouse,
        Res<AssetsServer> assets,
        Query<Data<ComputedNode, UOCustomRender>, Filter<With<UIMovable>>> movableQuery,
        Query<Data<ContainerWindow>> containerQuery,
        Query<Data<TinyEcs.Children>> childrenQ,
        EventWriter<ContainerClosedEvent> closedWriter,
        EventWriter<HostMessage> hostMsgs)
    {
        var pos = mouse.Value.Position;
        ulong topEnt = 0;
        int topOrder = int.MinValue;

        foreach (var (ent, computed, custom) in movableQuery)
        {
            var bb = computed.Ref;
            // Pixel-perfect: right-click on a transparent bg pixel passes
            // through instead of closing the window.
            if (!UiHitTest.PixelHit(assets.Value, custom.Ref, bb, pos)) continue;

            if (bb.PaintOrder >= topOrder)
            {
                topOrder = bb.PaintOrder;
                topEnt = ent.Ref;
            }
        }

        if (topEnt == 0) return;

        mouse.Value.Consume(MouseButtonType.Right);

        if (containerQuery.Contains(topEnt))
        {
            var (_, window) = containerQuery.Get(topEnt);
            closedWriter.Send(new ContainerClosedEvent(window.Ref.Serial));
            hostMsgs.Send(new HostMessage.ContainerClosed(window.Ref.Serial));
            return;
        }

        DespawnSubtree(commands, topEnt, childrenQ);
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

    private struct DragAnchor
    {
        public bool Active;
        public ulong Owner;
        public Vector2 Mouse;
        public float OriginX;
        public float OriginY;
    }

    private static void Drag(
        Res<MouseContext> mouse,
        Res<UiZCounter> zCounter,
        Res<GrabbedItem> grabbed,
        Res<DragGate> gate,
        Res<AssetsServer> assets,
        Local<DragAnchor> anchor,
        Query<Data<Node, Interaction, ComputedNode, GlobalZIndex, UOCustomRender>, Filter<With<UIMovable>>> q,
        Query<Data<ContainerItemUI, ComputedNode, Node>> itemsQ)
    {
        // IsPressed is false on the press-once frame (oldState=Released), so
        // include IsPressedOnce in the "held" check or the latch attempt
        // below would be skipped on the very frame it needs to fire.
        bool held = mouse.Value.IsPressed(MouseButtonType.Left)
                 || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            anchor.Value.Active = false;
            anchor.Value.Owner = 0;
            if (gate.Value.Mode == ActiveDrag.UIWindow)
                gate.Value.Mode = ActiveDrag.None;
            return;
        }

        // Skip latching a window while an item is held by the cursor.
        // Otherwise dragging the held item over a container window flips that
        // window's Interaction to Pressed and the continuous-held pattern
        // grabs it for window drag — the container ends up following the
        // cursor instead of receiving the drop.
        if (grabbed.Value.Serial != 0)
        {
            anchor.Value.Active = false;
            anchor.Value.Owner = 0;
            return;
        }

        // Latch on press-once via direct bbox hit-test (ComputedNode) so the
        // gesture begins on the entity actually under the cursor at click
        // time. Pick topmost by (GlobalZIndex desc, ClayId desc) to mirror
        // Clay's float stacking. Avoids both the continuous-held wander bug
        // and the press-once Interaction-staleness problem.
        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return; // another drag owns the gesture
            var pos = mouse.Value.Position;

            // If the cursor is over a container item slot, the pickup system
            // owns the gesture — don't latch the parent window or the item
            // sprite ends up moving the whole container instead of being
            // picked up.
            foreach (var (_, _, computed, itemNode) in itemsQ)
            {
                if (itemNode.Ref.Display == Display.None) continue;
                var ib = computed.Ref;
                if (pos.X < ib.Position.X || pos.Y < ib.Position.Y) continue;
                if (pos.X >= ib.Position.X + ib.Size.X) continue;
                if (pos.Y >= ib.Position.Y + ib.Size.Y) continue;
                return;
            }

            ulong topId = 0;
            int topZ = int.MinValue;
            int topOrder = int.MinValue;
            foreach (var (ent, _, _, computed, z, custom) in q)
            {
                var bb = computed.Ref;
                // Pixel-perfect: clicking a transparent bg pixel (e.g. the
                // paperdoll arch corners) misses the window so the drag never
                // latches there.
                if (!UiHitTest.PixelHit(assets.Value, custom.Ref, bb, pos)) continue;
                if (z.Ref.Value > topZ || (z.Ref.Value == topZ && bb.PaintOrder >= topOrder))
                {
                    topZ = z.Ref.Value;
                    topOrder = bb.PaintOrder;
                    topId = ent.Ref;
                }
            }
            if (topId == 0) return;

            var (_, node, _, _, _, _) = q.Get(topId);
            float ox = node.Ref.Left.Type == ValType.Px ? node.Ref.Left.Value : 0f;
            float oy = node.Ref.Top.Type == ValType.Px ? node.Ref.Top.Value : 0f;

            anchor.Value = new DragAnchor
            {
                Active = true,
                Owner = topId,
                Mouse = pos,
                OriginX = ox,
                OriginY = oy,
            };
            gate.Value.Mode = ActiveDrag.UIWindow;

            // Bring window to front on focus. Only the root carries a z;
            // LayoutSystem threads it down to every descendant float at layout
            // time, so a single in-place bump lifts the whole window — no
            // subtree walk, no one-frame child-lag flicker. Bevy.UI maps
            // GlobalZIndex to Clay's Floating.ZIndex (signed 16-bit); a
            // monotonic counter suffices until ~32k focus clicks per session.
            var newZ = zCounter.Value.Bump();
            var (_, _, _, _, rootZ, _) = q.Get(topId);
            rootZ.Ref.Value = newZ;
        }

        if (!anchor.Value.Active) return;

        if (!q.Contains(anchor.Value.Owner))
        {
            anchor.Value.Active = false;
            anchor.Value.Owner = 0;
            return;
        }

        var delta = mouse.Value.Position - anchor.Value.Mouse;
        var (_, ownerNode, _, _, _, _) = q.Get(anchor.Value.Owner);
        ownerNode.Ref.PositionType = PositionType.Absolute;
        ownerNode.Ref.Left = Val.Px(anchor.Value.OriginX + delta.X);
        ownerNode.Ref.Top = Val.Px(anchor.Value.OriginY + delta.Y);
    }
}

// Monotonic z-order counter for floating UI windows. Bump on focus/spawn so
// the most recently interacted window draws and hit-tests on top. Clamped to
// short.MaxValue inside the layout system; a session-long counter overflow is
// unrealistic in practice.
internal sealed class UiZCounter
{
    private int _next = 1;
    public int Bump() => _next++;
}
