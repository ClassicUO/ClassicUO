using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Ecs;

internal sealed class KeyboardContext : InputContext<Keys>
{
    private KeyboardState _oldState, _newState;

    internal KeyboardContext(Microsoft.Xna.Framework.Game game) : base(game) { }


    public override bool IsPressed(Keys input) => _game.IsActive && _newState.IsKeyDown(input) && _oldState.IsKeyDown(input);

    public override bool IsPressedOnce(Keys input) => _game.IsActive && _newState.IsKeyDown(input) && _oldState.IsKeyUp(input);

    public override bool IsReleased(Keys input) => _game.IsActive && _newState.IsKeyUp(input) && _oldState.IsKeyDown(input);

    public Keys[] GetPressedKeys() => _newState.GetPressedKeys();

    public override void Update(float deltaTime)
    {
        _oldState = _newState;
        _newState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        base.Update(deltaTime);
    }
}