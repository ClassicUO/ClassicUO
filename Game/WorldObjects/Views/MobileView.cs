using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.WorldObjects.Views
{
    public class MobileView : WorldRenderObject
    {
        const int USED_LAYER_COUNT = 23;
        private static readonly Layer[,] _usedLayers = new Layer[8, USED_LAYER_COUNT]
        {
            {
                Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.OuterLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet,
                Layer.Face, Layer.Arms, Layer.Gloves, Layer.InnerLegs, Layer.MiddleTorso, Layer.OuterTorso, Layer.Waist , Layer.Neck,
                Layer.Hair, Layer.FacialHair, Layer.Earrings, Layer.Helm, Layer.LeftHand, Layer.RightHand, Layer.Cloak
            },
            {
                Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.OuterLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet,
                Layer.Face, Layer.Arms, Layer.Gloves, Layer.InnerLegs, Layer.MiddleTorso, Layer.OuterTorso, Layer.Waist , Layer.Neck,
                Layer.Hair, Layer.FacialHair, Layer.Earrings, Layer.Helm, Layer.LeftHand, Layer.RightHand, Layer.Cloak
            },
            {
               Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.OuterLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet,
               Layer.Face, Layer.Arms, Layer.Gloves, Layer.InnerLegs, Layer.MiddleTorso, Layer.OuterTorso, Layer.Waist , Layer.Neck,
               Layer.Hair, Layer.FacialHair, Layer.Earrings, Layer.Helm, Layer.LeftHand, Layer.Cloak,  Layer.RightHand
            },
            {
               Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.OuterLegs, Layer.InnerLegs, Layer.RightHand, Layer.Talisman,
               Layer.Bracelet, Layer.Face, Layer.Arms, Layer.Gloves, Layer.InnerLegs, Layer.MiddleTorso, Layer.OuterTorso, Layer.Waist,
               Layer.Neck, Layer.Hair, Layer.FacialHair, Layer.Earrings, Layer.Helm, Layer.LeftHand, Layer.RightHand
            },
            {
               Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.OuterLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet,
               Layer.Face, Layer.Arms, Layer.Gloves, Layer.InnerLegs, Layer.MiddleTorso, Layer.OuterTorso, Layer.Waist , Layer.Neck,
               Layer.Hair, Layer.FacialHair, Layer.Earrings, Layer.Helm, Layer.LeftHand, Layer.Cloak,  Layer.RightHand
            },
            {
               Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.OuterLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet,
               Layer.Face, Layer.Arms, Layer.Gloves, Layer.InnerLegs, Layer.MiddleTorso, Layer.OuterTorso, Layer.Waist , Layer.Neck,
               Layer.Hair, Layer.FacialHair, Layer.Earrings, Layer.Helm, Layer.LeftHand, Layer.RightHand, Layer.Cloak
            },
            {
               Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.OuterLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet,
               Layer.Face, Layer.Arms, Layer.Gloves, Layer.InnerLegs, Layer.MiddleTorso, Layer.OuterTorso, Layer.Waist , Layer.Neck,
               Layer.Hair, Layer.FacialHair, Layer.Earrings, Layer.Helm, Layer.LeftHand, Layer.RightHand, Layer.Cloak
            },
            {
               Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.OuterLegs, Layer.InnerTorso, Layer.Ring, Layer.Talisman, Layer.Bracelet,
               Layer.Face, Layer.Arms, Layer.Gloves, Layer.InnerLegs, Layer.MiddleTorso, Layer.OuterTorso, Layer.Waist , Layer.Neck,
               Layer.Hair, Layer.FacialHair, Layer.Earrings, Layer.Helm, Layer.LeftHand, Layer.RightHand, Layer.Cloak
            },
        };


        public MobileView(in Mobile mobile) : base(mobile)
        {
            //Texture = TextureManager.GetOrCreateStaticTexture(567);
            //Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + (WorldObject.Position.Z * 4), Texture.Width, Texture.Height);
        }

        public sbyte AnimIndex { get; set; }

        public new Mobile WorldObject => (Mobile)base.WorldObject;




        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            spriteBatch.GetZ();

            bool mirror = false;
            byte dir = (byte)WorldObject.GetAnimationDirection();
            
            AssetsLoader.Animations.GetAnimDirection(ref dir, ref mirror);

            IsFlipped = mirror;

            byte animIndex = WorldObject.AnimIndex;
            byte animGroup = WorldObject.GetAnimationGroup();

            Graphic id = WorldObject.GetMountAnimation();

            AssetsLoader.Animations.AnimGroup = animGroup;
            AssetsLoader.Animations.Direction = dir;

            var direction = AssetsLoader.Animations.DataIndex[id].Groups[AssetsLoader.Animations.AnimGroup].Direction[AssetsLoader.Animations.Direction];
            AssetsLoader.Animations.AnimID = id;

            if (direction.FrameCount == 0 && !AssetsLoader.Animations.LoadDirectionGroup(ref direction))
                return false;

            int fc = direction.FrameCount;
            if (fc > 0 && animIndex >= fc)
            {
                animIndex = 0;
            }

            if (animIndex < direction.FrameCount)
            {
                var frame = direction.Frames[animIndex];

                if (frame.Pixels == null || frame.Pixels.Length <= 0)
                    return false;


                if (WorldObject.Offset !=  Vector3.Zero)
                {

                }

                int drawCenterY = frame.CenterY;
                int drawX = (int)position.X + (int)WorldObject.Offset.X ;
                int drawY = (int)position.Y + (int)WorldObject.Offset.Y - (int)WorldObject.Offset.Z  -3;

                //if (IsFlipped)
                //{
                //    drawX += 44;
                //    //drawY += 44;
                //}
                //else
                //{

                //}

                if (IsFlipped)
                {
                    drawX = -22 + (int)WorldObject.Offset.X /*+ (int)((WorldObject.Offset.X - WorldObject.Offset.Y) * 1)*/;
                    drawY = drawCenterY + (int)((WorldObject.Offset.Z + WorldObject.Position.Z) * 4) - 22 - (int)((/*WorldObject.Offset.X +*/ WorldObject.Offset.Y) * 1);
                }
                else
                {
                    drawX = -22 - (int)WorldObject.Offset.X/*- (int)((WorldObject.Offset.X - WorldObject.Offset.Y) * 1)*/;
                    drawY = drawCenterY - (int)((WorldObject.Offset.Z + WorldObject.Position.Z) * 4) - 22 - (int)((/*WorldObject.Offset.X +*/ WorldObject.Offset.Y) * 1);
                }

                int x = (drawX + frame.CenterX);
                int y = -drawY - (frame.Heigth + frame.CenterY) + drawCenterY;

                //if (Texture != null)
                //    Texture.Dispose();

                Texture = TextureManager.GetOrCreateAnimTexture(id, animGroup, dir, animIndex);

                //Texture = new Texture2D(TextureManager.Device, frame.Width, frame.Heigth, false, SurfaceFormat.Bgra5551);
                //Texture.SetData(frame.Pixels);
                Bounds = new Rectangle(x, -y, frame.Width, frame.Heigth);
                HueVector = RenderExtentions.GetHueVector(WorldObject.Hue);

                Vector3 t = new Vector3()
                {
                    X = drawX,
                    Y = drawY,
                    Z = 0
                };

                base.Draw(spriteBatch, position);

                for (int i = 0; i < USED_LAYER_COUNT; i++)
                {
                    
                }

            }
           
            return true;
        }

        public override void Update(in double frameMS)
        {
            WorldObject.ProcessAnimation();


            base.Update(frameMS);
        }
    }
}
