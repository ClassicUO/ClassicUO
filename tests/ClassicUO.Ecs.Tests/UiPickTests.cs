using System.Numerics;
using ClassicUO.Ecs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using Xunit;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs.Tests;

// Behavioural tests for the one hit-test every gump gesture shares (UiPick).
// These encode the bug class we kept re-hitting: overlapping windows picking
// the wrong (lower) one, and a click on a window's child failing to resolve to
// its window root. UiCustom.Data == null makes PixelHit treat the element as a
// solid bounding box, so no UO art assets are needed — the z/PaintOrder/overlap
// /walk-to-root logic (where every bug lived) is exercised in pure ECS.
public class UiPickTests
{
    private static ulong Element(World w, float x, float y, float width, float height, int paintOrder)
        => w.Entity()
            .Set(new Node())                   // Display.Flex (= 0)
            .Set(new UiCustom { Data = null }) // null render -> PixelHit = solid bbox
            .Set(new ComputedNode
            {
                Position = new Vector2(x, y),
                Size = new Vector2(width, height),
                PaintOrder = paintOrder,
            }).ID;

    private static void MakeMovable(World w, ulong id)
    {
        w.Set<UIMovable>(id);
        w.Set(id, new GlobalZIndex(0));
    }

    // Text-like element: a rendered label — has a Text component (so it paints
    // glyphs) but NO UiCustom. A server-gump title label / OK-Cancel label.
    private static ulong TextElement(World w, float x, float y, float width, float height, int paintOrder)
        => w.Entity()
            .Set(new Node())
            .Set(new Text("x"))
            .Set(new ComputedNode
            {
                Position = new Vector2(x, y),
                Size = new Vector2(width, height),
                PaintOrder = paintOrder,
            }).ID;

    // Bare LAYOUT node: rendered (ComputedNode) + laid out, but paints NOTHING —
    // no UiCustom, no Text, no BackgroundColor. A container's full-size `content`
    // wrapper or a server-gump text scroll wrapper. Must NOT be a hit.
    private static ulong BareLayoutNode(World w, float x, float y, float width, float height, int paintOrder)
        => w.Entity()
            .Set(new Node())
            .Set(new ComputedNode
            {
                Position = new Vector2(x, y),
                Size = new Vector2(width, height),
                PaintOrder = paintOrder,
            }).ID;

    private static Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> Rendered(App app)
    {
        var q = new Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>>();
        q.Initialize(app);
        q.Fetch(app);
        return q;
    }

    private static Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>> Movables(App app)
    {
        var q = new Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>>();
        q.Initialize(app);
        q.Fetch(app);
        return q;
    }

    private static Query<Data<TinyEcs.Parent>> Parents(App app)
    {
        var q = new Query<Data<TinyEcs.Parent>>();
        q.Initialize(app);
        q.Fetch(app);
        return q;
    }

    [Fact]
    public void Topmost_returns_none_when_nothing_under_pointer()
    {
        var app = new App();
        var w = app.GetWorld();
        Element(w, 0, 0, 10, 10, 1);
        var hit = UiPick.Topmost(new XnaVector2(500, 500), null, Rendered(app));
        Assert.False(hit.Found);
    }

    [Fact]
    public void Topmost_picks_the_only_element_under_pointer()
    {
        var app = new App();
        var w = app.GetWorld();
        var e = Element(w, 0, 0, 100, 100, 3);
        var hit = UiPick.Topmost(new XnaVector2(50, 50), null, Rendered(app));
        Assert.True(hit.Found);
        Assert.Equal(e, hit.Entity);
    }

    [Fact]
    public void Topmost_picks_highest_paint_order_among_overlaps()
    {
        var app = new App();
        var w = app.GetWorld();
        // Spawn the FRONT one first so entity-id order does NOT match paint
        // order — guards against the ClayId/entity-id tiebreak bug.
        var front = Element(w, 0, 0, 100, 100, 9);
        Element(w, 0, 0, 100, 100, 4);
        var hit = UiPick.Topmost(new XnaVector2(50, 50), null, Rendered(app));
        Assert.Equal(front, hit.Entity);
    }

