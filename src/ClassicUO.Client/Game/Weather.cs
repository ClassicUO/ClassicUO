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

        // Density thresholds for rain visual styles
        private const int DENSITY_SMALL_DOTS = 17;
        private const int DENSITY_LARGE_DOTS = 35;
        private const int DENSITY_SHORT_LINES = 52;
        // Above 52 = long bolts

        private const float BASE_RAIN_SPEED_Y = 15.0f;
        private const float BASE_RAIN_SPEED_X = -2.5f;
        private const float BASE_STORM_APPROACH_SPEED_Y = 6.0f;
        private const float BASE_STORM_APPROACH_SPEED_X = 4.0f;
        private const float BASE_SNOW_SPEED_Y = 5.0f;
        private const float BASE_SNOW_SPEED_X = 4.0f;

        // Ground collision thresholds for depth layers (as percentage of screen height)
        // All three layers fade across entire ground area for natural depth spread
        private const float BACKGROUND_GROUND_THRESHOLD = 0.35f;  // 35% down screen (far particles)
        private const float MIDDLE_GROUND_THRESHOLD = 0.68f;      // 68% down screen (mid particles)
        private const float FOREGROUND_GROUND_THRESHOLD = 0.81f;  // 81% down screen (near particles)
        private const float FADE_DISTANCE = 0.20f;                // Fade over 20% of screen
        
        // Creates overlapping fade zones spread across entire ground:
        // Background:  55-75% (20% fade zone) - appears most distant
        // Middle:      68-88% (20% fade zone) - overlaps with both layers
        // Foreground:  81-100% (19% fade zone) - appears closest, reaches near bottom
        
        // Splash effect configuration
        private const float SPLASH_DURATION = 0.3f;     // Splash lasts 300ms (shorter, less trail)
        private const float SPLASH_RISE_SPEED = -0.8f;   // Slower upward movement (reduces line-like appearance)
        private const int MAX_SPLASHES_PER_FRAME = 5;   // Limit splash creation rate

        private readonly WeatherEffect[] _effects = new WeatherEffect[byte.MaxValue];
        private readonly SplashEffect[] _splashes = new SplashEffect[byte.MaxValue];
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
                
                // Assign depth layer based on ScaleRatio (distribute evenly across 3 layers)
                float depthValue = effect.ScaleRatio;  // Reuse existing 0.0-1.0 value
                if (depthValue < 0.33f)
                    effect.Depth = DepthLayer.Background;
                else if (depthValue < 0.67f)
                    effect.Depth = DepthLayer.Middle;
                else
                    effect.Depth = DepthLayer.Foreground;
                
                // Assign random fade threshold based on depth layer for natural scattered depth
                switch (effect.Depth)
                {
                    case DepthLayer.Background:
                        // Background particles randomly fade in 0-30% range
                        effect.FadeThreshold = RandomHelper.GetValue(0, 30) * 0.01f;  // 0.0 to 0.3
                        effect.NeverFade = false;
                        break;
                        
                    case DepthLayer.Middle:
                        // Middle particles randomly fade in 31-70% range
                        effect.FadeThreshold = 0.31f + RandomHelper.GetValue(0, 39) * 0.01f;  // 0.31 to 0.70
                        effect.NeverFade = false;
                        break;
                        
                    case DepthLayer.Foreground:
                        // Foreground: 50% never fade, 50% fade randomly in 71-100% range
                        if (RandomHelper.RandomBool())
                        {
                            effect.NeverFade = true;
                            effect.FadeThreshold = 1.0f;  // Never reached
                        }
                        else
                        {
                            effect.NeverFade = false;
                            effect.FadeThreshold = 0.71f + RandomHelper.GetValue(0, 29) * 0.01f;  // 0.71 to 1.0
                        }
                        break;
                }
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

        private enum RainRenderStyle
        {
            SmallDots,      // 0-17
            LargeDots,      // 18-35
            ShortLines,     // 36-52
            LongBolts       // 53-70
        }

        private enum DepthLayer
        {
            Background = 0,  // Far (depth 0.0-0.33)
            Middle = 1,      // Medium (depth 0.34-0.66)
            Foreground = 2   // Near (depth 0.67-1.0)
        }

        private RainRenderStyle GetRainRenderStyle()
        {
            if (Count <= DENSITY_SMALL_DOTS)
                return RainRenderStyle.SmallDots;
            else if (Count <= DENSITY_LARGE_DOTS)
                return RainRenderStyle.LargeDots;
            else if (Count <= DENSITY_SHORT_LINES)
                return RainRenderStyle.ShortLines;
            else
                return RainRenderStyle.LongBolts;
        }

        private struct DepthProperties
        {
            public float SizeMultiplier;
            public float SpeedMultiplier;
            public float AlphaMultiplier;
            public float TrailMultiplier;
            public Color ColorTint;
        }

        private DepthProperties GetDepthProperties(DepthLayer layer, WeatherType weatherType)
        {
            DepthProperties props = new DepthProperties();
            
            switch (layer)
            {
                case DepthLayer.Background:
                    props.SizeMultiplier = 0.5f + (0.25f * RandomHelper.GetValue(0, 10) * 0.1f); // 50-75%
                    props.SpeedMultiplier = 0.5f + (0.2f * RandomHelper.GetValue(0, 10) * 0.1f); // 50-70%
                    props.AlphaMultiplier = 0.4f + (0.2f * RandomHelper.GetValue(0, 10) * 0.1f); // 40-60%
                    props.TrailMultiplier = 0.4f + (0.2f * RandomHelper.GetValue(0, 10) * 0.1f); // 40-60%
                    
                    // Atmospheric color tint (lighter for distance)
                    if (weatherType == WeatherType.WT_RAIN || weatherType == WeatherType.WT_STORM_APPROACH)
                        props.ColorTint = Color.Lerp(Color.LightBlue, Color.White, 0.5f);
                    else // Snow
                        props.ColorTint = Color.White;
                    break;
                    
                case DepthLayer.Middle:
                    props.SizeMultiplier = 0.85f + (0.15f * RandomHelper.GetValue(0, 10) * 0.1f); // 85-100%
                    props.SpeedMultiplier = 0.8f + (0.2f * RandomHelper.GetValue(0, 10) * 0.1f);  // 80-100%
                    props.AlphaMultiplier = 0.7f + (0.2f * RandomHelper.GetValue(0, 10) * 0.1f);  // 70-90%
                    props.TrailMultiplier = 0.8f + (0.2f * RandomHelper.GetValue(0, 10) * 0.1f);  // 80-100%
                    
                    // Standard colors with subtle tint
                    if (weatherType == WeatherType.WT_RAIN || weatherType == WeatherType.WT_STORM_APPROACH)
                        props.ColorTint = Color.LightGray;
                    else
                        props.ColorTint = Color.White;
                    break;
                    
                case DepthLayer.Foreground:
                    props.SizeMultiplier = 1.1f + (0.4f * RandomHelper.GetValue(0, 10) * 0.1f); // 110-150%
                    props.SpeedMultiplier = 1.2f + (0.3f * RandomHelper.GetValue(0, 10) * 0.1f); // 120-150%
                    props.AlphaMultiplier = 0.9f + (0.1f * RandomHelper.GetValue(0, 10) * 0.1f); // 90-100%
                    props.TrailMultiplier = 1.2f + (0.3f * RandomHelper.GetValue(0, 10) * 0.1f); // 120-150%
                    
                    // Darker/more saturated for proximity
                    if (weatherType == WeatherType.WT_RAIN || weatherType == WeatherType.WT_STORM_APPROACH)
                        props.ColorTint = Color.Gray;
                    else
                        props.ColorTint = Color.Lerp(Color.White, Color.LightGray, 0.2f);
                    break;
            }
            
            return props;
        }

        private void CreateSplash(ref WeatherEffect effect, float worldX, float worldY)
        {
            // Find an inactive splash slot
            for (int i = 0; i < _splashes.Length; i++)
            {
                ref SplashEffect splash = ref _splashes[i];
                if (!splash.Active)
                {
                    splash.Active = true;
                    splash.LifeTime = 0f;
                    splash.Depth = effect.Depth;
                    
                    // Store in absolute world coordinates
                    splash.WorldX = worldX;
                    splash.WorldY = worldY;
                    
                    // Initial upward velocity
                    splash.VelocityY = SPLASH_RISE_SPEED;
                    
                    // Size based on depth (smaller for background, larger for foreground)
                    // Increased base sizes to make splashes more visible and less line-like
                    switch (effect.Depth)
                    {
                        case DepthLayer.Background:
                            splash.Size = 2f + RandomHelper.GetValue(0, 8) * 0.1f; // 2-2.8px
                            break;
                        case DepthLayer.Middle:
                            splash.Size = 3f + RandomHelper.GetValue(0, 10) * 0.1f; // 3-4px
                            break;
                        case DepthLayer.Foreground:
                            splash.Size = 4f + RandomHelper.GetValue(0, 12) * 0.1f; // 4-5.2px
                            break;
                    }
                    
                    break; // Found a slot, exit
                }
            }
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
                        float scaleRatio = effect.ScaleRatio;
                        RainRenderStyle rainStyle = GetRainRenderStyle();
                        
                        // Get depth properties for this particle
                        DepthProperties depthProps = GetDepthProperties(effect.Depth, Type);
                        
                        float baseSpeedY = BASE_RAIN_SPEED_Y;
                        float densitySpeedMultiplier = 1.0f;
                        
                        // Higher density = faster speeds
                        switch (rainStyle)
                        {
                            case RainRenderStyle.SmallDots:
                                densitySpeedMultiplier = 1.0f;
                                break;
                            case RainRenderStyle.LargeDots:
                                densitySpeedMultiplier = 1.5f;
                                break;
                            case RainRenderStyle.ShortLines:
                                densitySpeedMultiplier = 1.8f;
                                break;
                            case RainRenderStyle.LongBolts:
                                densitySpeedMultiplier = 2.4f;
                                break;
                        }
                        
                        // Apply depth-based speed multiplier for parallax effect
                        effect.SpeedX = (BASE_RAIN_SPEED_X - scaleRatio) * densitySpeedMultiplier * depthProps.SpeedMultiplier;
                        effect.SpeedY = (baseSpeedY + scaleRatio) * densitySpeedMultiplier * depthProps.SpeedMultiplier;

                        break;

                    case WeatherType.WT_STORM_BREWING:
                        DepthProperties stormBrewingProps = GetDepthProperties(effect.Depth, Type);
                        effect.SpeedX = Wind * 1.5f * stormBrewingProps.SpeedMultiplier;
                        effect.SpeedY = 1.5f * stormBrewingProps.SpeedMultiplier;

                        if (windChanged)
                        {
                            PlayThunder();
                        }

                        break;

                    case WeatherType.WT_SNOW:
                    case WeatherType.WT_STORM_APPROACH:

                        DepthProperties snowStormProps = GetDepthProperties(effect.Depth, Type);

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
                        // Apply depth-based speed multiplier for parallax effect
                        effect.SpeedX = speedMagnitude * (float) Math.Sin(rad) * snowStormProps.SpeedMultiplier;
                        effect.SpeedY = speedMagnitude * (float) Math.Cos(rad) * snowStormProps.SpeedMultiplier;

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

                        if (Type == WeatherType.WT_RAIN)
                        {
                            RainRenderStyle rainStyle = GetRainRenderStyle();
                            DepthProperties rainDepthProps = GetDepthProperties(effect.Depth, Type);
                            
                            // Calculate ground fade using per-particle random threshold
                            float groundFade = 1.0f;
                            int viewportHeight = winsize.Y;
                            
                            // Unified fade logic for all particles (no layer-specific if-else)
                            // Skip fade check if particle never fades (some foreground particles)
                            if (!effect.NeverFade)
                            {
                                // Calculate particle's absolute fade position using its random threshold
                                int particleFadeY = (int)(viewportHeight * effect.FadeThreshold);
                                int fadeDistancePixels = (int)(viewportHeight * FADE_DISTANCE);
                                
                                if (newY >= particleFadeY)
                                {
                                    // Calculate fade progress
                                    int fadeEnd = particleFadeY + fadeDistancePixels;
                                    float fadeProgress = (float)(newY - particleFadeY) / fadeDistancePixels;
                                    groundFade = 1f - Math.Max(0f, Math.Min(1f, fadeProgress));
                                    
                                    // Create splash when particle just crosses its personal threshold
                                    int splashWindow = 20; // 20 pixel window
                                    if (newY >= particleFadeY && newY <= particleFadeY + splashWindow)
                                    {
                                        // Create splash effect at ground impact point
                                        CreateSplash(ref effect, effect.WorldX, effect.WorldY);
                                    }
                                    
                                    // If fully faded, respawn at top with new random threshold
                                    if (newY >= fadeEnd)
                                    {
                                        // Respawn particle at top
                                        int playerAbsIsoX = (tileOffX - tileOffY) * 22;
                                        int playerAbsIsoY = (tileOffX + tileOffY) * 22;
                                        effect.WorldX = playerAbsIsoX + RandomHelper.GetValue(-visibleRangeX, visibleRangeX);
                                        effect.WorldY = playerAbsIsoY - visibleRangeY; // Spawn at top
                                        
                                        // Re-randomize fade threshold for next cycle
                                        switch (effect.Depth)
                                        {
                                            case DepthLayer.Background:
                                                effect.FadeThreshold = RandomHelper.GetValue(0, 30) * 0.01f;
                                                effect.NeverFade = false;
                                                break;
                                            case DepthLayer.Middle:
                                                effect.FadeThreshold = 0.31f + RandomHelper.GetValue(0, 39) * 0.01f;
                                                effect.NeverFade = false;
                                                break;
                                            case DepthLayer.Foreground:
                                                if (RandomHelper.RandomBool())
                                                {
                                                    effect.NeverFade = true;
                                                    effect.FadeThreshold = 1.0f;
                                                }
                                                else
                                                {
                                                    effect.NeverFade = false;
                                                    effect.FadeThreshold = 0.71f + RandomHelper.GetValue(0, 29) * 0.01f;
                                                }
                                                break;
                                        }
                                        
                                        continue; // Skip rendering, particle respawning
                                    }
                                }
                            }
                            
                            // Calculate speed-based trail length
                            float speedMagnitude = (float)Math.Sqrt(effect.SpeedX * effect.SpeedX + effect.SpeedY * effect.SpeedY);
                            
                            switch (rainStyle)
                            {
                                case RainRenderStyle.SmallDots:
                                    // Apply depth-based size multiplier
                                    int smallDotSize = (int)(2 * rainDepthProps.SizeMultiplier);
                                    smallDotSize = Math.Max(2, smallDotSize);
                                    
                                    Rectangle smallDotRect = new Rectangle(
                                        newX - smallDotSize / 2, 
                                        newY - smallDotSize / 2, 
                                        smallDotSize, 
                                        smallDotSize
                                    );
                                    
                                    // Apply alpha and color tint with proper visibility
                                    // 10% blend towards tint
                                    Color smallDotColor = Color.Lerp(Color.LightBlue, rainDepthProps.ColorTint, 0.1f);
                                    // Boost alpha for visibility (minimum 80% even for background)
                                    float smallDotAlpha = Math.Max(0.8f, rainDepthProps.AlphaMultiplier);
                                    smallDotColor *= smallDotAlpha * groundFade; // Apply ground fade
                                    
                                    batcher.Draw
                                    (
                                        SolidColorTextureCache.GetTexture(smallDotColor),
                                        smallDotRect,
                                        Vector3.UnitZ,
                                        layerDepth
                                    );
                                    break;
                                
                                case RainRenderStyle.LargeDots:
                                    // Apply depth-based size multiplier
                                    int largeDotSize = (int)(2 * rainDepthProps.SizeMultiplier);
                                    largeDotSize = Math.Max(2, largeDotSize); 
                                    
                                    Rectangle largeDotRect = new Rectangle(
                                        newX - largeDotSize / 2, 
                                        newY - largeDotSize / 2, 
                                        largeDotSize, 
                                        largeDotSize
                                    );
                                    
                                    // Apply alpha and color tint with better visibility
                                    // 20% blend towards tint
                                    Color largeDotColor = Color.Lerp(Color.CornflowerBlue, rainDepthProps.ColorTint, 0.2f);
                                    // Boost alpha for visibility (minimum 70% even for background)
                                    float largeDotAlpha = Math.Max(0.7f, rainDepthProps.AlphaMultiplier);
                                    largeDotColor *= largeDotAlpha * groundFade; // Apply ground fade
                                    
                                    batcher.Draw
                                    (
                                        SolidColorTextureCache.GetTexture(largeDotColor),
                                        largeDotRect,
                                        Vector3.UnitZ,
                                        layerDepth
                                    );
                                    break;
                                
                                case RainRenderStyle.ShortLines:
                                    // Calculate depth-adjusted trail length
                                    float shortBaseTrail = 0.9f;
                                    float shortLineLength = speedMagnitude * shortBaseTrail * rainDepthProps.TrailMultiplier;
                                    int screenOfsx = newX - oldX;
                                    int screenOfsy = newY - oldY;
                                    
                                    if (screenOfsx >= shortLineLength)
                                        oldX = (int)(newX - shortLineLength);
                                    else if (screenOfsx <= -shortLineLength)
                                        oldX = (int)(newX + shortLineLength);
                                    
                                    if (screenOfsy >= shortLineLength)
                                        oldY = (int)(newY - shortLineLength);
                                    else if (screenOfsy <= -shortLineLength)
                                        oldY = (int)(newY + shortLineLength);
                                    
                                    Vector2 shortStart = new Vector2(oldX, oldY);
                                    Vector2 shortEnd = new Vector2(newX, newY);
                                    
                                    // Apply color and alpha - keep rain distinctly blue
                                    // 25% blend towards tint
                                    Color shortLineColor = Color.Lerp(Color.Gray, rainDepthProps.ColorTint, 0.25f);
                                    // Boost alpha for visibility
                                    float shortLineAlpha = Math.Max(0.65f, rainDepthProps.AlphaMultiplier);
                                    shortLineColor *= shortLineAlpha * groundFade; // Apply ground fade
                                    
                                    // Apply line width with depth
                                    int shortLineWidth = Math.Max(1, (int)(2 * rainDepthProps.SizeMultiplier));
                                    
                                    batcher.DrawLine
                                    (
                                        SolidColorTextureCache.GetTexture(shortLineColor),
                                        shortStart,
                                        shortEnd,
                                        Vector3.UnitZ,
                                        shortLineWidth,
                                        layerDepth
                                    );
                                    break;
                                
                                case RainRenderStyle.LongBolts:
                                    // Calculate depth-adjusted trail length
                                    float boltBaseTrail = 1.75f;
                                    float longBoltLength = speedMagnitude * boltBaseTrail * rainDepthProps.TrailMultiplier;
                                    int boltOfsx = newX - oldX;
                                    int boltOfsy = newY - oldY;
                                    
                                    if (boltOfsx >= longBoltLength)
                                        oldX = (int)(newX - longBoltLength);
                                    else if (boltOfsx <= -longBoltLength)
                                        oldX = (int)(newX + longBoltLength);
                                    
                                    if (boltOfsy >= longBoltLength)
                                        oldY = (int)(newY - longBoltLength);
                                    else if (boltOfsy <= -longBoltLength)
                                        oldY = (int)(newY + longBoltLength);
                                    
                                    Vector2 boltStart = new Vector2(oldX, oldY);
                                    Vector2 boltEnd = new Vector2(newX, newY);
                                    
                                    Color boltColor = Color.Lerp(Color.Gray, rainDepthProps.ColorTint, 0.25f);
                                    // Boost alpha for dramatic effect
                                    float boltAlpha = Math.Max(0.75f, rainDepthProps.AlphaMultiplier);
                                    boltColor *= boltAlpha * groundFade; // Apply ground fade
                                    
                                    // Apply line width with depth
                                    int boltLineWidth = Math.Max(1, (int)(3 * rainDepthProps.SizeMultiplier));
                                    
                                    batcher.DrawLine
                                    (
                                        SolidColorTextureCache.GetTexture(boltColor),
                                        boltStart,
                                        boltEnd,
                                        Vector3.UnitZ,
                                        boltLineWidth,
                                        layerDepth
                                    );
                                    break;
                            }
                        }
                        else // WT_STORM_APPROACH
                        {
                            // Original storm approach rendering
                            const float MAX_OFFSET_XY = 5.0f;
                            int screenOfsx = newX - oldX;
                            int screenOfsy = newY - oldY;

                            if (screenOfsx >= MAX_OFFSET_XY)
                                oldX = (int)(newX - MAX_OFFSET_XY);
                            else if (screenOfsx <= -MAX_OFFSET_XY)
                                oldX = (int)(newX + MAX_OFFSET_XY);

                            if (screenOfsy >= MAX_OFFSET_XY)
                                oldY = (int)(newY - MAX_OFFSET_XY);
                            else if (screenOfsy <= -MAX_OFFSET_XY)
                                oldY = (int)(newY + MAX_OFFSET_XY);

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
                        }

                        break;

                    case WeatherType.WT_SNOW:

                        DepthProperties snowDepthProps = GetDepthProperties(effect.Depth, Type);

                        // Apply physics in absolute isometric space
                        effect.WorldX += effect.SpeedX * speedOffset;
                        effect.WorldY += effect.SpeedY * speedOffset;

                        // Convert to viewport-relative coordinates for rendering
                        int snowX = (int)(effect.WorldX - viewportOffsetX);
                        int snowY = (int)(effect.WorldY - viewportOffsetY);

                        // Depth-based size
                        int snowSize = (int)(2 * snowDepthProps.SizeMultiplier);
                        snowSize = Math.Max(1, snowSize);

                        snowRect.X = snowX;
                        snowRect.Y = snowY;
                        snowRect.Width = snowSize;
                        snowRect.Height = snowSize;

                        // Depth-based color and alpha with better visibility
                        // Boost alpha for snow visibility (minimum 70% even for background)
                        float snowBoostedAlpha = Math.Max(0.7f, snowDepthProps.AlphaMultiplier);
                        Color snowColor = snowDepthProps.ColorTint * snowBoostedAlpha;

                        batcher.Draw
                        (
                            SolidColorTextureCache.GetTexture(snowColor),
                            snowRect,
                            Vector3.UnitZ,
                            layerDepth
                        );

                        break;
                }
            }

            // Render splash effects (rain hitting ground)
            float deltaTime = passed / 1000f; // Convert milliseconds to seconds
            float splashSpeedOffset = passed / SIMULATION_TIME; // Same calculation as particle physics
            
            for (int i = 0; i < _splashes.Length; i++)
            {
                ref SplashEffect splash = ref _splashes[i];
                if (!splash.Active) continue;
                
                // Update splash lifetime
                splash.LifeTime += deltaTime / SPLASH_DURATION;
                
                // Deactivate if splash animation complete
                if (splash.LifeTime >= 1.0f)
                {
                    splash.Active = false;
                    continue;
                }
                
                // Apply upward movement physics in absolute world space
                splash.WorldY += splash.VelocityY * splashSpeedOffset;
                
                // Convert to viewport-relative coordinates for rendering
                splash.X = splash.WorldX - viewportOffsetX;
                splash.Y = splash.WorldY - viewportOffsetY;
                
                // Check visibility
                if (splash.X < -visibleRangeX || splash.X > visibleRangeX * 2 ||
                    splash.Y < -visibleRangeY || splash.Y > visibleRangeY * 2)
                {
                    splash.Active = false;
                    continue;
                }
                
                // Animated splash properties
                float progress = splash.LifeTime;
                
                // Size grows then shrinks (parabolic curve)
                float sizeScale = 1f + (float)Math.Sin(progress * Math.PI) * 1.5f;
                float currentSize = splash.Size * sizeScale;
                
                // Ensure minimum size to avoid line-like appearance
                currentSize = Math.Max(2f, currentSize);
                
                // Alpha fades out linearly
                float alpha = 1f - progress;
                
                // Depth-based alpha adjustment
                switch (splash.Depth)
                {
                    case DepthLayer.Background:
                        alpha *= 0.5f; // Background splashes faint but visible
                        break;
                    case DepthLayer.Middle:
                        alpha *= 0.7f; // Middle splashes moderate
                        break;
                    case DepthLayer.Foreground:
                        alpha *= 0.9f; // Foreground splashes very visible
                        break;
                }
                
                // Draw splash as multiple small particles to create organic appearance
                // Use integer coordinates to avoid sub-pixel rendering issues
                int splashSize = (int)currentSize;
                int splashX = (int)splash.X;
                int splashY = (int)splash.Y;
                
                // Splash color: light blue-white for rain impact
                Color splashColor = Color.Lerp(Color.LightBlue, Color.White, 0.7f);
                splashColor *= alpha;
                
                // Draw central splash particle
                Rectangle centerSplash = new Rectangle(
                    splashX - splashSize / 2,
                    splashY - splashSize / 2,
                    splashSize,
                    splashSize
                );
                
                batcher.Draw(
                    SolidColorTextureCache.GetTexture(splashColor),
                    centerSplash,
                    Vector3.UnitZ,
                    layerDepth
                );
                
                // Draw 2-4 smaller particles around center for organic splash effect
                // Only draw if splash is large enough (foreground/middle layers)
                if (splashSize >= 3 && progress < 0.6f)
                {
                    int numParticles = splash.Depth == DepthLayer.Foreground ? 4 : 2;
                    int particleSize = Math.Max(1, splashSize / 3);
                    float spreadRadius = currentSize * 0.6f * progress; // Spreads outward
                    
                    for (int p = 0; p < numParticles; p++)
                    {
                        float angle = (p * (float)Math.PI * 2f / numParticles) + (progress * 2f);
                        int offsetX = (int)(Math.Cos(angle) * spreadRadius);
                        int offsetY = (int)(Math.Sin(angle) * spreadRadius * 0.5f); // Elliptical spread
                        
                        Rectangle particleRect = new Rectangle(
                            splashX + offsetX - particleSize / 2,
                            splashY + offsetY - particleSize / 2,
                            particleSize,
                            particleSize
                        );
                        
                        Color particleColor = splashColor * 0.6f; // Dimmer than center
                        
                        batcher.Draw(
                            SolidColorTextureCache.GetTexture(particleColor),
                            particleRect,
                            Vector3.UnitZ,
                            layerDepth
                        );
                    }
                }
            }

            _lastTick = Time.Ticks;
        }


        private struct WeatherEffect
        {
            public float SpeedX, SpeedY, WorldX, WorldY, X, Y, ScaleRatio, SpeedAngle, SpeedMagnitude;
            public uint ID;
            public DepthLayer Depth;  // Depth layer for 3D atmospheric effects
            public float FadeThreshold;  // Per-particle random fade position (0.0-1.0)
            public bool NeverFade;       // Flag for foreground particles that don't fade
        }

        private struct SplashEffect
        {
            public float WorldX, WorldY;        // Splash location in absolute world coordinates
            public float X, Y;                  // Viewport-relative position (cached for rendering)
            public float LifeTime;              // How long splash has existed (0.0 = just created, 1.0 = finished)
            public float VelocityY;             // Upward movement velocity
            public float Size;                  // Splash particle size
            public DepthLayer Depth;            // Depth layer (affects splash size/alpha)
            public bool Active;                 // Is this splash currently active
        }
    }
}