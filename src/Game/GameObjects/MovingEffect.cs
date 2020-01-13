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

using ClassicUO.Configuration;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class MovingEffect : GameEffect
    {
        private uint _lastMoveTime;
        private int _distance;
        //private Vector2 _velocity;

        private MovingEffect(ushort graphic, ushort hue)
        {
            Hue = hue;
            Graphic = graphic;
            Load();
        }

        public MovingEffect(uint src, uint trg, int xSource, int ySource, int zSource, int xTarget, int yTarget, int zTarget, ushort graphic, ushort hue, bool fixedDir, byte speed) : this(graphic, hue)
        {
            FixedDir = fixedDir;

            if (speed > 20)
                speed = (byte) (speed - 20);

            MovingDelay = (byte) (20 - speed);

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



            Calculate(true);
        }

        public float AngleToTarget;
        public bool Explode;
        public readonly bool FixedDir;
        public byte MovingDelay = 10;


        private void Calculate(bool angle)
        {
            int playerX = World.Player.X;
            int playerY = World.Player.Y;
            int playerZ = World.Player.Z;

            int screenCenterX = ProfileManager.Current.GameWindowPosition.X + (ProfileManager.Current.GameWindowSize.X >> 1);
            int screenCenterY = ProfileManager.Current.GameWindowPosition.Y + (ProfileManager.Current.GameWindowSize.Y >> 1);


            (int sX, int sY, int sZ) = GetSource();
            int offsetSourceX = sX - playerX;
            int offsetSourceY = sY - playerY;
            int offsetSourceZ = sZ - playerZ;

            int screenSourceX = screenCenterX + (offsetSourceX - offsetSourceY) * 22;
            int screenSourceY = screenCenterY + (offsetSourceX + offsetSourceY) * 22 - offsetSourceZ * 4;


            (int tX, int tY, int tZ) = GetTarget();
            int offsetTargetX = tX - playerX;
            int offsetTargetY = tY - playerY;
            int offsetTargetZ = tZ - playerZ;

            int screenTargetX = screenCenterX + (offsetTargetX - offsetTargetY) * 22;
            int screenTargetY = screenCenterY + (offsetTargetX + offsetTargetY) * 22 - offsetTargetZ * 4;


            _distance = (int) Math.Sqrt(Math.Pow(screenSourceX - screenTargetX, 2) + Math.Pow(screenSourceY - screenTargetY, 2));

            //_velocity.X = (screenTargetX - screenSourceX) * (MovingDelay / (float) _distance);
            //_velocity.Y = (screenTargetY - screenSourceY) * (MovingDelay / (float) _distance);

            //Vector2.Normalize(ref _velocity, out _velocity);

            if (angle)
                AngleToTarget = (float) -Math.Atan2(screenTargetY - screenSourceY, screenTargetX - screenSourceX);
        }


        public override void Update(double totalMS, double frameMS)
        {
            if (_lastMoveTime > Time.Ticks)
                return;

            base.Update(totalMS, frameMS);

            _lastMoveTime = Time.Ticks + MovingDelay;

            if (Target != null && Target.IsDestroyed)
            {
                Destroy();
                return;
            }


            int playerX = World.Player.X;
            int playerY = World.Player.Y;
            int playerZ = World.Player.Z;

            int screenCenterX = ProfileManager.Current.GameWindowPosition.X + (ProfileManager.Current.GameWindowSize.X >> 1);
            int screenCenterY = ProfileManager.Current.GameWindowPosition.Y + (ProfileManager.Current.GameWindowSize.Y >> 1);


            (int sX, int sY, int sZ) = GetSource();
            int offsetSourceX = sX - playerX;
            int offsetSourceY = sY - playerY;
            int offsetSourceZ = sZ - playerZ;

            int screenSourceX = screenCenterX + (offsetSourceX - offsetSourceY) * 22;
            int screenSourceY = screenCenterY + (offsetSourceX + offsetSourceY) * 22 - offsetSourceZ * 4;


            (int tX, int tY, int tZ) = GetTarget();
            int offsetTargetX = tX - playerX;
            int offsetTargetY = tY - playerY;
            int offsetTargetZ = tZ - playerZ;

            int screenTargetX = screenCenterX + (offsetTargetX - offsetTargetY) * 22;
            int screenTargetY = screenCenterY + (offsetTargetX + offsetTargetY) * 22 - offsetTargetZ * 4;


            int offX = (screenSourceX - screenTargetX) + (int) (Offset.X);
            int offY = (screenSourceY - screenTargetY) + (int) (Offset.Y);


            int startX = screenSourceX + offX;
            int startY = screenSourceY + offY;


            int realDrawX = screenSourceX + (int) (Offset.X);
            int realDrawY = screenSourceY + (int) (Offset.Y);
            int drawDestX = screenTargetX;
            int drawDestY = screenTargetY;

            int[] deltaXY =
            {
                Math.Abs(drawDestX - realDrawX), Math.Abs(drawDestY - realDrawY)
            };
            int x = 0;

            if (deltaXY[0] < deltaXY[1])
            {
                x = 1;
                int temp = deltaXY[0];
                deltaXY[0] = deltaXY[1];
                deltaXY[1] = temp;
            }

            int delta = deltaXY[0];
            int stepXY = 0;
            const int EFFECT_SPEED = 5;

            int[] tempXY =
            {
                EFFECT_SPEED, 0
            };

            for (int j = 0; j < EFFECT_SPEED; j++)
            {
                stepXY += deltaXY[1];

                if (stepXY >= delta)
                {
                    tempXY[1]++;
                    stepXY -= deltaXY[0];
                }
            }

            if (realDrawX < drawDestX)
            {
                realDrawX += tempXY[x];

                if (realDrawX > drawDestX)
                    realDrawX = drawDestX;
            }
            else
            {
                realDrawX -= tempXY[x];

                if (realDrawX < drawDestX)
                    realDrawX = drawDestX;
            }

            if (realDrawY < drawDestY)
            {
                realDrawY += tempXY[(x + 1) % 2];

                if (realDrawY > drawDestY)
                    realDrawY = drawDestY;
            }
            else
            {
                realDrawY -= tempXY[(x + 1) % 2];

                if (realDrawY < drawDestY)
                    realDrawY = drawDestY;
            }

            int newOffsetX = (realDrawX - screenCenterX) / 22;
            int newOffsetY = (realDrawY - screenCenterY) / 22;
            int newCoordX = 0;
            int newCoordY = 0;
            TileOffsetOnMonitorToXY(ref newOffsetX, ref newOffsetY, ref newCoordX, ref newCoordY);
            int newX = playerX + newCoordX;
            int newY = playerY + newCoordY;

            if (newX == tX && newY == tY)
            {
                Destroy();
                return;
            }


            int newDrawX = screenCenterX + (newCoordX - newCoordY) * 22;
            int newDrawY = screenCenterY + ((newCoordX + newCoordY) * 22 - (offsetSourceZ * 4));
            Offset.X = realDrawX - newDrawX;
            Offset.Y = realDrawY - newDrawY;
            IsPositionChanged = true;


            int distanceNow = (int) Math.Sqrt(Math.Pow(startX - screenTargetX, 2) + Math.Pow(startY - screenTargetY, 2));

            if (distanceNow <= _distance)
            {
                Destroy();
                return;
            }

            if (newX != sX || newY != sY)
            {
                SetSource(newX, newY, sZ);
                Calculate(false);
            }

            //Offset.X += _velocity.X/* * (float) frameMS*/;
            //Offset.Y += _velocity.Y/* * (float) frameMS*/;
        }

        private static void TileOffsetOnMonitorToXY(ref int ofsX, ref int ofsY, ref int x, ref int y)
        {
            if (ofsX == 0)
                x = y = ofsY >> 1;
            else if (ofsY == 0)
            {
                x = ofsX >> 1;
                y = -x;
            }
            else
            {
                int absX = Math.Abs(ofsX);
                int absY = Math.Abs(ofsY);
                x = ofsX;

                if (ofsY > ofsX)
                {
                    if (ofsX < 0 && ofsY < 0)
                        y = absX - absY;
                    else if (ofsX > 0 && ofsY > 0)
                        y = absY - absX;
                }
                else if (ofsX > ofsY)
                {
                    if (ofsX < 0 && ofsY < 0)
                        y = -(absY - absX);
                    else if (ofsX > 0 && ofsY > 0)
                        y = -(absX - absY);
                }

                if (y == 0 && ofsY != ofsX)
                {
                    if (ofsY < 0)
                        y = -(absX + absY);
                    else
                        y = absX + absY;
                }

                y /= 2;
                x += y;
            }
        }
    }
}