    [Fact]
    public void Topmost_ignores_elements_the_pointer_misses()
    {
        var app = new App();
        var w = app.GetWorld();
        Element(w, 0, 0, 40, 40, 9);     // high paint order, but pointer is outside it
        var inRange = Element(w, 100, 100, 40, 40, 2);
        var hit = UiPick.Topmost(new XnaVector2(120, 120), null, Rendered(app));
        Assert.Equal(inRange, hit.Entity);
    }

    [Fact]
    public void Topmost_skips_hidden_elements()
    {
        var app = new App();
        var w = app.GetWorld();
        var visible = Element(w, 0, 0, 100, 100, 2);
        w.Entity()
            .Set(new Node { Display = Display.None })
            .Set(new UiCustom { Data = null })
            .Set(new ComputedNode
            {
                Position = new Vector2(0, 0),
                Size = new Vector2(100, 100),
                PaintOrder = 99, // would win if not hidden (stale ComputedNode case)
            });
        var hit = UiPick.Topmost(new XnaVector2(50, 50), null, Rendered(app));
        Assert.Equal(visible, hit.Entity);
    }

    [Fact]
    public void MovableRoot_returns_self_when_the_hit_is_the_movable()
    {
        var app = new App();
        var w = app.GetWorld();
        var root = Element(w, 0, 0, 100, 100, 1);
        MakeMovable(w, root);
        Assert.Equal(root, UiPick.MovableRoot(root, Movables(app), Parents(app)));
    }

    [Fact]
    public void MovableRoot_walks_child_up_to_its_window_root()
    {
        var app = new App();
        var w = app.GetWorld();
        var root = Element(w, 0, 0, 100, 100, 1);
        MakeMovable(w, root);
        var content = Element(w, 0, 0, 100, 100, 2);
        var item = Element(w, 10, 10, 20, 20, 3);
        w.AddChild(root, content);
        w.AddChild(content, item);
        // A click resolves to the topmost child, which must walk up to the root.
        Assert.Equal(root, UiPick.MovableRoot(item, Movables(app), Parents(app)));
    }

    [Fact]
    public void MovableRoot_returns_zero_when_no_movable_ancestor()
    {
        var app = new App();
        var w = app.GetWorld();
        var loose = Element(w, 0, 0, 100, 100, 1); // no UIMovable anywhere in chain
        Assert.Equal(0UL, UiPick.MovableRoot(loose, Movables(app), Parents(app)));
    }

    [Fact]
    public void Topmost_hits_text_element_without_uicustom()
    {
        var app = new App();
        var w = app.GetWorld();
        var label = TextElement(w, 0, 0, 100, 20, 3);
        var hit = UiPick.Topmost(new XnaVector2(50, 10), null, Rendered(app));
        Assert.Equal(label, hit.Entity);
    }

    // The Global-Chat-title bug: a window's title TEXT (no UiCustom) sits over a
    // window behind it, in a spot the front window's own bg doesn't cover.
    // Clicking the text must resolve to the FRONT window (the text's owner), not
    // raise the window behind. Before text was hit-testable it fell through.
    [Fact]
    public void Title_text_resolves_to_its_own_window_not_the_one_behind()
    {
        var app = new App();
        var w = app.GetWorld();
        var backRoot = Element(w, 0, 0, 300, 300, 1); // opaque window behind
        MakeMovable(w, backRoot);

        var frontRoot = Element(w, 50, 0, 10, 10, 5); // front window root (small, elsewhere)
        MakeMovable(w, frontRoot);
        var title = TextElement(w, 100, 5, 120, 20, 9); // title text over the back window
        w.AddChild(frontRoot, title);

        var hit = UiPick.Topmost(new XnaVector2(110, 12), null, Rendered(app));
        Assert.Equal(title, hit.Entity);
        Assert.Equal(frontRoot, UiPick.MovableRoot(hit.Entity, Movables(app), Parents(app)));
    }

