using ClassicUO.Configuration;
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

            if (IsStretched)
            {
                posY += Z << 2;

                DrawLand
                (
                    batcher, Graphic, posX, posY, ref Rectangle, ref Normal0, ref Normal1, ref Normal2, ref Normal3,
                    ref HueVector
                );

                if (SelectedObject.IsPointInStretchedLand(ref Rectangle, posX, posY))
                {
                    SelectedObject.Object = this;
                }
            }
            else
            {
                DrawLand(batcher, Graphic, posX, posY, ref HueVector);

                if (SelectedObject.IsPointInLand(posX, posY))
                {
                    SelectedObject.Object = this;
                }
            }

            return true;
        }
    }
}