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
        protected static float PI = (float)Math.PI;


        public WorldRenderObject(in WorldObject parent)
        {
            WorldObject = parent;
            AllowedToDraw = true;
            SortZ = parent.Position.Z;
        }

        public WorldObject WorldObject { get; }
        public bool AllowedToDraw { get; set; }
        public sbyte SortZ { get; protected set; }

        protected Texture2D Texture { get; set; }
        protected Rectangle Bounds { get; set; }
        protected Vector3 HueVector { get; set; }
        protected bool HasShadow { get; set; }
        protected bool IsFlipped { get; set; }
        protected float Rotation { get; set; }



        public virtual bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (Texture == null || !AllowedToDraw)
                return false;

            SpriteVertex[] vertex;

            if (Rotation != 0)
            {
                float w = Bounds.Width / 2;
                float h = Bounds.Height / 2;
                Vector3 center = position - new Vector3(Bounds.X - 44 + w, Bounds.Y + h, 0);
                float sinx = (float)Math.Sin(Rotation) * w;
                float cosx = (float)Math.Cos(Rotation) * w;
                float siny = (float)Math.Sin(Rotation) * h;
                float cosy = (float)Math.Cos(Rotation) * h;

                vertex = SpriteVertex.PolyBufferFlipped;
                vertex[0].Position = center;
                vertex[0].Position.X += cosx - -siny;
                vertex[0].Position.Y -= sinx + -cosy;
                vertex[1].Position = center;
                vertex[1].Position.X += cosx - siny;
                vertex[1].Position.Y += -sinx + -cosy;
                vertex[2].Position = center;
                vertex[2].Position.X += -cosx - -siny;
                vertex[2].Position.Y += sinx + cosy;
                vertex[3].Position = center;
                vertex[3].Position.X += -cosx - siny;
                vertex[3].Position.Y += sinx + -cosy;
            }
            else if (IsFlipped)
            {
                vertex = SpriteVertex.PolyBufferFlipped;
                vertex[0].Position = position;
                vertex[0].Position.X += Bounds.X + 44f;
                vertex[0].Position.Y -= Bounds.Y;
                vertex[0].TextureCoordinate.Y = 0;
                vertex[1].Position = vertex[0].Position;
                vertex[1].Position.Y += Bounds.Height;
                vertex[2].Position = vertex[0].Position;
                vertex[2].Position.X -= Bounds.Width;
                vertex[2].TextureCoordinate.Y = 0;
                vertex[3].Position = vertex[1].Position;
                vertex[3].Position.X -= Bounds.Width;
            }
            else
            {
                vertex = SpriteVertex.PolyBuffer;
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
            }

        


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
