using System.Numerics;
using ClassicUO.Ecs;
using ClassicUO.Network;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using Xunit;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace ClassicUO.Ecs.Tests;

// End-to-end behavioural tests for WindowDragPlugin, run against a REAL App +
// schedule — no mocks (per repo rule: real ECS in tests). Two seams make this
// possible headless: an empty AssetsServer (no GPU/files) and TestMouseContext
// (injects raw mouse state without the OS). Windows use a null UiCustom payload
// so PixelHit treats them as solid boxes and never touches the (empty) asset
// loaders. Frames are advanced by setting mouse state then calling app.Update(),
// exactly as the runtime ticks.
public class WindowDragSystemTests
{
    private static MouseState Idle(int x, int y) => TestMouseContext.Idle(x, y);
    private static MouseState Left(int x, int y) => TestMouseContext.Left(x, y);
    private static MouseState Right(int x, int y) => TestMouseContext.Right(x, y);

    private static App MakeApp(out TestMouseContext mouse)
    {
        var app = new App();
        mouse = new TestMouseContext();
        app.AddResource<MouseContext>(mouse);
        app.AddResource(new AssetsServer());     // empty: no loaders needed for None/null-custom hits
        app.AddResource(new GrabbedItem());
        app.AddResource(new DragGate());
        app.AddResource(new NetClient());        // socketless ctor
        app.AddResource(new SelectedEntity());
        app.AddResource(new ServerGumpRegistry());
        app.AddResource(new Configuration.Profile());           // Drag reads HoldAltToMoveGumps
        app.AddResource<KeyboardContext>(new KeyboardContext(null));  // headless: empty snapshot, no Alt held
        app.AddPlugin<WindowDragPlugin>();       // registers UiZCounter + the 3 systems
        return app;
    }

    // Movable window root: Node (the drag target's mutable position) + a solid
    // bbox via ComputedNode + null UiCustom + UiMovable/GlobalZIndex tags.
    private static ulong SpawnWindow(World w, float left, float top, float width, float height, int paintOrder = 1)
    {
        var id = w.Entity()
            .Set(new Node { PositionType = PositionType.Absolute, Left = Val.Px(left), Top = Val.Px(top) })
            .Set(new UiCustom { Data = null })
            .Set(new ComputedNode
            {
                Position = new Vector2(left, top),
                Size = new Vector2(width, height),
                PaintOrder = paintOrder,
            }).ID;
        w.Set<UiMovable>(id);
        w.Set(id, new GlobalZIndex(0));
        return id;
    }

    // A non-movable UI element: solid bbox via ComputedNode + null UiCustom, but
    // NO UiMovable tag — the top bar, the menu bar, the viewport border, the
    // full-screen gutter background. Optionally tagged GameWindowUI to stand in
    // for the world viewport node itself.
    private static ulong SpawnPlainUi(World w, float left, float top, float width, float height, int paintOrder = 1, bool isGameWindow = false)
    {
        var id = w.Entity()
            .Set(new Node { PositionType = PositionType.Absolute, Left = Val.Px(left), Top = Val.Px(top) })
            .Set(new UiCustom { Data = null })
            .Set(new ComputedNode
            {
                Position = new Vector2(left, top),
                Size = new Vector2(width, height),
                PaintOrder = paintOrder,
            }).ID;
        if (isGameWindow)
            w.Set<GameScreenPlugin.GameWindowUI>(id);
        return id;
    }

    private static int CountMovables(App app)
    {
        var q = new Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>>();
        q.Initialize(app);
        q.Fetch(app);
        int n = 0;
        foreach (var _ in q) n++;
        return n;
    }

    [Fact]
    public void Held_left_drag_moves_the_window_by_the_cursor_delta()
    {
        var app = MakeApp(out var mouse);
        var w = app.GetWorld();
        var win = SpawnWindow(w, left: 100, top: 50, width: 80, height: 80);

        // Frame 1: press-once over the window — latches the drag anchor.
        mouse.Frame(Idle(140, 90), Left(140, 90));
        app.Update();

        // Frame 2: held, cursor moved +20/+20 — window follows.
        mouse.Frame(Left(140, 90), Left(160, 110));
        app.Update();

        ref var node = ref w.Get<Node>(win);
        Assert.Equal(ValType.Px, node.Left.Type);
        Assert.Equal(120f, node.Left.Value);
        Assert.Equal(70f, node.Top.Value);
    }

    [Fact]
    public void Drag_does_not_start_on_a_press_outside_the_window()
    {
        var app = MakeApp(out var mouse);
        var w = app.GetWorld();
        var win = SpawnWindow(w, left: 100, top: 50, width: 80, height: 80);

        mouse.Frame(Idle(10, 10), Left(10, 10));   // press far away
        app.Update();
        mouse.Frame(Left(10, 10), Left(40, 40));   // move
        app.Update();

        ref var node = ref w.Get<Node>(win);
        Assert.Equal(100f, node.Left.Value);           // unchanged
        Assert.Equal(50f, node.Top.Value);
    }

