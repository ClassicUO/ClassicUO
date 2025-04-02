// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Sdk.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Services;

namespace ClassicUO.Game.GameObjects
{
    internal class DragEffect : GameEffect
    {
        private uint _lastMoveTime;

        public DragEffect
        (
            World world,
            EffectManager manager,
            uint src,
            uint trg,
            ushort xSource,
            ushort ySource,
            sbyte zSource,
            ushort xTarget,
            ushort yTarget,
            sbyte zTarget,
            ushort graphic,
            ushort hue,
            int duration,
            byte speed
        )
            : base(world, manager, graphic, hue, duration, speed)
        {
            var source = World.Get(src);

            if (SerialHelper.IsValid(src) && source != null)
            {
                SetSource(source);
            }
            else
            {
                SetSource(xSource, ySource, zSource);
            }

            var target = World.Get(trg);

            if (SerialHelper.IsValid(trg) && target != null)
            {
                SetTarget(target);
            }
            else
            {
                SetTarget(xTarget, yTarget, zTarget);
            }

            Hue = hue;
            Graphic = graphic;
        }

        public override void Update()
        {
            if (_lastMoveTime > Time.Ticks)
            {
                return;
            }

            Offset.X += 8;
            Offset.Y += 8;

            _lastMoveTime = Time.Ticks + 20;

            base.Update();
        }

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (IsDestroyed)
            {
                return false;
            }

            ushort hue;
            if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }
            else
            {
                hue = Hue;
            }

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue);

            //Engine.DebugInfo.EffectsRendered++;

            DrawStatic
            (
                batcher,
                AnimationGraphic,
                posX - ((int) Offset.X + 22),
                posY - ((int) -Offset.Y + 22),
                hueVec,
                depth
            );

            ref var data = ref ServiceProvider.Get<UOService>().FileManager.TileData.StaticData[Graphic];

            if (data.IsLight && Source != null)
            {
                var scene = ServiceProvider.Get<SceneService>().Scene as GameScene;
                scene?.AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}