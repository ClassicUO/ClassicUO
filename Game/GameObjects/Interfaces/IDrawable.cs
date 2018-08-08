using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects.Interfaces
{
    public interface IDrawable
    {
        bool AllowedToDraw { get; set; }
        SpriteTexture Texture { get; set; }
        Vector3 HueVector { get; set; }

        bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position);
    }
}
