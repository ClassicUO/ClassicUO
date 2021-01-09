using ClassicUO.Configuration;
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

            bool isTree = StaticFilters.IsTree(graphic, out _);

            if (isTree && ProfileManager.CurrentProfile.TreeToStumps)
            {
                graphic = Constants.TREE_REPLACE_GRAPHIC;
            }

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