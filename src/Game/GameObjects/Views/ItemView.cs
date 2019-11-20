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
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item
    {
        private bool _force;
        private Graphic _originalGraphic;

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
                return false;

            //Engine.DebugInfo.ItemsRendered++;

            ResetHueVector();

            if (IsCorpse)
                return DrawCorpse(batcher, posX, posY - 3);


            ushort hue = Hue;

            if (ProfileManager.Current.FieldsType == 1 && StaticFilters.IsField(Graphic)) // static
            {
                unsafe
                {
                    IntPtr ptr = FileManager.AnimData.GetAddressToAnim(Graphic);

                    if (ptr != IntPtr.Zero)
                    {
                        AnimDataFrame2* animData = (AnimDataFrame2*) ptr;

                        if (animData->FrameCount != 0)
                        {
                            _originalGraphic = (Graphic) (Graphic + animData->FrameData[animData->FrameCount >> 1]);
                        }
                    }
                }

                _force = false;
            }
            else if (ProfileManager.Current.FieldsType == 2)
            {
                if (StaticFilters.IsFireField(Graphic))
                {
                    _originalGraphic = Constants.FIELD_REPLACE_GRAPHIC;
                    hue = 0x0020;
                }
                else if (StaticFilters.IsParalyzeField(Graphic))
                {
                    _originalGraphic = Constants.FIELD_REPLACE_GRAPHIC;
                    hue = 0x0058;
                }
                else if (StaticFilters.IsEnergyField(Graphic))
                {
                    _originalGraphic = Constants.FIELD_REPLACE_GRAPHIC;
                    hue = 0x0070;
                }
                else if (StaticFilters.IsPoisonField(Graphic))
                {
                    _originalGraphic = Constants.FIELD_REPLACE_GRAPHIC;
                    hue = 0x0044;
                }
                else if (StaticFilters.IsWallOfStone(Graphic))
                {
                    _originalGraphic = Constants.FIELD_REPLACE_GRAPHIC;
                    hue = 0x038A;
                }
            }

            if (_originalGraphic != DisplayedGraphic || _force || Texture == null || Texture.IsDisposed)
            {
                if (_originalGraphic == 0)
                    _originalGraphic = DisplayedGraphic;

                Texture = FileManager.Art.GetTexture(_originalGraphic);
                Bounds.X = (Texture.Width >> 1) - 22;
                Bounds.Y = Texture.Height - 44;
                Bounds.Width = Texture.Width;
                Bounds.Height = Texture.Height;

                _force = false;
            }

            if (ItemData.IsFoliage)
            {
                if (CharacterIsBehindFoliage)
                {
                    if (AlphaHue != Constants.FOLIAGE_ALPHA)
                        ProcessAlpha(Constants.FOLIAGE_ALPHA);
                }
                else
                {
                    if (AlphaHue != 0xFF)
                        ProcessAlpha(0xFF);
                }
            }

            if (ProfileManager.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                HueVector.X = 0x0023;
                HueVector.Y = 1;
            }
            else if (ProfileManager.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                HueVector.X = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && ProfileManager.Current.EnableBlackWhiteEffect)
            {
                HueVector.X = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else
            {
                bool isPartial = ItemData.IsPartialHue;

                if (SelectedObject.LastObject == this && !IsLocked && !IsMulti)
                {
                    isPartial = ItemData.Weight == 255;
                    hue = 0x0035;
                }
                else if (IsHidden)
                    hue = 0x038E;

                ShaderHuesTraslator.GetHueVector(ref HueVector, hue, isPartial, ItemData.IsTranslucent ? .5f : 0);
            }

            if (!IsCorpse && !IsMulti && Amount > 1 && ItemData.IsStackable && DisplayedGraphic == Graphic && _originalGraphic == Graphic)
            {
                base.Draw(batcher, posX - 5, posY - 5);
            }

            if (ItemData.IsLight)
            {
                CUOEnviroment.Client.GetScene<GameScene>()
                      .AddLight(this, this, posX + 22, posY + 22);
            }

            if (!Serial.IsValid && IsMulti && TargetManager.TargetingState == CursorTarget.MultiPlacement)
                HueVector.Z = 0.5f;

            return base.Draw(batcher, posX, posY);
        }

        private bool DrawCorpse(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed || World.CorpseManager.Exists(Serial, 0))
                return false;

            posX += 22;
            posY += 22;

            byte dir = (byte) ((byte) Layer & 0x7F & 7);
            bool mirror = false;
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;
            FileManager.Animations.Direction = dir;
            byte animIndex = (byte) AnimIndex;

            bool ishuman = MathHelper.InRange(Amount, 0x0190, 0x0193) ||
                           MathHelper.InRange(Amount, 0x00B7, 0x00BA) ||
                           MathHelper.InRange(Amount, 0x025D, 0x0260) ||
                           MathHelper.InRange(Amount, 0x029A, 0x029B) ||
                           MathHelper.InRange(Amount, 0x02B6, 0x02B7) ||
                           Amount == 0x03DB || Amount == 0x03DF || Amount == 0x03E2 || Amount == 0x02E8 || Amount == 0x02E9;

            DrawLayer(batcher, posX, posY, Layer.Invalid, animIndex, ishuman);

            for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[dir, i];
                DrawLayer(batcher, posX, posY, layer, animIndex, ishuman);
            }

            return true;
        }

        private static EquipConvData? _equipConvData;

        private void DrawLayer(UltimaBatcher2D batcher, int posX, int posY, Layer layer, byte animIndex, bool ishuman)
        {
            _equipConvData = null;
            ushort graphic;
            ushort color = 0;
            bool ispartialhue = false;
            if (layer == Layer.Invalid)
            {
                graphic = GetGraphicForAnimation();
                FileManager.Animations.AnimGroup = FileManager.Animations.GetDieGroupIndex(graphic, UsedLayer);

                if (color == 0)
                    color = Hue;
            }
            else if (HasEquipment && ishuman)
            {
                Item itemEquip = Equipment[(int) layer];

                if (itemEquip == null) return;

                graphic = itemEquip.ItemData.AnimID;
                ispartialhue = itemEquip.ItemData.IsPartialHue;

                if (FileManager.Animations.EquipConversions.TryGetValue(Amount, out Dictionary<ushort, EquipConvData> map))
                {
                    if (map.TryGetValue(graphic, out EquipConvData data))
                    {
                        _equipConvData = data;
                        graphic = data.Graphic;
                    }
                }

                color = itemEquip.Hue;
            }
            else
                return;



            byte animGroup = FileManager.Animations.AnimGroup;
            ushort newHue = 0;

            var gr = layer == Layer.Invalid
                         ? FileManager.Animations.GetCorpseAnimationGroup(ref graphic, ref animGroup, ref newHue)
                         : FileManager.Animations.GetBodyAnimationGroup(ref graphic, ref animGroup, ref newHue);

            FileManager.Animations.AnimID = graphic;

            if (color == 0)
                color = newHue;

            ref var direction = ref gr.Direction[FileManager.Animations.Direction];

            if (direction == null)
                return;

            if ((direction.FrameCount == 0 || direction.Frames == null) && !FileManager.Animations.LoadDirectionGroup(ref direction))
                return;

            direction.LastAccessTime = Time.Ticks;
            int fc = direction.FrameCount;

            if (fc > 0 && animIndex >= fc)
                animIndex = (byte) (fc - 1);

            if (animIndex < direction.FrameCount)
            {
                AnimationFrameTexture frame = direction.Frames[animIndex];

                if (frame == null || frame.IsDisposed)
                    return;

                frame.Ticks = Time.Ticks;

                if (IsFlipped)
                    posX -= frame.Width - frame.CenterX;
                else
                    posX -= frame.CenterX;

                posY -= frame.Height + frame.CenterY;


                if (color == 0)
                {
                    if ((color & 0x8000) != 0)
                    {
                        ispartialhue = true;
                        color &= 0x7FFF;
                    }

                    if (color == 0 && _equipConvData.HasValue)
                    {
                        color = _equipConvData.Value.Color;
                        ispartialhue = false;
                    }
                }

                ResetHueVector();
                
                if (ProfileManager.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
                {
                    HueVector.X = Constants.OUT_RANGE_COLOR;
                    HueVector.Y = 1;
                }
                else if (World.Player.IsDead && ProfileManager.Current.EnableBlackWhiteEffect)
                {
                    HueVector.X = Constants.DEAD_RANGE_COLOR;
                    HueVector.Y = 1;
                }
                else
                {
                    if (ProfileManager.Current.GridLootType > 0 && SelectedObject.CorpseObject == this)
                        color = 0x0034;
                    else if (ProfileManager.Current.HighlightGameObjects && SelectedObject.LastObject == this)
                        color = 0x0023;

                    ShaderHuesTraslator.GetHueVector(ref HueVector, color, ispartialhue, 0);
                }

                Texture = frame;
               
                batcher.DrawSprite(frame, posX, posY, IsFlipped, ref HueVector);
                Select(IsFlipped ? posX + frame.Width - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - posX, SelectedObject.TranslatedMousePositionByViewport.Y - posY);
            }
        }




        public override void Select(int x, int y)
        {
            if (!Serial.IsValid /*&& IsMulti*/ && TargetManager.TargetingState == CursorTarget.MultiPlacement)
                return;

            if (SelectedObject.Object == this || CharacterIsBehindFoliage)
                return;

            if (IsCorpse)
            {
                if (Texture.Contains(x, y))
                    SelectedObject.Object = this;
            }
            else
            {
                if (SelectedObject.IsPointInStatic(Texture, x - Bounds.X, y - Bounds.Y))
                    SelectedObject.Object = this;
            }
        }
    }
}
