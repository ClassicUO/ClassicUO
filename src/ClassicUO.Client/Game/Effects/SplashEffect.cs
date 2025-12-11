// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.Effects
{
    /// <summary>
    /// Configuration structure for splash effect visual parameters.
    /// All visual aspects of the splash can be customized via this struct.
    /// </summary>
    public struct SplashConfig
    {
        // Timing
        public float Duration;              // Animation duration in seconds
        public float RiseSpeed;            // Vertical velocity (negative = up, 0 = no movement)
        
        // Particle Count
        public int DropletCount;           // Number of droplets per splash
        
        // Spread Pattern
        public float SpreadMultiplier;     // How far droplets spread from impact point
        public float EllipseX;             // Horizontal ellipse factor (1.0 = circle, >1.0 = wider horizontally)
        public float EllipseY;             // Vertical ellipse factor (<1.0 = flatter)
        public float AngleRangeMin;         // Minimum angle in degrees (0-360)
        public float AngleRangeMax;         // Maximum angle in degrees (0-360)
        
        // Droplet Size
        public float BaseSize;             // Base size multiplier
        public int MinDropletSize;         // Minimum droplet size in pixels
        public int MaxDropletSize;         // Maximum droplet size in pixels
        public float SizeScaleMultiplier;  // Size animation scale (grow/shrink effect)
        
        // Color
        public Color BaseColor;             // Base splash color
        public float AlphaMultiplier;       // Overall alpha multiplier (0.0-1.0)
        public float AlphaVariationMin;     // Minimum alpha variation per droplet
        public float AlphaVariationMax;     // Maximum alpha variation per droplet
        
        // Coordinate Mode
        public bool UseWorldCoordinates;    // true = world coords, false = viewport coords
        
        /// <summary>
        /// Creates a water splash configuration (default rain splash settings).
        /// Light blue-white color, upward spread (0-180 degrees), moderate animation.
        /// </summary>
        public static SplashConfig WaterSplash()
        {
            return new SplashConfig
            {
                Duration = 3.25f,
                RiseSpeed = -1.0f,
                DropletCount = 6, // Default to middle layer count
                SpreadMultiplier = 1.5f,
                EllipseX = 1.2f,
                EllipseY = 0.2f,
                AngleRangeMin = 0f,
                AngleRangeMax = 180f, // Upper hemisphere only (water splashes upward)
                BaseSize = 1.5f,
                MinDropletSize = 1,
                MaxDropletSize = 3,
                SizeScaleMultiplier = 1.5f,
                BaseColor = Color.Lerp(Color.LightBlue, Color.White, 0.7f),
                AlphaMultiplier = 0.7f,
                AlphaVariationMin = 0.6f,
                AlphaVariationMax = 1.0f,
                UseWorldCoordinates = true
            };
        }
        
        /// <summary>
        /// Creates a fire splash configuration.
        /// Orange-red color, wider spread, faster animation.
        /// </summary>
        public static SplashConfig FireSplash()
        {
            return new SplashConfig
            {
                Duration = 0.5f,
                RiseSpeed = -2.0f,
                DropletCount = 12,
                SpreadMultiplier = 2.0f,
                EllipseX = 1.5f,
                EllipseY = 0.8f,
                AngleRangeMin = 0f,
                AngleRangeMax = 360f, // Full circle for fire
                BaseSize = 2.0f,
                MinDropletSize = 2,
                MaxDropletSize = 4,
                SizeScaleMultiplier = 2.0f,
                BaseColor = Color.OrangeRed,
                AlphaMultiplier = 0.9f,
                AlphaVariationMin = 0.7f,
                AlphaVariationMax = 1.0f,
                UseWorldCoordinates = true
            };
        }
        
        /// <summary>
        /// Creates an explosion splash configuration.
        /// Yellow-white color, very wide spread, many particles, fast animation.
        /// </summary>
        public static SplashConfig ExplosionSplash()
        {
            return new SplashConfig
            {
                Duration = 0.4f,
                RiseSpeed = -3.0f,
                DropletCount = 30,
                SpreadMultiplier = 3.0f,
                EllipseX = 1.0f, // Circular for explosion
                EllipseY = 1.0f,
                AngleRangeMin = 0f,
                AngleRangeMax = 360f, // Full circle
                BaseSize = 3.0f,
                MinDropletSize = 2,
                MaxDropletSize = 5,
                SizeScaleMultiplier = 2.5f,
                BaseColor = Color.Lerp(Color.Yellow, Color.White, 0.5f),
                AlphaMultiplier = 1.0f,
                AlphaVariationMin = 0.8f,
                AlphaVariationMax = 1.0f,
                UseWorldCoordinates = true
            };
        }
        
        /// <summary>
        /// Creates a metal conflict/spark configuration.
        /// Gray-white sparks, tight spread, quick fade.
        /// </summary>
        public static SplashConfig MetalConflictSplash()
        {
            return new SplashConfig
            {
                Duration = 0.3f,
                RiseSpeed = -1.5f,
                DropletCount = 8,
                SpreadMultiplier = 1.0f,
                EllipseX = 1.2f,
                EllipseY = 1.2f,
                AngleRangeMin = 0f,
                AngleRangeMax = 360f, // Full circle for sparks
                BaseSize = 1.0f,
                MinDropletSize = 1,
                MaxDropletSize = 2,
                SizeScaleMultiplier = 1.0f,
                BaseColor = Color.Lerp(Color.Gray, Color.White, 0.8f),
                AlphaMultiplier = 0.9f,
                AlphaVariationMin = 0.7f,
                AlphaVariationMax = 1.0f,
                UseWorldCoordinates = true
            };
        }
    }
    
    /// <summary>
    /// Independent splash effect system that can be used by any game system.
    /// Manages a pool of splash particles with configurable visual parameters.
    /// </summary>
    internal sealed class SplashEffect
    {
        /// <summary>
        /// Internal particle data structure.
        /// </summary>
        private struct SplashParticle
        {
            public float WorldX, WorldY;        // World coordinates
            public float X, Y;                  // Viewport-relative (cached)
            public float LifeTime;              // 0.0-1.0 (animation progress)
            public float VelocityY;             // Vertical velocity
            public float Size;                  // Base size
            public uint SeedID;                 // Random seed for consistent pattern
            public bool Active;                 // Is active
            public SplashConfig Config;         // Configuration snapshot (for this particle)
        }
        
        private readonly SplashParticle[] _particles;
        private const int MAX_PARTICLES = byte.MaxValue; // 255 particles
        
        /// <summary>
        /// Initializes a new instance of the SplashEffect.
        /// </summary>
        public SplashEffect()
        {
            _particles = new SplashParticle[MAX_PARTICLES];
        }
        
        /// <summary>
        /// Creates a new splash effect at the specified position.
        /// </summary>
        /// <param name="worldX">World X coordinate (or viewport X if UseWorldCoordinates is false)</param>
        /// <param name="worldY">World Y coordinate (or viewport Y if UseWorldCoordinates is false)</param>
        /// <param name="config">Visual configuration for the splash</param>
        public void CreateSplash(float worldX, float worldY, SplashConfig config)
        {
            // Find an inactive particle slot
            for (int i = 0; i < _particles.Length; i++)
            {
                ref SplashParticle particle = ref _particles[i];
                if (!particle.Active)
                {
                    particle.Active = true;
                    particle.LifeTime = 0f;
                    particle.SeedID = (uint)Time.Ticks + (uint)i; // Unique seed for random pattern
                    
                    // Store position based on coordinate mode
                    if (config.UseWorldCoordinates)
                    {
                        particle.WorldX = worldX;
                        particle.WorldY = worldY;
                    }
                    else
                    {
                        // Viewport coordinates - store directly in X/Y
                        particle.X = worldX;
                        particle.Y = worldY;
                        particle.WorldX = 0f; // Not used in viewport mode
                        particle.WorldY = 0f;
                    }
                    
                    // Initial velocity
                    particle.VelocityY = config.RiseSpeed;
                    
                    // Base size with some variation
                    float sizeVariation = RandomHelper.GetValue(0, 10) * 0.1f;
                    particle.Size = config.BaseSize + sizeVariation;
                    
                    // Store configuration snapshot
                    particle.Config = config;
                    
                    break; // Found a slot, exit
                }
            }
        }
        
        /// <summary>
        /// Updates all active splash particles.
        /// </summary>
        /// <param name="deltaTime">Time since last update in seconds</param>
        /// <param name="viewportOffsetX">Viewport offset X (only used if UseWorldCoordinates is true)</param>
        /// <param name="viewportOffsetY">Viewport offset Y (only used if UseWorldCoordinates is true)</param>
        /// <param name="visibleRangeX">Visible range X for culling</param>
        /// <param name="visibleRangeY">Visible range Y for culling</param>
        public void Update(float deltaTime, int viewportOffsetX = 0, int viewportOffsetY = 0,
                          int visibleRangeX = int.MaxValue, int visibleRangeY = int.MaxValue)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                ref SplashParticle particle = ref _particles[i];
                if (!particle.Active) continue;
                
                // Update lifetime
                particle.LifeTime += deltaTime / particle.Config.Duration;
                
                // Deactivate if splash animation complete
                if (particle.LifeTime >= 1.0f)
                {
                    particle.Active = false;
                    continue;
                }
                
                // Apply upward movement physics
                if (particle.VelocityY != 0.0f && particle.Config.UseWorldCoordinates)
                {
                    // Apply physics in world space
                    float speedOffset = deltaTime * 37.0f; // Same as SIMULATION_TIME scaling
                    particle.WorldY += particle.VelocityY * speedOffset;
                }
                
                // Convert to viewport-relative coordinates if using world coordinates
                if (particle.Config.UseWorldCoordinates)
                {
                    particle.X = particle.WorldX - viewportOffsetX;
                    particle.Y = particle.WorldY - viewportOffsetY;
                }
                
                // Check visibility and cull if outside viewport
                if (particle.X < -visibleRangeX || particle.X > visibleRangeX * 2 ||
                    particle.Y < -visibleRangeY || particle.Y > visibleRangeY * 2)
                {
                    particle.Active = false;
                    continue;
                }
            }
        }
        
        /// <summary>
        /// Renders all active splash particles.
        /// </summary>
        /// <param name="batcher">Renderer to draw with</param>
        /// <param name="layerDepth">Layer depth for rendering</param>
        public void Draw(UltimaBatcher2D batcher, float layerDepth)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                ref SplashParticle particle = ref _particles[i];
                if (!particle.Active) continue;
                
                SplashConfig config = particle.Config;
                float progress = particle.LifeTime;
                
                // Size animation: grows then shrinks (parabolic curve using sine)
                float sizeScale = 1f + (float)Math.Sin(progress * Math.PI) * config.SizeScaleMultiplier;
                float currentSize = particle.Size * sizeScale;
                
                // Ensure minimum size to prevent invisible droplets
                currentSize = Math.Max(2f, currentSize);
                
                // Alpha fades out linearly over animation duration
                float alpha = (1f - progress) * config.AlphaMultiplier;
                
                // Splash center position
                int splashX = (int)particle.X;
                int splashY = (int)particle.Y;
                
                // Base splash color with alpha
                Color baseSplashColor = config.BaseColor * alpha;
                
                // Draw scattered splash droplets
                for (int p = 0; p < config.DropletCount; p++)
                {
                    // Generate truly random pattern using hash function (prevents sequential angles)
                    uint particleSeed = (particle.SeedID * 73856093u) ^ ((uint)p * 19349663u);
                    
                    // Random angle within configured range
                    float angleRange = config.AngleRangeMax - config.AngleRangeMin;
                    float randomAngle = config.AngleRangeMin + (float)(particleSeed % (uint)angleRange);
                    float angle = randomAngle * 0.017453f; // Convert to radians
                    
                    // Random spread distance per droplet (0.5-1.0 variation)
                    float randomSpread = ((particleSeed % 100) / 100f) * 0.5f + 0.5f;
                    
                    // Calculate final position (where droplet will end up)
                    float finalSpreadRadius = currentSize * randomSpread * config.SpreadMultiplier;
                    
                    // Animated spreading: droplet position interpolates from center to final position
                    // progress = 0.0: droplet at impact point (center)
                    // progress = 1.0: droplet at final position
                    float currentSpreadRadius = finalSpreadRadius * progress;
                    
                    // Elliptical spread pattern
                    int offsetX = (int)(Math.Cos(angle) * currentSpreadRadius * config.EllipseX);
                    // Negate Y to make upward motion (screen Y increases downward)
                    int offsetY = -(int)(Math.Sin(angle) * currentSpreadRadius * config.EllipseY);
                    
                    // Random droplet size using configured min/max range
                    int dropletSizeRange = config.MaxDropletSize - config.MinDropletSize + 1;
                    int dropletSize = config.MinDropletSize + (int)(particleSeed % (uint)dropletSizeRange);
                    
                    // Position droplet - animates from center outward as progress increases
                    Rectangle dropletRect = new Rectangle(
                        splashX + offsetX - dropletSize / 2,
                        splashY + offsetY - dropletSize / 2,
                        dropletSize,
                        dropletSize
                    );
                    
                    // Random alpha variation per droplet using configured range
                    float alphaRange = config.AlphaVariationMax - config.AlphaVariationMin;
                    float alphaVariation = config.AlphaVariationMin + ((particleSeed % 100) / 100f) * alphaRange;
                    Color dropletColor = baseSplashColor * alphaVariation;
                    
                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(dropletColor),
                        dropletRect,
                        Vector3.UnitZ,
                        layerDepth
                    );
                }
            }
        }
    }
}

