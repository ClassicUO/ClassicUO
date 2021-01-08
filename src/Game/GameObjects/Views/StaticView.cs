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
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Static
    {
        private int _canBeTransparent;

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

            ushort graphic = Graphic;
            ushort hue = Hue;
            bool partial = ItemData.IsPartialHue;

            ResetHueVector();

            if (ProfileManager.CurrentProfile.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                partial = false;
            }
            else if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
                partial = false;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
                partial = false;
            }

            ShaderHueTranslator.GetHueVector(ref HueVector, hue, partial, 0);

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
                if (UOClassicCombatCollection.StaticFieldPreview(this))
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
            // ## BEGIN - END ## // ORIG
            /*
            bool isTree = StaticFilters.IsTree(graphic, out _);

            if (isTree && ProfileManager.CurrentProfile.TreeToStumps)
            {
                graphic = Constants.TREE_REPLACE_GRAPHIC;
            }
            */
            // ## BEGIN - END ## // ORIG
            graphic = UOClassicCombatCollection.ArtloaderFilters(graphic);

            bool isTree = StaticFilters.IsTree(graphic, out _);
            // ## BEGIN - END ## //

            if (AlphaHue != 255)
            {
                HueVector.Z = 1f - AlphaHue / 255f;
            }

            DrawStaticAnimated(batcher, graphic, posX, posY, ref HueVector, ref DrawTransparent, ProfileManager.CurrentProfile.ShadowsEnabled && ProfileManager.CurrentProfile.ShadowsStatics && (isTree || ItemData.IsFoliage || StaticFilters.IsRock(graphic)));

            if (ItemData.IsLight)
            {
                Client.Game.GetScene<GameScene>().AddLight(this, this, posX + 22, posY + 22);
            }

            if (!(SelectedObject.Object == this ||
                  FoliageIndex != -1 && Client.Game.GetScene<GameScene>().FoliageIndex == FoliageIndex))
            {
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