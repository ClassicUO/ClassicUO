#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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

namespace ClassicUO.Game.GameObjects
{
    internal partial class Mobile
    {
        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (IsDisposed)
                return false;

            bool mirror = false;
            byte dir = (byte)GetDirectionForAnimation();
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;

            if (Graphic == 0)
                return false;

            /*if (_frames[0].IsSitting)
            {
                int x1 = 0, y1 = 0;
                FileManager.Animations.FixSittingDirection(ref dir, ref mirror, ref x1, ref y1);
            }*/

            FrameInfo = Rectangle.Empty;
            Rectangle rect = Rectangle.Empty;

            Hue hue = 0, targetColor = 0;
            if (Engine.Profile.Current.HighlightMobilesByFlags)
            {
                if (IsPoisoned)
                    hue = 0x0044;

                if (IsParalyzed)
                    hue = 0x014C;

                if (NotorietyFlag != NotorietyFlag.Invulnerable && IsYellowHits)
                    hue = 0x0030;
            }

            bool isAttack = Serial == World.LastAttack;
            bool isUnderMouse = IsSelected && (TargetManager.IsTargeting || World.Player.InWarMode);
            bool needHpLine = false;

            if (this != World.Player && (isAttack || isUnderMouse || TargetManager.LastGameObject == Serial))
            {
                targetColor = Notoriety.GetHue(NotorietyFlag);

                if (isAttack || this == TargetManager.LastGameObject)
                {
                    Engine.UI.SetTargetLineGump(this);
                    needHpLine = true;
                }

                if (isAttack || isUnderMouse)
                    hue = targetColor;
            }

            DrawBody(batcher, position, objectList, dir, out int drawX, out int drawY, out int drawCenterY, ref rect, ref mirror, hue, out int mountHeight);

            if (IsHuman)
                DrawEquipment(batcher, position, objectList, dir, drawX, drawY, drawCenterY, ref rect, ref mirror, hue, ref mountHeight);

            FrameInfo.X = Math.Abs(rect.X);
            FrameInfo.Y = Math.Abs(rect.Y);
            FrameInfo.Width = FrameInfo.X + rect.Width;
            FrameInfo.Height = FrameInfo.Y + rect.Height;


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

        private void DrawBody(Batcher2D batcher, Vector3 position, MouseOverList objecList, byte dir, out int drawX, out int drawY, out int drawCenterY, ref Rectangle rect, ref bool mirror, Hue hue, out int mountHeight)
        {
            Graphic graphic = GetGraphicForAnimation();
            byte animGroup = Mobile.GetGroupForAnimation(this, graphic);
            sbyte animIndex = AnimIndex;
            mountHeight = 0;
            drawX = drawY = drawCenterY = 0;


            FileManager.Animations.AnimID = graphic;
            FileManager.Animations.AnimGroup = animGroup;
            FileManager.Animations.Direction = dir;

            ref AnimationDirection direction = ref FileManager.Animations.DataIndex[FileManager.Animations.AnimID].Groups[FileManager.Animations.AnimGroup].Direction[FileManager.Animations.Direction];

            if (direction.IsUOP)
                direction = ref FileManager.Animations.UOPDataIndex[FileManager.Animations.AnimID].Groups[FileManager.Animations.AnimGroup].Direction[FileManager.Animations.Direction];

            if ((direction.FrameCount == 0 || direction.FramesHashes == null) && !FileManager.Animations.LoadDirectionGroup(ref direction))
                return;

            direction.LastAccessTime = Engine.Ticks;
            int fc = direction.FrameCount;
            if (fc > 0 && animIndex >= fc)
                animIndex = 0;

            if (animIndex < direction.FrameCount)
            {
                var hash = direction.FramesHashes[animIndex];

                if (hash == null)
                    return;


                if (hue == 0)
                {
                    if (direction.Address != direction.PatchedAddress)
                        hue = FileManager.Animations.DataIndex[FileManager.Animations.AnimID].Color;
                }

                AnimationFrameTexture frame = direction.FramesHashes[animIndex];

                if (frame.IsDisposed)
                    return;

                bool hasmount = false;
                if (HasEquipment && IsHuman)
                {
                    Item mount = Equipment[(int)Layer.Mount];

                    if (mount != null)
                    {
                        mountHeight = FileManager.Animations.DataIndex[mount.GetGraphicForAnimation()].MountedHeightOffset;
                        hasmount = true;
                    }
                }

                drawCenterY = frame.CenterY;
                drawY = mountHeight + drawCenterY + (int)(Offset.Z / 4) - 22 - (int)(Offset.Y - Offset.Z - 3);

                if (IsFlipped)
                    drawX = -22 + (int)Offset.X;
                else
                    drawX = -22 - (int)Offset.X;


                if (hasmount)
                    DrawLayer(batcher, position, objecList, dir, drawX, drawY, drawCenterY, Layer.Mount, ref rect, ref mirror, hue, ref mountHeight);


                int x = drawX + frame.CenterX;
                int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY - mountHeight;

                int yy = -(frame.Height + frame.CenterY + 3);
                int xx = -frame.CenterX;

                if (IsFlipped)
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

                if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
                    HueVector = new Vector3(Constants.OUT_RANGE_COLOR, 1, HueVector.Z);
                else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
                    HueVector = new Vector3(Constants.DEAD_RANGE_COLOR, 1, HueVector.Z);
                else
                {
                    if (IsHuman && IsHidden)
                        hue = 0x038E;
                    else if (!IsHuman && IsDead)
                        hue = 0x0386;
 
                    HueVector = ShaderHuesTraslator.GetHueVector(hue == 0 ? Hue : hue, hue != 0 && IsHuman, 0, false);
                }

                base.Draw(batcher, position, objecList);
                Pick(frame, Bounds, position, objecList);
            }
        }

