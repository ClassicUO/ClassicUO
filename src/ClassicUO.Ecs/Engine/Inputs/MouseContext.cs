using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Ecs;

internal sealed class MouseContext : InputContext<MouseButtonType>
{
    private static float DCLICK_DELTA = 300;

    private MouseState _oldState, _newState;
    private float _lastClickTime, _currentTime;
    private readonly MouseButtonType?[] _lastClickButtons = new MouseButtonType?[2];
    private Vector2 _lastMouseClickPosition;
    // Buttons consumed by a UI handler this frame. Cleared at the start of
    // Update so the flag only suppresses reads for the remainder of the tick.
    // Lets UI close systems eat a right-click before PlayerMovement sees it.
    private readonly bool[] _consumed = new bool[(int)MouseButtonType.Size];

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

    internal MouseContext(Microsoft.Xna.Framework.Game game) : base(game) { }


    // All public positions return LOGICAL pixels (post-DpiScale). Mirrors
    // main's Mouse.Update which divides physical input by DpiScale before
    // storing. UI layout, Camera.Bounds, and gump hit-tests all reason in
    // logical space, so a single conversion here keeps every downstream
    // consumer (GameScene world picks, UI clicks, drag offsets) consistent.
    // AGENT_BUILD synthetic input is already in logical pixels (the agent
    // sends UO-grid coords), so no division needed in that path — _newState
    // values are stored pre-scaled below.
    public Vector2 Position
    {
        get
        {
            var (sx, sy) = GetScale();
            return new(_newState.X / sx, _newState.Y / sy);
        }
    }
    public Vector2 PositionOffset
    {
        get
        {
            var (sx, sy) = GetScale();
            return new((_newState.X - _oldState.X) / sx, (_newState.Y - _oldState.Y) / sy);
        }
    }
    public Vector2 DraggingOffset
    {
        get
        {
            // _lastMouseClickPosition is stored in logical pixels (set via
            // Position which already divides by DpiScale). Subtract in
            // logical space — do NOT mix raw _newState (physical) with
            // logical, that double-counts the scale and produces a non-zero
            // offset on press-down for any DpiScale != 1.
            var p = Position;
            return new(p.X - _lastMouseClickPosition.X, p.Y - _lastMouseClickPosition.Y);
        }
    }

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
    public float Wheel { get; private set; }
    // Set by a UI handler when the scroll wheel has been consumed for the
    // current frame (e.g. a scrollable gump was hovered + scrolled). The
    // camera plugin checks this before applying zoom so wheel input doesn't
    // pass through gump UI to the world. ConsumeWheel ALSO zeroes the
    // Wheel reading so any downstream consumer that doesn't honour the
    // flag still sees a no-op.
    public bool WheelConsumed { get; private set; }
    public void ConsumeWheel()
    {
        WheelConsumed = true;
        Wheel = 0f;
    }

    public override bool IsPressed(MouseButtonType input) => !IsConsumed(input) && VerifyCondition(input, ButtonState.Pressed, ButtonState.Pressed);

    public override bool IsPressedOnce(MouseButtonType input) => !IsConsumed(input) && VerifyCondition(input, ButtonState.Pressed, ButtonState.Released);

    public override bool IsReleased(MouseButtonType input) => !IsConsumed(input) && VerifyCondition(input, ButtonState.Released, ButtonState.Pressed);

    public bool IsPressedDouble(MouseButtonType input) => !IsConsumed(input) && _lastClickButtons[0] == input && _lastClickButtons[1] == input;

    public void Consume(MouseButtonType input)
    {
        var idx = (int)input;
        if (idx >= 0 && idx < _consumed.Length)
            _consumed[idx] = true;
    }

    public bool IsConsumed(MouseButtonType input)
    {
        var idx = (int)input;
        return idx >= 0 && idx < _consumed.Length && _consumed[idx];
    }

    public override void Update(float deltaTime)
    {
        for (int i = 0; i < _consumed.Length; i++)
            _consumed[i] = false;
        WheelConsumed = false;

        // Advance state FIRST so press-edge detection below uses the same
        // (_oldState, _newState) pair that downstream systems will see this
        // frame. Otherwise _lastMouseClickPosition lags by one frame and
        // DraggingOffset reads as (cursor − 0) on the press-once frame —
        // tripping pickup drag thresholds instantly.
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
        _currentTime = deltaTime;
        Wheel = (_newState.ScrollWheelValue - _oldState.ScrollWheelValue) / 120f;

        for (var button = MouseButtonType.None + 1; button < MouseButtonType.Size; button++)
        {
            if (IsPressedDouble(button))
            {
                _lastClickButtons[0] = _lastClickButtons[1] = null;
            }

            if (IsPressedOnce(button))
            {
                _lastMouseClickPosition = Position;

                if (_lastClickButtons[0] == null)
                {
                    _lastClickButtons[0] = button;
                    _lastClickTime = _currentTime + DCLICK_DELTA;
                }
                else if (_lastClickButtons[0] == button && _lastClickButtons[1] == null)
                {
                    _lastClickButtons[1] = button;
                }

                break;
            }

            if (IsReleased(button))
            {
                _lastMouseClickPosition = Vector2.Zero;
            }
        }

        if (_currentTime > _lastClickTime)
        {
            _lastClickButtons[0] = _lastClickButtons[1] = null;
        }

        base.Update(deltaTime);
    }

    private bool VerifyCondition(MouseButtonType button, ButtonState stateNew, ButtonState stateOld)
#if AGENT_BUILD
        => (_agentSynthEnabled || _game.IsActive) && button switch
#else
        => _game.IsActive && button switch
#endif
        {
            MouseButtonType.Left => _newState.LeftButton == stateNew && _oldState.LeftButton == stateOld,
            MouseButtonType.Middle => _newState.MiddleButton == stateNew && _oldState.MiddleButton == stateOld,
            MouseButtonType.Right => _newState.RightButton == stateNew && _oldState.RightButton == stateOld,
            MouseButtonType.XButton1 => _newState.XButton1 == stateNew && _oldState.XButton1 == stateOld,
            MouseButtonType.XButton2 => _newState.XButton2 == stateNew && _oldState.XButton2 == stateOld,
            _ => false
        };
}
