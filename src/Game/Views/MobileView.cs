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
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Views
{
    internal class MobileView : View
    {
        private readonly ViewLayer[] _frames;
        private int _layerCount;

        public MobileView(Mobile mobile) : base(mobile)
        {
            _frames = new ViewLayer[(int) Layer.Legs];
            HasShadow = true;
        }

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (GameObject.IsDisposed)
                return false;

            Mobile mobile = (Mobile)GameObject;

            bool mirror = false;
            byte dir = (byte)mobile.GetDirectionForAnimation();
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;
            SetupLayers(dir, mobile, out int mountOffset);



            AnimationFrameTexture bodyFrame = FileManager.Animations.GetTexture(_frames[0].Hash);

            if (bodyFrame == null)
                return false;
            int drawCenterY = bodyFrame.CenterY;
            int drawX;
            int drawY = mountOffset + drawCenterY + (int)(mobile.Offset.Z / 4) - 22 - (int)(mobile.Offset.Y - mobile.Offset.Z - 3);

            if (IsFlipped)
                drawX = -22 + (int)mobile.Offset.X;
            else
                drawX = -22 - (int)mobile.Offset.X;

            FrameInfo = Rectangle.Empty;
            Rectangle rect = Rectangle.Empty;

            Hue hue = 0, targetColor = 0;
            if (Engine.Profile.Current.HighlightMobilesByFlags)
            {
                if (mobile.IsPoisoned)
                    hue = 0x0044;

                if (mobile.IsParalyzed)
                    hue = 0x014C;

                if (mobile.NotorietyFlag != NotorietyFlag.Invulnerable && mobile.IsYellowHits)
                    hue = 0x0030;
            }

            bool isAttack = mobile.Serial == World.LastAttack;
            bool isUnderMouse = IsSelected && (TargetManager.IsTargeting || World.Player.InWarMode);
            bool needHpLine = false;

            if (mobile != World.Player && (isAttack || isUnderMouse || TargetManager.LastGameObject == mobile))
            {
                targetColor = Notoriety.GetHue(mobile.NotorietyFlag);

                if (isAttack || mobile == TargetManager.LastGameObject)
                {
                    if (TargetLineGump.TTargetLineGump?.Mobile != mobile)
                    {
                        if (TargetLineGump.TTargetLineGump == null || TargetLineGump.TTargetLineGump.IsDisposed)
                        {
                            TargetLineGump.TTargetLineGump = new TargetLineGump();
                            Engine.UI.Add(TargetLineGump.TTargetLineGump);
                        }
                        else
                        {
                            TargetLineGump.TTargetLineGump.SetMobile(mobile);
                        }
                    }

                    needHpLine = true;
                }

                if (isAttack || isUnderMouse)
                    hue = targetColor;
            }

            for (int i = 0; i < _layerCount; i++)
            {
                ViewLayer vl = _frames[i];
                AnimationFrameTexture frame = FileManager.Animations.GetTexture(vl.Hash);

                if (frame.IsDisposed) continue;
                int x = drawX + frame.CenterX;
                int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY - vl.OffsetY;

                int yy = -(frame.Height + frame.CenterY + 3);
                int xx = -frame.CenterX;

                if (mirror)
                    xx = -(frame.Width - frame.CenterX);

                if (xx < rect.X)
                    rect.X = xx;

                if (yy < rect.Y)
                    rect.Y = yy;

                if (rect.Width < xx + frame.Width)
                    rect.Width = xx + frame.Width;

                if (rect.Height < yy + frame.Height)
                    rect.Height = yy + frame.Height;

                Texture = frame;
                Bounds = new Rectangle(x, -y, frame.Width, frame.Height);

                if (Engine.Profile.Current.NoColorObjectsOutOfRange && GameObject.Distance > World.ViewRange)
                    HueVector = new Vector3(0x038E, 1, HueVector.Z);
                else
                    HueVector = ShaderHuesTraslator.GetHueVector(mobile.IsHidden ? 0x038E : hue == 0 ? vl.Hue : hue, vl.IsParital, 0, false);
                base.Draw(batcher, position, objectList);
                Pick(frame, Bounds, position, objectList);
            }

            FrameInfo.X = Math.Abs(rect.X);
            FrameInfo.Y = Math.Abs(rect.Y);
            FrameInfo.Width = FrameInfo.X + rect.Width;
            FrameInfo.Height = FrameInfo.Y + rect.Height;



            MessageOverHead(batcher, position, mobile.IsMounted ? 0 : -22);

            if (needHpLine)
            {
                position.X += Engine.Profile.Current.GameWindowPosition.X + 9;
                position.Y += Engine.Profile.Current.GameWindowPosition.Y + 30;

                TargetLineGump.TTargetLineGump.X = (int)(position.X /*+ 22*/ + GameObject.Offset.X);
                TargetLineGump.TTargetLineGump.Y = (int)(position.Y /*+ 22 + (mobile.IsMounted ? 22 : 0) */+ GameObject.Offset.Y - GameObject.Offset.Z - 3);
                TargetLineGump.TTargetLineGump.BackgroudHue = targetColor;
                
                if (mobile.IsPoisoned)
                    TargetLineGump.TTargetLineGump.HpHue = 63;
                else if (mobile.IsYellowHits)
                    TargetLineGump.TTargetLineGump.HpHue = 53;

                else
                    TargetLineGump.TTargetLineGump.HpHue = 90;
            }

            //if (_edge == null)
            //{
            //    _edge = new Texture2D(batcher.GraphicsDevice, 1, 1);
            //    _edge.SetData(new Color[] { Color.LightBlue });
            //}

            //batcher.DrawRectangle(_edge, GetOnScreenRectangle(), Vector3.Zero);
            Engine.DebugInfo.MobilesRendered++;
            return true;
        }
        //private static Texture2D _edge;



        private void Pick(SpriteTexture texture, Rectangle area, Vector3 drawPosition, MouseOverList list)
        {
            int x;

            if (IsFlipped)
                x = (int) drawPosition.X + area.X + 44 - list.MousePosition.X;
            else
                x = list.MousePosition.X - (int) drawPosition.X + area.X;
            int y = list.MousePosition.Y - ((int) drawPosition.Y - area.Y);
            if (texture.Contains(x, y)) list.Add(GameObject, drawPosition);
        }

        private void SetupLayers(byte dir, Mobile mobile, out int mountOffset)
        {
            _layerCount = 0;
            mountOffset = 0;

            if (mobile.IsHuman)
            {
                for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
                {
                    Layer layer = LayerOrder.UsedLayers[dir, i];

                    if (IsCovered(mobile, layer))
                        continue;

                    if (layer == Layer.Invalid)
                        AddLayer(dir, mobile.GetGraphicForAnimation(), GameObject.Hue, mobile);
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
                                    Graphic mountGraphic = item.GetGraphicForAnimation();

                                    if (mountGraphic < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                        mountOffset = FileManager.Animations.DataIndex[mountGraphic].MountedHeightOffset;
                                    AddLayer(dir, mountGraphic, mount.Hue, mobile, true, offsetY: mountOffset);
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

                                    if (FileManager.Animations.EquipConversions.TryGetValue(item.Graphic, out Dictionary<ushort, EquipConvData> map))
                                    {
                                        if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                                        {
                                            convertedItem = data;
                                            graphic = data.Graphic;
                                        }
                                    }

                                    AddLayer(dir, graphic, hue, mobile, false, convertedItem, item.ItemData.IsPartialHue);
                                }
                            }
                        }
                    }
                }
            }
            else
                AddLayer(dir, GameObject.Graphic, mobile.IsDead ? (Hue) 0x0386 : GameObject.Hue, mobile);
        }

        private void AddLayer(byte dir, Graphic graphic, Hue hue, Mobile mobile, bool mounted = false, EquipConvData? convertedItem = null, bool ispartial = false, int offsetY = 0)
        {
            byte animGroup = Mobile.GetGroupForAnimation(mobile, graphic);
            sbyte animIndex = GameObject.AnimIndex;
            FileManager.Animations.AnimID = graphic;
            FileManager.Animations.AnimGroup = animGroup;
            FileManager.Animations.Direction = dir;
            ref AnimationDirection direction = ref FileManager.Animations.DataIndex[FileManager.Animations.AnimID].Groups[FileManager.Animations.AnimGroup].Direction[FileManager.Animations.Direction];

            if ((direction.FrameCount == 0 || direction.FramesHashes == null) && !FileManager.Animations.LoadDirectionGroup(ref direction))
                return;
            direction.LastAccessTime = Engine.Ticks;
            int fc = direction.FrameCount;
            if (fc > 0 && animIndex >= fc) animIndex = 0;

            if (animIndex < direction.FrameCount)
            {
                uint hash = direction.FramesHashes[animIndex];

                if (hash == 0)
                    return;

                if (hue == 0)
                {
                    if (direction.Address != direction.PatchedAddress)
                        hue = FileManager.Animations.DataIndex[FileManager.Animations.AnimID].Color;
                    if (hue == 0 && convertedItem.HasValue) hue = convertedItem.Value.Color;
                }

                _frames[_layerCount++] = new ViewLayer(graphic, hue, hash, ispartial, offsetY);
            }
        }

        internal static bool IsCovered(Mobile mobile, Layer layer)
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

        private readonly struct ViewLayer
        {
            public ViewLayer(Graphic graphic, Hue hue, uint frame, bool partial, int offsetY)
            {
                Graphic = graphic;
                Hue = hue;
                Hash = frame;
                IsParital = partial;
                OffsetY = offsetY;
            }

            public readonly Graphic Graphic;
            public readonly Hue Hue;
            public readonly uint Hash;
            public readonly bool IsParital;
            public readonly int OffsetY;
        }
    }
}