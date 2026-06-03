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
        app.AddResource(new ForcedWindowDrag());
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
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>> movables,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<ContainerWindow>> containers)
    {
        // Resolve via the SHARED pixel-perfect hit-test, same as drag/pickup, so
        // the claim follows the real topmost element and walks to its window root.
        // The old per-root bbox loop missed windows whose root carries no sprite
        // of its own (server gumps: the hit is a child resizepic/text that walks
        // up to the bare UIMovable root) — those let world clicks trespass through.
        var pos = mouse.Value.Position;
        var hit = UiPick.Topmost(pos, assets.Value, rendered, parents);
        if (!hit.Found) return;

        var owner = UiPick.MovableRoot(hit.Entity, movables, parents);
        if (owner == 0) return;
        // Container windows have their own item-aware claim
        // (ContainerGumpPlugin.UpdateSelectedFromContainerUI) — don't race it.
        if (containers.Contains(owner)) return;

        // bypassViewport: a movable window parked in the side gutter / top bar
        // sits outside Camera.Bounds, so the world-pick gate is off there. The
        // window claim must still land or drop/pickup over it silently fail.
        selected.Value.Set(owner, float.MaxValue, bypassViewport: true);
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
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> allRenderedQ,
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
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> allRenderedQ,
        Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>> movablesQ,
        Query<Data<TinyEcs.Parent>> parentsQ)
        => UiPick.MovableRoot(UiPick.Topmost(pos, assets, allRenderedQ, parentsQ).Entity, movablesQ, parentsQ);

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
        Res<ForcedWindowDrag> forced,
        Local<DragAnchor> anchor,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>> movables,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<ContainerItemUI>> itemsQ,
        Query<Data<PaperdollEquipUI>> equipQ,
        Query<Data<UINoDrag>> noDragQ,
        Query<Data<UINoWindowDrag>> noWindowDragChildQ)
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
            forced.Value.Owner = 0;
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
            forced.Value.Owner = 0;
            return;
        }

        // Forced drag: a window spawned mid-gesture (a healthbar dragged off a
        // mobile) asks to ride the cursor without a fresh press. Latch it the
        // frame its entity materialises (spawn is deferred), re-centered under
        // the cursor so it tracks like legacy AttemptDragControl.
        if (forced.Value.Owner != 0)
        {
            if (movables.Contains(forced.Value.Owner))
            {
                var ownerF = forced.Value.Owner;
                var (_, nodeF, zF) = movables.Get(ownerF);
                var pos = mouse.Value.Position;
                float wF = nodeF.Ref.Width.Type == ValType.Px ? nodeF.Ref.Width.Value : 0f;
                float hF = nodeF.Ref.Height.Type == ValType.Px ? nodeF.Ref.Height.Value : 0f;
                anchor.Value = new DragAnchor
                {
                    Active = true,
                    Owner = ownerF,
                    Mouse = pos,
                    OriginX = pos.X - wF / 2f,
                    OriginY = pos.Y - hF / 2f,
                };
                gate.Value.Mode = ActiveDrag.UIWindow;
                zF.Ref.Value = zCounter.Value.Bump();
                forced.Value.Owner = 0;
            }
            // else: entity not applied yet — keep the request for next frame.
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return; // another drag owns the gesture
            var pos = mouse.Value.Position;

            // Topmost element under the cursor. Checking ALL rendered elements
            // (not just movable roots) is what lets the drag start on a window's
            // opaque child where its own bg is transparent — the paperdoll body
            // and arch interior, a container's slot art, etc.
            var hit = UiPick.Topmost(pos, assets.Value, rendered, parents);
            if (!hit.Found) return;

            // Pickup owns the gesture when the topmost hit is a liftable thing
            // (container item / equipped item) — clicking those lifts them, it
            // doesn't drag the window they sit in.
            if (itemsQ.Contains(hit.Entity) || equipQ.Contains(hit.Entity)) return;

            // Interactive in-window controls (skills group arrows, lock/use
            // buttons, reset, resize handle, checkboxes) opt out of window-drag:
            // a press on them must reach their own UiClick / drag handler, not
            // latch a window move that cancels the click.
            if (noWindowDragChildQ.Contains(hit.Entity)) return;

            var owner = UiPick.MovableRoot(hit.Entity, movables, parents);
            if (owner == 0) return;

            // nomove windows: still a window (close / click-capture work), but
            // the drag gesture is suppressed.
            if (noDragQ.Contains(owner)) return;

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

// Hand-off for a window that must start dragging without its own press edge —
// set Owner to a just-spawned window root and WindowDragPlugin.Drag latches it
// onto the held cursor on the next frame (the spawn is a deferred command).
// Used when a healthbar is dragged off a mobile so the bar follows the mouse.
internal sealed class ForcedWindowDrag
{
    public ulong Owner;
}

// Marker for an interactive child of a UIMovable window (button, toggle, resize
// handle, checkbox) whose press must NOT latch a window drag — WindowDragPlugin
// yields the gesture so the control's own UiClick / drag handler runs.
internal struct UINoWindowDrag;
