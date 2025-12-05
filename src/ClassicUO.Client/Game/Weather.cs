// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace ClassicUO.Game
{
    enum WeatherType
    {
        WT_RAIN = 0,
        WT_STORM_APPROACH,
        WT_SNOW,
        WT_STORM_BREWING,

        WT_INVALID_0 = 0xFE,
        WT_INVALID_1 = 0xFF
    }

    internal sealed class Weather
    {
        private const int MAX_WEATHER_EFFECT = 70;
        private const float SIMULATION_TIME = 37.0f;

        private readonly WeatherEffect[] _effects = new WeatherEffect[byte.MaxValue];
        private uint _timer, _windTimer, _lastTick;
        private readonly World _world;

        public Weather(World world)
        {
            _world = world;
        }


        public WeatherType? CurrentWeather { get; private set; }
        public WeatherType Type { get; private set; }
        public byte Count { get; private set; }
        public byte ScaledCount { get; private set; }
        public byte CurrentCount { get; private set; }
        public byte Temperature{ get; private set; }
        public sbyte Wind { get; private set; }



        private static float SinOscillate(float freq, int range, uint current_tick)
        {
            float anglef = (int) (current_tick / 2.7777f * freq) % 360;

            return Math.Sign(MathHelper.ToRadians(anglef)) * range;
        }


        public void Reset()
        {
            Type = 0;
            Count = CurrentCount = Temperature = 0;
            Wind = 0;
            _windTimer = _timer = 0;
            CurrentWeather = null;
        }

        public void Generate(WeatherType type, byte count, byte temp)
        {
            bool extended = CurrentWeather.HasValue && CurrentWeather == type;

            if (!extended)
            {
                Reset();
            }

            Type = type;
            Count = (byte) Math.Min(MAX_WEATHER_EFFECT, (int) count);
            Temperature = temp;
            _timer = Time.Ticks + Constants.WEATHER_TIMER;

            _lastTick = Time.Ticks;

            if (Type == WeatherType.WT_INVALID_0 || Type == WeatherType.WT_INVALID_1)
            {
                _timer = 0;
                CurrentWeather = null;

                return;
            }

            bool showMessage = Count > 0 && !extended;

            switch (type)
            {
                case WeatherType.WT_RAIN:
                    if (showMessage)
                    {
                        GameActions.Print
                        (
                            _world,
                            ResGeneral.ItBeginsToRain,
                            1154,
                            MessageType.System,
                            3,
                            false
                        );

                        CurrentWeather = type;
                    }

                    break;

                case WeatherType.WT_STORM_APPROACH:
                    if (showMessage)
                    {
                        GameActions.Print
                        (
                            _world,
                            ResGeneral.AFierceStormApproaches,
                            1154,
                            MessageType.System,
                            3,
                            false
                        );

                        CurrentWeather = type;

                        PlayThunder();
                    }

                    break;

                case WeatherType.WT_SNOW:
                    if (showMessage)
                    {
                        GameActions.Print
                        (
                            _world,
                            ResGeneral.ItBeginsToSnow,
                            1154,
                            MessageType.System,
                            3,
                            false
                        );

                        CurrentWeather = type;

                        PlayWind();
                    }

                    break;

                case WeatherType.WT_STORM_BREWING:
                    if (showMessage)
                    {
                        GameActions.Print
                        (
                            _world,
                            ResGeneral.AStormIsBrewing,
                            1154,
                            MessageType.System,
                            3,
                            false
                        );

                        CurrentWeather = type;

                        PlayThunder();
                    }

                    break;
            }


            _windTimer = 0;

            ScaledCount = CalculateScaledCount(Count);
            CurrentCount = ScaledCount;

            // Calculate player's absolute isometric pixel coordinates from tile coordinates
            // This follows the formula: isoX = (tileX - tileY) * 22, isoY = (tileX + tileY) * 22
            int tileOffX = _world.Player.X;
            int tileOffY = _world.Player.Y;
            int playerAbsIsoX = (tileOffX - tileOffY) * 22;
            int playerAbsIsoY = (tileOffX + tileOffY) * 22;
            
            int spreadX = Client.Game.Scene.Camera.Bounds.Width;
            int spreadY = Client.Game.Scene.Camera.Bounds.Height;

            for (int i = 0; i < _effects.Length; i++)
            {
                ref WeatherEffect effect = ref _effects[i];
                
                // Initialize particles in absolute isometric coordinates (NOT viewport-relative)
                // These coordinates are independent of viewport offset
                effect.WorldX = playerAbsIsoX + RandomHelper.GetValue(-spreadX, spreadX);
                effect.WorldY = playerAbsIsoY + RandomHelper.GetValue(-spreadY, spreadY);
                
                effect.ID = (uint)i;
                effect.ScaleRatio = RandomHelper.GetValue(0, 10) * 0.1f;
            }
        }

        private static byte CalculateScaledCount(byte count)
        {
            if (count <= 0)
            {
                return 0;
            }
            float legacyWindowSize = 640 * 480;
            return (byte)Math.Max(1, Math.Min(byte.MaxValue, count * (Client.Game.Scene.Camera.Bounds.Width * Client.Game.Scene.Camera.Bounds.Height) / legacyWindowSize));
        }

        private void PlayWind()
        {
            PlaySound(RandomHelper.RandomList(0x014, 0x015, 0x016));
        }

        private void PlayThunder()
        {
           PlaySound(RandomHelper.RandomList(0x028, 0x206));
        }

        private void PlaySound(int sound)
        {
            // randomize the sound of the weather around the player
            int randX = RandomHelper.GetValue(10, 18);
            if (RandomHelper.RandomBool())
            {
                randX *= -1;
            }

            int randY = RandomHelper.GetValue(10, 18);
            if (RandomHelper.RandomBool())
            {
                randY *= -1;
            }

            Client.Game.Audio.PlaySoundWithDistance(_world, sound, _world.Player.X + randX, _world.Player.Y + randY);
        }

        public void Draw(UltimaBatcher2D batcher, int x, int y, float layerDepth)
        {
            bool removeEffects = false;

            if (_timer < Time.Ticks)
            {
                if (CurrentCount == 0)
                {
                    // Time for the weather has passed and all weather effects have disappeared
                    Reset();
                    return;
                }

                removeEffects = true;
            }
            else if (Type == WeatherType.WT_INVALID_0 || Type == WeatherType.WT_INVALID_1)
            {
                return;
            }

            //Rescale the count if window size has changed
            byte newScaledCount = CalculateScaledCount(Count);

            if (newScaledCount != ScaledCount)
            {
                CurrentCount = (byte)Math.Min(byte.MaxValue, CurrentCount * newScaledCount / ScaledCount);
                ScaledCount = newScaledCount;
            }

            uint passed = Time.Ticks - _lastTick;

            if (passed > 7000)
            {
                _lastTick = Time.Ticks;
                passed = 25;
            }

            bool windChanged = false;

            if (_windTimer < Time.Ticks)
            {
                if (_windTimer == 0)
                {
                    windChanged = true;
                }

                _windTimer = Time.Ticks + (uint) (RandomHelper.GetValue(13, 19) * 1000);

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
            Point winsize = new Point(Client.Game.Scene.Camera.Bounds.Width, Client.Game.Scene.Camera.Bounds.Height);

            Rectangle snowRect = new Rectangle(0, 0, 2, 2);

            // Calculate viewport offset to convert absolute isometric coordinates to viewport-relative coordinates
            // This follows the same formula as GameSceneDrawingSorting.GetViewPort()
            int tileOffX = _world.Player.X;
            int tileOffY = _world.Player.Y;
            int winGameCenterX = (winsize.X >> 1) + (_world.Player.Z << 2);
            int winGameCenterY = (winsize.Y >> 1) + (_world.Player.Z << 2);
            winGameCenterX -= (int)_world.Player.Offset.X;
            winGameCenterY -= (int)(_world.Player.Offset.Y - _world.Player.Offset.Z);
            
            int viewportOffsetX = (tileOffX - tileOffY) * 22 - winGameCenterX;
            int viewportOffsetY = (tileOffX + tileOffY) * 22 - winGameCenterY;
            
            int visibleRangeX = winsize.X;
            int visibleRangeY = winsize.Y;

            for (int i = 0; i < CurrentCount; i++)
            {
                ref WeatherEffect effect = ref _effects[i];

                // Convert absolute isometric coordinates to viewport-relative coordinates
                // by subtracting the viewport offset (same as game objects do in UpdateRealScreenPosition)
                effect.X = effect.WorldX - viewportOffsetX;
                effect.Y = effect.WorldY - viewportOffsetY;

                // Check if particle is outside visible viewport bounds
                if (effect.X < -visibleRangeX || effect.X > visibleRangeX * 2 || 
                    effect.Y < -visibleRangeY || effect.Y > visibleRangeY * 2)
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
                    
                    // Respawn particle in visible range
                    // Calculate absolute isometric coordinates based on current player position
                    int playerAbsIsoX = (tileOffX - tileOffY) * 22;
                    int playerAbsIsoY = (tileOffX + tileOffY) * 22;
                    
                    effect.WorldX = playerAbsIsoX + RandomHelper.GetValue(-visibleRangeX, visibleRangeX);
                    effect.WorldY = playerAbsIsoY + RandomHelper.GetValue(-visibleRangeY, visibleRangeY);
                    
                    // Recalculate viewport-relative position
                    effect.X = effect.WorldX - viewportOffsetX;
                    effect.Y = effect.WorldY - viewportOffsetY;
                }

                switch (Type)
                {
                    case WeatherType.WT_RAIN:
                        float scaleRation = effect.ScaleRatio;
                        effect.SpeedX = -4.5f - scaleRation;
                        effect.SpeedY = 5.0f + scaleRation;

                        break;

                    case WeatherType.WT_STORM_BREWING:
                        effect.SpeedX = Wind * 1.5f;
                        effect.SpeedY = 1.5f;

                        if (windChanged)
                        {
                            PlayThunder();
                        }

                        break;

                    case WeatherType.WT_SNOW:
                    case WeatherType.WT_STORM_APPROACH:

                        if (Type == WeatherType.WT_SNOW)
                        {
                            effect.SpeedX = Wind;
                            effect.SpeedY = 1.0f;
                        }
                        else
                        {
                            effect.SpeedX = Wind;
                            effect.SpeedY = 6.0f;
                        }

                        if (windChanged)
                        {
                            effect.SpeedAngle = MathHelper.ToDegrees((float) Math.Atan2(effect.SpeedX, effect.SpeedY));

                            effect.SpeedMagnitude = (float) Math.Sqrt(Math.Pow(effect.SpeedX, 2) + Math.Pow(effect.SpeedY, 2));

                            if (Type == WeatherType.WT_SNOW)
                            {
                                PlayWind();
                            }
                            else
                            {
                                PlayThunder();
                            }
                        }

                        float speedAngle = effect.SpeedAngle;
                        float speedMagnitude = effect.SpeedMagnitude;

                        speedMagnitude += effect.ScaleRatio;

                        speedAngle += SinOscillate(0.4f, 20, Time.Ticks + effect.ID);

                        float rad = MathHelper.ToRadians(speedAngle);
                        effect.SpeedX = speedMagnitude * (float) Math.Sin(rad);
                        effect.SpeedY = speedMagnitude * (float) Math.Cos(rad);

                        break;
                }

                float speedOffset = passed / SIMULATION_TIME;

                switch (Type)
                {
                    case WeatherType.WT_RAIN:
                    case WeatherType.WT_STORM_APPROACH:

                        // Store old absolute position
                        float oldWorldX = effect.WorldX;
                        float oldWorldY = effect.WorldY;

                        // Apply physics in absolute isometric space
                        float ofsx = effect.SpeedX * speedOffset;
                        float ofsy = effect.SpeedY * speedOffset;

                        effect.WorldX += ofsx;
                        effect.WorldY += ofsy;

                        // Convert both positions to viewport-relative coordinates for rendering
                        int oldX = (int)(oldWorldX - viewportOffsetX);
                        int oldY = (int)(oldWorldY - viewportOffsetY);
                        int newX = (int)(effect.WorldX - viewportOffsetX);
                        int newY = (int)(effect.WorldY - viewportOffsetY);

                        // Clamp line length for aesthetic reasons
                        const float MAX_OFFSET_XY = 5.0f;

                        int screenOfsx = newX - oldX;
                        int screenOfsy = newY - oldY;

                        if (screenOfsx >= MAX_OFFSET_XY)
                        {
                            oldX = (int) (newX - MAX_OFFSET_XY);
                        }
                        else if (screenOfsx <= -MAX_OFFSET_XY)
                        {
                            oldX = (int) (newX + MAX_OFFSET_XY);
                        }

                        if (screenOfsy >= MAX_OFFSET_XY)
                        {
                            oldY = (int) (newY - MAX_OFFSET_XY);
                        }
                        else if (screenOfsy <= -MAX_OFFSET_XY)
                        {
                            oldY = (int) (newY + MAX_OFFSET_XY);
                        }

                        // Draw in viewport-relative space - batcher applies camera transform
                        Vector2 start = new Vector2(oldX, oldY);
                        Vector2 end = new Vector2(newX, newY);

                        batcher.DrawLine
                        (
                            SolidColorTextureCache.GetTexture(Color.Blue),
                            start,
                            end,
                            Vector3.UnitZ,
                            2,
                            layerDepth
                        );

                        break;

                    case WeatherType.WT_SNOW:

                        // Apply physics in absolute isometric space
                        effect.WorldX += effect.SpeedX * speedOffset;
                        effect.WorldY += effect.SpeedY * speedOffset;

                        // Convert to viewport-relative coordinates for rendering
                        int snowX = (int)(effect.WorldX - viewportOffsetX);
                        int snowY = (int)(effect.WorldY - viewportOffsetY);

                        snowRect.X = snowX;
                        snowRect.Y = snowY;

                        batcher.Draw
                        (
                            SolidColorTextureCache.GetTexture(Color.White),
                            snowRect,
                            Vector3.UnitZ,
                            layerDepth
                        );

                        break;
                }
            }

            _lastTick = Time.Ticks;
        }


        private struct WeatherEffect
        {
            public float SpeedX, SpeedY, WorldX, WorldY, X, Y, ScaleRatio, SpeedAngle, SpeedMagnitude;
            public uint ID;
        }
    }
}