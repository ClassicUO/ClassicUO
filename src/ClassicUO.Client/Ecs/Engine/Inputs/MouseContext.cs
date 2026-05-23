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

    internal void AgentSetSynthetic(int x, int y, ButtonState left, ButtonState middle, ButtonState right)
    {
        _agentSynthEnabled = true;
        _agentSynthX = x; _agentSynthY = y;
        _agentSynthLeft = left;
        _agentSynthMiddle = middle;
        _agentSynthRight = right;
    }

    internal void AgentClearSynthetic() => _agentSynthEnabled = false;

    internal bool AgentSyntheticActive => _agentSynthEnabled;
#endif

    internal MouseContext(Microsoft.Xna.Framework.Game game) : base(game) { }


    public Vector2 Position => new(_newState.X, _newState.Y);
    public Vector2 PositionOffset => new(_newState.X - _oldState.X, _newState.Y - _oldState.Y);
    public Vector2 DraggingOffset => new (_newState.X - _lastMouseClickPosition.X, _newState.Y - _lastMouseClickPosition.Y);
    public float Wheel { get; private set; }

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

        _oldState = _newState;
#if AGENT_BUILD
        if (_agentSynthEnabled)
        {
            _newState = new MouseState(
                _agentSynthX, _agentSynthY,
                _newState.ScrollWheelValue,
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
