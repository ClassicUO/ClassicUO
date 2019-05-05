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

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Mobile
    {
        public override bool Draw(Batcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed)
                return false;

            bool mirror = false;
            byte dir = (byte) GetDirectionForAnimation();
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

            Hue hue = 0;

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
            bool isUnderMouse = (SelectedObject.LastObject == this  && (TargetManager.IsTargeting || World.Player.InWarMode)) || SelectedObject.HealthbarObject == this;
            //bool needHpLine = false;

            if (this != World.Player && (isAttack || isUnderMouse || TargetManager.LastGameObject == Serial))
            {
                Hue targetColor = Notoriety.GetHue(NotorietyFlag);

                if (isAttack || this == TargetManager.LastGameObject)
                {
                    Engine.UI.SetTargetLineGump(this);
                    //needHpLine = true;
                }

                if (isAttack || isUnderMouse)
                    hue = targetColor;
            }

            bool drawShadow = !IsDead && !IsHidden && Engine.Profile.Current.ShadowsEnabled;

            DrawBody(batcher, posX, posY, dir, out int drawX, out int drawY, out int drawCenterY, ref rect, ref mirror, hue, drawShadow, isUnderMouse);

            if (IsHuman)
                DrawEquipment(batcher, posX, posY, dir, ref drawX, ref drawY, ref drawCenterY, ref rect, ref mirror, hue, isUnderMouse);

            FrameInfo.X = Math.Abs(rect.X);
            FrameInfo.Y = Math.Abs(rect.Y);
            FrameInfo.Width = FrameInfo.X + rect.Width;
            FrameInfo.Height = FrameInfo.Y + rect.Height;

            //var r = GetOnScreenRectangle();
            //batcher.DrawRectangle(Textures.GetTexture(Color.Red), r.X, r.Y, r.Width, r.Height , Vector3.Zero);

            Engine.DebugInfo.MobilesRendered++;


            return true;
        }

        private void DrawBody(Batcher2D batcher, int posX, int posY, byte dir, out int drawX, out int drawY, out int drawCenterY, ref Rectangle rect, ref bool mirror, ushort hue, bool shadow, bool isUnderMouse)
        {
            ushort graphic = GetGraphicForAnimation();
            byte animGroup = GetGroupForAnimation(this, graphic, true);
            sbyte animIndex = AnimIndex;
            drawX = drawY = drawCenterY = 0;

            ushort hueFromFile = hue;
            ref var direction = ref FileManager.Animations.GetBodyAnimationGroup(ref graphic, ref animGroup, ref hueFromFile, true).Direction[dir];

            FileManager.Animations.AnimID = graphic;
            FileManager.Animations.AnimGroup = animGroup;
            FileManager.Animations.Direction = dir;

            if ((direction.FrameCount == 0 || direction.Frames == null) && !FileManager.Animations.LoadDirectionGroup(ref direction))
                return;

            direction.LastAccessTime = Engine.Ticks;
            int fc = direction.FrameCount;

            if (fc != 0 && animIndex >= fc)
                animIndex = 0;

            if (animIndex < direction.FrameCount)
            {
                AnimationFrameTexture frame = direction.Frames[animIndex];

                if (frame == null || frame.IsDisposed)
                    return;

                drawCenterY = frame.CenterY;
                int yOff = ((int)Offset.Z >> 2) - 22 - (int) (Offset.Y - Offset.Z - 3);
                drawY = drawCenterY + yOff;

                if (IsFlipped)
                    drawX = -22 + (int) Offset.X;
                else
                    drawX = -22 - (int) Offset.X;

                int x = drawX + frame.CenterX;
                int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY;

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
                Bounds.X = x;
                Bounds.Y = -y;
                Bounds.Width = frame.Width;
                Bounds.Height = frame.Height;


                if (Engine.AuraManager.IsEnabled)
                {
                    Engine.AuraManager.Draw(batcher,
                                            IsFlipped ? posX + drawX + 44 : posX - drawX,
                                            posY - yOff, Notoriety.GetHue(NotorietyFlag));
                }


                if (IsHuman && Equipment[(int) Layer.Mount] != null)
                {
                    if (shadow)
                    {
                        DrawInternal(batcher, posX, posY, true);
                        DrawLayer(batcher, posX, posY, dir, ref drawX, ref drawY, ref drawCenterY, Layer.Mount, ref rect, ref mirror, hue, true, isUnderMouse);

                        Texture = frame;
                        Bounds.X = x;
                        Bounds.Y = -y;
                        Bounds.Width = frame.Width;
                        Bounds.Height = frame.Height;
                    }
                    else
                    {
                        DrawLayer(batcher, posX, posY, dir, ref drawX, ref drawY, ref drawCenterY, Layer.Mount, ref rect, ref mirror, hue, false, isUnderMouse);
                        Texture = frame;
                        Bounds.X = x;
                        Bounds.Y = -y;
                        Bounds.Width = frame.Width;
                        Bounds.Height = frame.Height;
                    }
                }
                else if (shadow) DrawInternal(batcher, posX, posY, true);

                if (Engine.Profile.Current.HighlightGameObjects && SelectedObject.LastObject == this && !isUnderMouse)
                {
                    HueVector.X = 0x0023;
                    HueVector.Y = 1;
                }
                else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
                {
                    HueVector.X = Constants.DEAD_RANGE_COLOR;
                    HueVector.Y = 1;
                }
                else
                {
                    bool isPartial = IsHuman && hue == 0;

                    if (IsHidden)
                    {
                        hue = 0x038E;
                        isPartial = false;
                    }
                    else if (!IsHuman && IsDead)
                    {
                        hue = 0x0386;
                        isPartial = false;
                    }

                    if (hue == 0)
                    {
                        hue = Hue;
                        if (hue == 0)
                            hue = hueFromFile;
                    }

                    ShaderHuesTraslator.GetHueVector(ref HueVector, hue, !IsHidden && isPartial, 0);
                }

                base.Draw(batcher, posX, posY);
                Select(mirror ? posX + x + 44 - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - posX + x, SelectedObject.TranslatedMousePositionByViewport.Y - posY - y);

            }
        }

        private void DrawEquipment(Batcher2D batcher, int posX, int posY, byte dir, ref int drawX, ref int drawY, ref int drawCenterY, ref Rectangle rect, ref bool mirror, Hue hue, bool isUnderMouse)
        {
            for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[dir, i];

                DrawLayer(batcher, posX, posY, dir, ref drawX, ref drawY, ref drawCenterY, layer, ref rect, ref mirror, hue, false, isUnderMouse);
            }
        }

        private void DrawLayer(Batcher2D batcher, int posX, int posY, byte dir, ref int drawX, ref int drawY, ref int drawCenterY, Layer layer, ref Rectangle rect, ref bool mirror, ushort hue, bool shadow, bool isUnderMouse)
        {
            Item item = Equipment[(int) layer];

            if (item == null)
                return;

            if (IsDead && (layer == Layer.Hair || layer == Layer.Beard))
                return;

            if (IsCovered(this, layer))
                return;

            EquipConvData? convertedItem = null;

            ushort graphic;
            int mountHeight = 0;

            if (layer == Layer.Mount && IsHuman)
            {
                graphic = item.GetGraphicForAnimation();
                mountHeight = FileManager.Animations.DataIndex[graphic].MountedHeightOffset;
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

            byte animGroup = GetGroupForAnimation(this, graphic);
            sbyte animIndex = AnimIndex;

            FileManager.Animations.AnimID = graphic;
            FileManager.Animations.AnimGroup = animGroup;
            FileManager.Animations.Direction = dir;

            ushort hueFromFile = hue;
            ref var direction = ref FileManager.Animations.GetBodyAnimationGroup(ref graphic, ref animGroup, ref hueFromFile, false).Direction[dir];

            if ((direction.FrameCount == 0 || direction.Frames == null) && !FileManager.Animations.LoadDirectionGroup(ref direction))
                return;

            if (hue == 0)
            {
                hue = item.Hue;

                if (hue == 0)
                    hue = hueFromFile;
            }

            direction.LastAccessTime = Engine.Ticks;
            int fc = direction.FrameCount;

            if (fc > 0 && animIndex >= fc)
                animIndex = 0;

            if (animIndex < direction.FrameCount)
            {
                var hash = direction.Frames[animIndex];

                if (hash == null)
                    return;

                bool partial = hue == 0 && !IsHidden && item.ItemData.IsPartialHue;

                if (hue == 0 && convertedItem.HasValue)
                {
                    hue = convertedItem.Value.Color;
                }

                AnimationFrameTexture frame = direction.Frames[animIndex];

                if (frame.IsDisposed)
                    return;


                if (drawX == 0 && drawY == 0 && drawCenterY == 0)
                {
                    drawCenterY = frame.CenterY;
                    drawY = mountHeight + drawCenterY + ((int)Offset.Z >> 2) - 22 - (int) (Offset.Y - Offset.Z - 3);

                    if (IsFlipped)
                        drawX = -22 + (int) Offset.X;
                    else
                        drawX = -22 - (int) Offset.X;
                }

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
                Bounds.X = x;
                Bounds.Y = -y;
                Bounds.Width = frame.Width;
                Bounds.Height = frame.Height;


                if (Engine.Profile.Current.HighlightGameObjects && SelectedObject.LastObject == this && !isUnderMouse)
                {
                    HueVector.X = 0x0023;
                    HueVector.Y = 1;
                }
                else if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
                {
                    HueVector.X = Constants.OUT_RANGE_COLOR;
                    HueVector.Y = 1;
                }
                else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
                {
                    HueVector.X = Constants.DEAD_RANGE_COLOR;
                    HueVector.Y = 1;
                }
                else
                {                
                    if (IsHidden)
                        hue = 0x038E;

                    ShaderHuesTraslator.GetHueVector(ref HueVector, hue, partial, 0);
                }

                //base.Draw(batcher, posX, posY);

                DrawInternal(batcher, posX, posY, shadow);
                Select(mirror ? posX + x + 44 - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - posX + x, SelectedObject.TranslatedMousePositionByViewport.Y - posY - y);


                if (item.ItemData.IsLight)
                {
                    Engine.SceneManager.GetScene<GameScene>()
                          .AddLight(this, item, IsFlipped ? posX + Bounds.X + 44 : posX - Bounds.X, posY + y + 22);
                }
            }
        }

        private void DrawInternal(Batcher2D batcher, int posX, int posY, bool hasShadow)
        {
            SpriteVertex[] vertex;

            if (IsFlipped)
            {
                vertex = SpriteVertex.PolyBufferFlipped;
                vertex[0].Position.X = posX;
                vertex[0].Position.Y = posY;
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
                vertex[0].Position.X = posX;
                vertex[0].Position.Y = posY;
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
                vertex[0].Hue = vertex[1].Hue = vertex[2].Hue = vertex[3].Hue = HueVector;


            if (hasShadow)
            {
                SpriteVertex[] vertexS =
                {
                    vertex[0],
                    vertex[1],
                    vertex[2],
                    vertex[3]
                };

                Vector3 hue = Vector3.Zero;
                hue.Y = ShaderHuesTraslator.SHADER_SHADOW;

                vertexS[0].Hue = vertexS[1].Hue = vertexS[2].Hue = vertexS[3].Hue = hue;

                batcher.DrawShadow(Texture, ref vertexS, posX + 22, (int) (posY + Offset.Y - Offset.Z + 22), IsFlipped);
            }


            batcher.DrawSprite(Texture, ref vertex);
            Texture.Ticks = Engine.Ticks;
        }

        public override void Select(int x, int y)
        {
            if (SelectedObject.Object != this && Texture.Contains(x, y))
            {
                SelectedObject.Object = this;
            }

            //if (SelectedObject.IsPointInMobile(this, x, y))
            //{
            //    SelectedObject.Object = this;
            //}
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