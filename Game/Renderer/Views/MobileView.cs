using System.Collections.Generic;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.WorldObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class MobileView : View
    {
        private int yOffset;

        public MobileView(in Mobile mobile) : base(mobile)
        {
        }

        public new Mobile WorldObject => (Mobile) base.WorldObject;


        public override bool DrawInternal(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (WorldObject.IsDisposed)
                return false;

            spriteBatch.GetZ();

            bool mirror = false;
            byte dir = (byte) WorldObject.GetDirectionForAnimation();
            Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;

            byte animGroup = 0;
            EquipConvData? convertedItem = null;

            yOffset = 0;

            int mountOffset = 0;

            for (int i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[dir, i];

                Hue color = 0;
                Graphic graphic = 0;

                if (layer == Layer.Mount)
                {
                    if (WorldObject.IsHuman)
                    {
                        Item mount = WorldObject.Equipment[(int) Layer.Mount];
                        if (mount != null)
                        {
                            graphic = mount.GetMountAnimation();

                            if (graphic < Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                mountOffset = Animations.DataIndex[graphic].MountedHeightOffset;

                            animGroup = WorldObject.GetGroupForAnimation(graphic);
                            color = mount.Hue;
                        }
                        else
                            continue;
                    }
                    else
                        continue;
                }
                else if (layer == Layer.Invalid)
                {
                    graphic = WorldObject.GetGraphicForAnimation();
                    animGroup = WorldObject.GetGroupForAnimation();
                    color = WorldObject.Hue;
                }
                else
                {
                    if (!WorldObject.IsHuman)
                        continue;

                    Item item = WorldObject.Equipment[(int) layer];
                    if (item == null)
                        continue;

                    graphic = item.ItemData.AnimID;

                    if (Animations.EquipConversions.TryGetValue(item.Graphic, out Dictionary<ushort, EquipConvData> map))
                    {
                        if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                        {
                            convertedItem = data;
                            graphic = data.Graphic;
                        }
                    }

                    color = item.Hue;
                }


                sbyte animIndex = WorldObject.AnimIndex;

                Animations.AnimID = graphic;
                Animations.AnimGroup = animGroup;
                Animations.Direction = dir;

                ref AnimationDirection direction = ref Animations.DataIndex[Animations.AnimID].Groups[Animations.AnimGroup].Direction[Animations.Direction];

                if (direction.FrameCount == 0 && !Animations.LoadDirectionGroup(ref direction))
                    continue;

                int fc = direction.FrameCount;
                if (fc > 0 && animIndex >= fc) animIndex = 0;

                if (animIndex < direction.FrameCount)
                {
                    ref AnimationFrame frame = ref direction.Frames[animIndex];

                    if (frame.Pixels == null || frame.Pixels.Length <= 0)
                        return false;


                    int drawCenterY = frame.CenterY;
                    int drawX;
                    int drawY = mountOffset + drawCenterY + (int) (WorldObject.Offset.Z / 4 + WorldObject.Position.Z * 4) - 22 - (int) (WorldObject.Offset.Y - WorldObject.Offset.Z - 3);

                    if (IsFlipped)
                        drawX = -22 + (int) WorldObject.Offset.X;
                    else
                        drawX = -22 - (int) WorldObject.Offset.X;


                    int x = drawX + frame.CenterX;
                    int y = -drawY - (frame.Heigth + frame.CenterY) + drawCenterY;

                    if (color <= 0)
                    {
                        if (direction.Address != direction.PatchedAddress)
                            color = Animations.DataIndex[Animations.AnimID].Color;

                        if (color <= 0 && convertedItem.HasValue)
                            color = convertedItem.Value.Color;
                    }

                    //if (yOffset > y)
                    //    yOffset = y;


                    Texture = TextureManager.GetOrCreateAnimTexture(frame);
                    Bounds = new Rectangle(x, -y, frame.Width, frame.Heigth);
                    HueVector = RenderExtentions.GetHueVector(color);


                    if ((layer == Layer.Mount || !WorldObject.IsHuman && layer == Layer.Invalid) && TextureWidth != Texture.Width) TextureWidth = Texture.Width;

                    if (layer == Layer.Invalid)
                        yOffset = y;

                    //Vector3 vv = position;
                    //vv.Z = WorldObject.Position.Z + 7;


                    //CalculateRenderDepth((sbyte)vv.Z, order, (byte)layer, ss);

                    base.Draw(spriteBatch, position);
                }
            }

            //Vector3 vv = new Vector3
            //{
            //    X = position.X + WorldObject.Offset.X,
            //    Y = position.Y - (int)(WorldObject.Offset.Z / 4 + WorldObject.Position.Z * 4) - 22 -
            //        (int)(WorldObject.Offset.Y - WorldObject.Offset.Z - 3) + yOffset,
            //    Z = position.Z
            //};

            ////yOffset = -(yOffset + 44);

            //MessageOverHead(spriteBatch, vv);

            return true;
        }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            PreDraw(position);
            return DrawInternal(spriteBatch, position);
        }

        public override void Update(in double frameMS)
        {
            WorldObject.ProcessAnimation();

            base.Update(frameMS);
        }

        protected override void MessageOverHead(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            base.MessageOverHead(in spriteBatch, in position);

            Text.Draw((SpriteBatchUI) spriteBatch, new Point((int) position.X, (int) position.Y));
        }
    }
}