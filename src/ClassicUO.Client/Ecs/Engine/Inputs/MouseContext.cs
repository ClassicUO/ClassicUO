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

    internal MouseContext(Microsoft.Xna.Framework.Game game) : base(game) { }


    public Vector2 Position => new(_newState.X, _newState.Y);
    public Vector2 PositionOffset => new(_newState.X - _oldState.X, _newState.Y - _oldState.Y);
    public Vector2 DraggingOffset => new (_newState.X - _lastMouseClickPosition.X, _newState.Y - _lastMouseClickPosition.Y);
    public float Wheel { get; private set; }

    public override bool IsPressed(MouseButtonType input) => VerifyCondition(input, ButtonState.Pressed, ButtonState.Pressed);

    public override bool IsPressedOnce(MouseButtonType input) => VerifyCondition(input, ButtonState.Pressed, ButtonState.Released);

    public override bool IsReleased(MouseButtonType input) => VerifyCondition(input, ButtonState.Released, ButtonState.Pressed);

    public bool IsPressedDouble(MouseButtonType input) => _lastClickButtons[0] == input && _lastClickButtons[1] == input;

    public override void Update(float deltaTime)
    {
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
        _newState = Microsoft.Xna.Framework.Input.Mouse.GetState();
        _currentTime = deltaTime;
        Wheel = (_newState.ScrollWheelValue - _oldState.ScrollWheelValue) / 120f;

        base.Update(deltaTime);
    }

    private bool VerifyCondition(MouseButtonType button, ButtonState stateNew, ButtonState stateOld)
        => _game.IsActive && button switch
        {
            MouseButtonType.Left => _newState.LeftButton == stateNew && _oldState.LeftButton == stateOld,
            MouseButtonType.Middle => _newState.MiddleButton == stateNew && _oldState.MiddleButton == stateOld,
            MouseButtonType.Right => _newState.RightButton == stateNew && _oldState.RightButton == stateOld,
            MouseButtonType.XButton1 => _newState.XButton1 == stateNew && _oldState.XButton1 == stateOld,
            MouseButtonType.XButton2 => _newState.XButton2 == stateNew && _oldState.XButton2 == stateOld,
            _ => false
        };
}