    // Repro of the server-gump html-text orphan: SpawnWrappedText makes `outer`
    // a parent of `inner`, then the caller adds `outer` as a child of the gump
    // root — both AddChilds in one deferred command flush. If the mapper drops
    // outer's parent link, the html text resolves to no movable root (un-draggable).
    [Fact]
    public void Deferred_addchild_parent_then_as_child_keeps_both_links()
    {
        var app = new App();
        var w = app.GetWorld();
        var root = w.Entity().Set(new Node()).ID;
        var outer = w.Entity().Set(new Node()).ID;
        var inner = w.Entity().Set(new Node()).ID;

        w.BeginDeferred();
        w.AddChild(outer, inner);   // outer becomes a parent (matches SpawnWrappedText)
        w.AddChild(root, outer);    // outer added as a child of root (matches caller)
        w.EndDeferred();

        Assert.Equal(outer, (ulong)w.GetParent(inner));
        Assert.Equal(root, (ulong)w.GetParent(outer));
    }

    // Faithful repro: spawn via Commands like ServerGumpPlugin — root (UIMovable),
    // a scroll wrapper `outer` (Overflow.Scroll + ScrollPosition) parenting `inner`,
    // then add `outer` under root — and run a full update (layout + scroll sync).
    [Fact]
    public void Html_scroll_wrapper_resolves_to_root_after_update()
    {
        var app = new App();
        ulong root = 0, outer = 0, inner = 0;
        app.AddSystem((Commands commands) =>
        {
            var r = commands.Spawn().Insert(new Node()).Insert<UIMovable>().Insert(new GlobalZIndex(0));
            var o = commands.Spawn().Insert(new Node { Overflow = Overflow.Scroll }).Insert(new ScrollPosition());
            var i = commands.Spawn().Insert(new Node());
            commands.AddChild(o.Id, i.Id);
            commands.AddChild(r.Id, o.Id);
            root = r.Id; outer = o.Id; inner = i.Id;
        }).InStage(Stage.Startup).Build();
        app.Update();

        var w = app.GetWorld();
        Assert.Equal(outer, (ulong)w.GetParent(inner));
        Assert.Equal(root, (ulong)w.GetParent(outer));
    }

    // The real symptom only shows after entity slots are RECYCLED (the html
    // elements had high generation). Build a parented structure, delete it
    // (DeleteDescendants), then rebuild reusing the freed slots and re-parent in
    // one deferred flush. If despawn cleanup left the relationship mapper dirty,
    // the reused outer's parent link silently fails -> un-draggable html gump.
    [Fact]
    public void Recycled_slots_reparent_correctly()
    {
        var app = new App();
        var w = app.GetWorld();

        var rootA = w.Entity().Set(new Node()).Set<UIMovable>().ID;
        var outerA = w.Entity().Set(new Node()).ID;
        var innerA = w.Entity().Set(new Node()).ID;
        w.AddChild(outerA, innerA);
        w.AddChild(rootA, outerA);
        w.Delete(rootA); // DeleteDescendants -> frees outerA/innerA slots

        var rootB = w.Entity().Set(new Node()).Set<UIMovable>().ID;
        var outerB = w.Entity().Set(new Node()).ID;
        var innerB = w.Entity().Set(new Node()).ID;
        w.BeginDeferred();
        w.AddChild(outerB, innerB);
        w.AddChild(rootB, outerB);
        w.EndDeferred();

        Assert.Equal(outerB, (ulong)w.GetParent(innerB));
        Assert.Equal(rootB, (ulong)w.GetParent(outerB));
    }

