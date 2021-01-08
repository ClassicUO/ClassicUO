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

using ClassicUO.Configuration;
// ## BEGIN - END ## //
using ClassicUO.Game.InteropServices.Runtime.UOClassicCombat;
// ## BEGIN - END ## //
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Multi
    {
        private int _canBeTransparent;
        public bool IsHousePreview;

        public override bool TransparentTest(int z)
        {
            bool r = true;

            if (Z <= z - ItemData.Height)
            {
                r = false;
            }
            else if (z < Z && (_canBeTransparent & 0xFF) == 0)
            {
                r = false;
            }

            return r;
        }

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            ResetHueVector();

            ushort hue = Hue;

            if (State != 0)
            {
                if ((State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER) != 0)
                {
                    return false;
                }

                if ((State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) != 0)
                {
                    hue = 0x002B;
                }

                if ((State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT) != 0)
                {
                    if (AlphaHue >= 192)
                    {
                        AlphaHue = 0xFF;
                    }
                    else
                    {
                        ProcessAlpha(192);
                    }
                }
            }

            ResetHueVector();

            ushort graphic = Graphic;
            bool partial = ItemData.IsPartialHue;

            Profile currentProfile = ProfileManager.CurrentProfile;

            if (currentProfile.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                partial = false;
            }
            else if (currentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
                partial = false;
            }
            else if (World.Player.IsDead && currentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
                partial = false;
            }

            ShaderHueTranslator.GetHueVector(ref HueVector, hue, partial, 0);

            //Engine.DebugInfo.MultiRendered++;

            // ## BEGIN - END ## //
            if (ProfileManager.CurrentProfile.HighlightTileAtRange && Distance == ProfileManager.CurrentProfile.HighlightTileAtRangeRange)
            {
                HueVector.X = ProfileManager.CurrentProfile.HighlightTileRangeHue;
                HueVector.Y = 1;
            }
            if (ProfileManager.CurrentProfile.HighlightTileAtRangeSpell)
            {
                if (GameCursor._spellTime >= 1 && Distance == ProfileManager.CurrentProfile.HighlightTileAtRangeRangeSpell)
                {
                    HueVector.X = ProfileManager.CurrentProfile.HighlightTileRangeHueSpell;
                    HueVector.Y = 1;
                }
            }
            if (ProfileManager.CurrentProfile.PreviewFields)
            {
                if (UOClassicCombatCollection.MultiFieldPreview(this))
                {
                    HueVector.X = 0x0040;
                    HueVector.Y = 1;
                }
                if (SelectedObject.LastObject == this)
                {
                    HueVector.X = 0x0023;
                    HueVector.Y = 1;
                }
            }
            // ## BEGIN - END ## //

            if (IsHousePreview)
            {
                HueVector.Z = 0.5f;
            }

            posX += (int) Offset.X;
            posY += (int) (Offset.Y + Offset.Z);

            if (AlphaHue != 255)
            {
                HueVector.Z = 1f - AlphaHue / 255f;
            }

            DrawStaticAnimated(batcher, graphic, posX, posY, ref HueVector, ref DrawTransparent, false);

            if (ItemData.IsLight)
            {
                Client.Game.GetScene<GameScene>().AddLight(this, this, posX + 22, posY + 22);
            }

            if (!(SelectedObject.Object == this || IsHousePreview ||
                  FoliageIndex != -1 && Client.Game.GetScene<GameScene>().FoliageIndex == FoliageIndex))
            {
                if (State != 0)
                {
                    if ((State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER |
                                  CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_PREVIEW)) != 0)
                    {
                        return true;
                    }
                }

                if (DrawTransparent)
                {
                    return true;
                }

                ref UOFileIndex index = ref ArtLoader.Instance.GetValidRefEntry(graphic + 0x4000);

                posX -= index.Width;
                posY -= index.Height;

                if (SelectedObject.IsPointInStatic(ArtLoader.Instance.GetTexture(graphic), posX, posY))
                {
                    SelectedObject.Object = this;
                }
            }

            return true;
        }
    }
}