        private void DrawEquipment(Batcher2D batcher, Vector3 position, MouseOverList objectList, byte dir, int drawX, int drawY, int drawCenterY, ref Rectangle rect, ref bool mirror, Hue hue, ref int mountHeight)
        {
            for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[dir, i];
              
                DrawLayer(batcher, position, objectList, dir, drawX, drawY, drawCenterY, layer, ref rect, ref mirror, hue, ref mountHeight);
            }
        }

        private void DrawLayer(Batcher2D batcher, Vector3 position, MouseOverList objectList, byte dir, int drawX, int drawY, int drawCenterY, Layer layer, ref Rectangle rect, ref bool mirror, Hue hue, ref int mountHeight)
        {
            if (IsCovered(this, layer))
                return;

            Item item = Equipment[(int)layer];

            if (item == null)
                return;

            if (IsDead && (layer == Layer.Hair || layer == Layer.Beard))
                return;

            EquipConvData? convertedItem = null;

            if (hue == 0)
                hue = item.Hue;

            Graphic graphic;

            if (layer == Layer.Mount)
            {
                graphic = item.GetGraphicForAnimation();
                //mountHeight = FileManager.Animations.DataIndex[graphic].MountedHeightOffset;
            }
            else if (item.ItemData.AnimID != 0)
            {
                graphic = item.ItemData.AnimID;

                if (FileManager.Animations.EquipConversions.TryGetValue(Graphic, out Dictionary<ushort, EquipConvData> map))
                {
                    if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                    {
                        convertedItem = data;
                        graphic = data.Graphic;
                    }
                }
            }
            else
                return;


            bool isequip = layer != Layer.Mount;
            byte animGroup = Mobile.GetGroupForAnimation(this, graphic, isequip);
            sbyte animIndex = AnimIndex;


            FileManager.Animations.AnimID = graphic;
            FileManager.Animations.AnimGroup = animGroup;
            FileManager.Animations.Direction = dir;

            ref AnimationDirection direction = ref FileManager.Animations.DataIndex[FileManager.Animations.AnimID].Groups[FileManager.Animations.AnimGroup].Direction[FileManager.Animations.Direction];

            if (direction.IsUOP && !isequip)
                direction = ref FileManager.Animations.UOPDataIndex[FileManager.Animations.AnimID].Groups[FileManager.Animations.AnimGroup].Direction[FileManager.Animations.Direction];

            if ((direction.FrameCount == 0 || direction.FramesHashes == null) && !FileManager.Animations.LoadDirectionGroup(ref direction, isequip))
                return;

            direction.LastAccessTime = Engine.Ticks;
            int fc = direction.FrameCount;
            if (fc > 0 && animIndex >= fc)
                animIndex = 0;

            if (animIndex < direction.FrameCount)
            {
                var hash = direction.FramesHashes[animIndex];

                if (hash == null)
                    return;

                if (hue == 0)
                {
                    if (direction.Address != direction.PatchedAddress)
                        hue = FileManager.Animations.DataIndex[FileManager.Animations.AnimID].Color;
                    if (hue == 0 && convertedItem.HasValue) hue = convertedItem.Value.Color;
                }

                AnimationFrameTexture frame = direction.FramesHashes[animIndex];

                if (frame.IsDisposed)
                    return;

                int x = drawX + frame.CenterX;
                int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY - mountHeight;

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

                if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
                    HueVector = new Vector3(Constants.OUT_RANGE_COLOR, 1, HueVector.Z);
                else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
                    HueVector = new Vector3(Constants.DEAD_RANGE_COLOR, 1, HueVector.Z);
                else
                    HueVector = ShaderHuesTraslator.GetHueVector(IsHidden ? 0x038E : hue, item.ItemData.IsPartialHue, 0, false);

                base.Draw(batcher, position, objectList);
                Pick(frame, Bounds, position, objectList);
            }

        }

        private void Pick(SpriteTexture texture, Rectangle area, Vector3 drawPosition, MouseOverList list)
        {
            int x;

            if (IsFlipped)
                x = (int) drawPosition.X + area.X + 44 - list.MousePosition.X;
            else
                x = list.MousePosition.X - (int) drawPosition.X + area.X;
            int y = list.MousePosition.Y - ((int) drawPosition.Y - area.Y);
            if (texture.Contains(x, y)) list.Add(this, drawPosition);
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
    }
}