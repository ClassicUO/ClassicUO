using System.Collections.Generic;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Renderer.Views
{
    public class MobileView : View
    {
        private ViewLayer[] _frames;
        private int _layerCount;

        private static Texture2D _texture;



        public MobileView(in Mobile mobile) : base(mobile)
        {
            _frames = new ViewLayer[(int)Layer.InnerLegs];
        }




        public new Mobile GameObject => (Mobile) base.GameObject;




        public override bool DrawInternal(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (GameObject.IsDisposed)
                return false;

            if (_texture == null)
            {
                _texture = new Texture2D(TextureManager.Device, 1, 1);
                _texture.SetData(new Color[1] { Color.White });
            }

            spriteBatch.GetZ();

            bool mirror = false;
            byte dir = (byte)GameObject.GetDirectionForAnimation();
            Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;


            SetupLayers(dir);

            ref var bodyFrame = ref _frames[0].Frame;


            int drawCenterY = bodyFrame.CenterY;
            int drawX;
            int drawY = /*mountOffset +*/ drawCenterY + (int)(GameObject.Offset.Z / 4 + GameObject.Position.Z * 4) - 22 - (int)(GameObject.Offset.Y - GameObject.Offset.Z - 3);

            if (IsFlipped)
                drawX = -22 + (int)GameObject.Offset.X;
            else
                drawX = -22 - (int)GameObject.Offset.X;



            //byte animGroup = 0;
            //EquipConvData? convertedItem = null;

            int yOffset = 0;
            //int mountOffset = 0;

            //int bodyWidth = 0;
            //int bodyHeight = 0;

            for (int i = 0; i < _layerCount; i++)
            {
                ref var vl = ref _frames[i];
                var frame = vl.Frame;

                if (!frame.IsValid)
                    continue;

                int x = drawX + frame.CenterX;
                int y = -drawY - (frame.Heigth + frame.CenterY) + drawCenterY;

                if (yOffset > y)
                    yOffset = y;


                Texture = TextureManager.GetOrCreateAnimTexture(frame);
                Bounds = new Rectangle(x, -y, frame.Width, frame.Heigth);
                HueVector = RenderExtentions.GetHueVector(vl.Hue);

                base.Draw(spriteBatch, position);
            }

            //for (int i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
            //{
            //    Layer layer = LayerOrder.UsedLayers[dir, i];

            //    Hue color = 0;
            //    Graphic graphic = 0;

            //    if (layer == Layer.Mount)
            //    {
            //        if (GameObject.IsHuman)
            //        {
            //            Item mount = GameObject.Equipment[(int)Layer.Mount];
            //            if (mount != null)
            //            {
            //                graphic = mount.GetMountAnimation();

            //                if (graphic < Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT)
            //                    mountOffset = Animations.DataIndex[graphic].MountedHeightOffset;

            //                animGroup = GameObject.GetGroupForAnimation(graphic);
            //                color = mount.Hue;
            //            }
            //            else
            //                continue;
            //        }
            //        else
            //            continue;
            //    }
            //    else if (layer == Layer.Invalid)
            //    {
            //        graphic = GameObject.GetGraphicForAnimation();
            //        animGroup = GameObject.GetGroupForAnimation();
            //        color = GameObject.Hue;
            //    }
            //    else
            //    {
            //        if (!GameObject.IsHuman)
            //            continue;

            //        Item item = GameObject.Equipment[(int)layer];
            //        if (item == null)
            //            continue;

            //        graphic = item.ItemData.AnimID;

            //        if (Animations.EquipConversions.TryGetValue(item.Graphic, out Dictionary<ushort, EquipConvData> map))
            //        {
            //            if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
            //            {
            //                convertedItem = data;
            //                graphic = data.Graphic;
            //            }
            //        }

            //        color = item.Hue;
            //    }


            //    sbyte animIndex = GameObject.AnimIndex;

            //    Animations.AnimID = graphic;
            //    Animations.AnimGroup = animGroup;
            //    Animations.Direction = dir;

            //    ref AnimationDirection direction = ref Animations.DataIndex[Animations.AnimID].Groups[Animations.AnimGroup].Direction[Animations.Direction];

            //    if (direction.FrameCount == 0 && !Animations.LoadDirectionGroup(ref direction))
            //        continue;

            //    int fc = direction.FrameCount;
            //    if (fc > 0 && animIndex >= fc) animIndex = 0;

            //    if (animIndex < direction.FrameCount)
            //    {
            //        ref AnimationFrame frame = ref direction.Frames[animIndex];

            //        if (frame.Pixels == null || frame.Pixels.Length <= 0)
            //            return false;


            //        int drawCenterY = frame.CenterY;
            //        int drawX;
            //        int drawY = mountOffset + drawCenterY + (int)(GameObject.Offset.Z / 4 + GameObject.Position.Z * 4) - 22 - (int)(GameObject.Offset.Y - GameObject.Offset.Z - 3);

            //        if (IsFlipped)
            //            drawX = -22 + (int)GameObject.Offset.X;
            //        else
            //            drawX = -22 - (int)GameObject.Offset.X;


            //        int x = drawX + frame.CenterX;
            //        int y = -drawY - (frame.Heigth + frame.CenterY) + drawCenterY;

            //        if (color <= 0)
            //        {
            //            if (direction.Address != direction.PatchedAddress)
            //                color = Animations.DataIndex[Animations.AnimID].Color;

            //            if (color <= 0 && convertedItem.HasValue)
            //                color = convertedItem.Value.Color;
            //        }

            //        if (yOffset > y)
            //            yOffset = y;


            //        Texture = TextureManager.GetOrCreateAnimTexture(frame);
            //        Bounds = new Rectangle(x, -y, frame.Width, frame.Heigth);
            //        HueVector = RenderExtentions.GetHueVector(color);


            //        if ((layer == Layer.Mount || !GameObject.IsHuman && layer == Layer.Invalid) && TextureWidth != Texture.Width) TextureWidth = Texture.Width;

            //        if (layer == Layer.Invalid)
            //            yOffset = y;


            //        base.Draw(spriteBatch, position);


            //        if (layer == Layer.Invalid)
            //        {
            //            bodyWidth = Texture.Width;
            //            bodyHeight = Texture.Height;
            //            //spriteBatch.DrawRectangle(_texture, new Rectangle((int)position.X + (IsFlipped ? x + 44 - Bounds.Width : -x) , (int)position.Y + y, Bounds.Width, Bounds.Height), RenderExtentions.GetHueVector(38));
            //        }
            //    }
            //}


            Vector3 overheadPosition = new Vector3
            {
                X = position.X + GameObject.Offset.X,
                Y = position.Y - (int)(GameObject.Offset.Z / 4 + GameObject.Position.Z * 4) - 22 -
                    (int)(GameObject.Offset.Y - GameObject.Offset.Z - 3) + yOffset,
                Z = position.Z
            };

            if (bodyFrame.IsValid)
            {
                yOffset = bodyFrame.Heigth + drawY - (int) GameObject.Offset.X;
            }
            else
            {
                yOffset -= -(yOffset + 44);
            }

            //if (bodyWidth > 0 && bodyHeight > 0)
            //{
            //    yOffset = bodyHeight + draw
            //}

            ////yOffset = -(yOffset + 44);

            //MessageOverHead(spriteBatch, vv);

            MessageOverHead(spriteBatch, overheadPosition, GameObject.IsMounted ? yOffset + 16 : yOffset);

            return true;
        }


        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            return !PreDraw(position) && DrawInternal(spriteBatch, position);
        }



        public override void Update(in double frameMS)
        {
            base.Update(frameMS);


            GameObject.ProcessAnimation();
        }


        private void SetupLayers(in byte dir)
        {
            _layerCount = 0;

            if (GameObject.IsHuman)
            {
                bool hasOuterTorso = GameObject.Equipment[(int) Layer.OuterTorso] != null && GameObject.Equipment[(int) Layer.OuterTorso].ItemData.AnimID != 0;

                for (int i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
                {
                    Layer layer = LayerOrder.UsedLayers[dir, i];
                    if (hasOuterTorso && (layer == Layer.InnerTorso || layer == Layer.MiddleTorso))
                        continue;

                    if (layer == Layer.Invalid)
                    {
                        AddLayer(dir, GameObject.Graphic, GameObject.Hue);
                    }
                    else
                    {
                        Item item;
                        if ( (item = GameObject.Equipment[(int) layer]) != null)
                        {
                            if (layer == Layer.Mount)
                            {
                                Item mount = GameObject.Equipment[(int)Layer.Mount];
                                if (mount != null)
                                {
                                    //if (graphic < Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                    //    mountOffset = Animations.DataIndex[graphic].MountedHeightOffset;

                                    AddLayer(dir, mount.GetMountAnimation(), mount.Hue, true);
                                }
                            }
                            else
                            {
                                if (item.ItemData.AnimID != 0)
                                {
                                    if (GameObject.IsDead && (layer == Layer.Hair || layer == Layer.FacialHair))
                                        continue;
                                    EquipConvData? convertedItem = null;
                                    Graphic graphic = item.ItemData.AnimID;
                                    Hue hue = item.Hue;

                                    if (Animations.EquipConversions.TryGetValue(item.Graphic, out Dictionary<ushort, EquipConvData> map))
                                    {
                                        if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                                        {
                                            convertedItem = data;
                                            graphic = data.Graphic;
                                        }
                                    }

                                    AddLayer(dir, graphic, hue, false, convertedItem);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                AddLayer(dir, GameObject.Graphic, GameObject.Hue);
            }
        }

        private void AddLayer(in byte dir, in Graphic graphic, Hue hue, in bool mounted = false, in EquipConvData? convertedItem = null)
        {         
            sbyte animIndex = GameObject.AnimIndex;
            byte animGroup = GameObject.GetGroupForAnimation(graphic);

            Animations.AnimID = graphic;
            Animations.AnimGroup = animGroup;
            Animations.Direction = dir;

            ref AnimationDirection direction = ref Animations.DataIndex[Animations.AnimID].Groups[Animations.AnimGroup].Direction[Animations.Direction];

            if (direction.FrameCount == 0 && !Animations.LoadDirectionGroup(ref direction))
                return;

            int fc = direction.FrameCount;
            if (fc > 0 && animIndex >= fc) animIndex = 0;

            if (animIndex < direction.FrameCount)
            {
                ref AnimationFrame frame = ref direction.Frames[animIndex];

                if (frame.Pixels == null || frame.Pixels.Length <= 0)
                    return;

                if (hue == 0)
                {
                    if (direction.Address != direction.PatchedAddress)
                        hue = Animations.DataIndex[Animations.AnimID].Color;

                    if (hue <= 0 && convertedItem.HasValue)
                        hue = convertedItem.Value.Color;
                }

                _frames[_layerCount++] = new ViewLayer()
                {
                    Hue = hue,
                    Frame = frame,
                    Graphic = graphic
                };
                
            }
        }

        struct ViewLayer
        {
            public Hue Hue;
            public AnimationFrame Frame;
            public Graphic Graphic;
        }
    }
}