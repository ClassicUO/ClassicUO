using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Ecs;

internal class KeyboardContext : InputContext<Keys>
{
    // protected so a headless test double can inject a frame's raw state
    // without an OS keyboard or FNA Game (mirrors MouseContext).
    protected KeyboardState _oldState, _newState;

    internal KeyboardContext(Microsoft.Xna.Framework.Game game) : base(game) { }

    // Window-focus gate; a headless subclass overrides this to stay "focused"
    // with no Game.
    protected virtual bool IsActiveWindow => _game?.IsActive ?? false;

    public override bool IsPressed(Keys input) => IsActiveWindow && _newState.IsKeyDown(input) && _oldState.IsKeyDown(input);

    public override bool IsPressedOnce(Keys input) => IsActiveWindow && _newState.IsKeyDown(input) && _oldState.IsKeyUp(input);

    public override bool IsReleased(Keys input) => IsActiveWindow && _newState.IsKeyUp(input) && _oldState.IsKeyDown(input);

    public Keys[] GetPressedKeys() => _newState.GetPressedKeys();

    public override void Update(float deltaTime)
    {
        _oldState = _newState;
        _newState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        base.Update(deltaTime);
    }
}