    [Fact]
    public void Focusing_a_window_bumps_its_z_to_the_front()
    {
        var app = MakeApp(out var mouse);
        var w = app.GetWorld();
        var win = SpawnWindow(w, left: 100, top: 50, width: 80, height: 80);
        Assert.Equal(0, w.Get<GlobalZIndex>(win).Value);

        mouse.Frame(Idle(140, 90), Left(140, 90));
        app.Update();

        Assert.True(w.Get<GlobalZIndex>(win).Value > 0); // lifted on focus
    }

    [Fact]
    public void Movable_under_cursor_claims_selection_so_world_systems_bail()
    {
        var app = MakeApp(out var mouse);
        var w = app.GetWorld();
        var win = SpawnWindow(w, left: 100, top: 50, width: 80, height: 80);
        var selected = app.GetResource<SelectedEntity>();

        mouse.Frame(Idle(140, 90), Left(140, 90));
        app.Update();

        // ClaimSelectedFromMovable stages the claim at top priority; DepthZ is
        // written immediately, the public Entity only publishes on the frame
        // commit (SelectedEntity.Clear, run by another system at runtime).
        Assert.Equal(float.MaxValue, selected.DepthZ); // claimed above everything
        selected.Clear();
        Assert.Equal(win, selected.Entity);
    }

    // Regression: a window parked in the side gutter / top bar sits outside the
    // world viewport, so SelectedEntity.Enabled is off there. The claim must
    // still land (bypassViewport) or drop/pickup over the gump silently fails —
    // releasing a held item over it would clear the cursor with no drop packet
    // while the server kept the item in the drag-hold state.
    [Fact]
    public void Gutter_parked_window_claims_selection_even_outside_viewport()
    {
        var app = MakeApp(out var mouse);
        var w = app.GetWorld();
        var win = SpawnWindow(w, left: 100, top: 50, width: 80, height: 80);
        var selected = app.GetResource<SelectedEntity>();
        selected.Enabled = false; // mouse outside Camera.Bounds

        mouse.Frame(Idle(140, 90), Left(140, 90));
        app.Update();

        Assert.Equal(float.MaxValue, selected.DepthZ);
        selected.Clear();
        Assert.Equal(win, selected.Entity); // claim survives the world-pick gate
    }

    // Regression: clicking NON-movable UI (the top bar, menu bar, viewport
    // border, gutter background) must also claim SelectedEntity so world naming /
    // use / pickup bail. The old gate only claimed when the hit walked up to a
    // UiMovable root, so a click on the top bar fell straight through to the
    // static / object rendered behind it (names popped over statics, objects got
    // used). Same shape as the movable test below but with NO UiMovable tag.
    [Fact]
    public void Non_movable_ui_under_cursor_claims_selection()
    {
        var app = MakeApp(out var mouse);
        var w = app.GetWorld();
        var bar = SpawnPlainUi(w, left: 0, top: 0, width: 400, height: 22);
        var selected = app.GetResource<SelectedEntity>();

        mouse.Frame(Idle(40, 10), Left(40, 10));
        app.Update();

        Assert.Equal(float.MaxValue, selected.DepthZ); // claimed above everything
        selected.Clear();
        Assert.Equal(bar, selected.Entity);
    }

    // Invariant the fix must preserve: a hit on the GameWindowUI viewport node
    // itself is "over the world", NOT over UI — it must NOT claim, or world
    // clicking breaks entirely. Identical to the test above except the node is
    // tagged GameWindowUI, which is the only thing that flips claim -> no-claim.
    [Fact]
    public void Game_window_viewport_hit_does_not_claim_selection()
    {
        var app = MakeApp(out var mouse);
        var w = app.GetWorld();
        var viewport = SpawnPlainUi(w, left: 0, top: 0, width: 400, height: 400, isGameWindow: true);
        var selected = app.GetResource<SelectedEntity>();

        mouse.Frame(Idle(40, 10), Left(40, 10));
        app.Update();

        Assert.Equal(0f, selected.DepthZ);       // no claim landed
        selected.Clear();
        Assert.NotEqual(viewport, selected.Entity);
    }

    [Fact]
    public void Right_click_press_then_release_over_a_window_closes_it()
    {
        var app = MakeApp(out var mouse);
        var w = app.GetWorld();
        var win = SpawnWindow(w, left: 100, top: 50, width: 80, height: 80);
        Assert.Equal(1, CountMovables(app));

        // Press latches the close target (consumes the click, no despawn yet).
        mouse.Frame(Idle(140, 90), Right(140, 90));
        app.Update();
        Assert.Equal(1, CountMovables(app));

        // Release over the same window fires the close (UiClick semantics).
        mouse.Frame(Right(140, 90), Idle(140, 90));
        app.Update();
        Assert.Equal(0, CountMovables(app));
    }

    [Fact]
    public void Right_click_released_after_dragging_off_does_not_close()
    {
        var app = MakeApp(out var mouse);
        var w = app.GetWorld();
        var win = SpawnWindow(w, left: 100, top: 50, width: 80, height: 80);

        mouse.Frame(Idle(140, 90), Right(140, 90)); // press over window
        app.Update();
        mouse.Frame(Right(140, 90), Idle(10, 10));  // release far away
        app.Update();

        Assert.Equal(1, CountMovables(app));            // still open
    }
}
