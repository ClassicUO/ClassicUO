using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyEcs.Bevy.Input;

namespace ClassicUO.Ecs;

// Thin FNA adapter over TinyEcs.Bevy.Input.MouseInput. The adapter owns
// everything device/host specific — FNA polling, physical→logical DpiScale
// conversion, window-focus gate, AGENT_BUILD synthetic injection — and feeds
// the library a per-frame snapshot. Edge detection, double-click timing,
// consume flags and wheel-consume semantics live in the library; this class
// re-exposes them in XNA types (Vector2, MouseButtonType) so call sites are
// untouched. MouseButtonType and MouseButton share numeric values — casts are
// direct.
internal class MouseContext
{
    // protected so a headless test double (see ClassicUO.Ecs.Tests) can feed
    // frames straight into the library without an OS mouse or FNA Game.
    protected readonly MouseInput Input = new();
    protected readonly Microsoft.Xna.Framework.Game _game;

    private MouseState _oldState, _newState;

#if AGENT_BUILD
    private bool _agentSynthEnabled;
    private int _agentSynthX, _agentSynthY;
    private ButtonState _agentSynthLeft, _agentSynthMiddle, _agentSynthRight;
    // Accumulated synthetic scroll-wheel value (mirrors MouseState's running
    // ScrollWheelValue total). Update diffs it against last frame to produce
    // Wheel, exactly like real input. One notch = 120.
    private int _agentSynthWheel;

    internal void AgentSetSynthetic(int x, int y, ButtonState left, ButtonState middle, ButtonState right)
    {
        _agentSynthEnabled = true;
        _agentSynthX = x; _agentSynthY = y;
        _agentSynthLeft = left;
        _agentSynthMiddle = middle;
        _agentSynthRight = right;
    }

    internal void AgentAddSyntheticWheel(int notches) => _agentSynthWheel += notches * 120;

    internal void AgentClearSynthetic() => _agentSynthEnabled = false;

    internal bool AgentSyntheticActive => _agentSynthEnabled;
#endif

    internal MouseContext(Microsoft.Xna.Framework.Game game) => _game = game;

    // Window-focus gate for the press-edge checks. Real input only counts while
    // the FNA window is focused; a headless subclass overrides this to stay
    // "focused" with no Game.
    protected virtual bool IsActiveWindow => _game?.IsActive ?? false;

    // All public positions are LOGICAL pixels (post-DpiScale): Update divides
    // physical input by DpiScale before feeding the library. UI layout,
    // Camera.Bounds and gump hit-tests all reason in logical space, so a single
    // conversion here keeps every downstream consumer consistent. AGENT_BUILD
    // synthetic input is already logical (the agent sends UO-grid coords), so
    // GetScale returns 1 in that path.
    public Vector2 Position => ToXna(Input.Position);
    public Vector2 PositionOffset => ToXna(Input.PositionOffset);
    public Vector2 DraggingOffset => ToXna(Input.DraggingOffset);

    public float Wheel => Input.Wheel;
    public bool WheelConsumed => Input.WheelConsumed;
    public void ConsumeWheel() => Input.ConsumeWheel();

    public bool IsPressed(MouseButtonType button) => Input.IsPressed((MouseButton)button);
    public bool IsPressedOnce(MouseButtonType button) => Input.IsPressedOnce((MouseButton)button);
    public bool IsReleased(MouseButtonType button) => Input.IsReleased((MouseButton)button);
    public bool IsPressedDouble(MouseButtonType button) => Input.IsPressedDouble((MouseButton)button);

    public void Consume(MouseButtonType button) => Input.Consume((MouseButton)button);
    public bool IsConsumed(MouseButtonType button) => Input.IsConsumed((MouseButton)button);

    public virtual void Update(float totalTimeMs)
    {
        _oldState = _newState;
#if AGENT_BUILD
        if (_agentSynthEnabled)
        {
            _newState = new MouseState(
                _agentSynthX, _agentSynthY,
                _agentSynthWheel,
                _agentSynthLeft, _agentSynthMiddle, _agentSynthRight,
                _newState.XButton1, _newState.XButton2);
        }
        else
#endif
        {
            _newState = Microsoft.Xna.Framework.Input.Mouse.GetState();
        }

        var (sx, sy) = GetScale();
        var wheelDelta = (_newState.ScrollWheelValue - _oldState.ScrollWheelValue) / 120f;
#if AGENT_BUILD
        var active = _agentSynthEnabled || IsActiveWindow;
#else
        var active = IsActiveWindow;
#endif
        Input.SetSnapshot(
            new System.Numerics.Vector2(_newState.X / sx, _newState.Y / sy),
            ToButtons(_newState),
            wheelDelta,
            active);
        Input.Update(totalTimeMs);
    }

    protected static MouseButtons ToButtons(MouseState state)
    {
        var down = MouseButtons.None;
        if (state.LeftButton == ButtonState.Pressed) down |= MouseButtons.Left;
        if (state.MiddleButton == ButtonState.Pressed) down |= MouseButtons.Middle;
        if (state.RightButton == ButtonState.Pressed) down |= MouseButtons.Right;
        if (state.XButton1 == ButtonState.Pressed) down |= MouseButtons.XButton1;
        if (state.XButton2 == ButtonState.Pressed) down |= MouseButtons.XButton2;
        return down;
    }

    private static Vector2 ToXna(System.Numerics.Vector2 v) => new(v.X, v.Y);

    private (float, float) GetScale()
    {
#if AGENT_BUILD
        if (_agentSynthEnabled) return (1f, 1f);
#endif
        if (_game is UoGame ug)
        {
            var d = ug.DpiScale;
            if (d <= 0f) d = 1f;
            return (d, d);
        }
        return (1f, 1f);
    }
}
