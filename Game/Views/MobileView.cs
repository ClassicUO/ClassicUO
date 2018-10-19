#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class MobileView : View
    {
        private readonly ViewLayer[] _frames;
        private int _layerCount;

        public MobileView(Mobile mobile) : base(mobile)
        {
            _frames = new ViewLayer[(int)Layer.InnerLegs];
            HasShadow = true;
        } 


        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            PreDraw(position);
            return DrawInternal(spriteBatch, position, objectList);
        }

        public override bool DrawInternal(SpriteBatch3D spriteBatch, Vector3 position,
            MouseOverList objectList)
        {
            if (GameObject.IsDisposed)
                return false;

            ShadowZDepth = spriteBatch.GetZ();

            Mobile mobile = (Mobile) GameObject;

            bool mirror = false;
            byte dir = (byte) mobile.GetDirectionForAnimation();
            Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;

            int mountOffset = 0;
            SetupLayers(dir, ref mobile, ref mountOffset);

            ref TextureAnimationFrame bodyFrame = ref _frames[0].Frame;
            if (bodyFrame == null)
                return false;

            int drawCenterY = bodyFrame.CenterY;
            int drawX;
            int drawY = mountOffset + drawCenterY + (int) (mobile.Offset.Z / 4 + GameObject.Position.Z * 4) - 22 -
                        (int) (mobile.Offset.Y - mobile.Offset.Z - 3);

            if (IsFlipped)
                drawX = -22 + (int) mobile.Offset.X;
            else
                drawX = -22 - (int) mobile.Offset.X;

            Rectangle rect = new Rectangle();

            for (int i = 0; i < _layerCount; i++)
            {
                ref ViewLayer vl = ref _frames[i];
                TextureAnimationFrame frame = vl.Frame;

                if (frame.IsDisposed) continue;

                int x = drawX + frame.CenterX;
                int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY;

                Texture = frame;
                Bounds = new Rectangle(x, -y, frame.Width, frame.Height);

                if (Bounds.X < rect.X)
                    rect.X = Bounds.X;
                if (Bounds.Y < rect.Y)
                    rect.Y = Bounds.Y;
                if (Bounds.Width > rect.Width)
                    rect.Width = Bounds.Width;
                if (Bounds.Height > rect.Height)
                    rect.Height = Bounds.Height;

                //if (i == 0)
                //    rect = Bounds;

                HueVector = RenderExtentions.GetHueVector(vl.Hue, vl.IsParital, 0, false);

                base.Draw(spriteBatch, position, objectList);

                Pick(frame.ID, Bounds, position, objectList);
            }

            //Bounds = bodyFrame.Bounds;

            //int xx = IsFlipped ? (int)position.X + rect.X + 44 : -(int)position.X + rect.X;

            //BoudsStrange = new Rectangle((int) position.X + rect.X, (int) position.Y + rect.Y, rect.Width, rect.Height);

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

            //mirror = false;
            //dir = 0 & 0x7F;
            //Animations.GetAnimDirection(ref dir, ref mirror);

            //ref var direction = ref Animations.DataIndex[GameObject.Graphic].Groups[0].Direction[0];

            //if (direction.Address > 0 || direction.IsUOP)
            //{
            //    if (direction.Frames != null || direction.FrameCount > 0)
            //    {
            //        if (Animations.LoadDirectionGroup(ref direction))
            //        {
            //            centerY = direction.Frames[0].CenterY;
            //            height = direction.Frames[0].Height;
            //        }
            //        else
            //        {
            //            height = mobile.IsMounted ? 100 : 60;
            //        }
            //    }
            //    else
            //    {
            //        height = mobile.IsMounted ? 100 : 60;
            //    }
            //}
            //else
            //{
            //    height = mobile.IsMounted ? 100 : 60;
            //}

            if (GameObject.OverHeads.Count > 0)
            {
                GetAnimationDimensions(mobile, 0xFF, out int height, out int centerY);

                Vector3 overheadPosition = new Vector3
                {
                    X = position.X + mobile.Offset.X,
                    Y = position.Y - mobile.Position.Z * 4 + (mobile.Offset.Y - mobile.Offset.Z) -
                        (height + centerY + 8),
                    Z = position.Z
                };

                MessageOverHead(spriteBatch, overheadPosition, mobile.IsMounted ? 0 : -22);
            }


            return true;
        }

        private static void GetAnimationDimensions(Mobile mobile, byte frameIndex, out int height, out int centerY)
        {
            byte dir = 0 & 0x7F;
            byte animGroup = 0;
            bool mirror = false;

            Animations.GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
                frameIndex = (byte) mobile.AnimIndex;

            Animations.GetAnimationDimensions(frameIndex, mobile.Graphic, dir, animGroup, out int x, out centerY, out int w, out height);

            if (x <= 0 && centerY <= 0 && w <= 0 && height <= 0)
            {
                height = mobile.IsMounted ? 100 : 60;
            }     
        }



        private void Pick(int id, Rectangle area, Vector3 drawPosition, MouseOverList list)
        {
            int x;

            if (IsFlipped)
                x = (int) drawPosition.X + area.X + 44 - list.MousePosition.X;
            else
                x = list.MousePosition.X - (int) drawPosition.X + area.X;

            int y = list.MousePosition.Y - ((int) drawPosition.Y - area.Y);

            if (Animations.Contains(id, x, y)) list.Add(GameObject, drawPosition);
        }


        private void SetupLayers(byte dir, ref Mobile mobile, ref int mountOffset)
        {
            _layerCount = 0;


            if (mobile.IsHuman)
            {
                bool hasOuterTorso = mobile.Equipment[(int) Layer.OuterTorso] != null &&
                                     mobile.Equipment[(int) Layer.OuterTorso].ItemData.AnimID != 0;

                for (int i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
                {
                    Layer layer = LayerOrder.UsedLayers[dir, i];
                    if (hasOuterTorso && (layer == Layer.InnerTorso || layer == Layer.MiddleTorso)) continue;

                    if (layer == Layer.Invalid)
                        AddLayer(dir, GameObject.Graphic, GameObject.Hue, ref mobile);
                    else
                    {
                        Item item;
                        if ((item = mobile.Equipment[(int) layer]) != null)
                        {
                            if (layer == Layer.Mount)
                            {
                                Item mount = mobile.Equipment[(int) Layer.Mount];
                                if (mount != null)
                                {
                                    Graphic mountGraphic = item.GetMountAnimation();
                                    if (mountGraphic < Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                        mountOffset = Animations.DataIndex[mountGraphic].MountedHeightOffset;

                                    AddLayer(dir, mountGraphic, mount.Hue, ref mobile, true);
                                }
                            }
                            else
                            {
                                if (item.ItemData.AnimID != 0)
                                {
                                    if (mobile.IsDead && (layer == Layer.Hair || layer == Layer.FacialHair)) continue;

                                    EquipConvData? convertedItem = null;
                                    Graphic graphic = item.ItemData.AnimID;
                                    Hue hue = item.Hue;

                                    if (Animations.EquipConversions.TryGetValue(item.Graphic,
                                        out Dictionary<ushort, EquipConvData> map))
                                    {
                                        if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                                        {
                                            convertedItem = data;
                                            graphic = data.Graphic;
                                        }
                                    }

                                    AddLayer(dir, graphic, hue, ref mobile, false, convertedItem,
                                        TileData.IsPartialHue((long) item.ItemData.Flags));
                                }
                            }
                        }
                    }
                }
            }
            else
                AddLayer(dir, GameObject.Graphic, mobile.IsDead ? (Hue) 0x0386 : GameObject.Hue, ref mobile);
        }

        private void AddLayer(byte dir, Graphic graphic, Hue hue, ref Mobile mobile, bool mounted = false,
            EquipConvData? convertedItem = null, bool ispartial = false)
        {
            byte animGroup = Mobile.GetGroupForAnimation(mobile, graphic);


            sbyte animIndex = GameObject.AnimIndex;

            Animations.AnimID = graphic;
            Animations.AnimGroup = animGroup;
            Animations.Direction = dir;

            ref AnimationDirection direction = ref Animations.DataIndex[Animations.AnimID].Groups[Animations.AnimGroup]
                .Direction[Animations.Direction];

            if ((direction.FrameCount == 0 || direction.Frames == null) && !Animations.LoadDirectionGroup(ref direction))
                return;

            direction.LastAccessTime = CoreGame.Ticks;

            int fc = direction.FrameCount;
            if (fc > 0 && animIndex >= fc) animIndex = 0;

            if (animIndex < direction.FrameCount)
            {
                ref TextureAnimationFrame frame = ref direction.Frames[animIndex];

                if (frame == null || frame.IsDisposed)
                {
                    if (!Animations.LoadDirectionGroup(ref direction))
                        return;

                    frame = ref direction.Frames[animIndex];
                    if (frame == null)
                        return;
                }

                if (hue == 0)
                {
                    if (direction.Address != direction.PatchedAddress)
                        hue = Animations.DataIndex[Animations.AnimID].Color;

                    if (hue <= 0 && convertedItem.HasValue) hue = convertedItem.Value.Color;
                }

                _frames[_layerCount++] = new ViewLayer
                {
                    Hue = hue,
                    Frame = frame,
                    Graphic = graphic,
                    IsParital = ispartial
                };
            }
        }

        private struct ViewLayer
        {
            public Hue Hue;
            public TextureAnimationFrame Frame;
            public Graphic Graphic;
            public bool IsParital;
        }
    }
}