#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item
    {
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
                return false;

            //Engine.DebugInfo.ItemsRendered++;

            ResetHueVector();
            DrawTransparent = false;

            posX += (int) Offset.X;
            posY += (int) (Offset.Y + Offset.Z);

            if (ItemData.IsTranslucent)
            {
                HueVector.Z = 0.5f;
            }

            if (AlphaHue != 255)
                HueVector.Z = 1f - AlphaHue / 255f;

            if (IsCorpse)
                return DrawCorpse(batcher, posX, posY - 3);


            ushort hue = Hue;
            ushort graphic = DisplayedGraphic;
            bool partial = ItemData.IsPartialHue;

            if (OnGround && ItemData.IsAnimated)
            {
                if (ProfileManager.Current.FieldsType == 2)
                {
                    if (StaticFilters.IsFireField(Graphic))
                    {
                        graphic = Constants.FIELD_REPLACE_GRAPHIC;
                        hue = 0x0020;
                    }
                    else if (StaticFilters.IsParalyzeField(Graphic))
                    {
                        graphic = Constants.FIELD_REPLACE_GRAPHIC;
                        hue = 0x0058;
                    }
                    else if (StaticFilters.IsEnergyField(Graphic))
                    {
                        graphic = Constants.FIELD_REPLACE_GRAPHIC;
                        hue = 0x0070;
                    }
                    else if (StaticFilters.IsPoisonField(Graphic))
                    {
                        graphic = Constants.FIELD_REPLACE_GRAPHIC;
                        hue = 0x0044;
                    }
                    else if (StaticFilters.IsWallOfStone(Graphic))
                    {
                        graphic = Constants.FIELD_REPLACE_GRAPHIC;
                        hue = 0x038A;
                    }
                }
            }

            if (ProfileManager.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                partial = false;
            }
            else if (ProfileManager.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.Current.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }
            else
            {
                if (!IsLocked && !IsMulti && SelectedObject.LastObject == this)
                {
                    // TODO: check why i put this.
                    //isPartial = ItemData.Weight == 0xFF;
                    hue = 0x0035;
                }
                else if (IsHidden)
                    hue = 0x038E;
            }

            ShaderHuesTraslator.GetHueVector(ref HueVector, hue, partial, HueVector.Z);

            if (!IsMulti && !IsCoin && Amount > 1 && ItemData.IsStackable)
            {
                DrawStaticAnimated(batcher, graphic, posX - 5, posY - 5, ref HueVector, ref DrawTransparent);
            }

            if (ItemData.IsLight)
            {
                Client.Game.GetScene<GameScene>()
                      .AddLight(this, this, posX + 22, posY + 22);
            }

            if (!SerialHelper.IsValid(Serial) && IsMulti && TargetManager.TargetingState == CursorTarget.MultiPlacement)
                HueVector.Z = 0.5f;

            DrawStaticAnimated(batcher, graphic, posX, posY, ref HueVector, ref DrawTransparent);

            if (SelectedObject.Object == this || TargetManager.TargetingState == CursorTarget.MultiPlacement)
                return false;

            var texture = ArtLoader.Instance.GetTexture(graphic);

            if (texture != null)
            {
                ref var index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                posX -= index.Width;
                posY -= index.Height;

                if (SelectedObject.IsPointInStatic(texture, posX, posY))
                {
                    SelectedObject.Object = this;
                }
            }

            return true;
        }

        private bool DrawCorpse(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed || World.CorpseManager.Exists(Serial, 0))
                return false;

            posX += 22;
            posY += 22;

            AnimationsLoader.Instance.Direction = (byte) (((byte) Layer & 0x7F) & 7);
            AnimationsLoader.Instance.GetAnimDirection(ref AnimationsLoader.Instance.Direction, ref IsFlipped);

            byte animIndex = (byte) AnimIndex;
            ushort graphic = GetGraphicForAnimation();
            AnimationsLoader.Instance.ConvertBodyIfNeeded(ref graphic);
            AnimationsLoader.Instance.AnimGroup = AnimationsLoader.Instance.GetDieGroupIndex(graphic, UsedLayer);

            bool ishuman = MathHelper.InRange(Amount, 0x0190, 0x0193) ||
                           MathHelper.InRange(Amount, 0x00B7, 0x00BA) ||
                           MathHelper.InRange(Amount, 0x025D, 0x0260) ||
                           MathHelper.InRange(Amount, 0x029A, 0x029B) ||
                           MathHelper.InRange(Amount, 0x02B6, 0x02B7) ||
                           Amount == 0x03DB || Amount == 0x03DF || Amount == 0x03E2 || Amount == 0x02E8 || Amount == 0x02E9;

            DrawLayer(batcher, posX, posY, this, Layer.Invalid, animIndex, ishuman, Hue, IsFlipped, HueVector.Z);

            for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[AnimationsLoader.Instance.Direction, i];
                DrawLayer(batcher, posX, posY, this, layer, animIndex, ishuman, 0, IsFlipped, HueVector.Z);
            }

            return true;
        }

        private static EquipConvData? _equipConvData;

        private static void DrawLayer(UltimaBatcher2D batcher, int posX, int posY, Item owner, Layer layer, byte animIndex, bool ishuman, ushort color, bool flipped, float alpha)
        {
            _equipConvData = null;
            bool ispartialhue = false;

            ushort graphic;
            
            if (layer == Layer.Invalid)
            {
                graphic = owner.GetGraphicForAnimation();
            }
            else if (ishuman)
            {
                Item itemEquip = owner.FindItemByLayer(layer);

                if (itemEquip == null) 
                    return;

                graphic = itemEquip.ItemData.AnimID;
                ispartialhue = itemEquip.ItemData.IsPartialHue;

                if (AnimationsLoader.Instance.EquipConversions.TryGetValue(graphic, out Dictionary<ushort, EquipConvData> map))
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

            byte animGroup = AnimationsLoader.Instance.AnimGroup;
            ushort newHue = 0;

            AnimationGroup gr = layer == Layer.Invalid
                                    ? AnimationsLoader.Instance.GetCorpseAnimationGroup(ref graphic, ref animGroup, ref newHue)
                                    : AnimationsLoader.Instance.GetBodyAnimationGroup(ref graphic, ref animGroup, ref newHue);

            AnimationsLoader.Instance.AnimID = graphic;

            if (color == 0)
                color = newHue;

            var direction = gr.Direction[AnimationsLoader.Instance.Direction];

            if (direction == null)
                return;

            if ((direction.FrameCount == 0 || direction.Frames == null) && !AnimationsLoader.Instance.LoadDirectionGroup(ref direction))
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

                if (flipped)
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
                
                if (ProfileManager.Current.NoColorObjectsOutOfRange && owner.Distance > World.ClientViewRange)
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
                    if (ProfileManager.Current.GridLootType > 0 && SelectedObject.CorpseObject == owner)
                        color = 0x0034;
                    else if (ProfileManager.Current.HighlightGameObjects && SelectedObject.LastObject == owner)
                        color = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;

                    ShaderHuesTraslator.GetHueVector(ref HueVector, color, ispartialhue, alpha);
                }

                batcher.DrawSprite(frame, posX, posY, flipped, ref HueVector);

                if (!SerialHelper.IsValid(owner))
                {
                    return;
                }

                if (SelectedObject.Object == owner)
                    return;

                if (frame.Contains(flipped ? posX + frame.Width - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - posX, SelectedObject.TranslatedMousePositionByViewport.Y - posY))
                {
                    SelectedObject.Object = owner;
                }

            }
        }
    }
}
