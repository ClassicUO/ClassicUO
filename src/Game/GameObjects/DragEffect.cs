using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    internal class DragEffect : GameEffect
    {
        private uint _lastMoveTime;

        public DragEffect
        (
            uint src,
            uint trg,
            int xSource,
            int ySource,
            int zSource,
            int xTarget,
            int yTarget,
            int zTarget,
            ushort graphic,
            ushort hue
        )
        {
            Entity source = World.Get(src);

            if (SerialHelper.IsValid(src) && source != null)
            {
                SetSource(source);
            }
            else
            {
                SetSource(xSource, ySource, zSource);
            }


            Entity target = World.Get(trg);

            if (SerialHelper.IsValid(trg) && target != null)
            {
                SetTarget(target);
            }
            else
            {
                SetTarget(xTarget, yTarget, zTarget);
            }

            AlphaHue = 255;
            Hue = hue;
            Graphic = graphic;
            Load();
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (_lastMoveTime > Time.Ticks)
            {
                return;
            }

            Offset.X += 8;
            Offset.Y += 8;

            _lastMoveTime = Time.Ticks + 20;

            base.Update(totalTime, frameTime);
        }

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed)
            {
                return false;
            }

            ResetHueVector();


            if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                HueVector.X = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                HueVector.X = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else
            {
                ShaderHueTranslator.GetHueVector(ref HueVector, Hue);
            }

            //Engine.DebugInfo.EffectsRendered++;

            DrawStatic
                (batcher, AnimationGraphic, posX - ((int) Offset.X + 22), posY - ((int) -Offset.Y + 22), ref HueVector);

            ref StaticTiles data = ref TileDataLoader.Instance.StaticData[Graphic];

            if (data.IsLight && Source != null)
            {
                Client.Game.GetScene<GameScene>().AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}