using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class MovingEffect
    {
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed || !AllowedToDraw)
            {
                return false;
            }

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

            ShaderHueTranslator.GetHueVector(ref HueVector, hue);

            //Engine.DebugInfo.EffectsRendered++;

            if (FixedDir)
            {
                DrawStatic(batcher, AnimationGraphic, posX, posY, ref HueVector);
            }
            else
            {
                posX += (int) Offset.X;
                posY += (int) (Offset.Y + Offset.Z);

                DrawStaticRotated(batcher, AnimationGraphic, posX, posY, 0, 0, AngleToTarget, ref HueVector);
            }


            ref StaticTiles data = ref TileDataLoader.Instance.StaticData[AnimationGraphic];

            if (data.IsLight && Source != null)
            {
                Client.Game.GetScene<GameScene>().AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}