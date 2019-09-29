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

using System.Collections.Generic;

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

            Engine.DebugInfo.ItemsRendered++;

            ResetHueVector();

            if (IsCorpse)
                return DrawCorpse(batcher, posX, posY - 3);


            ushort hue = Hue;

            if (Engine.Profile.Current.FieldsType == 1 && StaticFilters.IsField(Graphic)) // static
            {
                unsafe
                {
                    _originalGraphic = (Graphic) (Graphic + _animDataFrame.FrameData[_animDataFrame.FrameCount >> 1]);
                }

                _force = false;
            }
            else if (Engine.Profile.Current.FieldsType == 2)
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

            if (Engine.Profile.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                HueVector.X = 0x0023;
                HueVector.Y = 1;
            }
            else if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
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
                Engine.SceneManager.GetScene<GameScene>()
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

            byte dir = (byte) ((byte) Layer & 0x7F & 7);
            bool mirror = false;
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);
            IsFlipped = mirror;
            FileManager.Animations.Direction = dir;
            byte animIndex = (byte) AnimIndex;

            DrawLayer(batcher, posX, posY, Layer.Invalid, animIndex);

            for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[dir, i];
                DrawLayer(batcher, posX, posY, layer, animIndex);
            }

            return true;
        }

        private void DrawLayer(UltimaBatcher2D batcher, int posX, int posY, Layer layer, byte animIndex)
        {
            ushort graphic;
            ushort color = 0;

            if (layer == Layer.Invalid)
            {
                graphic = GetGraphicForAnimation();
                FileManager.Animations.AnimGroup = FileManager.Animations.GetDieGroupIndex(graphic, UsedLayer);

                if (color == 0)
                    color = Hue;
            }
            else if (HasEquipment && MathHelper.InRange(Amount, 0x0190, 0x0193) ||
                     MathHelper.InRange(Amount, 0x00B7, 0x00BA) ||
                     MathHelper.InRange(Amount, 0x025D, 0x0260) ||
                     MathHelper.InRange(Amount, 0x029A, 0x029B) ||
                     MathHelper.InRange(Amount, 0x02B6, 0x02B7) ||
                     Amount == 0x03DB || Amount == 0x03DF || Amount == 0x03E2 || Amount == 0x02E8 || Amount == 0x02E9)
            {
                Item itemEquip = Equipment[(int) layer];

                if (itemEquip == null) return;

                graphic = itemEquip.ItemData.AnimID;

                if (FileManager.Animations.EquipConversions.TryGetValue(Amount, out Dictionary<ushort, EquipConvData> map))
                {
                    if (map.TryGetValue(graphic, out EquipConvData data))
                        graphic = data.Graphic;
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

            direction.LastAccessTime = Engine.Ticks;
            int fc = direction.FrameCount;

            if (fc > 0 && animIndex >= fc)
                animIndex = (byte) (fc - 1);

            if (animIndex < direction.FrameCount)
            {
                AnimationFrameTexture frame = direction.Frames[animIndex];

                if (frame == null || frame.IsDisposed)
                    return;

                int drawCenterY = frame.CenterY;
                const int drawX = -22;
                int drawY = drawCenterY - 22;
                drawY -= 3;
                int x = drawX + frame.CenterX;
                int y = -drawY - (frame.Height + frame.CenterY) + drawCenterY;
                Texture = frame;
                Bounds.X = x;
                Bounds.Y = -y;
                Bounds.Width = frame.Width;
                Bounds.Height = frame.Height;


                if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
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
                    if (Engine.Profile.Current.GridLootType > 0 && SelectedObject.CorpseObject == this)
                        color = 0x0034;
                    else if (Engine.Profile.Current.HighlightGameObjects && SelectedObject.LastObject == this)
                        color = 0x0023;

                    ShaderHuesTraslator.GetHueVector(ref HueVector, color);
                }

                DrawInternal(batcher, posX, posY);
                Select(IsFlipped ? posX + x + 44 - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - posX + x, SelectedObject.TranslatedMousePositionByViewport.Y - posY - y);
            }
        }

        private void DrawInternal(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsFlipped)
                batcher.DrawSpriteFlipped(Texture, posX, posY, Bounds.Width, Bounds.Height, Bounds.X, Bounds.Y, ref HueVector);
            else
                batcher.DrawSprite(Texture, posX, posY, Bounds.Width, Bounds.Height, Bounds.X, Bounds.Y, ref HueVector);

            Texture.Ticks = Engine.Ticks;
        }


        public override void Select(int x, int y)
        {
            if (!Serial.IsValid && IsMulti && TargetManager.TargetingState == CursorTarget.MultiPlacement)
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
