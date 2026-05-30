using System.Numerics;
using ClassicUO.Ecs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using Xunit;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

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

    // Text-like element: rendered (has ComputedNode) and laid out, but carries
    // NO UiCustom — exactly a server-gump title label.
    private static ulong TextElement(World w, float x, float y, float width, float height, int paintOrder)
        => w.Entity()
            .Set(new Node())
            .Set(new ComputedNode
            {
                Position = new Vector2(x, y),
                Size = new Vector2(width, height),
                PaintOrder = paintOrder,
            }).ID;

    private static Query<Data<ComputedNode, Node, UiCustom>, Filter<Optional<UiCustom>>> Rendered(App app)
    {
        var q = new Query<Data<ComputedNode, Node, UiCustom>, Filter<Optional<UiCustom>>>();
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
