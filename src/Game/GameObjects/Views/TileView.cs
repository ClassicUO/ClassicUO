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
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Land
    {
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            //Engine.DebugInfo.LandsRendered++;

            ResetHueVector();

            ushort hue = Hue;

            if (ProfileManager.CurrentProfile.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
            }
            else if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }


            if (hue != 0)
            {
                HueVector.X = hue - 1;
                HueVector.Y = IsStretched ? ShaderHueTranslator.SHADER_LAND_HUED : ShaderHueTranslator.SHADER_HUED;
            }
            else
            {
                HueVector.Y = IsStretched ? ShaderHueTranslator.SHADER_LAND : ShaderHueTranslator.SHADER_NONE;
            }
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
                if (UOClassicCombatCollection.LandFieldPreview(this))
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
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.HueImpassableView)
            {
                if (this.TileData.IsImpassable)
                {
                    HueVector.X = ProfileManager.CurrentProfile.HueImpassableViewHue;
                    HueVector.Y = 1;
                }
            }
            // ## BEGIN - END ## //

            if (IsStretched)
            {
                posY += Z << 2;

                // ## BEGIN - END ## // ORIG
                /*
                DrawLand
                (
                    batcher, Graphic, posX, posY, ref Rectangle, ref Normal0, ref Normal1, ref Normal2, ref Normal3,
                    ref HueVector
                );
                */
                // ## BEGIN - END ## //
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.WireFrameView)
                {
                    DrawLandWF
                    (
                        batcher, Graphic, posX, posY, ref Rectangle, ref Normal0, ref Normal1, ref Normal2, ref Normal3,
                        ref HueVector, this.TileData.IsImpassable
                    );
                }
                else
                {
                    DrawLand
                    (
                        batcher, Graphic, posX, posY, ref Rectangle, ref Normal0, ref Normal1, ref Normal2, ref Normal3,
                        ref HueVector
                    );
                }
                // ## BEGIN - END ## //

                if (SelectedObject.IsPointInStretchedLand(ref Rectangle, posX, posY))
                {
                    SelectedObject.Object = this;
                }
            }
            else
            {
                // ## BEGIN - END ## // ORIG
                //DrawLand(batcher, Graphic, posX, posY, ref HueVector);
                // ## BEGIN - END ## //
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.WireFrameView)
                {
                    DrawLandWF(batcher, Graphic, posX, posY, ref HueVector, this.TileData.IsImpassable);
                }
                else
                {
                    DrawLand(batcher, Graphic, posX, posY, ref HueVector);
                }
                // ## BEGIN - END ## //

                if (SelectedObject.IsPointInLand(posX, posY))
                {
                    SelectedObject.Object = this;
                }
            }

            return true;
        }
    }
}