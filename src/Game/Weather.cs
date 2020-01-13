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
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace ClassicUO.Game
{
    class Weather
    {
        private class WeatherEffect
        {
            public float SpeedX, SpeedY, X, Y, ScaleRatio, SpeedAngle, SpeedMagnitude;
            public uint ID;
        }

        enum WEATHER_TYPE
        {
            WT_RAIN = 0,
            WT_FIERCE_STORM,
            WT_SNOW,
            WT_STORM
        };


        public byte Type, Count, CurrentCount, Temperature;
        public sbyte Wind;
        public uint Timer, WindTimer, LastTick;
        public float SimulationRation = 37.0f;

        private readonly List<WeatherEffect> _effects = new List<WeatherEffect>();
        private Vector3 _hueVector;
        public sbyte? CurrentWeather { get; set; }

        private float SinOscillate(float freq, int range, uint current_tick)
        {
            float anglef = (int) ((current_tick / 2.7777f) * freq) % 360;
            return Math.Sign(MathHelper.ToRadians(anglef)) * range;
        }

        public void Reset()
        {
            Type = Count = CurrentCount = Temperature = 0;
            Wind = 0;
            WindTimer = Timer = 0;
            CurrentWeather = null;

            _effects.Clear();
        }

        public void Generate()
        {
            LastTick = Time.Ticks;

            if (Type == 0xFF || Type == 0xFE)
                return;

            //int drawX = ProfileManager.Current.GameWindowPosition.X;
            //int drawY = ProfileManager.Current.GameWindowPosition.Y;

            if (Count > 70)
                Count = 70;

            WindTimer = 0;

            while (CurrentCount < Count)
            {
                WeatherEffect effect = new WeatherEffect()
                {
                    X = RandomHelper.GetValue( 0, ProfileManager.Current.GameWindowSize.X),
                    Y = RandomHelper.GetValue(0, ProfileManager.Current.GameWindowSize.Y)
                };

                _effects.Add(effect);

                CurrentCount++;
            }
        }

        public void Draw(UltimaBatcher2D batcher, int x, int y)
        {
            bool removeEffects = false;

            if (Timer < Time.Ticks)
            {
                if (CurrentCount == 0)
                    return;

                removeEffects = true;
            }
            else if (Type == 0xFF || Type == 0xFE)
                return;

            uint passed = Time.Ticks - LastTick;

            if (passed > 7000)
            {
                LastTick = Time.Ticks;
                passed = 25;
            }

            bool windChanged = false;

            if (WindTimer < Time.Ticks)
            {
                if (WindTimer == 0)
                    windChanged = true;

                WindTimer = Time.Ticks + (uint)(RandomHelper.GetValue(7, 13) * 1000);

                sbyte lastWind = Wind;

                Wind = (sbyte) RandomHelper.GetValue(0, 4);

                if (RandomHelper.GetValue(0, 2) != 0)
                    Wind *= -1;

                if (Wind < 0 && lastWind > 0)
                {
                    Wind = 0;
                }
                else if (Wind > 0 && lastWind < 0)
                {
                    Wind = 0;
                }

                if (lastWind != Wind)
                {
                    windChanged = true;
                }
            }

            //switch ((WEATHER_TYPE) Type)
            //{
            //    case WEATHER_TYPE.WT_RAIN:
            //    case WEATHER_TYPE.WT_FIERCE_STORM:
            //        // TODO: set color
            //        break;
            //    case WEATHER_TYPE.WT_SNOW:
            //    case WEATHER_TYPE.WT_STORM:
            //        // TODO: set color
            //        break;
            //    default:
            //        break;
            //}

            //Point winpos = ProfileManager.Current.GameWindowPosition;
            Point winsize = ProfileManager.Current.GameWindowSize;

            for (int i = 0; i < _effects.Count; i++)
            {
                var effect = _effects[i];

                if (effect.X < x || effect.X > x + winsize.X ||
                    effect.Y < y || effect.Y > y + winsize.Y)
                {
                    if (removeEffects)
                    {
                        _effects.RemoveAt(i--);

                        if (CurrentCount > 0)
                            CurrentCount--;
                        else
                            CurrentCount = 0;

                        continue;
                    }

                    effect.X = x + RandomHelper.GetValue(0, winsize.X);
                    effect.Y = y + RandomHelper.GetValue(0, winsize.Y);
                }


                switch ((WEATHER_TYPE)Type)
                {
                    case WEATHER_TYPE.WT_RAIN:
                        float scaleRation = effect.ScaleRatio;
                        effect.SpeedX = -4.5f - scaleRation;
                        effect.SpeedY = 5.0f + scaleRation;
                        break;
                    case WEATHER_TYPE.WT_FIERCE_STORM:
                        effect.SpeedX = Wind;
                        effect.SpeedY = 6.0f;
                        break;
                    case WEATHER_TYPE.WT_SNOW:
                    case WEATHER_TYPE.WT_STORM:

                        if (Type == (byte)WEATHER_TYPE.WT_SNOW)
                        {
                            effect.SpeedX = Wind;
                            effect.SpeedY = 1.0f;
                        }
                        else
                        {
                            effect.SpeedX = Wind * 1.5f;
                            effect.SpeedY = 1.5f;
                        }

                        if (windChanged)
                        {
                            effect.SpeedAngle = MathHelper.ToDegrees((float)Math.Atan2(effect.SpeedX, effect.SpeedY));
                            effect.SpeedMagnitude =
                                (float)Math.Sqrt(Math.Pow(effect.SpeedX, 2) + Math.Pow(effect.SpeedY, 2));
                        }

                        float speed_angle = effect.SpeedAngle;
                        float speed_magnitude = effect.SpeedMagnitude;

                        speed_magnitude += effect.ScaleRatio;

                        speed_angle += SinOscillate(0.4f, 20, Time.Ticks + effect.ID);

                        var rad = MathHelper.ToRadians(speed_angle);
                        effect.SpeedX = speed_magnitude * (float)Math.Sin(rad);
                        effect.SpeedY = speed_magnitude * (float)Math.Cos(rad);

                        break;
                }

                float speedOffset = passed / SimulationRation;

                switch ((WEATHER_TYPE)Type)
                {
                    case WEATHER_TYPE.WT_RAIN:
                    case WEATHER_TYPE.WT_FIERCE_STORM:

                        int oldX = (int)effect.X;
                        int oldY = (int)effect.Y;

                        float ofsx = effect.SpeedX * speedOffset;
                        float ofsy = effect.SpeedY * speedOffset;

                        effect.X += ofsx;
                        effect.Y += ofsy;

                        const float MAX_OFFSET_XY = 5.0f;

                        if (ofsx >= MAX_OFFSET_XY)
                        {
                            oldX = (int)(effect.X - MAX_OFFSET_XY);
                        }
                        else if (ofsx <= -MAX_OFFSET_XY)
                        {
                            oldX = (int)(effect.X + MAX_OFFSET_XY);
                        }

                        if (ofsy >= MAX_OFFSET_XY)
                        {
                            oldY = (int)(effect.Y - MAX_OFFSET_XY);
                        }
                        else if (oldY <= -MAX_OFFSET_XY)
                        {
                            oldY = (int)(effect.Y + MAX_OFFSET_XY);
                        }

                        int startX = x + oldX;
                        int startY = y + oldY;
                        int endX = x + (int) effect.X;
                        int endY = y + (int) effect.Y;

                        batcher.DrawLine(Texture2DCache.GetTexture(Color.Gray), startX, startY, endX, endY, startX + (endX - startX) / 2, startY + (endY - startY) / 2);
                        break;
                    case WEATHER_TYPE.WT_SNOW:
                    case WEATHER_TYPE.WT_STORM:

                        effect.X += effect.SpeedX * speedOffset;
                        effect.Y += effect.SpeedY * speedOffset;

                        batcher.Draw2D(Texture2DCache.GetTexture(Color.White),
                            x + (int)effect.X, y + (int)effect.Y, 2, 2, ref _hueVector);

                        break;
                }

            }

            LastTick = Time.Ticks;
        }
    }
}
