#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using ClassicUO.Configuration;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace ClassicUO.Game
{
    internal class Weather
    {
        private const int MAX_WEATHER_EFFECT = 70;

        private readonly WeatherEffect[] _effects = new WeatherEffect[MAX_WEATHER_EFFECT];
        private Vector3 _hueVector;
        public sbyte? CurrentWeather { get; set; }
        public float SimulationRation = 37.0f;
        public uint Timer, WindTimer, LastTick;


        public byte Type, Count, CurrentCount, Temperature;
        public sbyte Wind;

        private float SinOscillate(float freq, int range, uint current_tick)
        {
            float anglef = (int) (current_tick / 2.7777f * freq) % 360;

            return Math.Sign(MathHelper.ToRadians(anglef)) * range;
        }

        public void Reset()
        {
            Type = Count = CurrentCount = Temperature = 0;
            Wind = 0;
            WindTimer = Timer = 0;
            CurrentWeather = null;
        }

        public void Generate()
        {
            LastTick = Time.Ticks;

            if (Type == 0xFF || Type == 0xFE)
            {
                return;
            }

            if (Count > MAX_WEATHER_EFFECT)
            {
                Count = MAX_WEATHER_EFFECT;
            }

            WindTimer = 0;

            while (CurrentCount < Count)
            {
                ref WeatherEffect effect = ref _effects[CurrentCount++];
                effect.X = RandomHelper.GetValue(0, ProfileManager.CurrentProfile.GameWindowSize.X);
                effect.Y = RandomHelper.GetValue(0, ProfileManager.CurrentProfile.GameWindowSize.Y);
            }
        }

        public void Draw(UltimaBatcher2D batcher, int x, int y)
        {
            bool removeEffects = false;

            if (Timer < Time.Ticks)
            {
                if (CurrentCount == 0)
                {
                    return;
                }

                removeEffects = true;
            }
            else if (Type == 0xFF || Type == 0xFE)
            {
                return;
            }

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
                {
                    windChanged = true;
                }

                WindTimer = Time.Ticks + (uint) (RandomHelper.GetValue(7, 13) * 1000);

                sbyte lastWind = Wind;

                Wind = (sbyte) RandomHelper.GetValue(0, 4);

                if (RandomHelper.GetValue(0, 2) != 0)
                {
                    Wind *= -1;
                }

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

            //Point winpos = ProfileManager.CurrentProfile.GameWindowPosition;
            Point winsize = ProfileManager.CurrentProfile.GameWindowSize;

            for (int i = 0; i < CurrentCount; i++)
            {
                ref WeatherEffect effect = ref _effects[i];

                if (effect.X < x || effect.X > x + winsize.X || effect.Y < y || effect.Y > y + winsize.Y)
                {
                    if (removeEffects)
                    {
                        if (CurrentCount > 0)
                        {
                            CurrentCount--;
                        }
                        else
                        {
                            CurrentCount = 0;
                        }

                        continue;
                    }

                    effect.X = x + RandomHelper.GetValue(0, winsize.X);
                    effect.Y = y + RandomHelper.GetValue(0, winsize.Y);
                }


                switch ((WEATHER_TYPE) Type)
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

                        if (Type == (byte) WEATHER_TYPE.WT_SNOW)
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
                            effect.SpeedAngle = MathHelper.ToDegrees((float) Math.Atan2(effect.SpeedX, effect.SpeedY));

                            effect.SpeedMagnitude = (float) Math.Sqrt(Math.Pow(effect.SpeedX, 2) + Math.Pow(effect.SpeedY, 2));
                        }

                        float speed_angle = effect.SpeedAngle;
                        float speed_magnitude = effect.SpeedMagnitude;

                        speed_magnitude += effect.ScaleRatio;

                        speed_angle += SinOscillate(0.4f, 20, Time.Ticks + effect.ID);

                        float rad = MathHelper.ToRadians(speed_angle);
                        effect.SpeedX = speed_magnitude * (float) Math.Sin(rad);
                        effect.SpeedY = speed_magnitude * (float) Math.Cos(rad);

                        break;
                }

                float speedOffset = passed / SimulationRation;

                switch ((WEATHER_TYPE) Type)
                {
                    case WEATHER_TYPE.WT_RAIN:
                    case WEATHER_TYPE.WT_FIERCE_STORM:

                        int oldX = (int) effect.X;
                        int oldY = (int) effect.Y;

                        float ofsx = effect.SpeedX * speedOffset;
                        float ofsy = effect.SpeedY * speedOffset;

                        effect.X += ofsx;
                        effect.Y += ofsy;

                        const float MAX_OFFSET_XY = 5.0f;

                        if (ofsx >= MAX_OFFSET_XY)
                        {
                            oldX = (int) (effect.X - MAX_OFFSET_XY);
                        }
                        else if (ofsx <= -MAX_OFFSET_XY)
                        {
                            oldX = (int) (effect.X + MAX_OFFSET_XY);
                        }

                        if (ofsy >= MAX_OFFSET_XY)
                        {
                            oldY = (int) (effect.Y - MAX_OFFSET_XY);
                        }
                        else if (oldY <= -MAX_OFFSET_XY)
                        {
                            oldY = (int) (effect.Y + MAX_OFFSET_XY);
                        }

                        int startX = x + oldX;
                        int startY = y + oldY;
                        int endX = x + (int) effect.X;
                        int endY = y + (int) effect.Y;

                        batcher.DrawLine
                        (
                            SolidColorTextureCache.GetTexture(Color.Gray),
                            startX,
                            startY,
                            endX,
                            endY,
                            startX + (endX - startX) / 2,
                            startY + (endY - startY) / 2
                        );

                        break;

                    case WEATHER_TYPE.WT_SNOW:
                    case WEATHER_TYPE.WT_STORM:

                        effect.X += effect.SpeedX * speedOffset;
                        effect.Y += effect.SpeedY * speedOffset;

                        batcher.Draw2D
                        (
                            SolidColorTextureCache.GetTexture(Color.White),
                            x + (int) effect.X,
                            y + (int) effect.Y,
                            2,
                            2,
                            ref _hueVector
                        );

                        break;
                }
            }

            LastTick = Time.Ticks;
        }


        private struct WeatherEffect
        {
            public float SpeedX, SpeedY, X, Y, ScaleRatio, SpeedAngle, SpeedMagnitude;
            public uint ID;
        }

        private enum WEATHER_TYPE
        {
            WT_RAIN = 0,
            WT_FIERCE_STORM,
            WT_SNOW,
            WT_STORM
        }
    }
}