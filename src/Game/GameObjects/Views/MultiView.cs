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

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || IsDisposed)
                return false;

            if (Texture == null || Texture.IsDisposed)
            {
                ArtTexture texture = FileManager.Art.GetTexture(Graphic);
                Texture = texture;
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);

                FrameInfo.X = texture.ImageRectangle.X;
                FrameInfo.Y = texture.ImageRectangle.Y;
                FrameInfo.Width = texture.ImageRectangle.Width;
                FrameInfo.Height = texture.ImageRectangle.Height;
            }

            int distance = Distance;

            if (Engine.Profile.Current.UseCircleOfTransparency)
            {
                int z = World.Player.Z + 5;

                bool r = true;
                
                if (Z <= z - ItemData.Height)
                    r = false;
                else if (z < Z && (_canBeTransparent & 0xFF) == 0)
                    r = false;
            
                if (r)
                {
                    int distanceMax = Engine.Profile.Current.CircleOfTransparencyRadius;

                    if (distance <= distanceMax)
                    {
                        if (distance <= 0)
                            distance = 1;

                        ProcessAlpha((byte)(235 - (200 / distance)));
                    }
                    else if (AlphaHue != 0xFF)
                        ProcessAlpha(0xFF);
                }
            }

            if (Engine.Profile.Current.NoColorObjectsOutOfRange && distance > World.ViewRange)
                HueVector = new Vector3(0x038E, 1, HueVector.Z);
            else
                HueVector = ShaderHuesTraslator.GetHueVector(Hue);

            MessageOverHead(batcher, position, Bounds.Y - 44);
            Engine.DebugInfo.MultiRendered++;
            return base.Draw(batcher, position, objectList);
        }

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int)vertex[0].Position.X;
            int y = list.MousePosition.Y - (int)vertex[0].Position.Y;
            if (Texture.Contains(x, y))
                list.Add(this, vertex[0].Position);
        }
    }
}
