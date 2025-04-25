using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Ecs;

internal sealed class MouseContext : InputContext<MouseButtonType>
{
    private static float DCLICK_DELTA = 300;

    private MouseState _oldState, _newState;
    private float _lastClickTime, _currentTime;
    private MouseButtonType? _lastClickButton;

    internal MouseContext(Microsoft.Xna.Framework.Game game) : base(game) { }


    public Vector2 Position => new(_newState.X, _newState.Y);
    public Vector2 PositionOffset => new(_newState.X - _oldState.X, _newState.Y - _oldState.Y);
    public float Wheel { get; private set; }

    public override bool IsPressed(MouseButtonType input) => VerifyCondition(input, ButtonState.Pressed, ButtonState.Pressed);

    public override bool IsPressedOnce(MouseButtonType input) => VerifyCondition(input, ButtonState.Pressed, ButtonState.Released);

    public override bool IsReleased(MouseButtonType input) => VerifyCondition(input, ButtonState.Released, ButtonState.Pressed);

    public bool IsPressedDouble(MouseButtonType input)
    {
        if (IsPressedOnce(input))
        {
            if (_lastClickButton == input && _lastClickTime + DCLICK_DELTA > _currentTime)
            {
                _lastClickButton = null;
                return true;
            }

            _lastClickButton = input;
            _lastClickTime = _currentTime;
        }

        return false;
    }

    public override void Update(float deltaTime)
    {
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