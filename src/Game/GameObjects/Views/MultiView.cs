using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Multi = ClassicUO.Game.GameObjects.Multi;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Multi
    {
        private readonly int _canBeTransparent;

        public override bool TransparentTest(int z)
        {
            bool r = true;

            if (Z <= z - ItemData.Height)
                r = false;
            else if (z < Z && (_canBeTransparent & 0xFF) == 0)
                r = false;

            return r;
        }

        public bool CharacterIsBehindFoliage { get; set; }
        private readonly bool _isFoliage;

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || IsDisposed)
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
            
            if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
                HueVector = new Vector3(Constants.OUT_RANGE_COLOR, 1, HueVector.Z);
            else if (World.Player.IsDead)
                HueVector = new Vector3(Constants.DEAD_RANGE_COLOR, 1, HueVector.Z);
            else
                HueVector = ShaderHuesTraslator.GetHueVector(Hue);

            //MessageOverHead(batcher, position, Bounds.Y - 44);
            Engine.DebugInfo.MultiRendered++;

            base.Draw(batcher, position, objectList);
            //if (_isFoliage)
            //{
            //    if (_texture == null)
            //    {
            //        _texture = new Texture2D(batcher.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            //        _texture.SetData(new Color[] { Color.Red });
            //    }

            //    batcher.DrawRectangle(_texture, new Rectangle((int)position.X - FrameInfo.X, (int)position.Y - FrameInfo.Y, FrameInfo.Width, FrameInfo.Height), Vector3.Zero);
            //}

            return true;
        }

       // private static Texture2D _texture;

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex, bool istransparent)
        {
            int x = list.MousePosition.X - (int)vertex[0].Position.X;
            int y = list.MousePosition.Y - (int)vertex[0].Position.Y;
            if (!istransparent && Texture.Contains(x, y))
                list.Add(this, vertex[0].Position);
        }
    }
}
