using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.WorldObjects
{
    public abstract class WorldRenderObject
    {
        public WorldRenderObject(in WorldObject parent)
        {
            WorldObject = parent;
            AllowedToDraw = true;

            SortZ = parent.Position.Z;
        }

        public WorldObject WorldObject { get; }
        public sbyte SortZ { get; set; }
        public bool AllowedToDraw { get; set; }

        protected Texture2D Texture { get; set; }
        protected Rectangle Bounds { get; set; }
        protected Vector3 HueVector { get; set; }


        public virtual bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (Texture == null || !AllowedToDraw)
                return false;

            SpriteVertex[] vertex = SpriteVertex.PolyBuffer;
            vertex[0].Position = position;
            vertex[0].Position.X -= Bounds.X;
            vertex[0].Position.Y -= Bounds.Y;
            vertex[0].TextureCoordinate.Y = 0;
            vertex[1].Position = vertex[0].Position;
            vertex[1].Position.X += Bounds.Width;
            vertex[1].TextureCoordinate.Y = 0;
            vertex[2].Position = vertex[0].Position;
            vertex[2].Position.Y += Bounds.Height;
            vertex[3].Position = vertex[1].Position;
            vertex[3].Position.Y += Bounds.Height;


            if (vertex[0].Hue != HueVector)
            {
                vertex[0].Hue =
                    vertex[1].Hue =
                    vertex[2].Hue =
                    vertex[3].Hue = HueVector;
            }


            if (!spriteBatch.DrawSprite(Texture, vertex))
                return false;

            MousePick(vertex);

            return true;
        }

        protected virtual void MousePick(in SpriteVertex[] vertex)
        {

        }
    }
}
