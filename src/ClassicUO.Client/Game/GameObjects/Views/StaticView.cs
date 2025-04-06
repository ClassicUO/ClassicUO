// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.Sdk.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using ClassicUO.Services;

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

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            ushort graphic = Graphic;
            ushort hue = Hue;
            bool partial = ItemData.IsPartialHue;

            if (ProfileManager.CurrentProfile.HighlightGameObjects && SelectedObject.Object == this)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                partial = false;
            }
            else if (
                ProfileManager.CurrentProfile.NoColorObjectsOutOfRange
                && Distance > World.ClientViewRange
            )
            {
                hue = Constants.OUT_RANGE_COLOR;
                partial = false;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
                partial = false;
            }

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, partial, AlphaHue / 255f);

            bool isTree = StaticFilters.IsTree(graphic, out _);

            if (isTree && ProfileManager.CurrentProfile.TreeToStumps)
            {
                graphic = Constants.TREE_REPLACE_GRAPHIC;
            }

            DrawStaticAnimated(
                batcher,
                graphic,
                posX,
                posY,
                hueVec,
                ProfileManager.CurrentProfile.ShadowsEnabled
                    && ProfileManager.CurrentProfile.ShadowsStatics
                    && (isTree || ItemData.IsFoliage || StaticFilters.IsRock(graphic)),
                depth,
                ProfileManager.CurrentProfile.AnimatedWaterEffect && ItemData.IsWet
            );

            if (ItemData.IsLight)
            {
                ServiceProvider.Get<SceneService>().GetScene<GameScene>()?.AddLight(this, this, posX + 22, posY + 22);
            }

            return true;
        }

        public override bool CheckMouseSelection()
        {
            if (
                !(
                    SelectedObject.Object == this
                    || FoliageIndex != -1
                        && ServiceProvider.Get<SceneService>().GetScene<GameScene>().FoliageIndex == FoliageIndex
                )
            )
            {
                ushort graphic = Graphic;

                bool isTree = StaticFilters.IsTree(graphic, out _);

                if (isTree && ProfileManager.CurrentProfile.TreeToStumps)
                {
                    graphic = Constants.TREE_REPLACE_GRAPHIC;
                }

                var uoService = ServiceProvider.Get<UOService>();
                ref var index = ref uoService.FileManager.Arts.File.GetValidRefEntry(graphic + 0x4000);

                Point position = RealScreenPosition;
                position.X -= index.Width;
                position.Y -= index.Height;

                return uoService.Arts.PixelCheck(
                    graphic,
                    SelectedObject.TranslatedMousePositionByViewport.X - position.X,
                    SelectedObject.TranslatedMousePositionByViewport.Y - position.Y
                );
            }

            return false;
        }
    }
}
