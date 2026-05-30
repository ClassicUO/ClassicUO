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

using System;
using ClassicUO.Ecs.Modding.Host;
using ClassicUO.Input;
using ClassicUO.Network;
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
        // Runs every frame (PreUpdate, before PlayerMovementPlugin in Update):
        // it must keep consuming the held right button so the player doesn't
        // walk while the press is parked over a window, and fire the close on
        // RELEASE (UiClick semantics — press + release over the same window).
        app.AddSystem(closeOnRightClickFn)
            .InStage(Stage.PreUpdate)
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
        Query<Data<ComputedNode, GlobalZIndex, UiCustom>, Filter<With<UIMovable>, Without<ContainerWindow>>> q)
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
            if (!UiHitTest.PixelHit(assets.Value, custom.Ref.Render(), bb, pos)) continue;
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

    // Right-click-close with UiClick semantics: the close fires on the right
    // button RELEASE over the window the press started on (drag-off cancels),
    // not on press-down. While the right button is held over a window the
    // press is consumed each frame so the player doesn't walk underneath it.
    //   * Container windows route through ContainerClosedEvent +
    //     HostMessage.ContainerClosed so the server / mods learn about it;
    //     ContainerGumpPlugin.TearDownClosedUi does the actual despawn.
    //   * Server-pushed gumps reply GumpResponse button 0 (OOP
    //     Gump.CloseWithRightClick → OnButtonClick(0)) and drop the registry
    //     entry, then despawn.
    //   * Any other UIMovable is despawned in-place along with its subtree.
    private static void CloseOnRightClick(
        Commands commands,
        Res<MouseContext> mouse,
        Res<AssetsServer> assets,
        Local<ulong> pressTarget,
        Query<Data<ComputedNode, Node, UiCustom>, Filter<Optional<UiCustom>>> allRenderedQ,
        Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>> movablesQ,
        Query<Data<TinyEcs.Parent>> parentsQ,
        Query<Data<ContainerWindow>> containerQuery,
        Query<Data<ServerGump>> serverGumpQuery,
        Query<Data<TinyEcs.Children>> childrenQ,
        Res<NetClient> net,
        ResMut<ServerGumpRegistry> serverGumpRegistry,
        EventWriter<ContainerClosedEvent> closedWriter,
        EventWriter<HostMessage> hostMsgs)
    {
        bool once = mouse.Value.IsPressedOnce(MouseButtonType.Right);
        bool held = mouse.Value.IsPressed(MouseButtonType.Right);
        bool released = mouse.Value.IsReleased(MouseButtonType.Right);

        if (!once && !held && !released && pressTarget.Value == 0)
            return;

        // Press: latch the window under the cursor (don't close yet) and
        // consume so the world / movement systems don't see the right press.
        if (once)
        {
            pressTarget.Value = TopmostMovable(mouse.Value.Position, assets.Value, allRenderedQ, movablesQ, parentsQ);
            if (pressTarget.Value != 0)
                mouse.Value.Consume(MouseButtonType.Right);
            return;
        }

        // Held: keep the press consumed while parked over the latched window.
        if (held)
        {
            if (pressTarget.Value != 0)
                mouse.Value.Consume(MouseButtonType.Right);
            return;
        }

        // Release: close only if the cursor is still over the latched window.
        if (released)
        {
            var target = pressTarget.Value;
            pressTarget.Value = 0;
            if (target == 0)
                return;

            mouse.Value.Consume(MouseButtonType.Right);

            if (TopmostMovable(mouse.Value.Position, assets.Value, allRenderedQ, movablesQ, parentsQ) != target)
                return; // dragged off — cancel, like UiClick

            if (containerQuery.Contains(target))
            {
                var (_, window) = containerQuery.Get(target);
                closedWriter.Send(new ContainerClosedEvent(window.Ref.Serial));
                hostMsgs.Send(new HostMessage.ContainerClosed(window.Ref.Serial));
                return;
            }

            if (serverGumpQuery.Contains(target))
            {
                var (_, sg) = serverGumpQuery.Get(target);
                net.Value.Send_GumpResponse(sg.Ref.Sender, sg.Ref.GumpId, 0,
                    Array.Empty<uint>(), Array.Empty<Tuple<ushort, string>>());
                if (serverGumpRegistry.Value.ByGumpId.TryGetValue(sg.Ref.GumpId, out var r) && r == target)
                    serverGumpRegistry.Value.ByGumpId.Remove(sg.Ref.GumpId);
            }

            DespawnSubtree(commands, target, childrenQ);
        }
    }

    // The movable window root under the cursor (0 if none): topmost rendered
    // element resolved to its owning UIMovable root. See UiPick. Without the
    // walk-to-root a right-click on window A's content fell through to window B
    // behind it (A's root bg is transparent over its interior) and closed B.
    private static ulong TopmostMovable(
        Vector2 pos,
        AssetsServer assets,
        Query<Data<ComputedNode, Node, UiCustom>, Filter<Optional<UiCustom>>> allRenderedQ,
        Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>> movablesQ,
        Query<Data<TinyEcs.Parent>> parentsQ)
        => UiPick.MovableRoot(UiPick.Topmost(pos, assets, allRenderedQ).Entity, movablesQ, parentsQ);

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
        Query<Data<ComputedNode, Node, UiCustom>, Filter<Optional<UiCustom>>> rendered,
        Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>> movables,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<ContainerItemUI>> itemsQ,
        Query<Data<PaperdollEquipUI>> equipQ)
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
        // Otherwise dragging the held item over a container window grabs it for
        // window drag and the container follows the cursor instead of receiving
        // the drop.
        if (grabbed.Value.Serial != 0)
        {
            anchor.Value.Active = false;
            anchor.Value.Owner = 0;
            return;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return; // another drag owns the gesture
            var pos = mouse.Value.Position;

            // Topmost element under the cursor. Checking ALL rendered elements
            // (not just movable roots) is what lets the drag start on a window's
            // opaque child where its own bg is transparent — the paperdoll body
            // and arch interior, a container's slot art, etc.
            var hit = UiPick.Topmost(pos, assets.Value, rendered);
            if (!hit.Found) return;

            // Pickup owns the gesture when the topmost hit is a liftable thing
            // (container item / equipped item) — clicking those lifts them, it
            // doesn't drag the window they sit in.
            if (itemsQ.Contains(hit.Entity) || equipQ.Contains(hit.Entity)) return;

            var owner = UiPick.MovableRoot(hit.Entity, movables, parents);
            if (owner == 0) return;

            var (_, node, _) = movables.Get(owner);
            float ox = node.Ref.Left.Type == ValType.Px ? node.Ref.Left.Value : 0f;
            float oy = node.Ref.Top.Type == ValType.Px ? node.Ref.Top.Value : 0f;

            anchor.Value = new DragAnchor
            {
                Active = true,
                Owner = owner,
                Mouse = pos,
                OriginX = ox,
                OriginY = oy,
            };
            gate.Value.Mode = ActiveDrag.UIWindow;

            // Bring window to front on focus. Only the root carries a z;
            // LayoutSystem threads it down to every descendant float at layout
            // time, so a single in-place bump lifts the whole window.
            var (_, _, rootZ) = movables.Get(owner);
            rootZ.Ref.Value = zCounter.Value.Bump();
        }

        if (!anchor.Value.Active) return;

        if (!movables.Contains(anchor.Value.Owner))
        {
            anchor.Value.Active = false;
            anchor.Value.Owner = 0;
            return;
        }

        var delta = mouse.Value.Position - anchor.Value.Mouse;
        var (_, ownerNode, _) = movables.Get(anchor.Value.Owner);
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
