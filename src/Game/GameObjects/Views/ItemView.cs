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
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Item
    {
        private Graphic _originalGraphic;


        public override bool Draw(Batcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
                return false;

            Engine.DebugInfo.ItemsRendered++;

            if (IsCorpse)
                return DrawCorpse(batcher, posX, posY);

            if (_originalGraphic != DisplayedGraphic || Texture == null || Texture.IsDisposed)
            {
                _originalGraphic = DisplayedGraphic;
                Texture = FileManager.Art.GetTexture(_originalGraphic);
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);
            }

            if (ItemData.IsFoliage)
            {
                if (CharacterIsBehindFoliage)
                {
                    if (AlphaHue != 76)
                        ProcessAlpha(76);
                }
                else
                {
                    if (AlphaHue != 0xFF)
                        ProcessAlpha(0xFF);
                }
            }


            if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
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
                ushort hue = Hue;
                bool isPartial = ItemData.IsPartialHue;

                if (Engine.Profile.Current.HighlightGameObjects && IsSelected)
                {
                    hue = 0x0023;
                    isPartial = false;
                }
                else if (IsSelected && !IsLocked)
                    hue = 0x0035;
                else if (IsHidden)
                    hue = 0x038E;

                ShaderHuesTraslator.GetHueVector(ref HueVector, hue, isPartial, ItemData.IsTranslucent ? .5f : 0);
            }

            if (Amount > 1 && ItemData.IsStackable && DisplayedGraphic == Graphic)
            {
                //SpriteRenderer.DrawStaticArt(DisplayedGraphic, Hue, (int) offsetDrawPosition.X, (int) offsetDrawPosition.Y);
                base.Draw(batcher, posX - 5, posY - 5);
            }

            if (ItemData.IsLight)
            {
                Engine.SceneManager.GetScene<GameScene>()
                      .AddLight(this, this, posX + 22, posY + 22);
            }

            // SpriteRenderer.DrawStaticArt(DisplayedGraphic, Hue, (int)position.X, (int)position.Y);
            // return true;

            if (base.Draw(batcher, posX, posY)) return true;

            return false;
        }

        private bool DrawCorpse(Batcher2D batcher, int posX, int posY)
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

        private void DrawLayer(Batcher2D batcher, int posX, int posY, Layer layer, byte animIndex)
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

            FileManager.Animations.AnimID = graphic;


            byte animGroup = FileManager.Animations.AnimGroup;

            var gr = layer == Layer.Invalid
                         ? FileManager.Animations.GetCorpseAnimationGroup(ref graphic, ref animGroup, ref color)
                         : FileManager.Animations.GetBodyAnimationGroup(ref graphic, ref animGroup, ref color);

            ref var direction = ref gr.Direction[FileManager.Animations.Direction];


            if ((direction.FrameCount == 0 || direction.Frames == null) && !FileManager.Animations.LoadDirectionGroup(ref direction))
                return;

            direction.LastAccessTime = Engine.Ticks;
            int fc = direction.FrameCount;

            if (fc > 0 && animIndex >= fc)
                animIndex = (byte) (fc - 1);

            if (animIndex < direction.FrameCount)
            {
                AnimationFrameTexture frame = direction.Frames[animIndex]; // FileManager.Animations.GetTexture(direction.FramesHashes[animIndex]);

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


                if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ViewRange)
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
                    if (Engine.Profile.Current.HighlightGameObjects && IsSelected) color = 0x0023;
                    ShaderHuesTraslator.GetHueVector(ref HueVector, color);
                }

                base.Draw(batcher, posX, posY);
                Select(IsFlipped ? posX + x + 44 - SelectedObject.TranslatedMousePositionByViewport.X : SelectedObject.TranslatedMousePositionByViewport.X - posX + x, SelectedObject.TranslatedMousePositionByViewport.Y - posY - y);
            }
        }

        public override void Select(int x, int y)
        {
            if (SelectedObject.Object == this)
                return;

            if (IsCorpse)
            {
                if (Texture.Contains( x, y))
                    SelectedObject.Object = this;
                //if (SelectedObject.IsPointInCorpse(this, x - Bounds.X, y - Bounds.Y))
                //{
                //    SelectedObject.Object = this;
                //}
            }
            else
            {
                if (SelectedObject.IsPointInStatic(Graphic, x - Bounds.X, y - Bounds.Y))
                    SelectedObject.Object = this;
            }
        }
    }
}