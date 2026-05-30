using ClassicUO.Ecs;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Ecs.Tests;

// Headless MouseContext for ECS system tests. No FNA Game, no OS mouse: it
// stays "focused" (IsActiveWindow) and advances frames via Frame(old, new)
// instead of Update() (which would read Mouse.GetState()). Press-edge
// detection (IsPressedOnce / IsReleased / IsPressed) runs the real base logic
// against the injected states, so systems see exactly what they would at
// runtime. Registered as the MouseContext resource — systems take
// Res<MouseContext> and get this transparently.
internal sealed class TestMouseContext : MouseContext
{
    public TestMouseContext() : base(null) { }

    protected override bool IsActiveWindow => true;

    // Advance one frame: reset per-frame consume flags (as Update does), then
    // set the previous + current raw mouse states the edge checks compare.
    public void Frame(MouseState previous, MouseState current)
    {
        ClearConsumed();
        _oldState = previous;
        _newState = current;
    }

    // Convenience builders for the common button layouts.
    public static MouseState Idle(int x, int y)
        => new MouseState(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    public static MouseState Left(int x, int y)
        => new MouseState(x, y, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    public static MouseState Right(int x, int y)
        => new MouseState(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Pressed, ButtonState.Released, ButtonState.Released);
}