    // A scrollable text run keeps its full-content bbox (taller than the scroll
    // viewport). UiPick must clip it to the Overflow.Scroll ancestor's box, or
    // the overflow is grab-able past the window edge.
    [Fact]
    public void Topmost_clips_overflowing_child_to_scroll_ancestor()
    {
        var app = new App();
        var w = app.GetWorld();
        var root = Element(w, 0, 0, 100, 200, 1);
        MakeMovable(w, root);
        // scroll viewport: 100x50
        var outer = w.Entity()
            .Set(new Node { Overflow = Overflow.Scroll })
            .Set(new UiCustom { Data = null })
            .Set(new ComputedNode { Position = new Vector2(0, 0), Size = new Vector2(100, 50), PaintOrder = 2 }).ID;
        // content overflows to 200 tall
        var inner = Element(w, 0, 0, 100, 200, 3);
        w.AddChild(root, outer);
        w.AddChild(outer, inner);

        // Inside inner's full bbox but BELOW the viewport -> clipped, not the inner.
        var below = UiPick.Topmost(new XnaVector2(50, 120), null, Rendered(app), Parents(app));
        Assert.NotEqual(inner, below.Entity);
        // Inside the viewport -> inner is the topmost hit.
        var inside = UiPick.Topmost(new XnaVector2(50, 30), null, Rendered(app), Parents(app));
        Assert.Equal(inner, inside.Entity);
    }

    // Integration test through the REAL Clay layout: a window with a bare
    // Overflow.Scroll container holding tall content. A click well below the
    // window must NOT grab it via the content that overflows the viewport.
    // (Unit test above hand-builds ComputedNode; this one exercises the actual
    // layout + ComputedNode write, which is where a bare scroll container with
    // no render command of its own would otherwise have no clip box.)
    [Fact]
    public void Scroll_overflow_not_grabbable_below_window_through_layout()
    {
        var app = new App();
        app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
        var w = app.GetWorld();

        var root = w.Entity()
            .Set(new Node { PositionType = PositionType.Absolute, Left = Val.Px(50), Top = Val.Px(50), Width = Val.Px(200), Height = Val.Px(100) })
            .Set<UIMovable>()
            .Set(new GlobalZIndex(0))
            .Set(new BackgroundColor(new ClayColor(40, 40, 40, 255))).ID;
        var outer = w.Entity()
            .Set(new Node { PositionType = PositionType.Absolute, Left = Val.Px(10), Top = Val.Px(10), Width = Val.Px(180), Height = Val.Px(50), Overflow = Overflow.Scroll })
            .Set(new ScrollPosition()).ID;                 // bare scroll viewport (no own paint)
        var inner = w.Entity()
            .Set(new Node { Width = Val.Px(180), Height = Val.Px(300) })  // overflows the 50px viewport
            .Set(new BackgroundColor(new ClayColor(200, 0, 0, 255))).ID;
        w.AddChild(root, outer);
        w.AddChild(outer, inner);

        app.Update();   // Clay layout + ComputedNode write

        // Below the window (root bottom = 150) but within the inner's overflow.
        var below = UiPick.Topmost(new XnaVector2(120, 250), null, Rendered(app), Parents(app));
        var owner = UiPick.MovableRoot(below.Entity, Movables(app), Parents(app));
        Assert.NotEqual(root, owner);   // overflow must not be a grab handle outside the window

        // Inside the viewport: still resolves to the window.
        var inside = UiPick.Topmost(new XnaVector2(120, 75), null, Rendered(app), Parents(app));
        Assert.Equal(root, UiPick.MovableRoot(inside.Entity, Movables(app), Parents(app)));
    }

    private sealed class RepushLatest { public ulong Root, Outer, Inner; }

