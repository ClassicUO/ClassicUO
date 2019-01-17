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

namespace ClassicUO.Game.Views
{
    internal class MultiView : View
    {
        private readonly int _canBeTransparent;

        public MultiView(Multi st) : base(st)
        {
            AllowedToDraw = !GameObjectHelper.IsNoDrawable(st.Graphic);

            if (st.ItemData.Height > 5)
                _canBeTransparent = 1;
            else if (st.ItemData.IsRoof || (st.ItemData.IsSurface && st.ItemData.IsBackground) || st.ItemData.IsWall)
                _canBeTransparent = 1;
            else if (st.ItemData.Height == 5 && st.ItemData.IsSurface && !st.ItemData.IsBackground)
                _canBeTransparent = 1;
            else
                _canBeTransparent = 0;
        }

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
                return false;

            Multi st = (Multi) GameObject;

            if (Texture == null || Texture.IsDisposed)
            {
                ArtTexture texture = FileManager.Art.GetTexture(GameObject.Graphic);
                Texture = texture;
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);

                FrameInfo.X = texture.ImageRectangle.X;
                FrameInfo.Y = texture.ImageRectangle.Y;
                FrameInfo.Width = texture.ImageRectangle.Width;
                FrameInfo.Height = texture.ImageRectangle.Height;
            }

            float alpha = 0;
            if (Engine.Profile.Current.UseCircleOfTransparency)
            {
                int z = World.Player.Z + 5;

                bool r = true;

                if (st.Z <= z - st.ItemData.Height)
                    r = false;
                else if (z < st.Z && (_canBeTransparent & 0xFF) == 0)
                    r = false;

                if (r)
                {
                    int distanceMax = Engine.Profile.Current.CircleOfTransparencyRadius + 1;
                    int distance = GameObject.Distance;

                    if (distance <= distanceMax)
                        alpha = 1.0f - 1.0f / (distanceMax / (float)distance);
                }
            }

            if (Engine.Profile.Current.NoColorObjectsOutOfRange && GameObject.Distance > World.ViewRange)
                HueVector = new Vector3(0x038E, 1, HueVector.Z);
            else
                HueVector = ShaderHuesTraslator.GetHueVector(GameObject.Hue, false, alpha, false);
            MessageOverHead(batcher, position, Bounds.Y - 44);
            Engine.DebugInfo.MultiRendered++;
            return base.Draw(batcher, position, objectList);
        }

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int)vertex[0].Position.X;
            int y = list.MousePosition.Y - (int)vertex[0].Position.Y;
            if (Texture.Contains(x, y))
                list.Add(GameObject, vertex[0].Position);
        }
    }
}
