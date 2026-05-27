namespace ClassicUO.Ecs;

internal abstract class InputContext<T>
{
    protected Microsoft.Xna.Framework.Game _game;

    protected InputContext(Microsoft.Xna.Framework.Game game) => _game = game;

    public abstract bool IsPressed(T input);
    public abstract bool IsPressedOnce(T input);
    public abstract bool IsReleased(T input);

    public virtual void Update(float deltaTime) { }
}
