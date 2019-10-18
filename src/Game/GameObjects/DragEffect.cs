using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            sbyte zSourceB = (sbyte)zSource;
            sbyte zTargB = (sbyte)zTarget;

            if (src.IsValid)
            {
                Entity source = World.Get(src);

                if (source is Mobile mobile)
                {
                    SetSource(mobile.Position.X, mobile.Position.Y, mobile.Position.Z);

                    //if (mobile != World.Player && !mobile.IsMoving && (xSource | ySource | zSource) != 0)
                    //    mobile.Position = new Position((ushort) xSource, (ushort) ySource, zSourceB);
                }
                else if (source is Item)
                {
                    SetSource(source.Position.X, source.Position.Y, source.Position.Z);

                    //if ((xSource | ySource | zSource) != 0)
                    //    source.Position = new Position((ushort) xSource, (ushort) ySource, zSourceB);
                }
                else
                    SetSource(xSource, ySource, zSourceB);
            }
            else
                SetSource(xSource, ySource, zSource);

            if (trg.IsValid)
            {
                Entity target = World.Get(trg);

                if (target is Mobile mobile)
                {
                    SetTarget(target);

                    //if (mobile != World.Player && !mobile.IsMoving && (xTarget | yTarget | zTarget) != 0)
                    //    mobile.Position = new Position((ushort) xTarget, (ushort) yTarget, zTargB);
                }
                else if (target is Item)
                {
                    SetTarget(target);

                    //if ((xTarget | yTarget | zTarget) != 0)
                    //    target.Position = new Position((ushort) xTarget, (ushort) yTarget, zTargB);
                }
                else
                    SetTarget(xTarget, yTarget, zTargB);
            }
            else
                SetTarget(xTarget, yTarget, zTargB);


            AlphaHue = 255;
            Hue = hue;
            Graphic = graphic;
            Load();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_lastMoveTime > Engine.Ticks)
                return;
            
            Offset.X += 8;
            Offset.Y += 8;

            _lastMoveTime = Engine.Ticks + 20;

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
                Texture = FileManager.Art.GetTexture(AnimationGraphic);
                Bounds.X = 0;
                Bounds.Y = 0;
                Bounds.Width = Texture.Width;
                Bounds.Height = Texture.Height;
            }

            Bounds.X = (int) Offset.X + 22;
            Bounds.Y = (int) -Offset.Y + 22;

            if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                HueVector.X = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
            {
                HueVector.X = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else
                ShaderHuesTraslator.GetHueVector(ref HueVector, Hue);

            Engine.DebugInfo.EffectsRendered++;
            base.Draw(batcher, posX, posY);

            ref readonly StaticTiles data = ref FileManager.TileData.StaticData[_displayedGraphic];

            if (data.IsLight && (Source is Item || Source is Static || Source is Multi))
            {
                Engine.SceneManager.GetScene<GameScene>()
                      .AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}
