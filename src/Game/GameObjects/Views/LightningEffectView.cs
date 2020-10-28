using ClassicUO.Configuration;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class LightningEffect
    {
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            ResetHueVector();

            ushort hue = Hue;

            if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }

            ShaderHueTranslator.GetHueVector(ref HueVector, hue, false, 0);
            HueVector.Y = ShaderHueTranslator.SHADER_LIGHTS;

            //Engine.DebugInfo.EffectsRendered++;

            ref UOFileIndex index = ref GumpsLoader.Instance.GetValidRefEntry(AnimationGraphic);

            posX -= index.Width >> 1;
            posY -= index.Height;

            batcher.SetBlendState(BlendState.Additive);
            DrawGump(batcher, AnimationGraphic, posX, posY, ref HueVector);
            batcher.SetBlendState(null);

            return true;
        }
    }
}