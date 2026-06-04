using System.Collections.Generic;
using ClassicUO.Ecs.Modding.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs.Modding.UI;

using ParentQuery = TinyEcs.Bevy.Query<TinyEcs.Bevy.Data<TinyEcs.Parent>>;

// Routes Bevy.UI pointer events on mod-owned nodes back to the WASM mod.
//
// Flow: the mod registers listeners via cuo_ui_add_event_listener (Api.cs) ->
// ModListeners. Global observers on Bevy.UI's On<UiClick>/On<UiOver>/... fire
// when a node is interacted with; they enqueue a UIEvent per matching listener.
// A SingleThreaded drain system in Stage.Last calls the mod's `on_ui_event`
// export — deferred out of the trigger dispatch because the call can re-render
// (mutating World via cuo_ui_node), which must not race other systems.
internal readonly struct UIEventsPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new ModListeners());
        app.AddResource(new PendingUiEvents());

        // UiClick = press+release on the same element => onClick (OnMouseReleased).
        app.AddObserver<On<UiClick>, Res<ModListeners>, ResMut<PendingUiEvents>, ParentQuery>(
            (t, l, p, par) => Enqueue(l.Value, p.Value, par, t.EntityId, InputEventType.OnMouseReleased, t.Event.Position.X, t.Event.Position.Y));

        app.AddObserver<On<UiPointerDown>, Res<ModListeners>, ResMut<PendingUiEvents>, ParentQuery>(
            (t, l, p, par) => Enqueue(l.Value, p.Value, par, t.EntityId, InputEventType.OnMousePressed, t.Event.Position.X, t.Event.Position.Y));

        app.AddObserver<On<UiDoubleClick>, Res<ModListeners>, ResMut<PendingUiEvents>, ParentQuery>(
            (t, l, p, par) => Enqueue(l.Value, p.Value, par, t.EntityId, InputEventType.OnMouseDoubleClick, t.Event.Position.X, t.Event.Position.Y));

        // NOTE: OnMouseEnter/OnMouseLeave are NOT driven off UiOver/UiOut here.
        // Clay fires UiOver/UiOut on every change of the topmost hovered entity,
        // so moving the cursor from a wrapper onto its own child (icon) flips the
        // target and — once bubbled to the wrapper's listener — produces a
        // spurious leave+enter while the pointer never left the wrapper subtree.
        // That churn cancelled tooltips' pending show timers (only delay:0 ones
        // survived). HoverBoundary (below) instead derives DOM-style enter/leave
        // at the listener's SUBTREE boundary, matching react-dom mouseenter/leave.

        app.AddObserver<On<UiMove>, Res<ModListeners>, ResMut<PendingUiEvents>, ParentQuery>(
            (t, l, p, par) =>
            {
                Enqueue(l.Value, p.Value, par, t.EntityId, InputEventType.OnMouseMove, t.Event.Position.X, t.Event.Position.Y);
                Enqueue(l.Value, p.Value, par, t.EntityId, InputEventType.OnMouseOver, t.Event.Position.X, t.Event.Position.Y);
            });

        app.AddObserver<On<UiScroll>, Res<ModListeners>, ResMut<PendingUiEvents>, ParentQuery>(
            (t, l, p, par) => Enqueue(l.Value, p.Value, par, t.EntityId, InputEventType.OnMouseWheel, t.Event.Position.X, t.Event.Position.Y, t.Event.Delta.Y));

        // DOM-style hover enter/leave at listener subtree boundaries. Runs before
        // the drain so its events ship the same frame. Reads the frame's hovered
        // entity (set by Bevy.UI InteractionSystem.PostLayout in an earlier stage).
        var hoverFn = HoverBoundary;
        app.AddSystem(hoverFn).InStage(Stage.Last).Label("modHoverBoundary").Build();

        var drainFn = DrainUiEvents;
        app.AddSystem(drainFn)
            .InStage(Stage.Last)
            .SingleThreaded()
            .After("modHoverBoundary")
            .RunIf((Res<PendingUiEvents> pending) => pending.Value.Items.Count > 0)
            .Build();

        // A mod's top-level node is a parentless layout root. A Percent ("grow")
        // size can't resolve without a parent and collapses to 0 — taking the
        // whole flow subtree with it (see reference_ecs_root_node_layout). Pin
        // such roots to the viewport and give them a z so they draw over the
        // world. Runs after the mod's per-frame setNode (Update) and before the
        // layout pass (UiPreLayoutStage).
        var promoteFn = PromoteModRoots;
        app.AddSystem(promoteFn).InStage(UiPlugin.UiPreLayoutStage).Build();
    }

    private const int ModRootZ = 100;

    private static void PromoteModRoots(
        Query<Data<Node, PluginEntity>, Without<Parent>> roots,
        Res<UiSurface> surface,
        Commands commands,
        Local<HashSet<ulong>> zApplied)
    {
        var size = surface.Value.LogicalSize;
        foreach (var (e, node, _) in roots)
        {
            if (node.Ref.Width.Type == ValType.Percent)
                node.Ref.Width = Val.Px(size.X);
            if (node.Ref.Height.Type == ValType.Percent)
                node.Ref.Height = Val.Px(size.Y);

            // Absolute is required for GlobalZIndex to apply (Clay z-sorts only
            // floating elements); otherwise the mod renders in tree order and
            // ends up under the host UI. Anchor at the viewport origin.
            node.Ref.PositionType = PositionType.Absolute;
            node.Ref.Left = Val.Px(0);
            node.Ref.Top = Val.Px(0);

            if (zApplied.Value.Add(e.Ref))
                commands.Entity(e.Ref).Insert(new GlobalZIndex(ModRootZ));
        }
    }

    // Bubble the event up the parent chain (DOM-style): the hit element is the
    // topmost interactive node, but the listener (e.g. onMouseEnter on a wrapper)
    // may sit on an ancestor. EmitTrigger marks propagate:true, but global
    // observers fire once at the target — so we walk the chain ourselves.
    private static void Enqueue(ModListeners listeners, PendingUiEvents pending, ParentQuery parents, ulong nodeId, InputEventType type, float x, float y, float? wheel = null)
    {
        var id = nodeId;
        for (var guard = 0; id != 0 && guard < 64; guard++)
        {
            foreach (var l in listeners.Match(id, type))
                pending.Items.Add((l.Mod, new UIEvent(type, id, l.EventId, x, y, wheel)));

            if (!parents.Contains(id))
                break;
            (_, var parent) = parents.Get(id);
            id = parent.Ref.Id;
        }
    }

    // DOM-style mouseenter/mouseleave for mod listeners. A listener node L is
    // "entered" when the hovered entity crosses from outside L's subtree to
    // inside it, and "left" on the reverse — so moving between L and its own
    // descendants (the wrapper↔icon flip) fires nothing for L. Mirrors the
    // non-bubbling boundary semantics of DOM mouseenter/mouseleave; verified
    // against the react-dom reference (src/Mods/user-interface/reference).
    private static void HoverBoundary(
        Res<UiClayContext> ctx,
        Res<ModListeners> listeners,
        ResMut<PendingUiEvents> pending,
        ParentQuery parents,
        Local<ulong> prevHovered)
    {
        var cur = ctx.Value.HoveredEntity;
        var prev = prevHovered.Value;
        if (cur == prev)
            return;
        prevHovered.Value = cur;

        var curAnc = AncestorsOrSelf(cur, parents);
        var prevAnc = AncestorsOrSelf(prev, parents);

        foreach (var node in curAnc)
            if (!prevAnc.Contains(node))
                foreach (var l in listeners.Value.Match(node, InputEventType.OnMouseEnter))
                    pending.Value.Items.Add((l.Mod, new UIEvent(InputEventType.OnMouseEnter, node, l.EventId, 0, 0, null)));

        foreach (var node in prevAnc)
            if (!curAnc.Contains(node))
                foreach (var l in listeners.Value.Match(node, InputEventType.OnMouseLeave))
                    pending.Value.Items.Add((l.Mod, new UIEvent(InputEventType.OnMouseLeave, node, l.EventId, 0, 0, null)));
    }

    private static HashSet<ulong> AncestorsOrSelf(ulong id, ParentQuery parents)
    {
        var set = new HashSet<ulong>();
        for (var guard = 0; id != 0 && guard < 64; guard++)
        {
            if (!set.Add(id))
                break;
            if (!parents.Contains(id))
                break;
            (_, var parent) = parents.Get(id);
            id = parent.Ref.Id;
        }
        return set;
    }

    private static void DrainUiEvents(ResMut<PendingUiEvents> pending)
    {
        // A mod's on_ui_event may register/remove listeners, but cannot append
        // to this frame's queue (drained then cleared here).
        var items = pending.Value.Items;
        for (var i = 0; i < items.Count; i++)
            items[i].Mod.Plugin.Call("on_ui_event", items[i].Ev.ToJson());
        items.Clear();
    }
}

