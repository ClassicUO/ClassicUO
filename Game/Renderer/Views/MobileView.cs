using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Renderer;
using ClassicUO.Game.WorldObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Renderer.Views
{
    public class MobileView : View
    {
        const int USED_LAYER_COUNT = 25;
        private static readonly Layer[,] _usedLayers = new Layer[8, USED_LAYER_COUNT]
        {
            {
                Layer.Mount, Layer.Invalid, Layer.Cloak, Layer.Shirt,
                Layer.Pants, Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso,
                Layer.Ring, Layer.Talisman, Layer.Bracelet, Layer.Face,
                Layer.Arms, Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso,
                Layer.Neck, Layer.Hair, Layer.OuterTorso, Layer.Waist,
                Layer.FacialHair, Layer.Earrings, Layer.LeftHand, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },
            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            },

            {
                Layer.Mount, Layer.Invalid, Layer.Shirt, Layer.Pants,
                Layer.Shoes, Layer.InnerLegs, Layer.InnerTorso, Layer.Ring,
                Layer.Talisman, Layer.Bracelet, Layer.Face, Layer.Arms,
                Layer.Gloves, Layer.OuterLegs, Layer.MiddleTorso, Layer.Neck,
                Layer.Hair, Layer.OuterTorso, Layer.Waist, Layer.FacialHair,
                Layer.Earrings, Layer.LeftHand, Layer.Cloak, Layer.Helm,
                Layer.RightHand
            }

        };


        public MobileView(in Mobile mobile) : base(mobile)
        {

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


            sbyte animIndex = WorldObject.AnimIndex;
            byte animGroup = WorldObject.GetAnimationGroup();


            for (int i = 0; i < USED_LAYER_COUNT; i++)
            {
                Layer layer = _usedLayers[dir, i];

                Graphic graphic = 0;

                if (layer == Layer.Mount)
                {
                    if (WorldObject.IsHuman)
                    {
                        Item mount = WorldObject.Equipment[(int)Layer.Mount];
                        if (mount != null)
                        {
                            graphic = mount.GetMountAnimation();
                            int mountedHeightOffset = 0;

                            if (graphic < AssetsLoader.Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                mountedHeightOffset = AssetsLoader.Animations.DataIndex[graphic].MountedHeightOffset;


                            HueVector = RenderExtentions.GetHueVector(mount.Hue);

                        }
                        else
                            continue;
                    }
                    else
                        continue;
                }
                else if (layer == Layer.Invalid)
                {
                    graphic = WorldObject.GetMountAnimation();


                    HueVector = RenderExtentions.GetHueVector(WorldObject.Hue);
                }
                else
                {
                    if (!WorldObject.IsHuman)
                        continue;

                    Item item = WorldObject.Equipment[(int)layer];
                    if (item == null)
                        continue;

                    graphic = item.ItemData.AnimID;

                    if (AssetsLoader.Animations.EquipConversions.TryGetValue(item.Graphic, out var map))
                    {
                        if (map.TryGetValue(item.ItemData.AnimID, out var data))
                        {
                            graphic = data.Graphic;
                        }
                    }


                    HueVector = RenderExtentions.GetHueVector(item.Hue);
                }







                AssetsLoader.Animations.AnimID = graphic;
                AssetsLoader.Animations.AnimGroup = WorldObject.GetAnimationGroup(graphic);
                AssetsLoader.Animations.Direction = dir;

                var direction = AssetsLoader.Animations.DataIndex[AssetsLoader.Animations.AnimID].Groups[AssetsLoader.Animations.AnimGroup].Direction[AssetsLoader.Animations.Direction];

                if (direction.FrameCount == 0 && !AssetsLoader.Animations.LoadDirectionGroup(ref direction))
                    continue;

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


                    int drawCenterY = frame.CenterY;
                    int drawX;
                    int drawY = drawCenterY + (( /*WorldObject.Offset.Z +*/ WorldObject.Position.Z) * 4) - 22 - (int)(WorldObject.Offset.Y - WorldObject.Offset.Z - 3);

                    if (IsFlipped)
                    {
                        drawX = -22 + (int)(WorldObject.Offset.X /*- WorldObject.Offset.Y */);
                    }
                    else
                    {
                        drawX = -22 - (int)(WorldObject.Offset.X/* - WorldObject.Offset.Y*/);
                    }


                    int x = (drawX + frame.CenterX);
                    int y = -drawY - (frame.Heigth + frame.CenterY) + drawCenterY;


                    Texture = TextureManager.GetOrCreateAnimTexture(graphic, AssetsLoader.Animations.AnimGroup, dir, animIndex);
                    Bounds = new Rectangle(x, -y, frame.Width, frame.Heigth);

                    base.Draw(spriteBatch, position);
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
