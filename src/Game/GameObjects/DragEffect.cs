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
        private ushort _displayedGraphic = 0xFFFF;
        private uint _lastMoveTime;


        public DragEffect(uint src, uint trg, int xSource, int ySource, int zSource, int xTarget, int yTarget, int zTarget, ushort graphic, ushort hue)
        {
            Entity source = World.Get(src);

            if (SerialHelper.IsValid(src) && source != null)
                SetSource(source);
            else
                SetSource(xSource, ySource, zSource);


            Entity target = World.Get(trg);

            if (SerialHelper.IsValid(trg) && target != null)
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
                Texture = ArtLoader.Instance.GetTexture(AnimationGraphic);
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

            ref readonly StaticTiles data = ref TileDataLoader.Instance.StaticData[_displayedGraphic];

            if (data.IsLight && Source != null)
            {
                Client.Game.GetScene<GameScene>()
                      .AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}
