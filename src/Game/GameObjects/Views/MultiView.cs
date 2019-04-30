﻿using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Multi
    {
        private readonly int _canBeTransparent;
        private readonly bool _isFoliage;

        public bool CharacterIsBehindFoliage { get; set; }

        public override bool TransparentTest(int z)
        {
            bool r = true;

            if (Z <= z - ItemData.Height)
                r = false;
            else if (z < Z && (_canBeTransparent & 0xFF) == 0)
                r = false;

            return r;
        }

        public override bool Draw(Batcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
                return false;

            if (Texture == null || Texture.IsDisposed)
            {
                ArtTexture texture = FileManager.Art.GetTexture(Graphic);
                Texture = texture;
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);

                FrameInfo.Width = texture.ImageRectangle.Width;
                FrameInfo.Height = texture.ImageRectangle.Height;

                FrameInfo.X = (Texture.Width >> 1) - 22 - texture.ImageRectangle.X;
                FrameInfo.Y = Texture.Height - 44 - texture.ImageRectangle.Y;
            }

            if (_isFoliage)
            {
                if (CharacterIsBehindFoliage)
                {
                    if (AlphaHue != 76)
                        ProcessAlpha(76);
                }
                else
                {
                    if (AlphaHue != 0xFF)
                        ProcessAlpha(0xFF);
                }
            }

            if (Engine.Profile.Current.HighlightGameObjects && IsSelected)
            {
                HueVector.X = 0x0023;
                HueVector.Y = 1;
            }
            else if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
            {
                HueVector.X = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
            {
                HueVector.X = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else
            {
                ShaderHuesTraslator.GetHueVector(ref HueVector, Hue);
            }

            Engine.DebugInfo.MultiRendered++;

            //SpriteRenderer.DrawStaticArt(Graphic, Hue, (int) position.X, (int) position.Y);
            base.Draw(batcher, posX, posY);

            if (ItemData.IsLight)
            {
                Engine.SceneManager.GetScene<GameScene>()
                      .AddLight(this, this, posX + 22, posY + 22);
            }

            return true;
        }

        public override void Select(int x, int y)
        {
            if (SelectedObject.Object == this)
                return;
            if (SelectedObject.IsPointInStatic(Graphic, x - Bounds.X, y - Bounds.Y))
                SelectedObject.Object = this;
        }
    }
}