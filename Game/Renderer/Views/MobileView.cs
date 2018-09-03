using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ClassicUO.Game.Renderer.Views
{
    public class MobileView : View
    {
        private readonly ViewLayer[] _frames;
        private int _layerCount;

        private static Texture2D _texture;



        public MobileView(in Mobile mobile) : base(mobile)
        {
            _frames = new ViewLayer[(int)Layer.InnerLegs];
        }




        public new Mobile GameObject => (Mobile)base.GameObject;




        public override bool DrawInternal(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (GameObject.IsDisposed)
            {
                return false;
            }

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
            {
                drawX = -22 + (int)(GameObject.Offset.X);
            }
            else
            {
                drawX = -22 - (int)(GameObject.Offset.X);
            }

            int yOffset = 0;

            for (int i = 0; i < _layerCount; i++)
            {
                ref var vl = ref _frames[i];
                var frame = vl.Frame;

                if (!frame.IsValid)
                {
                    continue;
                }

                int x = drawX + frame.CenterX;
                int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY;

                if (yOffset > y)
                {
                    yOffset = y;
                }

                Texture = TextureManager.GetOrCreateAnimTexture(frame);
                Bounds = new Rectangle(x, -y, frame.Width, frame.Height);
                HueVector = RenderExtentions.GetHueVector(vl.Hue);

                base.Draw(spriteBatch, position);
            }


            //spriteBatch.DrawRectangle(_texture, 
            //    new Rectangle
            //    (
            //        (int)position.X + (IsFlipped ? drawX + bodyFrame.CenterX + 44 - bodyFrame.Width : -(drawX + bodyFrame.CenterX)), 
            //        (int)position.Y - (drawY + (bodyFrame.Height + bodyFrame.CenterY)), 
            //        bodyFrame.Width, 
            //        bodyFrame.Height
            //    ), 

            //    RenderExtentions.GetHueVector(38));

            //int xx = drawX + bodyFrame.CenterX;
            //int yy = -drawY - (bodyFrame.Height + bodyFrame.CenterY) + drawCenterY;

            //spriteBatch.DrawRectangle(_texture,
            //    new Rectangle
            //    (
            //        (int)position.X - xx,
            //        (int)position.Y + yy - (GameObject.IsMounted ? 16 : 0),
            //        bodyFrame.Width,
            //        bodyFrame.Height + (GameObject.IsMounted ? 16 : 0)
            //    ),

            //    RenderExtentions.GetHueVector(38));

            Vector3 overheadPosition = new Vector3
            {
                X = position.X + GameObject.Offset.X,
                Y = position.Y - (int)(GameObject.Offset.Z / 4 + GameObject.Position.Z * 4),
                Z = position.Z
            };

            if (bodyFrame.IsValid)
            {
                yOffset = bodyFrame.Height + drawY - (int)(GameObject.Offset.Z / 4 + GameObject.Position.Z * 4);
            }
            else
            {
                yOffset -= -(yOffset + 44);
            }

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
                bool hasOuterTorso = GameObject.Equipment[(int)Layer.OuterTorso] != null && GameObject.Equipment[(int)Layer.OuterTorso].ItemData.AnimID != 0;

                for (int i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
                {
                    Layer layer = LayerOrder.UsedLayers[dir, i];
                    if (hasOuterTorso && (layer == Layer.InnerTorso || layer == Layer.MiddleTorso))
                    {
                        continue;
                    }

                    if (layer == Layer.Invalid)
                    {
                        AddLayer(dir, GameObject.Graphic, GameObject.Hue);
                    }
                    else
                    {
                        Item item;
                        if ((item = GameObject.Equipment[(int)layer]) != null)
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
                                    {
                                        continue;
                                    }

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
            {
                return;
            }

            int fc = direction.FrameCount;
            if (fc > 0 && animIndex >= fc)
            {
                animIndex = 0;
            }

            if (animIndex < direction.FrameCount)
            {
                ref AnimationFrame frame = ref direction.Frames[animIndex];

                if ((frame.Pixels == null || frame.Pixels.Length <= 0))
                {
                    if (!Animations.LoadDirectionGroup(ref direction))
                    {
                        return;
                    }
                }

                if (hue == 0)
                {
                    if (direction.Address != direction.PatchedAddress)
                    {
                        hue = Animations.DataIndex[Animations.AnimID].Color;
                    }

                    if (hue <= 0 && convertedItem.HasValue)
                    {
                        hue = convertedItem.Value.Color;
                    }
                }

                _frames[_layerCount++] = new ViewLayer()
                {
                    Hue = hue,
                    Frame = frame,
                    Graphic = graphic
                };

                TextureWidth = frame.Width;
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