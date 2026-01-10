// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Effects;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using MathHelper = Microsoft.Xna.Framework.MathHelper;
using ClassicUO.Game.Map;
using ClassicUO.IO.Audio;
using ClassicUO.Configuration;

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
        private const int DENSITY_SMALL_DOTS = 10;
        private const int DENSITY_LARGE_DOTS = 30;
        private const int DENSITY_SHORT_LINES = 50;
        // Above 50 = long bolts

        private const float BASE_RAIN_SPEED_Y = 20.0f;
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

        // --- Splash Enable/Disable by Density ---
        private const bool SPLASH_ENABLED_SMALL_DOTS = true;
        // Enable splash effects for small dots (density 0-10)
        // - false: No splashes for light drizzle (subtle, minimal effect)
        // - true: Splashes even at lowest density

        private const bool SPLASH_ENABLED_LARGE_DOTS = true;
        // Enable splash effects for large dots (density 11-30)
        // - false: No splashes for moderate rain
        // - true: Splashes appear at moderate density ✓ RECOMMENDED

        private const bool SPLASH_ENABLED_SHORT_LINES = true;
        // Enable splash effects for short lines (density 30-50)
        // - Always recommended to be true for heavy rain

        private const bool SPLASH_ENABLED_LONG_BOLTS = true;
        // Enable splash effects for long bolts (density 50-70)
        // - Always recommended to be true for storm conditions

        private const float RAIN_VOLUME_MULTIPLIER = 0.30f;
        private const int MINOR_RAIN_SOUND_ID = 0x011;
        private const int HEAVY_RAIN_SOUND_ID = 0x010;

        private readonly WeatherEffect[] _effects = new WeatherEffect[byte.MaxValue];
        private uint _timer, _windTimer, _lastTick;
        private readonly World _world;
        private UOSound _currentRainSound;

        private static Texture2D _whiteTexture;
        private static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    Console.WriteLine("Initializing Weather WhiteTexture");
                    _whiteTexture = SolidColorTextureCache.GetTexture(Color.White);
                    Console.WriteLine("Initialized Weather WhiteTexture");
                    if (_whiteTexture == null)
                    {
                        throw new Exception("Failed to initialize Weather WhiteTexture");
                    }
                }

                return _whiteTexture;
            }
        }

        public Weather(World world)
        {
            _world = world;
        }

        private static Vector3 ColorToVector3(Color color)
        {
            return new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
        }


        public WeatherType? CurrentWeather { get; private set; }
        public WeatherType Type { get; private set; }
        public byte Count { get; private set; }
        public byte ScaledCount { get; private set; }
        public byte CurrentCount { get; private set; }
        public byte Temperature { get; private set; }
        public sbyte Wind { get; private set; }



        private static float SinOscillate(float freq, int range, uint current_tick)
        {
            float anglef = (int)(current_tick / 2.7777f * freq) % 360;

            return Math.Sign(MathHelper.ToRadians(anglef)) * range;
        }


        public void Reset()
        {
            Type = 0;
            Count = CurrentCount = Temperature = 0;
            Wind = 0;
            _windTimer = _timer = 0;
            CurrentWeather = null;
            StopRainSound();
        }

        public void Generate(WeatherType type, byte count, byte temp)
        {
            bool extended = CurrentWeather.HasValue && CurrentWeather == type;

            if (!extended)
            {
                Reset();
            }

            Type = type;
            Count = (byte)Math.Min(MAX_WEATHER_EFFECT, (int)count);
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

                    // Start looping rain sound
                    UpdateRainSound();

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

                // Initialize particles randomly around player position (original behavior)
                // This creates natural scattered appearance at weather start
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
            SmallDots,      
            LargeDots,      
            ShortLines,     
            LongBolts       
        }

        private enum SnowRenderStyle
        {
            LightFlakes,    
            MediumFlakes,   
            HeavyFlakes,    
            Blizzard        
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

        private SnowRenderStyle GetSnowRenderStyle()
        {
            if (Count <= DENSITY_SMALL_DOTS)
                return SnowRenderStyle.LightFlakes;
            else if (Count <= DENSITY_LARGE_DOTS)
                return SnowRenderStyle.MediumFlakes;
            else if (Count <= DENSITY_SHORT_LINES)
                return SnowRenderStyle.HeavyFlakes;
            else
                return SnowRenderStyle.Blizzard;
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
            // Get base water splash configuration
            SplashConfig config = SplashConfig.WaterSplash();

            // Customize based on depth layer
            switch (effect.Depth)
            {
                case DepthLayer.Background:
                    config.DropletCount = 3;
                    config.BaseSize = 1.15f; // Average of 1-1.3 range
                    config.AlphaMultiplier = 0.5f;
                    break;
                case DepthLayer.Middle:
                    config.DropletCount = 6;
                    config.BaseSize = 1.25f; // Average of 1-1.5 range
                    config.AlphaMultiplier = 0.7f;
                    break;
                case DepthLayer.Foreground:
                    config.DropletCount = 10;
                    config.BaseSize = 1.5f; // Average of 1-2 range
                    config.AlphaMultiplier = 0.9f;
                    break;
            }

            _world.SplashEffect.CreateSplash(worldX, worldY, config);
        }

        private void PlayWind()
        {
            PlaySound(RandomHelper.RandomList(0x014, 0x015, 0x016));
        }

        private void PlayThunder()
        {
            PlaySound(RandomHelper.RandomList(0x028, 0x206));
        }

        private void PlayMinorRain()
        {
            PlayRainSoundLoop(MINOR_RAIN_SOUND_ID);
        }

        private void PlayHeavyRain()
        {
            PlayRainSoundLoop(HEAVY_RAIN_SOUND_ID);
        }

        private void PlayRainSoundLoop(int soundId)
        {
            if (!_world.InGame || _world.Player == null)
            {
                return;
            }

            StopRainSound();

            UOSound rainSound = (UOSound)Client.Game.UO.Sounds.GetSound(soundId);
            if (rainSound != null)
            {
                Profile currentProfile = ProfileManager.CurrentProfile;
                if (currentProfile == null || !currentProfile.EnableSound || !currentProfile.EnableRainSound)
                {
                    return;
                }

                const float SOUND_DELTA = 250.0f;
                float volume = currentProfile.SoundVolume / SOUND_DELTA;

                if (!Client.Game.IsActive && !currentProfile.ReproduceSoundsInBackground)
                {
                    volume = 0;
                }

                // Rain sound is quieter than normal sound effects
                volume *= RAIN_VOLUME_MULTIPLIER;

                if (volume > 0 && volume <= 1.0f)
                {
                    rainSound.IsLooping = true;

                    if (rainSound.Play(Time.Ticks, volume, 0.0f))
                    {
                        // Submit additional buffers upfront for seamless playback (prevents gaps)
                        // DynamicSoundEffectInstance needs at least 3 buffers for smooth playback
                        rainSound.SubmitAdditionalBuffers(2);

                        rainSound.X = _world.Player.X;
                        rainSound.Y = _world.Player.Y;
                        rainSound.CalculateByDistance = false;

                        // Only set _currentRainSound after successfully starting playback
                        _currentRainSound = rainSound;
                    }
                    else
                    {
                        // If sound failed to play, ensure reference is cleared
                        rainSound.IsLooping = false;
                    }
                }
            }
        }

        private void StopRainSound()
        {
            if (_currentRainSound != null)
            {
                _currentRainSound.IsLooping = false;
                _currentRainSound.Stop();
                _currentRainSound = null;
            }
        }

        private void UpdateRainSound()
        {
            if (Type != WeatherType.WT_RAIN && Type != WeatherType.WT_STORM_APPROACH)
            {
                StopRainSound();
                return;
            }


            // Check if we need to start or restart the rain sound
            bool shouldPlaySound = Count > 0 && CurrentCount > 0;

            if (!shouldPlaySound)
            {
                StopRainSound();
                return;
            }

            // Determine if rain is minor or heavy based on density
            // Minor: Count <= DENSITY_LARGE_DOTS (SmallDots + LargeDots)
            // Heavy: Count > DENSITY_LARGE_DOTS (ShortLines + LongBolts)
            bool shouldPlayHeavyRain = Count > DENSITY_SHORT_LINES;

            // Determine what sound should be playing
            int expectedSoundId = shouldPlayHeavyRain ? HEAVY_RAIN_SOUND_ID : MINOR_RAIN_SOUND_ID;

            // Only restart if we need to switch between minor/heavy, or if sound is not currently playing
            // When looping is enabled, sound will continue automatically, so we only need to check for type changes
            bool needsRestart = _currentRainSound == null || _currentRainSound.Index != expectedSoundId;

            if (needsRestart)
            {
                if (shouldPlayHeavyRain)
                {
                    PlayHeavyRain();
                }
                else
                {
                    PlayMinorRain();
                }
            }
            else
            {
                // Update volume to follow system sound effect volume changes
                UpdateRainSoundVolume();
            }
        }

        private void UpdateRainSoundVolume()
        {
            if (_currentRainSound == null || !_world.InGame || _world.Player == null)
            {
                return;
            }

            Profile currentProfile = ProfileManager.CurrentProfile;
            if (currentProfile == null || !currentProfile.EnableSound || !currentProfile.EnableRainSound)
            {
                _currentRainSound.Volume = 0;
                return;
            }

            const float SOUND_DELTA = 250.0f;
            float volume = currentProfile.SoundVolume / SOUND_DELTA;

            if (!Client.Game.IsActive && !currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            // Rain sound is kept at 50% of system sound effect volume
            volume *= RAIN_VOLUME_MULTIPLIER;

            _currentRainSound.Volume = Math.Clamp(volume, 0f, 1.0f);
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

            // Update rain sound looping for rain weather types
            if (Type == WeatherType.WT_RAIN || Type == WeatherType.WT_STORM_APPROACH)
            {
                UpdateRainSound();
            }

            bool windChanged = false;

            if (_windTimer < Time.Ticks)
            {
                if (_windTimer == 0)
                {
                    windChanged = true;
                }

                _windTimer = Time.Ticks + (uint)(RandomHelper.GetValue(13, 19) * 1000);

                sbyte lastWind = Wind;

                Wind = (sbyte)RandomHelper.GetValue(0, 4);

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

                    // Respawn particle at top of viewport
                    // Calculate viewport top in world coordinates
                    int viewportTopY = viewportOffsetY - visibleRangeY;
                    int playerAbsIsoX = (tileOffX - tileOffY) * 22;

                    effect.WorldX = playerAbsIsoX + RandomHelper.GetValue(-visibleRangeX, visibleRangeX);
                    effect.WorldY = viewportTopY; // Spawn at exact top of viewport

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
                        float densitySpeedMultiplier = 1.4f;

                        // Higher density = faster speeds
                        switch (rainStyle)
                        {
                            case RainRenderStyle.SmallDots:
                                densitySpeedMultiplier = 1.4f;
                                break;
                            case RainRenderStyle.LargeDots:
                                densitySpeedMultiplier = 1.6f;
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

                        DepthProperties snowProps = GetDepthProperties(effect.Depth, Type);
                        SnowRenderStyle snowStyle = GetSnowRenderStyle();

                        float snowBaseSpeedY = BASE_SNOW_SPEED_Y;
                        float snowDensitySpeedMultiplier = 1.0f;

                        // Higher density = faster speeds
                        switch (snowStyle)
                        {
                            case SnowRenderStyle.LightFlakes:
                                snowDensitySpeedMultiplier = 1.0f;
                                break;
                            case SnowRenderStyle.MediumFlakes:
                                snowDensitySpeedMultiplier = 1.2f;
                                break;
                            case SnowRenderStyle.HeavyFlakes:
                                snowDensitySpeedMultiplier = 1.5f;
                                break;
                            case SnowRenderStyle.Blizzard:
                                snowDensitySpeedMultiplier = 2.0f;
                                break;
                        }

                        // Vertical fall with wind influence
                        // Wind strength multiplier - moderate effect for natural drift
                        float windStrengthMultiplier = 0.8f;
                        effect.SpeedY = snowBaseSpeedY * snowDensitySpeedMultiplier * snowProps.SpeedMultiplier;
                        effect.SpeedX = Wind * windStrengthMultiplier * snowProps.SpeedMultiplier;

                        if (windChanged)
                        {
                            PlayWind();
                        }

                        break;

                    case WeatherType.WT_STORM_APPROACH:

                        DepthProperties snowStormProps = GetDepthProperties(effect.Depth, Type);

                        effect.SpeedX = Wind;
                        effect.SpeedY = 6.0f;

                        if (windChanged)
                        {
                            effect.SpeedAngle = MathHelper.ToDegrees((float)Math.Atan2(effect.SpeedX, effect.SpeedY));

                            effect.SpeedMagnitude = (float)Math.Sqrt(Math.Pow(effect.SpeedX, 2) + Math.Pow(effect.SpeedY, 2));

                            PlayThunder();
                        }

                        float speedAngle = effect.SpeedAngle;
                        float speedMagnitude = effect.SpeedMagnitude;

                        speedMagnitude += effect.ScaleRatio;

                        speedAngle += SinOscillate(0.4f, 20, Time.Ticks + effect.ID);

                        float rad = MathHelper.ToRadians(speedAngle);
                        // Apply depth-based speed multiplier for parallax effect
                        effect.SpeedX = speedMagnitude * (float)Math.Sin(rad) * snowStormProps.SpeedMultiplier;
                        effect.SpeedY = speedMagnitude * (float)Math.Cos(rad) * snowStormProps.SpeedMultiplier;

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
                            int rainViewportHeight = winsize.Y;

                            // Skip disposal check if particle never fades (some foreground particles)
                            if (!effect.NeverFade)
                            {
                                // Calculate particle's absolute fade position using its random threshold
                                int particleFadeY = (int)(rainViewportHeight * effect.FadeThreshold);

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
                                    if (splashEnabled && ProfileManager.CurrentProfile?.EnableWeatherEffects == true)
                                    {
                                        if (!HasNonRenderingCoveringTileAtPosition(effect.WorldX, effect.WorldY))
                                        {
                                            CreateSplash(ref effect, effect.WorldX, effect.WorldY);
                                        }

                                        // Trigger ripple effect if rain hits water tile (only once per particle)
                                        if (!effect.RippleCreated)
                                        {
                                            var isWaterTile = IsWaterTileAtPosition(effect.WorldX, effect.WorldY);
                                            if (isWaterTile)
                                            {
                                                _world.RippleEffect.CreateRipple(effect.WorldX, effect.WorldY);
                                                effect.RippleCreated = true;
                                            }
                                        }
                                    }

                                    // Immediately respawn particle at top of viewport with new random threshold
                                    // Calculate viewport top in world coordinates
                                    int viewportTopY = viewportOffsetY - visibleRangeY;
                                    int playerAbsIsoX = (tileOffX - tileOffY) * 22;
                                    effect.WorldX = playerAbsIsoX + RandomHelper.GetValue(-visibleRangeX, visibleRangeX);
                                    effect.WorldY = viewportTopY; // Spawn at exact top of viewport
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
                                    // Multi-layer rendering for soft, glowing rain drops
                                    int smallDotSize = (int)(2 * rainDepthProps.SizeMultiplier);
                                    smallDotSize = Math.Max(2, smallDotSize);

                                    // Base color for small rain drops - light blue
                                    Color smallDotBaseColor = Color.Lerp(Color.LightBlue, rainDepthProps.ColorTint, 0.1f);
                                    // Boost alpha for visibility
                                    float smallDotBoostedAlpha = Math.Max(0.8f, rainDepthProps.AlphaMultiplier);

                                    // Outer layer (largest, most transparent - edge)
                                    if (smallDotSize >= 2)
                                    {
                                        int outerSize = smallDotSize;
                                        float outerAlpha = smallDotBoostedAlpha * 0.25f; // 25% of base alpha for outer edge
                                        Color outerColor = smallDotBaseColor * outerAlpha;

                                        Rectangle outerRect = new Rectangle(
                                            newX - outerSize / 2,
                                            newY - outerSize / 2,
                                            outerSize,
                                            outerSize
                                        );

                                        batcher.Draw
                                        (
                                            WhiteTexture,
                                            outerRect,
                                            ColorToVector3(outerColor),
                                            layerDepth
                                        );
                                    }

                                    // Middle-outer layer (75% size, 45% alpha)
                                    if (smallDotSize >= 2)
                                    {
                                        int midOuterSize = Math.Max(1, (int)(smallDotSize * 0.75f));
                                        float midOuterAlpha = smallDotBoostedAlpha * 0.45f;
                                        Color midOuterColor = smallDotBaseColor * midOuterAlpha;

                                        Rectangle midOuterRect = new Rectangle(
                                            newX - midOuterSize / 2,
                                            newY - midOuterSize / 2,
                                            midOuterSize,
                                            midOuterSize
                                        );

                                        batcher.Draw
                                        (
                                            WhiteTexture,
                                            midOuterRect,
                                            ColorToVector3(midOuterColor),
                                            layerDepth + 0.0001f
                                        );
                                    }

                                    // Middle layer (50% size, 70% alpha)
                                    if (smallDotSize >= 2)
                                    {
                                        int middleSize = Math.Max(1, (int)(smallDotSize * 0.5f));
                                        float middleAlpha = smallDotBoostedAlpha * 0.70f;
                                        Color middleColor = smallDotBaseColor * middleAlpha;

                                        Rectangle middleRect = new Rectangle(
                                            newX - middleSize / 2,
                                            newY - middleSize / 2,
                                            middleSize,
                                            middleSize
                                        );

                                        batcher.Draw
                                        (
                                            WhiteTexture,
                                            middleRect,
                                            ColorToVector3(middleColor),
                                            layerDepth + 0.0002f
                                        );
                                    }

                                    // Inner core (35% size, fully opaque)
                                    int smallCoreSize = Math.Max(1, (int)(smallDotSize * 0.35f));
                                    Color smallCoreColor = smallDotBaseColor * smallDotBoostedAlpha;

                                    Rectangle smallCoreRect = new Rectangle(
                                        newX - smallCoreSize / 2,
                                        newY - smallCoreSize / 2,
                                        smallCoreSize,
                                        smallCoreSize
                                    );

                                    batcher.Draw
                                    (
                                        WhiteTexture,
                                        smallCoreRect,
                                        ColorToVector3(smallCoreColor),
                                        layerDepth + 0.0003f
                                    );
                                    break;

                                case RainRenderStyle.LargeDots:
                                    // Multi-layer rendering for soft, glowing rain drops
                                    int largeDotSize = (int)(3 * rainDepthProps.SizeMultiplier);
                                    largeDotSize = Math.Max(2, largeDotSize);

                                    // Base color for large rain drops - cornflower blue (more saturated)
                                    Color largeDotBaseColor = Color.Lerp(Color.CornflowerBlue, rainDepthProps.ColorTint, 0.2f);
                                    // Boost alpha for visibility (more opaque than small dots)
                                    float largeDotBoostedAlpha = Math.Max(0.9f, rainDepthProps.AlphaMultiplier);

                                    // Outer layer (largest, most transparent - edge)
                                    if (largeDotSize >= 2)
                                    {
                                        int outerSize = largeDotSize;
                                        float outerAlpha = largeDotBoostedAlpha * 0.25f; // 25% of base alpha for outer edge
                                        Color outerColor = largeDotBaseColor * outerAlpha;

                                        Rectangle outerRect = new Rectangle(
                                            newX - outerSize / 2,
                                            newY - outerSize / 2,
                                            outerSize,
                                            outerSize
                                        );

                                        batcher.Draw
                                        (
                                            WhiteTexture,
                                            outerRect,
                                            ColorToVector3(outerColor),
                                            layerDepth
                                        );
                                    }

                                    // Middle-outer layer (75% size, 45% alpha)
                                    if (largeDotSize >= 2)
                                    {
                                        int midOuterSize = Math.Max(1, (int)(largeDotSize * 0.75f));
                                        float midOuterAlpha = largeDotBoostedAlpha * 0.45f;
                                        Color midOuterColor = largeDotBaseColor * midOuterAlpha;

                                        Rectangle midOuterRect = new Rectangle(
                                            newX - midOuterSize / 2,
                                            newY - midOuterSize / 2,
                                            midOuterSize,
                                            midOuterSize
                                        );

                                        batcher.Draw
                                        (
                                            WhiteTexture,
                                            midOuterRect,
                                            ColorToVector3(midOuterColor),
                                            layerDepth + 0.0001f
                                        );
                                    }

                                    // Middle layer (50% size, 70% alpha)
                                    if (largeDotSize >= 2)
                                    {
                                        int middleSize = Math.Max(1, (int)(largeDotSize * 0.5f));
                                        float middleAlpha = largeDotBoostedAlpha * 0.70f;
                                        Color middleColor = largeDotBaseColor * middleAlpha;

                                        Rectangle middleRect = new Rectangle(
                                            newX - middleSize / 2,
                                            newY - middleSize / 2,
                                            middleSize,
                                            middleSize
                                        );

                                        batcher.Draw
                                        (
                                            WhiteTexture,
                                            middleRect,
                                            ColorToVector3(middleColor),
                                            layerDepth + 0.0002f
                                        );
                                    }

                                    // Inner core (35% size, fully opaque)
                                    int largeCoreSize = Math.Max(1, (int)(largeDotSize * 0.35f));
                                    Color largeCoreColor = largeDotBaseColor * largeDotBoostedAlpha;

                                    Rectangle largeCoreRect = new Rectangle(
                                        newX - largeCoreSize / 2,
                                        newY - largeCoreSize / 2,
                                        largeCoreSize,
                                        largeCoreSize
                                    );

                                    batcher.Draw
                                    (
                                        WhiteTexture,
                                        largeCoreRect,
                                        ColorToVector3(largeCoreColor),
                                        layerDepth + 0.0003f
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
                                        WhiteTexture,
                                        shortStart,
                                        shortEnd,
                                        ColorToVector3(shortLineColor),
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
                                        WhiteTexture,
                                        boltStart,
                                        boltEnd,
                                        ColorToVector3(boltColor),
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
                                WhiteTexture,
                                start,
                                end,
                                ColorToVector3(Color.Blue),
                                2,
                                layerDepth
                            );
                        }

                        break;

                    case WeatherType.WT_SNOW:

                        SnowRenderStyle snowStyle = GetSnowRenderStyle();
                        DepthProperties snowDepthProps = GetDepthProperties(effect.Depth, Type);

                        // Store old absolute position for fade threshold detection
                        float oldSnowWorldX = effect.WorldX;
                        float oldSnowWorldY = effect.WorldY;

                        // Apply physics in absolute isometric space
                        effect.WorldX += effect.SpeedX * speedOffset;
                        effect.WorldY += effect.SpeedY * speedOffset;

                        // Convert both positions to viewport-relative coordinates for rendering
                        int oldSnowX = (int)(oldSnowWorldX - viewportOffsetX);
                        int oldSnowY = (int)(oldSnowWorldY - viewportOffsetY);
                        int snowX = (int)(effect.WorldX - viewportOffsetX);
                        int snowY = (int)(effect.WorldY - viewportOffsetY);

                        // Immediate disposal logic: particles disappear instantly when crossing fade threshold
                        int snowViewportHeight = winsize.Y;

                        // Skip disposal check if particle never fades (some foreground particles)
                        if (!effect.NeverFade)
                        {
                            // Calculate particle's absolute fade position using its random threshold
                            int particleFadeY = (int)(snowViewportHeight * effect.FadeThreshold);

                            // Check if particle just crossed fade threshold (first crossing only)
                            if (snowY >= particleFadeY && oldSnowY < particleFadeY)
                            {
                                // Immediately respawn particle at top of viewport with new random fade threshold
                                // Calculate viewport top in world coordinates
                                int viewportTopY = viewportOffsetY - visibleRangeY;
                                int playerAbsIsoX = (tileOffX - tileOffY) * 22;
                                effect.WorldX = playerAbsIsoX + RandomHelper.GetValue(-visibleRangeX, visibleRangeX);
                                effect.WorldY = viewportTopY; // Spawn at exact top of viewport

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

                        // Density-based size multiplier
                        float densitySizeMultiplier = 1.0f;
                        switch (snowStyle)
                        {
                            case SnowRenderStyle.LightFlakes:
                                densitySizeMultiplier = 1.5f;
                                break;
                            case SnowRenderStyle.MediumFlakes:
                                densitySizeMultiplier = 1.8f;
                                break;
                            case SnowRenderStyle.HeavyFlakes:
                                densitySizeMultiplier = 2.1f;
                                break;
                            case SnowRenderStyle.Blizzard:
                                densitySizeMultiplier = 2.8f;
                                break;
                        }

                        // Apply both depth and density multipliers to size
                        int snowSize = (int)(2 * snowDepthProps.SizeMultiplier * densitySizeMultiplier);
                        snowSize = Math.Max(1, snowSize);

                        // Depth-based color and alpha with better visibility
                        // Boost alpha for snow visibility (minimum 70% even for background)
                        float snowBoostedAlpha = Math.Max(0.7f, snowDepthProps.AlphaMultiplier);

                        // Draw snow particle with semi-transparent edges using layered gradient
                        // This creates a soft, circular-like fade from center to edge
                        // Outer layer (largest, most transparent - edge)
                        if (snowSize >= 2)
                        {
                            int outerSize = snowSize;
                            float outerAlpha = snowBoostedAlpha * 0.25f; // 25% of base alpha for outer edge
                            Color outerColor = snowDepthProps.ColorTint * outerAlpha;

                            Rectangle outerRect = new Rectangle(
                                snowX - outerSize / 2,
                                snowY - outerSize / 2,
                                outerSize,
                                outerSize
                            );

                            batcher.Draw
                            (
                                WhiteTexture,
                                outerRect,
                                ColorToVector3(outerColor),
                                layerDepth
                            );
                        }

                        // Middle-outer layer (75% size, 45% alpha)
                        if (snowSize >= 2)
                        {
                            int midOuterSize = Math.Max(1, (int)(snowSize * 0.75f));
                            float midOuterAlpha = snowBoostedAlpha * 0.45f;
                            Color midOuterColor = snowDepthProps.ColorTint * midOuterAlpha;

                            Rectangle midOuterRect = new Rectangle(
                                snowX - midOuterSize / 2,
                                snowY - midOuterSize / 2,
                                midOuterSize,
                                midOuterSize
                            );

                            batcher.Draw
                            (
                                WhiteTexture,
                                midOuterRect,
                                ColorToVector3(midOuterColor),
                                layerDepth + 0.0001f
                            );
                        }

                        // Middle layer (50% size, 70% alpha)
                        if (snowSize >= 2)
                        {
                            int middleSize = Math.Max(1, (int)(snowSize * 0.5f));
                            float middleAlpha = snowBoostedAlpha * 0.70f;
                            Color middleColor = snowDepthProps.ColorTint * middleAlpha;

                            Rectangle middleRect = new Rectangle(
                                snowX - middleSize / 2,
                                snowY - middleSize / 2,
                                middleSize,
                                middleSize
                            );

                            batcher.Draw
                            (
                                WhiteTexture,
                                middleRect,
                                ColorToVector3(middleColor),
                                layerDepth + 0.0002f
                            );
                        }

                        // Inner core (smallest, fully opaque)
                        int coreSize = Math.Max(1, (int)(snowSize * 0.35f));
                        Color coreColor = snowDepthProps.ColorTint * snowBoostedAlpha;

                        Rectangle coreRect = new Rectangle(
                            snowX - coreSize / 2,
                            snowY - coreSize / 2,
                            coreSize,
                            coreSize
                        );

                        batcher.Draw
                        (
                            WhiteTexture,
                            coreRect,
                            ColorToVector3(coreColor),
                            layerDepth + 0.0003f
                        );

                        break;
                }
            }

            // Only update and render if weather effects are enabled
            if (ProfileManager.CurrentProfile?.EnableWeatherEffects == true)
            {
                float deltaTime = passed / 1000f; // Convert milliseconds to seconds
                _world.SplashEffect.Update(deltaTime, viewportOffsetX, viewportOffsetY, visibleRangeX, visibleRangeY);
                _world.SplashEffect.Draw(batcher, layerDepth);

                _world.RippleEffect.Update(deltaTime, viewportOffsetX, viewportOffsetY, visibleRangeX, visibleRangeY);
                _world.RippleEffect.Draw(batcher, layerDepth);
            }

            _lastTick = Time.Ticks;
        }

        /// <summary>
        /// Checks if the given absolute isometric position is on a water tile.
        /// Converts directly from absolute isometric coordinates to tile coordinates.
        /// </summary>
        /// <param name="worldX">Absolute isometric X coordinate.</param>
        /// <param name="worldY">Absolute isometric Y coordinate.</param>
        /// <returns>True if the position is on a water tile, false otherwise.</returns>
        /// <remarks>
        /// Thanks to [markdwags](https://github.com/markdwags) for the code 
        /// in [this comment](https://github.com/ClassicUO/ClassicUO/pull/1852#issuecomment-3656749076).
        /// </remarks>
        private bool IsWaterTileAtPosition(float worldX, float worldY)
        {
            (int targetTileX, int targetTileY) = CoordinateHelper.IsometricToTile(worldX, worldY);
            return TileDetectionHelper.IsWaterTile(_world.Map, targetTileX, targetTileY);
        }

        /// <summary>
        /// Checks if the given absolute isometric position has a covering tile above the player.
        /// A covering tile is a roof or other structure that blocks weather effects, even if it's not currently rendering
        /// (e.g., hidden roof when player is inside a house).
        /// Converts directly from absolute isometric coordinates to tile coordinates.
        /// </summary>
        /// <param name="worldX">Absolute isometric X coordinate.</param>
        /// <param name="worldY">Absolute isometric Y coordinate.</param>
        /// <returns>True if the position has a covering tile above the player, false otherwise.</returns>
        private bool HasNonRenderingCoveringTileAtPosition(float worldX, float worldY)
        {
            (int targetTileX, int targetTileY) = CoordinateHelper.IsometricToTile(worldX, worldY);
            int playerZ = _world.Player?.Z ?? 0;
            return TileDetectionHelper.HasNonRenderingCoveringTile(_world.Map, targetTileX, targetTileY, playerZ);
        }

        private struct WeatherEffect
        {
            public float SpeedX, SpeedY, WorldX, WorldY, X, Y, ScaleRatio, SpeedAngle, SpeedMagnitude;
            public uint ID;
            public DepthLayer Depth;  // Depth layer for 3D atmospheric effects
            public float FadeThreshold;  // Per-particle random fade position (0.0-1.0)
            public bool NeverFade;       // Flag for foreground particles that don't fade
            public bool RippleCreated;   // Flag to track if ripple was already created for this particle
            public bool SplashCreated;   // Flag to track if splash was already created
        }
    }
}