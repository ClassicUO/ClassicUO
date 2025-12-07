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
        
        // ============================================================================
        // SPLASH EFFECT CONFIGURATION
        // ============================================================================
        // These constants control the appearance of water splash effects when rain hits ground
        
        // --- Timing Parameters ---
        private const float SPLASH_DURATION = 3.25f;
        // Duration of splash animation in seconds (0.25 = 250ms)
        // - Shorter (0.15-0.2): Quick, subtle splashes
        // - Medium (0.25-0.35): Balanced, noticeable splashes ✓ RECOMMENDED
        // - Longer (0.4-0.5): Slow, dramatic splashes
        
        private const float SPLASH_RISE_SPEED = -1.0f;
        // Upward movement speed of splash particles (negative = upward, 0 = no movement)
        // - Zero (0.0): No vertical movement, splashes stay at ground ✓ RECOMMENDED (prevents thread appearance)
        // - Small (-0.1 to -0.3): Slight bounce effect
        // - Medium (-0.5 to -0.8): Noticeable bounce, may create trails
        // - Large (-1.0+): High bounce, creates "thread" appearance (AVOID)
        
        // --- Droplet Count by Depth Layer ---
        private const int SPLASH_DROPLETS_BACKGROUND = 3;
        // Number of water droplets in background layer splash (distant, subtle)
        // Recommended: 2-5 droplets
        
        private const int SPLASH_DROPLETS_MIDDLE = 6;
        // Number of water droplets in middle layer splash (mid-distance, moderate)
        // Recommended: 4-8 droplets
        
        private const int SPLASH_DROPLETS_FOREGROUND = 10;
        // Number of water droplets in foreground layer splash (close, dramatic)
        // Recommended: 8-15 droplets
        
        // --- Spread Pattern Parameters ---
        private const float SPLASH_SPREAD_MULTIPLIER = 1.5f;
        // How far droplets spread from impact point (multiplier of base size)
        // - Small (0.8-1.2): Tight, compact splash
        // - Medium (1.5-2.0): Balanced spread ✓ RECOMMENDED
        // - Large (2.5-3.5): Wide, explosive splash
        
        private const float SPLASH_ELLIPSE_X = 1.2f;
        // Horizontal ellipse factor (1.0 = circle, >1.0 = wider horizontally)
        // - 1.0: Perfect circle
        // - 1.2: Slightly wider ✓ RECOMMENDED (natural ground splash)
        // - 1.5+: Very wide, exaggerated horizontal spread
        
        private const float SPLASH_ELLIPSE_Y = 0.2f;
        // Vertical ellipse factor (<1.0 = flatter)
        // - 1.0: Perfect circle
        // - 0.6: Flattened ✓ RECOMMENDED (ground splash)
        // - 0.3-0.4: Very flat, pancake-like splash
        
        // --- Droplet Size Range ---
        private const int SPLASH_MIN_DROPLET_SIZE = 1;
        // Minimum size of individual splash droplets in pixels
        // Recommended: 1-2 pixels
        
        private const int SPLASH_MAX_DROPLET_SIZE = 3;
        // Maximum size of individual splash droplets in pixels
        // Recommended: 2-4 pixels
        
        // --- Alpha/Visibility Parameters ---
        private const float SPLASH_ALPHA_BACKGROUND = 0.5f;
        // Alpha multiplier for background splashes (0.0 = invisible, 1.0 = full brightness)
        // - Low (0.3-0.5): Subtle, distant splashes
        // - Medium (0.5-0.7): Balanced visibility ✓ RECOMMENDED
        // - High (0.7-1.0): Bright, prominent splashes
        
        private const float SPLASH_ALPHA_MIDDLE = 0.7f;
        // Alpha multiplier for middle layer splashes
        // Recommended: 0.6-0.8 (more visible than background)
        
        private const float SPLASH_ALPHA_FOREGROUND = 0.9f;
        // Alpha multiplier for foreground splashes
        // Recommended: 0.8-1.0 (most visible)
        
        private const float SPLASH_ALPHA_VARIATION_MIN = 0.6f;
        private const float SPLASH_ALPHA_VARIATION_MAX = 1.0f;
        // Per-droplet random alpha variation range (creates brightness variation within splash)
        // Each droplet gets random alpha between min and max
        // - Narrow range (0.8-1.0): Uniform brightness
        // - Wide range (0.5-1.0): High variation, more organic ✓ RECOMMENDED
        
        // --- Animation Parameters ---
        private const float SPLASH_SIZE_SCALE_MULTIPLIER = 1.5f;
        // Size animation amplitude (how much splash expands)
        // Formula: baseSize × (1 + sin(progress × π) × THIS_VALUE)
        // - Small (0.5-1.0): Subtle size change
        // - Medium (1.5-2.0): Noticeable expand/contract ✓ RECOMMENDED
        // - Large (2.5-3.5): Dramatic size animation
        
        // --- Splash Enable/Disable by Density ---
        private const bool SPLASH_ENABLED_SMALL_DOTS = false;
        // Enable splash effects for small dots (density 0-17)
        // - false: No splashes for light drizzle (subtle, minimal effect)
        // - true: Splashes even at lowest density
        
        private const bool SPLASH_ENABLED_LARGE_DOTS = true;
        // Enable splash effects for large dots (density 18-35)
        // - false: No splashes for moderate rain
        // - true: Splashes appear at moderate density ✓ RECOMMENDED
        
        private const bool SPLASH_ENABLED_SHORT_LINES = true;
        // Enable splash effects for short lines (density 36-52)
        // - Always recommended to be true for heavy rain
        
        private const bool SPLASH_ENABLED_LONG_BOLTS = true;
        // Enable splash effects for long bolts (density 53-70)
        // - Always recommended to be true for storm conditions
        
        private const int MAX_SPLASHES_PER_FRAME = 5;
        // Rate limit for splash creation (currently not enforced)
        // Can be used to prevent splash spam in extreme conditions

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
                effect.RippleCreated = false; // Initialize ripple flag
                
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
                    splash.SeedID = effect.ID + (uint)Time.Ticks; // Unique seed for random pattern
                    
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
                            splash.Size = 1f + RandomHelper.GetValue(0, 3) * 0.1f; // 1-1.3px
                            break;
                        case DepthLayer.Middle:
                            splash.Size = 1f + RandomHelper.GetValue(0, 5) * 0.1f; // 1-1.5px
                            break;
                        case DepthLayer.Foreground:
                            splash.Size = 1f + RandomHelper.GetValue(0, 10) * 0.1f; // 1-2px
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
                            
                            // Immediate disposal logic: particles disappear instantly when crossing fade threshold
                            int viewportHeight = winsize.Y;
                            
                            // Skip disposal check if particle never fades (some foreground particles)
                            if (!effect.NeverFade)
                            {
                                // Calculate particle's absolute fade position using its random threshold
                                int particleFadeY = (int)(viewportHeight * effect.FadeThreshold);
                                
                                // Check if particle just crossed fade threshold (first crossing only)
                                if (newY >= particleFadeY && oldY < particleFadeY)
                                {
                                    // Check if splash is enabled for current density level
                                    bool splashEnabled = false;
                                    switch (rainStyle)
                                    {
                                        case RainRenderStyle.SmallDots:
                                            splashEnabled = SPLASH_ENABLED_SMALL_DOTS;
                                            break;
                                        case RainRenderStyle.LargeDots:
                                            splashEnabled = SPLASH_ENABLED_LARGE_DOTS;
                                            break;
                                        case RainRenderStyle.ShortLines:
                                            splashEnabled = SPLASH_ENABLED_SHORT_LINES;
                                            break;
                                        case RainRenderStyle.LongBolts:
                                            splashEnabled = SPLASH_ENABLED_LONG_BOLTS;
                                            break;
                                    }
                                    
                                    // Create splash at current position (if enabled)
                                    if (splashEnabled)
                                    {
                                        CreateSplash(ref effect, effect.WorldX, effect.WorldY);
                                        
                                        // Trigger ripple effect if rain hits water tile (only once per particle)
                                        if (!effect.RippleCreated)
                                        {
                                            _world.RippleEffect.CreateRipple(effect.WorldX, effect.WorldY);
                                            effect.RippleCreated = true;
                                        }
                                    }
                                    
                                    // Immediately respawn particle at top with new random threshold
                                    int playerAbsIsoX = (tileOffX - tileOffY) * 22;
                                    int playerAbsIsoY = (tileOffX + tileOffY) * 22;
                                    effect.WorldX = playerAbsIsoX + RandomHelper.GetValue(-visibleRangeX, visibleRangeX);
                                    effect.WorldY = playerAbsIsoY - visibleRangeY; // Spawn at top
                                    effect.RippleCreated = false; // Reset ripple flag for respawned particle
                                    
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
                                    smallDotColor *= smallDotAlpha;
                                    
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
                                    largeDotColor *= largeDotAlpha;
                                    
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
                                    shortLineColor *= shortLineAlpha;
                                    
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
                                    boltColor *= boltAlpha;
                                    
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
                // When SPLASH_RISE_SPEED = 0.0, VelocityY = 0.0, so no movement occurs
                if (splash.VelocityY != 0.0f)
                {
                    splash.WorldY += splash.VelocityY * splashSpeedOffset;
                }
                
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
                
                // Size animation: grows then shrinks (parabolic curve using sine)
                float sizeScale = 1f + (float)Math.Sin(progress * Math.PI) * SPLASH_SIZE_SCALE_MULTIPLIER;
                float currentSize = splash.Size * sizeScale;
                
                // Ensure minimum size to prevent invisible droplets
                currentSize = Math.Max(2f, currentSize);
                
                // Alpha fades out linearly over animation duration
                float alpha = 1f - progress;
                
                // Apply depth-based alpha adjustment using configured constants
                switch (splash.Depth)
                {
                    case DepthLayer.Background:
                        alpha *= SPLASH_ALPHA_BACKGROUND;
                        break;
                    case DepthLayer.Middle:
                        alpha *= SPLASH_ALPHA_MIDDLE;
                        break;
                    case DepthLayer.Foreground:
                        alpha *= SPLASH_ALPHA_FOREGROUND;
                        break;
                }
                
                // Draw natural water splash as scattered droplets
                // Real water splashes have many irregular particles spreading outward
                int splashX = (int)splash.X;
                int splashY = (int)splash.Y;
                
                // Base splash color: light blue-white for rain impact
                Color baseSplashColor = Color.Lerp(Color.LightBlue, Color.White, 0.7f);
                baseSplashColor *= alpha;
                
                // Determine number of droplets based on depth layer using configured constants
                int numDroplets;
                switch (splash.Depth)
                {
                    case DepthLayer.Background:
                        numDroplets = SPLASH_DROPLETS_BACKGROUND;
                        break;
                    case DepthLayer.Middle:
                        numDroplets = SPLASH_DROPLETS_MIDDLE;
                        break;
                    case DepthLayer.Foreground:
                        numDroplets = SPLASH_DROPLETS_FOREGROUND;
                        break;
                    default:
                        numDroplets = SPLASH_DROPLETS_MIDDLE;
                        break;
                }
                
                // Draw scattered splash droplets with realistic physics
                // SPREADING ANIMATION: Droplets start at impact point, spread outward (bounce effect)
                // UPPER HEMISPHERE ONLY: Water splashes upward, not downward
                for (int p = 0; p < numDroplets; p++)
                {
                    // Generate truly random pattern using hash function (prevents sequential angles)
                    uint particleSeed = (splash.SeedID * 73856093u) ^ ((uint)p * 19349663u);
                    
                    // Random angle for each droplet - UPPER HEMISPHERE ONLY (0-180 degrees)
                    // Water splashing on ground only sprays upward, not downward
                    // 0° = right, 90° = up, 180° = left (all above ground)
                    float randomAngle = (float)(particleSeed % 180);  // 0-180 degrees (upper half)
                    float angle = randomAngle * 0.017453f; // Convert to radians
                    
                    // Random spread distance per droplet (0.5-1.0 variation)
                    float randomSpread = ((particleSeed % 100) / 100f) * 0.5f + 0.5f;
                    
                    // Calculate FINAL position (where droplet will end up)
                    float finalSpreadRadius = splash.Size * randomSpread * SPLASH_SPREAD_MULTIPLIER;
                    
                    // ANIMATED SPREADING: Droplet position interpolates from center to final position
                    // progress = 0.0: droplet at impact point (center)
                    // progress = 0.5: droplet halfway to final position
                    // progress = 1.0: droplet at final position
                    // This creates realistic "bouncing outward" effect
                    float currentSpreadRadius = finalSpreadRadius * progress;
                    
                    // Elliptical spread pattern using configured factors (wider than tall)
                    int offsetX = (int)(Math.Cos(angle) * currentSpreadRadius * SPLASH_ELLIPSE_X);
                    // Negate Y to make upward motion (sin(0-180°) is positive, screen Y increases downward)
                    int offsetY = -(int)(Math.Sin(angle) * currentSpreadRadius * SPLASH_ELLIPSE_Y);
                    // Now: 0° = right horizontal, 90° = straight up, 180° = left horizontal
                    
                    // Random droplet size using configured min/max range
                    int dropletSizeRange = SPLASH_MAX_DROPLET_SIZE - SPLASH_MIN_DROPLET_SIZE + 1;
                    int dropletSize = SPLASH_MIN_DROPLET_SIZE + (int)(particleSeed % (uint)dropletSizeRange);
                    
                    // Position droplet - animates from center outward as progress increases
                    Rectangle dropletRect = new Rectangle(
                        splashX + offsetX - dropletSize / 2,
                        splashY + offsetY - dropletSize / 2,
                        dropletSize,
                        dropletSize
                    );
                    
                    // Random alpha variation per droplet using configured range
                    float alphaRange = SPLASH_ALPHA_VARIATION_MAX - SPLASH_ALPHA_VARIATION_MIN;
                    float alphaVariation = SPLASH_ALPHA_VARIATION_MIN + ((particleSeed % 100) / 100f) * alphaRange;
                    Color dropletColor = baseSplashColor * alphaVariation;
                    
                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(dropletColor),
                        dropletRect,
                        Vector3.UnitZ,
                        layerDepth
                    );
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
            public bool RippleCreated;   // Flag to track if ripple was already created for this particle
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
            public uint SeedID;                 // Random seed for consistent splash pattern
        }
    }
}