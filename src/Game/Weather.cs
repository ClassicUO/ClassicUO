using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace ClassicUO.Game
{
    class WeatherEffect
    {
        public float SpeedX, SpeedY, X, Y, ScaleRatio, SpeedAngle, SpeedMagnitude;
        public uint ID;
    }

    class Weather
    {
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

        public List<WeatherEffect> Effects = new List<WeatherEffect>();


        private float SinOscillate(float freq, int range, uint current_tick)
        {
            float anglef = (float) ((int) ((current_tick / 2.7777f) * freq) % 360);
            return Math.Sign(MathHelper.ToRadians(anglef)) * range;
        }

        public void Reset()
        {
            Type = Count = CurrentCount = Temperature = 0;
            Wind = 0;
            WindTimer = Timer = 0;

            Effects.Clear();
        }

        public void Generate()
        {
            LastTick = Engine.Ticks;

            if (Type == 0xFF || Type == 0xFE)
                return;

            //int drawX = Engine.Profile.Current.GameWindowPosition.X;
            //int drawY = Engine.Profile.Current.GameWindowPosition.Y;

            if (Count > 70)
                Count = 70;

            WindTimer = 0;

            while (CurrentCount < Count)
            {
                WeatherEffect effect = new WeatherEffect()
                {
                    X = RandomHelper.GetValue( 0, Engine.Profile.Current.GameWindowSize.X),
                    Y = RandomHelper.GetValue(0, Engine.Profile.Current.GameWindowSize.Y)
                };

                Effects.Add(effect);

                CurrentCount++;
            }
        }

        private Vector3 _hueVector;

        public void Draw(UltimaBatcher2D batcher, int x, int y)
        {
            bool removeEffects = false;

            if (Timer < Engine.Ticks)
            {
                if (CurrentCount == 0)
                    return;

                removeEffects = true;
            }
            else if (Type == 0xFF || Type == 0xFE)
                return;

            uint passed = Engine.Ticks - LastTick;

            if (passed > 7000)
            {
                LastTick = Engine.Ticks;
                passed = 25;
            }

            bool windChanged = false;

            if (WindTimer < Engine.Ticks)
            {
                if (WindTimer == 0)
                    windChanged = true;

                WindTimer = Engine.Ticks + (uint)(RandomHelper.GetValue(7, 13) * 1000);

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

            Point winpos = Engine.Profile.Current.GameWindowPosition;
            Point winsize = Engine.Profile.Current.GameWindowSize;

            for (int i = 0; i < Effects.Count; i++)
            {
                var effect = Effects[i];

                if (effect.X < x || effect.X > x + winsize.X ||
                    effect.Y < y || effect.Y > y + winsize.Y)
                {
                    if (removeEffects)
                    {
                        Effects.RemoveAt(i--);

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

                        speed_angle += SinOscillate(0.4f, 20, Engine.Ticks + effect.ID);

                        var rad = MathHelper.ToRadians(speed_angle);
                        effect.SpeedX = speed_magnitude * (float)Math.Sin(rad);
                        effect.SpeedY = speed_magnitude * (float)Math.Cos(rad);

                        break;
                    default:
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

                        batcher.DrawLine(Textures.GetTexture(Color.Gray), x + oldX, y + oldY,
                            x + (int)effect.X, y + (int)effect.Y, 0, 0);
                        break;
                    case WEATHER_TYPE.WT_SNOW:
                    case WEATHER_TYPE.WT_STORM:

                        effect.X += effect.SpeedX * speedOffset;
                        effect.Y += effect.SpeedY * speedOffset;

                        batcher.Draw2D(Textures.GetTexture(Color.White),
                            x + (int)effect.X, y + (int)effect.Y, 2, 2, ref _hueVector);

                        break;
                    default:
                        break;
                }

            }

            LastTick = Engine.Ticks;
        }
    }
}