    // Faithful re-push repro: each frame despawn the previous gump root and
    // rebuild (root+outer+inner, AddChild(outer,inner) then AddChild(root,outer))
    // in ONE command buffer — exactly ServerGumpPlugin's re-push path. Repeated
    // frames recycle slots (the high-gen ids seen live). After several frames the
    // latest outer must still resolve to its root.
    [Fact]
    public void Repush_rebuild_keeps_html_wrapper_parented()
    {
        var app = new App();
        app.AddResource(new RepushLatest());
        app.AddSystem((Commands commands, Res<RepushLatest> latest) =>
        {
            if (latest.Value.Root != 0)
                commands.Entity(latest.Value.Root).Despawn();
            var r = commands.Spawn().Insert(new Node()).Insert<UIMovable>().Insert(new GlobalZIndex(0));
            var o = commands.Spawn().Insert(new Node { Overflow = Overflow.Scroll }).Insert(new ScrollPosition());
            var i = commands.Spawn().Insert(new Node());
            commands.AddChild(o.Id, i.Id);
            commands.AddChild(r.Id, o.Id);
            latest.Value.Root = r.Id; latest.Value.Outer = o.Id; latest.Value.Inner = i.Id;
        }).InStage(Stage.Update).Build();

        for (int f = 0; f < 6; f++)
            app.Update();

        var w = app.GetWorld();
        var l = app.GetResource<RepushLatest>();
        Assert.Equal(l.Outer, (ulong)w.GetParent(l.Inner));
        Assert.Equal(l.Root, (ulong)w.GetParent(l.Outer));
    }

    // Server-gump shape: a UIMovable root that carries NO render surface of its
    // own (the old whole-bbox None hit was removed for pixel-perfect parity). It
    // must be hittable ONLY through its painting children — a click on a child
    // walks up to the root (so drag / click-capture claim it), but a click on the
    // root's empty area is NOT a hit (it passes through to the world / window
    // behind instead of the whole rect trapping it).
    [Fact]
    public void Bare_movable_root_only_hit_via_its_painting_children()
    {
        var app = new App();
        var w = app.GetWorld();
        var root = w.Entity()
            .Set(new Node())          // bare: no UiCustom/Text/Bg, no ComputedNode
            .Set<UIMovable>()
            .Set(new GlobalZIndex(0)).ID;
        var child = Element(w, 0, 0, 50, 50, 2);   // paints (solid hit), covers only the top-left
        w.AddChild(root, child);

        // Over the painting child -> resolves up to the root (claim/drag target).
        var onChild = UiPick.Topmost(new XnaVector2(25, 25), null, Rendered(app), Parents(app));
        Assert.Equal(child, onChild.Entity);
        Assert.Equal(root, UiPick.MovableRoot(onChild.Entity, Movables(app), Parents(app)));

        // Over the root's empty area (no child) -> NO hit: nothing of the window
        // traps the click, so it passes through (the trespass-vs-no-trespass line).
        var onGap = UiPick.Topmost(new XnaVector2(150, 150), null, Rendered(app), Parents(app));
        Assert.False(onGap.Found);
    }

    // A bare layout node paints nothing — it must not be a hit, or a window's
    // invisible full-size wrapper would capture clicks over its whole box.
    [Fact]
    public void Topmost_skips_bare_layout_node()
    {
        var app = new App();
        var w = app.GetWorld();
        BareLayoutNode(w, 0, 0, 100, 100, 5);
        var hit = UiPick.Topmost(new XnaVector2(50, 50), null, Rendered(app));
        Assert.False(hit.Found);
    }

    // Server-gump shape: a window root that DOES paint (a None-kind drag frame /
    // bg sprite — modelled as a UiCustom-bearing solid bbox) sits under a bare
    // full-size content wrapper. A click resolves THROUGH the invisible wrapper
    // to the root, so the gump stays draggable from anywhere over its frame.
    [Fact]
    public void Bare_wrapper_falls_through_to_the_painting_window_root()
    {
        var app = new App();
        var w = app.GetWorld();
        var root = Element(w, 0, 0, 200, 80, 1); // UiCustom-bearing => a hit surface
        MakeMovable(w, root);
        var wrapper = BareLayoutNode(w, 0, 0, 200, 80, 7); // invisible, on top
        w.AddChild(root, wrapper);

        var hit = UiPick.Topmost(new XnaVector2(100, 40), null, Rendered(app));
        Assert.Equal(root, hit.Entity);
        Assert.Equal(root, UiPick.MovableRoot(hit.Entity, Movables(app), Parents(app)));
    }