internal sealed class ModListeners
{
    public readonly record struct Listener(ulong EventId, InputEventType Type, Mod Mod);

    private readonly Dictionary<ulong, List<Listener>> _byNode = new();
    private readonly Dictionary<ulong, ulong> _eventToNode = new();
    private ulong _next = 1;

    public ulong Add(ulong nodeId, InputEventType type, Mod mod)
    {
        var id = _next++;
        if (!_byNode.TryGetValue(nodeId, out var list))
            _byNode[nodeId] = list = new List<Listener>();
        list.Add(new Listener(id, type, mod));
        _eventToNode[id] = nodeId;
        return id;
    }

    public void Remove(ulong eventId)
    {
        if (!_eventToNode.Remove(eventId, out var nodeId))
            return;
        if (_byNode.TryGetValue(nodeId, out var list))
        {
            list.RemoveAll(l => l.EventId == eventId);
            if (list.Count == 0)
                _byNode.Remove(nodeId);
        }
    }

    // Drop every listener for a node — called when its entity is despawned so
    // listeners don't accumulate across show/hide churn (e.g. tooltips).
    public void RemoveNode(ulong nodeId)
    {
        if (!_byNode.Remove(nodeId, out var list))
            return;
        foreach (var l in list)
            _eventToNode.Remove(l.EventId);
    }

    public IEnumerable<Listener> Match(ulong nodeId, InputEventType type)
    {
        if (!_byNode.TryGetValue(nodeId, out var list))
            yield break;
        foreach (var l in list)
            if (l.Type == type)
                yield return l;
    }
}

internal sealed class PendingUiEvents
{
    public readonly List<(Mod Mod, UIEvent Ev)> Items = new();
}
