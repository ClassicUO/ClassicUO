using System;
using Microsoft.Xna.Framework.Input;
using TinyEcs.Bevy.Input;

namespace ClassicUO.Ecs;

// Thin FNA adapter over TinyEcs.Bevy.Input.KeyboardInput (mirrors
// MouseContext): polls the device, feeds the library snapshot, re-exposes the
// edge API in FNA's Keys. KeyCode mirrors Keys numerically — casts are direct.
internal class KeyboardContext
{
    protected readonly KeyboardInput Input = new();
    private readonly Microsoft.Xna.Framework.Game _game;

    internal KeyboardContext(Microsoft.Xna.Framework.Game game) => _game = game;

    // Window-focus gate; a headless subclass overrides this to stay "focused"
    // with no Game.
    protected virtual bool IsActiveWindow => _game?.IsActive ?? false;

    public bool IsPressed(Keys input) => Input.IsPressed((KeyCode)input);

    public bool IsPressedOnce(Keys input) => Input.IsPressedOnce((KeyCode)input);

    public bool IsReleased(Keys input) => Input.IsReleased((KeyCode)input);

    public Keys[] GetPressedKeys()
    {
        var pressed = Input.PressedKeys;
        var keys = new Keys[pressed.Length];
        for (var i = 0; i < pressed.Length; i++)
            keys[i] = (Keys)pressed[i];
        return keys;
    }

    public virtual void Update(float totalTimeMs)
    {
        var state = Microsoft.Xna.Framework.Input.Keyboard.GetState();

        // IsKeyDown over the full VK range instead of GetPressedKeys() — the
        // latter allocates a fresh array every frame.
        Span<KeyCode> buf = stackalloc KeyCode[256];
        var count = 0;
        for (var key = Keys.None + 1; key <= Keys.OemClear; key++)
            if (state.IsKeyDown(key))
                buf[count++] = (KeyCode)key;

        Input.SetSnapshot(buf[..count], IsActiveWindow);
        Input.Update(totalTimeMs);
    }
}