    private static Query<Data<ContainerItemUI>> Items(App app)
    {
        var q = new Query<Data<ContainerItemUI>>();
        q.Initialize(app);
        q.Fetch(app);
        return q;
    }

    private static Query<Data<ContainerWindow>> ContainerWindows(App app)
    {
        var q = new Query<Data<ContainerWindow>>();
        q.Initialize(app);
        q.Fetch(app);
        return q;
    }

    // Mirrors the pickup/container-selection decision: an item directly under
    // the cursor is the target; over bare window chrome the owning container is.
    [Fact]
    public void Container_item_under_cursor_resolves_to_the_item_and_its_window()
    {
        var app = new App();
        var w = app.GetWorld();
        var root = Element(w, 0, 0, 100, 100, 1);
        MakeMovable(w, root);
        w.Set(root, new ContainerWindow { Serial = 1 });
        var item = Element(w, 10, 10, 20, 20, 5);
        w.Set(item, new ContainerItemUI { Container = root, Serial = 2 });
        w.AddChild(root, item);

        var hit = UiPick.Topmost(new XnaVector2(15, 15), null, Rendered(app));
        Assert.Equal(item, hit.Entity);
        Assert.True(Items(app).Contains(hit.Entity));            // classified as item -> pickup target
        Assert.Equal(root, UiPick.MovableRoot(hit.Entity, Movables(app), Parents(app)));
    }

    // The occlusion case for pickup: an item in a BACK container under a FRONT
    // container's chrome must NOT be the hit — the front window is.
    [Fact]
    public void Item_behind_a_front_window_is_not_the_pickup_target()
    {
        var app = new App();
        var w = app.GetWorld();
        var backRoot = Element(w, 0, 0, 100, 100, 1);
        MakeMovable(w, backRoot);
        w.Set(backRoot, new ContainerWindow { Serial = 1 });
        var backItem = Element(w, 40, 40, 20, 20, 2);
        w.Set(backItem, new ContainerItemUI { Container = backRoot, Serial = 2 });
        w.AddChild(backRoot, backItem);

        var frontRoot = Element(w, 30, 30, 100, 100, 8); // covers backItem, higher paint order
        MakeMovable(w, frontRoot);
        w.Set(frontRoot, new ContainerWindow { Serial = 3 });

        var hit = UiPick.Topmost(new XnaVector2(45, 45), null, Rendered(app));
        Assert.NotEqual(backItem, hit.Entity);                  // occluded item is not the hit
        Assert.False(Items(app).Contains(hit.Entity));          // hit is chrome, not an item
        var owner = UiPick.MovableRoot(hit.Entity, Movables(app), Parents(app));
        Assert.Equal(frontRoot, owner);
        Assert.True(ContainerWindows(app).Contains(owner));     // front container claims it
    }

    [Fact]
    public void Topmost_then_MovableRoot_resolves_front_window_over_back()
    {
        var app = new App();
        var w = app.GetWorld();
        // Back window (root + child) painted first; front window painted on top.
        var backRoot = Element(w, 0, 0, 100, 100, 1);
        MakeMovable(w, backRoot);
        var backChild = Element(w, 0, 0, 100, 100, 2);
        w.AddChild(backRoot, backChild);

        var frontRoot = Element(w, 40, 40, 100, 100, 5);
        MakeMovable(w, frontRoot);
        var frontChild = Element(w, 40, 40, 100, 100, 6);
        w.AddChild(frontRoot, frontChild);

        // Pointer in the overlap region: must resolve to the FRONT window.
        var hit = UiPick.Topmost(new XnaVector2(60, 60), null, Rendered(app));
        var owner = UiPick.MovableRoot(hit.Entity, Movables(app), Parents(app));
        Assert.Equal(frontRoot, owner);
    }
}
