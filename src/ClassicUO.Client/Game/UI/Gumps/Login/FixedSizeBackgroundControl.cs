#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.

#endregion

using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class FixedSizeBackgroundControl : Control
    {
        private const int REF_WIDTH = 1024;
        private const int REF_HEIGHT = 768;
        private readonly Texture2D _texture;

        public FixedSizeBackgroundControl(Texture2D texture)
        {
            _texture = texture;
            AcceptMouseInput = false;
            Width = REF_WIDTH;
            Height = REF_HEIGHT;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            Vector3 blackHue = ShaderHueTranslator.GetHueVector(0, false, 1f, true);

            batcher.Draw(
                SolidColorTextureCache.GetTexture(Color.Black),
                new Rectangle(x, y, REF_WIDTH, REF_HEIGHT),
                blackHue
            );

            if (_texture != null)
            {
                Vector3 texHue = ShaderHueTranslator.GetHueVector(0, false, Alpha, true);
                batcher.Draw(_texture, new Rectangle(x, y, REF_WIDTH, REF_HEIGHT), texHue);
            }

            return true;
        }
    }
}
