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
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed)
                return false;

            ResetHueVector();

            DrawCharacter(batcher, posX, posY);

            //bool mirror = false;
            //byte dir = (byte) GetDirectionForAnimation();
            //FileManager.Animations.GetAnimDirection(ref dir, ref mirror);
            //IsFlipped = mirror;

            //if (Graphic == 0)
            //    return false;

            ///*if (_frames[0].IsSitting)
            //{
            //    int x1 = 0, y1 = 0;
            //    FileManager.Animations.FixSittingDirection(ref dir, ref mirror, ref x1, ref y1);
            //}*/

            //FrameInfo = Rectangle.Empty;
            //Rectangle rect = Rectangle.Empty;

            //Hue hue = 0;

            //if (Engine.Profile.Current.HighlightMobilesByFlags)
            //{
            //    if (IsPoisoned)
            //        hue = 0x0044;

            //    if (IsParalyzed)
            //        hue = 0x014C;

            //    if (NotorietyFlag != NotorietyFlag.Invulnerable && IsYellowHits)
            //        hue = 0x0030;
            //}

            //bool isAttack = Serial == TargetManager.LastAttack;
            //bool isUnderMouse = (SelectedObject.LastObject == this  && (TargetManager.IsTargeting || World.Player.InWarMode)) || SelectedObject.HealthbarObject == this;
            ////bool needHpLine = false;

            //if (this != World.Player && (isAttack || isUnderMouse || TargetManager.LastTarget == Serial))
            //{
            //    Hue targetColor = Notoriety.GetHue(NotorietyFlag);

            //    if (isAttack || this == TargetManager.LastTarget)
            //    {
            //        Engine.UI.SetTargetLineGump(this);
            //        //needHpLine = true;
            //    }

            //    if (isAttack || isUnderMouse)
            //        hue = targetColor;
            //}

            //bool drawShadow = !IsDead && !IsHidden && Engine.Profile.Current.ShadowsEnabled;
            //DrawBody(batcher, posX, posY, dir, out int drawX, out int drawY, out int drawCenterY, ref rect, ref mirror, hue, drawShadow, isUnderMouse, out int sitting, out bool transform);

            //if (IsHuman)
            //    DrawEquipment(batcher, posX, posY, dir, ref drawX, ref drawY, ref drawCenterY, ref rect, ref mirror, hue, isUnderMouse, sitting);

            //FrameInfo.X = Math.Abs(rect.X);
            //FrameInfo.Y = Math.Abs(rect.Y);
            //FrameInfo.Width = FrameInfo.X + rect.Width;
            //FrameInfo.Height = FrameInfo.Y + rect.Height;

            //var r = GetOnScreenRectangle();
            //batcher.DrawRectangle(Textures.GetTexture(Color.Red), r.X, r.Y, r.Width, r.Height , Vector3.Zero);

            Engine.DebugInfo.MobilesRendered++;


            return true;
        }

        //private void DrawBody(UltimaBatcher2D batcher, int posX, int posY, byte dir, out int drawX, out int drawY, out int drawCenterY, ref Rectangle rect, ref bool mirror, ushort hue, bool shadow, bool isUnderMouse, out int sitting, out bool transform)
        //{
        //    ushort graphic = GetGraphicForAnimation();
        //    byte animGroup = GetGroupForAnimation(this, graphic, true);
        //    sbyte animIndex = AnimIndex;
        //    drawX = drawY = drawCenterY = 0;

        //    ushort hueFromFile = hue;
        //    ref var direction = ref FileManager.Animations.GetBodyAnimationGroup(ref graphic, ref animGroup, ref hueFromFile, true).Direction[dir];

        //    FileManager.Animations.AnimID = graphic;
        //    FileManager.Animations.AnimGroup = animGroup;
        //    FileManager.Animations.Direction = dir;

        //    sitting = 0;
        //    transform = false;

        //    if ((direction.FrameCount == 0 || direction.Frames == null) && !FileManager.Animations.LoadDirectionGroup(ref direction))
        //        return;

        //    direction.LastAccessTime = Engine.Ticks;
        //    int fc = direction.FrameCount;

        //    if (fc != 0 && animIndex >= fc)
        //        animIndex = 0;

        //    if (animIndex < direction.FrameCount)
        //    {
        //        AnimationFrameTexture frame = direction.Frames[animIndex];

        //        if (frame == null || frame.IsDisposed)
        //            return;

        //        drawCenterY = frame.CenterY;
        //        int yOff = ((int)Offset.Z >> 2) - 22 - (int) (Offset.Y - Offset.Z - 3);
        //        drawY = drawCenterY + yOff;

        //        if (IsFlipped)
        //            drawX = -22 + (int) Offset.X;
        //        else
        //            drawX = -22 - (int) Offset.X;

        //        int x = drawX + frame.CenterX;
        //        int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY;

        //        int yy = -(frame.Height + frame.CenterY + 3);
        //        int xx = -frame.CenterX;

        //        if (IsFlipped)
        //            xx = -(frame.Width - frame.CenterX);

        //        if (xx < rect.X)
        //            rect.X = xx;

        //        if (yy < rect.Y)
        //            rect.Y = yy;

        //        if (rect.Width < xx + frame.Width)
        //            rect.Width = xx + frame.Width;

        //        if (rect.Height < yy + frame.Height)
        //            rect.Height = yy + frame.Height;

        //        Texture = frame;
        //        Bounds.X = x;
        //        Bounds.Y = -y;
        //        Bounds.Width = frame.Width;
        //        Bounds.Height = frame.Height;


        //        if (Engine.AuraManager.IsEnabled)
        //        {
        //            Engine.AuraManager.Draw(batcher,
        //                                    IsFlipped ? posX + drawX + 44 : posX - drawX,
        //                                    posY - yOff, Notoriety.GetHue(NotorietyFlag));
        //        }


        //        if (IsHuman && Equipment[(int) Layer.Mount] != null)
        //        {
        //            FileManager.Animations.SittingValue = 0;

        //            if (shadow)
        //            {
        //                DrawInternal(batcher, posX, posY, true, sitting);
        //                DrawLayer(batcher, posX, posY, dir, ref drawX, ref drawY, ref drawCenterY, Layer.Mount, ref rect, ref mirror, hue, true, isUnderMouse, sitting);

        //                Texture = frame;
        //                Bounds.X = x;
        //                Bounds.Y = -y;
        //                Bounds.Width = frame.Width;
        //                Bounds.Height = frame.Height;
        //            }
        //            else
        //            {
        //                DrawLayer(batcher, posX, posY, dir, ref drawX, ref drawY, ref drawCenterY, Layer.Mount, ref rect, ref mirror, hue, false, isUnderMouse, sitting);
        //                Texture = frame;
        //                Bounds.X = x;
        //                Bounds.Y = -y;
        //                Bounds.Width = frame.Width;
        //                Bounds.Height = frame.Height;
        //            }
        //        }
        //        else
        //        {
        //            sitting = FileManager.Animations.SittingValue = IsSitting;

        //            if (sitting != 0)
        //            {
        //                animGroup = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND;
        //                animIndex = 0;

        //                FileManager.Animations.FixSittingDirection(ref dir, ref mirror, ref posX, ref posY);

        //                if (Direction == Direction.Down)
        //                    animGroup = 25;
        //                else
        //                {
        //                    transform = true;
        //                }
        //            }
        //            else if (shadow)
        //                DrawInternal(batcher, posX, posY, true, 0);
        //        }

        //        if (Engine.Profile.Current.HighlightGameObjects && SelectedObject.LastObject == this && !isUnderMouse)
        //        {
        //            HueVector.X = 0x0023;
        //            HueVector.Y = 1;
        //        }
        //        else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
        //        {
        //            HueVector.X = Constants.DEAD_RANGE_COLOR;
        //            HueVector.Y = 1;
        //        }
        //        else
        //        {
        //            bool isPartial = IsHuman && hue == 0;

        //            if (IsHidden)
        //            {
        //                hue = 0x038E;
        //                isPartial = false;
        //            }
        //            else if (!IsHuman && IsDead)
        //            {
        //                hue = 0x0386;
        //                isPartial = false;
        //            }

        //            if (hue == 0)
        //            {
        //                hue = Hue;
        //                if (hue == 0)
        //                    hue = hueFromFile;
        //            }

        //            ShaderHuesTraslator.GetHueVector(ref HueVector, hue, !IsHidden && isPartial, 0);
        //        }

        //        base.Draw(batcher, posX, posY);
        //        Select(mirror ? posX + x + 44 - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - posX + x, SelectedObject.TranslatedMousePositionByViewport.Y - posY - y);

        //    }
        //}

        //private void DrawEquipment(UltimaBatcher2D batcher, int posX, int posY, byte dir, ref int drawX, ref int drawY, ref int drawCenterY, ref Rectangle rect, ref bool mirror, Hue hue, bool isUnderMouse, int sitting)
        //{
        //    for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
        //    {
        //        Layer layer = LayerOrder.UsedLayers[dir, i];

        //        DrawLayer(batcher, posX, posY, dir, ref drawX, ref drawY, ref drawCenterY, layer, ref rect, ref mirror, hue, false, isUnderMouse, sitting);
        //    }
        //}

        //private void DrawLayer(UltimaBatcher2D batcher, int posX, int posY, byte dir, ref int drawX, ref int drawY, ref int drawCenterY, Layer layer, ref Rectangle rect, ref bool mirror, ushort hue, bool shadow, bool isUnderMouse, int sitting)
        //{
        //    Item item = Equipment[(int) layer];

        //    if (item == null)
        //        return;

        //    if (IsDead && (layer == Layer.Hair || layer == Layer.Beard))
        //        return;

        //    if (IsCovered(this, layer))
        //        return;

        //    EquipConvData? convertedItem = null;

        //    ushort graphic;
        //    int mountHeight = 0;

        //    if (layer == Layer.Mount && IsHuman)
        //    {
        //        graphic = item.GetGraphicForAnimation();
        //        mountHeight = FileManager.Animations.DataIndex[graphic].MountedHeightOffset;
        //    }
        //    else if (item.ItemData.AnimID != 0)
        //    {
        //        graphic = item.ItemData.AnimID;

        //        if (FileManager.Animations.EquipConversions.TryGetValue(Graphic, out Dictionary<ushort, EquipConvData> map))
        //        {
        //            if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
        //            {
        //                convertedItem = data;
        //                graphic = data.Graphic;
        //            }
        //        }
        //    }
        //    else
        //        return;

        //    byte animGroup = GetGroupForAnimation(this, graphic);
        //    sbyte animIndex = AnimIndex;

        //    FileManager.Animations.AnimID = graphic;
        //    FileManager.Animations.AnimGroup = animGroup;
        //    FileManager.Animations.Direction = dir;

        //    ushort hueFromFile = hue;
        //    ref var direction = ref FileManager.Animations.GetBodyAnimationGroup(ref graphic, ref animGroup, ref hueFromFile, false).Direction[dir];

        //    if ((direction.FrameCount == 0 || direction.Frames == null) && !FileManager.Animations.LoadDirectionGroup(ref direction))
        //        return;

        //    if (hue == 0)
        //    {
        //        hue = item.Hue;

        //        if (hue == 0)
        //            hue = hueFromFile;
        //    }

        //    direction.LastAccessTime = Engine.Ticks;
        //    int fc = direction.FrameCount;

        //    if (fc > 0 && animIndex >= fc)
        //        animIndex = 0;

        //    if (animIndex < direction.FrameCount)
        //    {
        //        AnimationFrameTexture frame = direction.Frames[animIndex];

        //        if (frame == null || frame.IsDisposed)
        //            return;

        //        bool partial = hue == 0 && !IsHidden && item.ItemData.IsPartialHue;

        //        if (hue == 0 && convertedItem.HasValue)
        //        {
        //            hue = convertedItem.Value.Color;
        //        }

        //        if (drawX == 0 && drawY == 0 && drawCenterY == 0)
        //        {
        //            drawCenterY = frame.CenterY;
        //            drawY = mountHeight + drawCenterY + ((int)Offset.Z >> 2) - 22 - (int) (Offset.Y - Offset.Z - 3);

        //            if (IsFlipped)
        //                drawX = -22 + (int) Offset.X;
        //            else
        //                drawX = -22 - (int) Offset.X;
        //        }

        //        int x = drawX + frame.CenterX;
        //        int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY - mountHeight;

        //        int yy = -(frame.Height + frame.CenterY + 3);
        //        int xx = -frame.CenterX;

        //        if (mirror)
        //            xx = -(frame.Width - frame.CenterX);

        //        if (xx < rect.X)
        //            rect.X = xx;

        //        if (yy < rect.Y)
        //            rect.Y = yy;

        //        if (rect.Width < xx + frame.Width)
        //            rect.Width = xx + frame.Width;

        //        if (rect.Height < yy + frame.Height)
        //            rect.Height = yy + frame.Height;

        //        Texture = frame;
        //        Bounds.X = x;
        //        Bounds.Y = -y;
        //        Bounds.Width = frame.Width;
        //        Bounds.Height = frame.Height;


        //        if (Engine.Profile.Current.HighlightGameObjects && SelectedObject.LastObject == this && !isUnderMouse)
        //        {
        //            HueVector.X = 0x0023;
        //            HueVector.Y = 1;
        //        }
        //        else if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
        //        {
        //            HueVector.X = Constants.OUT_RANGE_COLOR;
        //            HueVector.Y = 1;
        //        }
        //        else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
        //        {
        //            HueVector.X = Constants.DEAD_RANGE_COLOR;
        //            HueVector.Y = 1;
        //        }
        //        else
        //        {                
        //            if (IsHidden)
        //                hue = 0x038E;

        //            ShaderHuesTraslator.GetHueVector(ref HueVector, hue, partial, 0);
        //        }

        //        DrawInternal(batcher, posX, posY, shadow, sitting);
        //        Select(mirror ? posX + x + 44 - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - posX + x, SelectedObject.TranslatedMousePositionByViewport.Y - posY - y);


        //        if (item.ItemData.IsLight)
        //        {
        //            Engine.SceneManager.GetScene<GameScene>()
        //                  .AddLight(this, item, IsFlipped ? posX + Bounds.X + 44 : posX - Bounds.X, posY + y + 22);
        //        }
        //    }
        //}


        private void DrawCharacter(UltimaBatcher2D batcher, int posX, int posY)
        {
            _equipConvData = null;
            _transform = false;
            FileManager.Animations.SittingValue = 0;
            FrameInfo = Rectangle.Empty;
            
            int drawX = posX + (int) Offset.X;
            int drawY = (int) (posY + Offset.Y - Offset.Z - 3);

            bool hasShadow = !IsDead && !IsHidden && Engine.Profile.Current.ShadowsEnabled;


            if (Engine.AuraManager.IsEnabled)
                Engine.AuraManager.Draw(batcher, drawX + 22, drawY + 22, Notoriety.GetHue(NotorietyFlag));

            if (Engine.Profile.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                _viewHue = 0x0023;
                HueVector.Y = 1;
            }
            else if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
            {
                _viewHue = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
            {
                _viewHue = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (IsHidden)
            {
                _viewHue = 0x038E;
            }
            else if (SelectedObject.HealthbarObject == this)
            {
                _viewHue = Notoriety.GetHue(NotorietyFlag);
            }
            else
            {
                _viewHue = 0;

                if (IsDead)
                {
                    _viewHue = 0x0386;
                }
                else if (Engine.Profile.Current.HighlightMobilesByFlags)
                {
                    if (IsPoisoned)
                        _viewHue = 0x0044;

                    if (IsParalyzed)
                        _viewHue = 0x014C;

                    if (NotorietyFlag != NotorietyFlag.Invulnerable && IsYellowHits)
                        _viewHue = 0x0030;
                }
            }



            bool isAttack = Serial == TargetManager.LastAttack;
            bool isUnderMouse = SelectedObject.LastObject == this && TargetManager.IsTargeting;
            //bool needHpLine = false;

            if (this != World.Player && (isAttack || isUnderMouse || TargetManager.LastTarget == Serial))
            {
                Hue targetColor = Notoriety.GetHue(NotorietyFlag);

                if (isAttack || this == TargetManager.LastTarget)
                {
                    Engine.UI.SetTargetLineGump(this);
                    //needHpLine = true;
                }

                if (isAttack || isUnderMouse)
                    _viewHue = targetColor;
            }


            bool mirror = false;
            byte dir = (byte)GetDirectionForAnimation();
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;

            ushort graphic = GetGraphicForAnimation();
            byte animGroup = GetGroupForAnimation(this, graphic, true);
            sbyte animIndex = AnimIndex;

            FileManager.Animations.Direction = dir;
            FileManager.Animations.AnimGroup = animGroup;

            Item mount = HasEquipment ? Equipment[(int) Layer.Mount] : null;

            if (IsHuman && mount != null)
            {
                FileManager.Animations.SittingValue = 0;

                ushort mountGraphic = mount.GetGraphicForAnimation();

                int mountHeightOffset = 0;

                //if (mountGraphic < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                //    mountHeightOffset = FileManager.Animations.DataIndex[graphic].MountedHeightOffset;

                if (hasShadow)
                {
                    DrawInternal(batcher, this, null, drawX, drawY + 10, mirror, animIndex, true, graphic);
                    FileManager.Animations.AnimGroup = GetGroupForAnimation(this, mountGraphic);
                    DrawInternal(batcher, this, mount, drawX, drawY, mirror, animIndex, true, mountGraphic);
                }
                else
                {
                    FileManager.Animations.AnimGroup = GetGroupForAnimation(this, mountGraphic);
                }

                mountHeightOffset = DrawInternal(batcher, this, mount, drawX, drawY, mirror, animIndex, false, mountGraphic);
                drawY += mountHeightOffset;
            }
            else
            {
                if ((FileManager.Animations.SittingValue = IsSitting()) != 0)
                {
                    animGroup = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND;
                    animIndex = 0;

                    ProcessAnimation(out dir);
                    FileManager.Animations.Direction = dir;
                    FileManager.Animations.FixSittingDirection(ref dir, ref mirror, ref drawX, ref drawY);

                    if (FileManager.Animations.Direction == 3)
                    {
                        animGroup = 25;
                    }
                    else
                    {
                        _transform = true;
                    }
                }
                else if (hasShadow)
                {
                    DrawInternal(batcher, this, null, drawX, drawY, mirror, animIndex, true, graphic);
                }
            }

            FileManager.Animations.AnimGroup = animGroup;

            DrawInternal(batcher, this, null, drawX, drawY, mirror, animIndex, false, graphic);

            if (IsHuman && HasEquipment)
            {
                for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
                {
                    Layer layer = LayerOrder.UsedLayers[dir, i];

                    Item item = Equipment[(int)layer];

                    if (item == null)
                        continue;

                    if (IsDead && (layer == Layer.Hair || layer == Layer.Beard))
                        continue;

                    if (IsCovered(this, layer))
                        continue;

                    if (item.ItemData.AnimID != 0)
                    {
                        graphic = item.ItemData.AnimID;

                        if (FileManager.Animations.EquipConversions.TryGetValue(Graphic, out Dictionary<ushort, EquipConvData> map))
                        {
                            if (map.TryGetValue(item.ItemData.AnimID, out EquipConvData data))
                            {
                                _equipConvData = data;
                                graphic = data.Graphic;
                            }
                        }

                        DrawInternal(batcher, this, item, drawX, drawY, mirror, animIndex, false, graphic, false);
                    }
                    _equipConvData = null;
                }

                //if (FileManager.Animations.SittingValue != 0)
                //{
                //    ref var sittingData = ref FileManager.Animations.SittingInfos[FileManager.Animations.SittingValue - 1];

                //    if (FileManager.Animations.Direction == 3 && sittingData.DrawBack &&
                //        HasEquipment && Equipment[(int) Layer.Cloak] == null)
                //    {

                //    }
                //}
            }


            FrameInfo.X = Math.Abs(FrameInfo.X);
            FrameInfo.Y = Math.Abs(FrameInfo.Y);
            FrameInfo.Width = FrameInfo.X + FrameInfo.Width;
            FrameInfo.Height = FrameInfo.Y + FrameInfo.Height;
        }

        private static ushort _viewHue;
        private static EquipConvData? _equipConvData;
        private static bool _transform;
        private static int _characterFrameStartY = 0;
        private static int _startCharacterWaistY = 0;
        private static int _startCharacterKneesY = 0;
        private static int _startCharacterFeetY = 0;
        private static int _characterFrameHeight;

        private static sbyte DrawInternal(UltimaBatcher2D batcher, Mobile owner, Item entity, int x, int y, bool mirror, sbyte frameIndex, bool hasShadow, ushort id, bool isParent = true)
        {
            if (id >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                return 0;

            x += 22;
            y += 22;

            ushort hueFromFile = _viewHue;
            byte animGroup = FileManager.Animations.AnimGroup;
            ref var direction = ref FileManager.Animations.GetBodyAnimationGroup(ref id, ref animGroup, ref hueFromFile, isParent).Direction[FileManager.Animations.Direction];

            FileManager.Animations.AnimID = id;

            if ((direction.FrameCount == 0 || direction.Frames == null) && !FileManager.Animations.LoadDirectionGroup(ref direction))
                return 0;

            direction.LastAccessTime = Engine.Ticks;

            int fc = direction.FrameCount;

            if (fc > 0 && frameIndex >= fc)
            {
                frameIndex = 0;
            }

            if (frameIndex < direction.FrameCount)
            {
                ref var frame = ref direction.Frames[frameIndex];

                if (frame == null || frame.IsDisposed)
                    return 0;

                frame.Ticks = Engine.Ticks;

                if (mirror)
                    x -= frame.Width - frame.CenterX;
                else
                    x -= frame.CenterX;
                
                y -= frame.Height + frame.CenterY;

                if (hasShadow)
                {
                    batcher.DrawSpriteShadow(frame, x, y, frame.Width, frame.Height,  mirror ? frame.Width - 44 : 0, 0, 0, y + 44 + 22, mirror);
                }
                else
                {
                    ushort hue = _viewHue;
                    bool partialHue = false;

                    if (hue == 0)
                    {
                        hue = entity?.Hue ?? owner.Hue;
                        partialHue = entity != null && entity.ItemData.IsPartialHue;

                        if ((hue & 0x8000) != 0)
                        {
                            partialHue = true;
                            hue &= 0x7FFF;
                        }

                        if (hue == 0)
                        {
                            hue = hueFromFile;

                            if (hue == 0 && _equipConvData.HasValue)
                                hue = _equipConvData.Value.Color;

                            partialHue = false;
                        }
                    }

                    ShaderHuesTraslator.GetHueVector(ref HueVector, hue, partialHue, 0);

                    if (_transform)
                    {
                        const float UPPER_BODY_RATIO = 0.35f;
                        const float MID_BODY_RATIO = 0.60f;
                        const float LOWER_BODY_RATIO = 0.94f;


                        if (entity == null && owner.IsHuman)
                        {
                            int frameHeight = frame.Height;
                            _characterFrameStartY = y;
                            _characterFrameHeight = frame.Height;
                            _startCharacterWaistY = (int)(frameHeight * UPPER_BODY_RATIO) + _characterFrameStartY;
                            _startCharacterKneesY = (int)(frameHeight * MID_BODY_RATIO) + _characterFrameStartY;
                            _startCharacterFeetY = (int)(frameHeight * LOWER_BODY_RATIO) + _characterFrameStartY;
                        }

                        float h3mod = UPPER_BODY_RATIO;
                        float h6mod = MID_BODY_RATIO;
                        float h9mod = LOWER_BODY_RATIO;


                        if (entity != null)
                        {
                            float itemsEndY = (float) (y + frame.Height);

                            if (y >= _startCharacterWaistY)
                                h3mod = 0;
                            else if (itemsEndY <= _startCharacterWaistY)
                                h3mod = 1.0f;
                            else
                            {
                                float upperBodyDiff = (float) (_startCharacterWaistY - y);
                                h3mod = upperBodyDiff / frame.Height;

                                if (h3mod < 0)
                                    h3mod = 0;
                            }


                            if (_startCharacterWaistY >= itemsEndY || y >= _startCharacterKneesY)
                                h6mod = 0;
                            else if (_startCharacterWaistY <= y && itemsEndY <= _startCharacterKneesY)
                                h6mod = 1.0f;
                            else
                            {
                                float midBodyDiff;

                                if (y >= _startCharacterWaistY)
                                    midBodyDiff = (float) (_startCharacterKneesY - y);
                                else if (itemsEndY <= _startCharacterKneesY)
                                    midBodyDiff = (float) (itemsEndY - _startCharacterWaistY);
                                else
                                    midBodyDiff = (float) (_startCharacterKneesY - _startCharacterWaistY);

                                h6mod = h3mod + midBodyDiff / frame.Height;

                                if (h6mod < 0)
                                    h6mod = 0;
                            }


                            if (itemsEndY <= _startCharacterKneesY)
                                h9mod = 0;
                            else if (y >= _startCharacterKneesY)
                                h9mod = 1.0f;
                            else
                            {
                                float lowerBodyDiff = itemsEndY - _startCharacterKneesY;
                                h9mod = h6mod + lowerBodyDiff / frame.Height;

                                if (h9mod < 0)
                                    h9mod = 0;
                            }
                        }

                        batcher.DrawCharacterSitted(frame, x, y, frame.Width - 44,0, mirror, h3mod, h6mod, h9mod, HueVector);
                    }
                    else
                    {
                        if (mirror)
                            batcher.DrawSpriteFlipped(frame, x, y, frame.Width, frame.Height, frame.Width - 44, 0, HueVector);
                        else
                            batcher.DrawSprite(frame, x, y, frame.Width, frame.Height, 0, 0, HueVector);
                        
                        int yy = -(frame.Height + frame.CenterY + 3);
                        int xx = -frame.CenterX;

                        if (mirror)
                            xx = -(frame.Width - frame.CenterX);

                        if (xx < owner.FrameInfo.X)
                            owner.FrameInfo.X = xx;

                        if (yy < owner.FrameInfo.Y)
                            owner.FrameInfo.Y = yy;

                        if (owner.FrameInfo.Width < xx + frame.Width)
                            owner.FrameInfo.Width = xx + frame.Width;

                        if (owner.FrameInfo.Height < yy + frame.Height)
                            owner.FrameInfo.Height = yy + frame.Height;
                    }

                    owner.Texture = frame;
                    owner.Select(mirror ? x + frame.Width - SelectedObject.TranslatedMousePositionByViewport.X: SelectedObject.TranslatedMousePositionByViewport.X - x, SelectedObject.TranslatedMousePositionByViewport.Y - y);

                    if (entity != null && entity.ItemData.IsLight)
                    {
                        Engine.SceneManager.GetScene<GameScene>().AddLight(owner, entity, mirror ? x + frame.Width : x , y);
                    }
                }

                return FileManager.Animations.DataIndex[id].MountedHeightOffset;
            }

            return 0;
        }

        //private void DrawInternal(UltimaBatcher2D batcher, int posX, int posY, bool hasShadow, int sitting, bool mirror)
        //{
        //    Texture.Ticks = Engine.Ticks;

        //    if (hasShadow)
        //    {
        //        batcher.DrawSpriteShadow(Texture, posX, posY, Bounds.Width, Bounds.Height, Bounds.X, Bounds.Y, 22, (int)(posY + (Offset.Y - Offset.Z) + 22), IsFlipped);
        //    }

        //    if (IsFlipped)
        //    {
        //        batcher.DrawSpriteFlipped(Texture, posX, posY, Bounds.Width, Bounds.Height, Bounds.X, Bounds.Y, HueVector);
        //    }
        //    else
        //    {
        //        batcher.DrawSprite(Texture, posX, posY, Bounds.Width, Bounds.Height, Bounds.X, Bounds.Y, HueVector);
        //    }
        //}

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