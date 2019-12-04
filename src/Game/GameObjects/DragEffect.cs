using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    class DragEffect : GameEffect
    {
        private Graphic _displayedGraphic = Graphic.INVALID;
        private uint _lastMoveTime;


        public DragEffect(Serial src, Serial trg, int xSource, int ySource, int zSource, int xTarget, int yTarget, int zTarget, Graphic graphic, Hue hue)
        {
            Entity source = World.Get(src);

            if (src.IsValid && source != null)
                SetSource(source);
            else
                SetSource(xSource, ySource, zSource);


            Entity target = World.Get(trg);

            if (trg.IsValid && target != null)
                SetTarget(target);
            else
                SetTarget(xTarget, yTarget, zTarget);

            AlphaHue = 255;
            Hue = hue;
            Graphic = graphic;
            Load();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_lastMoveTime > Time.Ticks)
                return;
            
            Offset.X += 8;
            Offset.Y += 8;

            _lastMoveTime = Time.Ticks + 20;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed)
                return false;

            ResetHueVector();

            if (AnimationGraphic != _displayedGraphic || Texture == null || Texture.IsDisposed)
            {
                _displayedGraphic = AnimationGraphic;
                Texture = UOFileManager.Art.GetTexture(AnimationGraphic);
                Bounds.X = 0;
                Bounds.Y = 0;
                Bounds.Width = Texture.Width;
                Bounds.Height = Texture.Height;
            }

            Bounds.X = (int) Offset.X + 22;
            Bounds.Y = (int) -Offset.Y + 22;

            if (ProfileManager.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                HueVector.X = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && ProfileManager.Current.EnableBlackWhiteEffect)
            {
                HueVector.X = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else
                ShaderHuesTraslator.GetHueVector(ref HueVector, Hue);

            //Engine.DebugInfo.EffectsRendered++;
            base.Draw(batcher, posX, posY);

            ref readonly StaticTiles data = ref UOFileManager.TileData.StaticData[_displayedGraphic];

            if (data.IsLight && Source != null)
            {
                CUOEnviroment.Client.GetScene<GameScene>()
                      .AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}
