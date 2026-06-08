using System;
using System.Collections.Generic;
using ClassicUO.Ecs.Modding.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

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
        app.AddResource(new ModTextValues());

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

        // Editable mod text nodes (TextProxy.Editable) reuse the host's shared
        // stb editor (TextEditPlugin): Api.cs tags the glyph with TextInput +
        // EditableText + TextFieldGeom + ModEditable. The three pieces the editor
        // can't infer from a flat node we add here: focus-on-click, the caret +
        // selection overlay children, and OnTextChanged back to the guest.

        // Click an editable node -> give it keyboard focus + seed the caret click,
        // exactly like SpawnTextField's per-field UiPointerDown observer.
        app.AddObserver<On<UiPointerDown>, Query<Data<ModEditable>>, ResMut<FocusedInput>, ResMut<ActiveTextEdit>>(
            (t, editables, focused, edit) =>
            {
                if (!editables.Contains(t.EntityId))
                    return;
                focused.Value.Entity = t.EntityId;
                edit.Value.PendingClickEntity = t.EntityId;
                edit.Value.PendingClickX = t.Event.Position.X;
            });

        var setupEditFn = SetupModEditable;
        app.AddSystem(setupEditFn).InStage(Stage.PreUpdate).Build();

        // Detect user edits (TextEditPlugin.WriteBack ran in Update) and ship one
        // OnTextChanged per changed field. Before the hover boundary so the event
        // drains the same frame.
        var reportEditFn = ReportModTextChanges;
        app.AddSystem(reportEditFn).InStage(Stage.Last).Before("modHoverBoundary").Build();
    }

    // Spawn the caret bar + selection highlight for each editable node once and
    // parent them to the node, mirroring SpawnTextField. PositionOverlays
    // (TextEditPlugin) shows/places them while the node holds focus.
    private static void SetupModEditable(
        Query<Data<ModEditable, TextFont, TextColor>, Without<ModEditableReady>> pending,
        Commands commands)
    {
        foreach (var (e, _, font, color) in pending)
        {
            var glyphId = e.Ref;
            float h = font.Ref.Size;

            var selection = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(0), Top = Val.Px(0),
                    Width = Val.Px(0), Height = Val.Px(h),
                    Display = Display.None,
                })
                .Insert(new BackgroundColor(new ClayColor(70, 110, 180, 120)))
                .Insert(new TextEditSelection { Target = glyphId });

            var caret = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(0), Top = Val.Px(0),
                    Width = Val.Px(2), Height = Val.Px(h),
                    Display = Display.None,
                })
                .Insert(new BackgroundColor(color.Ref.Value))
                .Insert(new TextEditCaret { Target = glyphId });

            commands.Entity(glyphId)
                .AddChild(selection.Id)
                .AddChild(caret.Id)
                .Insert<ModEditableReady>();
        }
    }

    // Fire OnTextChanged whenever a field's live value diverges from the last
    // value host + guest agreed on (the same ModTextValues entry Api.cs writes
    // when the guest sets the value). User edits land in Text/MaskedText via
    // WriteBack; this turns them into a guest event.
    private static void ReportModTextChanges(
        Query<Data<ModEditable, PluginEntity>> editables,
        Query<Data<Text>> textQ,
        Query<Data<MaskedText>> maskedQ,
        Res<ModListeners> listeners,
        ResMut<PendingUiEvents> pending,
        ResMut<ModTextValues> agreed)
    {
        foreach (var (e, ed, _) in editables)
        {
            var id = e.Ref;
            string value;
            if (ed.Ref.Masked && maskedQ.Contains(id))
            {
                var (_, mt) = maskedQ.Get(id);
                value = mt.Ref.Value ?? string.Empty;
            }
            else if (textQ.Contains(id))
            {
                var (_, txt) = textQ.Get(id);
                value = txt.Ref.Value ?? string.Empty;
            }
            else
            {
                continue;
            }

            if (agreed.Value.TryGetValue(id, out var prev) && string.Equals(prev, value, StringComparison.Ordinal))
                continue;
            agreed.Value[id] = value;

            foreach (var l in listeners.Value.Match(id, InputEventType.OnTextChanged))
                pending.Value.Items.Add((l.Mod, new UIEvent(InputEventType.OnTextChanged, id, l.EventId, Value: value)));
        }
    }

    private const int ModRootZ = 100;

    private static void PromoteModRoots(
        Query<Data<Node, PluginEntity>, Without<Parent>> roots,
        Query<Data<UIMovable>> movableQ,
        Res<UiSurface> surface,
        Commands commands,
        Local<HashSet<ulong>> zApplied)
    {
        var size = surface.Value.LogicalSize;
        foreach (var (e, node, _) in roots)
        {
            // A movable mod root (a gump window) owns its own position and is
            // dragged by WindowDragPlugin — pinning it to the viewport origin
            // each frame would fight the drag and make its spawn position
            // non-deterministic. Only give it Absolute + a z so it floats over
            // the host UI; leave Left/Top (and size) as the mod set them.
            var movable = movableQ.Contains(e.Ref);

            if (!movable)
            {
                if (node.Ref.Width.Type == ValType.Percent)
                    node.Ref.Width = Val.Px(size.X);
                if (node.Ref.Height.Type == ValType.Percent)
                    node.Ref.Height = Val.Px(size.Y);
            }

            // Absolute is required for GlobalZIndex to apply (Clay z-sorts only
            // floating elements); otherwise the mod renders in tree order and
            // ends up under the host UI.
            node.Ref.PositionType = PositionType.Absolute;
            if (!movable)
            {
                // Non-movable full-screen canvas roots anchor at the origin.
                node.Ref.Left = Val.Px(0);
                node.Ref.Top = Val.Px(0);
            }

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

// On a mod node whose text was flagged editable (TextProxy.Editable). Drives the
// mod-only editor extras (focus-on-click, overlay spawn, OnTextChanged). The
// generic editing (caret index, selection, clipboard) comes from TextEditPlugin
// off the TextInput + EditableText markers Api.cs also adds.
internal struct ModEditable { public bool Masked; }

// Guard so the caret + selection overlays are spawned once per editable node.
internal struct ModEditableReady;

// The last text value host and guest agree on, per editable node. Api.cs writes
// it when the guest sets a (changed) value; ReportModTextChanges writes it on a
// user edit and only fires OnTextChanged when the live value diverges from it.
internal sealed class ModTextValues : Dictionary<ulong, string> { }
