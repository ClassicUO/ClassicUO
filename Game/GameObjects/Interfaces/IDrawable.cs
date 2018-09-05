using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects.Interfaces
{
    public interface IDrawable
    {
        bool AllowedToDraw { get; set; }
        SpriteTexture Texture { get; set; }
        Vector3 HueVector { get; set; }

        bool Draw(SpriteBatch3D spriteBatch,  Vector3 position);
    }
}
