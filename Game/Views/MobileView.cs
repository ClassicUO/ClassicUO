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

using System;
using System.Collections.Generic;

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.GameObjects.Managers;
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
            _frames = new ViewLayer[(int) Layer.Legs];
            HasShadow = true;
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position, MouseOverList objectList)
        {
            if (GameObject.IsDisposed)
                return false;

            Mobile mobile = (Mobile)GameObject;
            bool mirror = false;
            byte dir = (byte)mobile.GetDirectionForAnimation();
            Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;
            int mountOffset = 0;
            SetupLayers(dir, ref mobile, ref mountOffset);
            ref AnimationFrameTexture bodyFrame = ref _frames[0].Frame;

            if (bodyFrame == null)
                return false;
            int drawCenterY = bodyFrame.CenterY;
            int drawX;
            int drawY = mountOffset + drawCenterY + (int)(mobile.Offset.Z / 4) - 22 - (int)(mobile.Offset.Y - mobile.Offset.Z - 3);

            if (IsFlipped)
                drawX = -22 + (int)mobile.Offset.X;
            else
                drawX = -22 - (int)mobile.Offset.X;

            FrameInfo = FrameInfo.Empty;

            for (int i = 0; i < _layerCount; i++)
            {
                ViewLayer vl = _frames[i];
                AnimationFrameTexture frame = vl.Frame;

                if (frame.IsDisposed) continue;
                int x = drawX + frame.CenterX;
                int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY - vl.OffsetY;

                int yy = -(frame.Height + frame.CenterY + 3);
                int xx = -frame.CenterX;

                if (mirror)
                    xx = -(frame.Width - frame.CenterX);

                if (xx < FrameInfo.X)
                    FrameInfo.X = xx;

                if (yy < FrameInfo.Y)
                    FrameInfo.Y = yy;

                if (FrameInfo.EndX < xx + frame.Width)
                    FrameInfo.EndX = xx + frame.Width;

                if (FrameInfo.EndY < yy + frame.Height)
                    FrameInfo.EndY = yy + frame.Height;

                Texture = frame;
                Bounds = new Rectangle(x, -y, frame.Width, frame.Height);
                HueVector = ShaderHuesTraslator.GetHueVector(mobile.IsHidden ? 0x038E : vl.Hue, vl.IsParital, 0, false);
                base.Draw(spriteBatch, position, objectList);
                Pick(frame, Bounds, position, objectList);
            }

            FrameInfo.OffsetX = Math.Abs(FrameInfo.X);
            FrameInfo.OffsetY = Math.Abs(FrameInfo.Y);
            FrameInfo.Width = FrameInfo.OffsetX + FrameInfo.EndX;
            FrameInfo.Height = FrameInfo.OffsetY + FrameInfo.EndY;


            int height = 0;
            int centerY = 0;

            if (GameObject.OverHeads.Count > 0)
            {
                GetAnimationDimensions(mobile, 0xFF, out height, out centerY);

                Vector3 overheadPosition = new Vector3
                {
                    X = position.X + mobile.Offset.X,
                    Y = position.Y + (mobile.Offset.Y - mobile.Offset.Z) - (height + centerY + 8),
                    Z = position.Z
                };
                MessageOverHead(spriteBatch, overheadPosition, mobile.IsMounted ? 0 : -22);
            }

            if (mobile.DamageList.Count > 0)
            {
                if (height == 0 && centerY == 0)
                    GetAnimationDimensions(mobile, 0xFF, out height, out centerY);

                Vector3 damagePosition = new Vector3
                {
                    X = position.X + mobile.Offset.X,
                    Y = position.Y + (mobile.Offset.Y - mobile.Offset.Z) - (height + centerY + 8),
                    Z = position.Z
                };
                DamageOverhead(mobile, spriteBatch, damagePosition, mobile.IsMounted ? 0 : -22);
            }

            return true;
        }

        private void DamageOverhead(Mobile mobile, SpriteBatch3D spriteBatch, Vector3 position, int offY)
        {
            for (int i = 0; i < mobile.DamageList.Count; i++)
            {
                DamageOverhead dmg = mobile.DamageList[i];
                View v = dmg.View;
                v.Bounds.X = v.Texture.Width / 2 - 22;
                v.Bounds.Y = offY + v.Texture.Height - dmg.OffsetY;
                v.Bounds.Width = v.Texture.Width;
                v.Bounds.Height = v.Texture.Height;
                OverheadManager.AddDamage(v, position);
                offY += v.Texture.Height;
            }
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
            if (x == 0 && centerY == 0 && w == 0 && height == 0) height = mobile.IsMounted ? 100 : 60;
        }

        private void Pick(SpriteTexture texture, Rectangle area, Vector3 drawPosition, MouseOverList list)
        {
            int x;

            if (IsFlipped)
                x = (int) drawPosition.X + area.X + 44 - list.MousePosition.X;
            else
                x = list.MousePosition.X - (int) drawPosition.X + area.X;
            int y = list.MousePosition.Y - ((int) drawPosition.Y - area.Y);
            if (texture.Contains(x, y)) list.Add(GameObject, drawPosition);
            //if (Animations.Contains(id, x, y)) list.Add(GameObject, drawPosition);
        }

        private void SetupLayers(byte dir, ref Mobile mobile, ref int mountOffset)
        {
            _layerCount = 0;

            if (mobile.IsHuman)
            {
                for (int i = 0; i < LayerOrder.USED_LAYER_COUNT; i++)
                {
                    Layer layer = LayerOrder.UsedLayers[dir, i];

                    if (IsCovered(mobile, layer))
                        continue;

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
                                    AddLayer(dir, mountGraphic, mount.Hue, ref mobile, true, offsetY: mountOffset);
                                }
                            }
                            else
                            {
                                if (item.ItemData.AnimID != 0)
                                {
                                    if (mobile.IsDead && (layer == Layer.Hair || layer == Layer.Beard)) continue;
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

                                    AddLayer(dir, graphic, hue, ref mobile, false, convertedItem, TileData.IsPartialHue( item.ItemData.Flags));
                                }
                            }
                        }
                    }
                }
            }
            else
                AddLayer(dir, GameObject.Graphic, mobile.IsDead ? (Hue) 0x0386 : GameObject.Hue, ref mobile);
        }

        private void AddLayer(byte dir, Graphic graphic, Hue hue, ref Mobile mobile, bool mounted = false, EquipConvData? convertedItem = null, bool ispartial = false, int offsetY = 0)
        {
            byte animGroup = Mobile.GetGroupForAnimation(mobile, graphic);
            sbyte animIndex = GameObject.AnimIndex;
            Animations.AnimID = graphic;
            Animations.AnimGroup = animGroup;
            Animations.Direction = dir;
            ref AnimationDirection direction = ref Animations.DataIndex[Animations.AnimID].Groups[Animations.AnimGroup].Direction[Animations.Direction];

            if ((direction.FrameCount == 0 || direction.Frames == null) && !Animations.LoadDirectionGroup(ref direction))
                return;
            direction.LastAccessTime = CoreGame.Ticks;
            int fc = direction.FrameCount;
            if (fc > 0 && animIndex >= fc) animIndex = 0;

            if (animIndex < direction.FrameCount)
            {
                ref AnimationFrameTexture frame = ref direction.Frames[animIndex];

                if (frame == null || frame.IsDisposed)
                {
                    if (!Animations.LoadDirectionGroup(ref direction))
                    {
                        Log.Message(LogTypes.Panic, $"graphic: {graphic}\tgroup: {animGroup}");

                        return;
                    }

                    frame = ref direction.Frames[animIndex];

                    if (frame == null)
                    {
                        Log.Message(LogTypes.Panic, $"graphic: {graphic}\tgroup: {animGroup}\tframe missed: {animIndex}");

                        return;
                    }
                }

                if (hue == 0)
                {
                    if (direction.Address != direction.PatchedAddress)
                        hue = Animations.DataIndex[Animations.AnimID].Color;
                    if (hue == 0 && convertedItem.HasValue) hue = convertedItem.Value.Color;
                }

                _frames[_layerCount++] = new ViewLayer
                {
                    Hue = hue,
                    Frame = frame,
                    Graphic = graphic,
                    IsParital = ispartial,
                    OffsetY = offsetY
                };
            }
        }

        public static bool IsCovered(Mobile mobile, Layer layer)
        {
            switch (layer)
            {
                case Layer.Shoes:
                    Item pants = mobile.Equipment[(int) Layer.Pants];
                    Item robe;

                    if (mobile.Equipment[(int) Layer.Legs] != null || pants != null && pants.Graphic == 0x1411)
                        return true;
                    else
                    {
                        robe = mobile.Equipment[(int) Layer.Robe];

                        if (pants != null && (pants.Graphic == 0x0513 || pants.Graphic == 0x0514) || robe != null && robe.Graphic == 0x0504)
                            return true;
                    }

                    break;
                case Layer.Pants:
                    Item skirt;
                    robe = mobile.Equipment[(int) Layer.Robe];
                    pants = mobile.Equipment[(int) Layer.Pants];

                    if (mobile.Equipment[(int) Layer.Legs] != null || robe != null && robe.Graphic == 0x0504)
                        return true;

                    if (pants != null && (pants.Graphic == 0x01EB || pants.Graphic == 0x03E5 || pants.Graphic == 0x03eB))
                    {
                        skirt = mobile.Equipment[(int) Layer.Skirt];

                        if (skirt != null && skirt.Graphic != 0x01C7 && skirt.Graphic != 0x01E4)
                            return true;

                        if (robe != null && robe.Graphic != 0x0229 && (robe.Graphic <= 0x04E7 || robe.Graphic > 0x04EB))
                            return true;
                    }

                    break;
                case Layer.Tunic:
                    robe = mobile.Equipment[(int) Layer.Robe];
                    Item tunic = mobile.Equipment[(int) Layer.Tunic];

                    if (robe != null && robe.Graphic != 0)
                        return true;
                    else if (tunic != null && tunic.Graphic == 0x0238)
                        return robe != null && robe.Graphic != 0x9985 && robe.Graphic != 0x9986;

                    break;
                case Layer.Torso:
                    robe = mobile.Equipment[(int) Layer.Robe];

                    if (robe != null && robe.Graphic != 0 && robe.Graphic != 0x9985 && robe.Graphic != 0x9986)
                        return true;
                    else
                    {
                        tunic = mobile.Equipment[(int) Layer.Tunic];
                        Item torso = mobile.Equipment[(int) Layer.Torso];

                        if (tunic != null && tunic.Graphic != 0x1541 && tunic.Graphic != 0x1542)
                            return true;

                        if (torso != null && (torso.Graphic == 0x782A || torso.Graphic == 0x782B))
                            return true;
                    }

                    break;
                case Layer.Arms:
                    robe = mobile.Equipment[(int) Layer.Robe];

                    return robe != null && robe.Graphic != 0 && robe.Graphic != 0x9985 && robe.Graphic != 0x9986;
                case Layer.Helmet:
                case Layer.Hair:
                    robe = mobile.Equipment[(int) Layer.Robe];

                    if (robe != null)
                    {
                        if (robe.Graphic > 0x3173)
                        {
                            if (robe.Graphic == 0x4B9D || robe.Graphic == 0x7816)
                                return true;
                        }
                        else
                        {
                            if (robe.Graphic <= 0x2687)
                            {
                                if (robe.Graphic < 0x2683)
                                    return robe.Graphic >= 0x204E && robe.Graphic <= 0x204F;

                                return true;
                            }

                            if (robe.Graphic == 0x2FB9 || robe.Graphic == 0x3173)
                                return true;
                        }
                    }

                    break;
                case Layer.Skirt:
                    skirt = mobile.Equipment[(int) Layer.Skirt];

                    break;
            }

            return false;
        }

        private struct ViewLayer
        {
            public Hue Hue;
            public AnimationFrameTexture Frame;
            public Graphic Graphic;
            public bool IsParital;
            public int OffsetY;
        }
    }
}