using ClassicUO.Ecs;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Ecs.Tests;

// Headless MouseContext for ECS system tests. No FNA Game, no OS mouse: it
// stays "focused" (IsActiveWindow) and advances frames via Frame(old, new)
// instead of Update() (which would read Mouse.GetState()). Each Frame feeds
// both raw states straight into the TinyEcs.Bevy.Input library, so press-edge
// detection (IsPressedOnce / IsReleased / IsPressed) runs the real logic
// systems see at runtime. Registered as the MouseContext resource — systems
// take Res<MouseContext> and get this transparently.
internal sealed class TestMouseContext : MouseContext
{
    private float _time;

    public TestMouseContext() : base(null) { }

    protected override bool IsActiveWindow => true;

    // Advance one frame: feed the previous then current raw mouse states so
    // the library's edge checks compare exactly this pair.
    public void Frame(MouseState previous, MouseState current)
    {
        Feed(previous);
        Feed(current);
    }

    private void Feed(MouseState state)
    {
        Input.SetSnapshot(new System.Numerics.Vector2(state.X, state.Y), ToButtons(state));
        Input.Update(_time += 16f);
    }

    // Convenience builders for the common button layouts.
    public static MouseState Idle(int x, int y)
        => new MouseState(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    public static MouseState Left(int x, int y)
        => new MouseState(x, y, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    public static MouseState Right(int x, int y)
        => new MouseState(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Pressed, ButtonState.Released, ButtonState.Released);
}
