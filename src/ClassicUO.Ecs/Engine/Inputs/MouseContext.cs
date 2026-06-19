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
    // The one double-click window (ms) for the whole ECS: two presses of the
    // same button closer than this count as a double-click. Bound into the
    // library in the ctor so MouseInput.IsPressedDouble (world use, nameplates,
    // popups) and the host gestures that roll their own timer (text-field
    // select-word, popup deadlines) all measure against this single value.
    public const float DoubleClickDelta = 300f;

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

    internal MouseContext(Microsoft.Xna.Framework.Game game)
    {
        _game = game;
        Input.DoubleClickDelta = DoubleClickDelta;
    }

    // Window-focus gate for the press-edge checks. Real input only counts while
    // the FNA window is focused; a headless subclass overrides this to stay
    // "focused" with no Game.
    protected virtual bool IsActiveWindow => _game?.IsActive ?? false;

    // All public positions are UI-LAYOUT pixels: Update divides physical input by
    // DpiScale * UiScale (see GetScale) before feeding the library. EVERYTHING the
    // client draws — gumps, text, AND the game viewport (a UI node sampling the
    // world RT) — is laid out at logical/UiScale and upscaled by UiScale at render,
    // so this is the one space every hit-test (gump bounds, Camera.Bounds, world
    // pick) reasons in. Offsets are UI-space deltas for the same reason (camera pan
    // / drag thresholds scale correctly for free). AGENT_BUILD synthetic input is
    // already UI-grid coords → GetScale returns 1.
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

    // Combined input divisor. Physical mouse → /DpiScale (hi-DPI logical) →
    // /UiScale (Options "UI size"). Folding UiScale in here is THE single point
    // that makes every Position consumer UI-space: the whole client — gumps AND
    // the game viewport (a UI node) AND world picking via camera.Bounds — is laid
    // out at logical/UiScale and upscaled by UiScale at render, so the cursor must
    // be divided by it once, before the library ever sees it. AGENT_BUILD synthetic
    // input is already UI-grid coords → divisor 1.
    private (float, float) GetScale()
    {
#if AGENT_BUILD
        if (_agentSynthEnabled) return (1f, 1f);
#endif
        var ui = UiScaleRuntime.Value;
        if (ui <= 0f) ui = 1f;
        if (_game is UoGame ug)
        {
            var d = ug.DpiScale;
            if (d <= 0f) d = 1f;
            return (d * ui, d * ui);
        }
        return (ui, ui);
    }
}
