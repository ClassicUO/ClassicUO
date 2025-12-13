// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.Effects
{
    /// <summary>
    /// Ripple effect system for water tiles.
    /// </summary>
    internal sealed class RippleEffect
    {
        // Configuration constants
        private const int MAX_RIPPLES = 64;
        private const float RIPPLE_DURATION = 0.8f;
        private const int CIRCLE_SEGMENTS = 32;
        private const float RIPPLE_MAX_RADIUS = 20f;
        private const float RIPPLE_ALPHA_MULTIPLIER = 0.7f;
        private const float RIPPLE_RING_SPACING = 0.3f;
        private const float ISOMETRIC_VERTICAL_SCALE = 0.5f;

        private readonly Ripple[] _ripples = new Ripple[MAX_RIPPLES];
        private readonly World _world;
        private uint _lastTick;

        public RippleEffect(World world)
        {
            _world = world;
            _lastTick = Time.Ticks;
        }

        public void CreateRipple(float worldX, float worldY)
        {
            if (_world.Map == null)
            {
                return;
            }

            if (!IsWaterTileAtPosition(worldX, worldY))
            {
                return; 
            }

            // Find inactive ripple slot
            for (int i = 0; i < _ripples.Length; i++)
            {
                ref Ripple ripple = ref _ripples[i];
                if (!ripple.Active)
                {
                    ripple.Active = true;
                    ripple.WorldX = worldX;
                    ripple.WorldY = worldY;
                    ripple.LifeTime = 0.0f;
                    ripple.MaxRadius = RIPPLE_MAX_RADIUS;
                    ripple.SeedID = (uint)(Time.Ticks + i);

                    break;
                }
            }
        }

        /// <summary>
        /// Updates all active ripple particles.
        /// </summary>
        /// <param name="deltaTime">Time since last update in seconds</param>
        /// <param name="viewportOffsetX">Viewport offset X</param>
        /// <param name="viewportOffsetY">Viewport offset Y</param>
        /// <param name="visibleRangeX">Visible range X for culling</param>
        /// <param name="visibleRangeY">Visible range Y for culling</param>
        public void Update(float deltaTime, int viewportOffsetX, int viewportOffsetY,
                          int visibleRangeX, int visibleRangeY)
        {
            // Update and cull each active ripple
            for (int i = 0; i < _ripples.Length; i++)
            {
                ref Ripple ripple = ref _ripples[i];
                if (!ripple.Active) continue;

                // Update lifetime
                ripple.LifeTime += deltaTime / RIPPLE_DURATION;

                // Deactivate if animation complete
                if (ripple.LifeTime >= 1.0f)
                {
                    ripple.Active = false;
                    continue;
                }

                // Convert to viewport-relative coordinates
                ripple.X = ripple.WorldX - viewportOffsetX;
                ripple.Y = ripple.WorldY - viewportOffsetY;

                // Check visibility (off-screen ripples can be skipped for performance)
                if (ripple.X < -visibleRangeX || ripple.X > visibleRangeX ||
                    ripple.Y < -visibleRangeY || ripple.Y > visibleRangeY)
                {
                    ripple.Active = false; // Deactivate off-screen ripples
                    continue;
                }
            }
        }

        /// <summary>
        /// Renders all active ripple particles.
        /// </summary>
        /// <param name="batcher">Renderer to draw with</param>
        /// <param name="layerDepth">Layer depth for rendering</param>
        public void Draw(UltimaBatcher2D batcher, float layerDepth)
        {
            // Render each active ripple
            for (int i = 0; i < _ripples.Length; i++)
            {
                ref Ripple ripple = ref _ripples[i];
                if (!ripple.Active) continue;

                // Calculate ripple properties
                float progress = ripple.LifeTime;
                float currentRadius = ripple.MaxRadius * progress; // Expand over time
                float alpha = (1.0f - progress) * RIPPLE_ALPHA_MULTIPLIER; // Fade out

                Color rippleColor = Color.Lerp(Color.LightBlue, Color.Transparent, progress) * alpha;

                DrawEllipse(
                    batcher,
                    new Vector2(ripple.X, ripple.Y),
                    currentRadius,
                    rippleColor,
                    layerDepth
                );

                // Optional: Draw 2nd concentric ring for depth (if progress > 0.3)
                if (progress > RIPPLE_RING_SPACING)
                {
                    float ring2Radius = ripple.MaxRadius * (progress - RIPPLE_RING_SPACING);
                    float ring2Progress = (progress - RIPPLE_RING_SPACING) / (1.0f - RIPPLE_RING_SPACING);
                    float ring2Alpha = (1.0f - ring2Progress) * 0.5f;
                    Color ring2Color = Color.LightBlue * ring2Alpha;

                    DrawEllipse(
                        batcher,
                        new Vector2(ripple.X, ripple.Y),
                        ring2Radius,
                        ring2Color,
                        layerDepth
                    );
                }
            }
        }

        public void Reset()
        {
            for (int i = 0; i < _ripples.Length; i++)
            {
                _ripples[i].Active = false;
            }
        }

        /// <summary>
        /// Checks if the given absolute isometric position is on a water tile.
        /// Converts directly from absolute isometric coordinates to tile coordinates.
        /// </summary>
        /// <param name="worldX">Absolute isometric X coordinate.</param>
        /// <param name="worldY">Absolute isometric Y coordinate.</param>
        /// <returns>True if the position is on a water tile, false otherwise.</returns>
        private bool IsWaterTileAtPosition(float worldX, float worldY)
        {
            if (_world.Map == null)
            {
                return false;
            }

            
            int targetTileX = (int)Math.Round((worldX + worldY) / 44f);
            int targetTileY = (int)Math.Round((worldY - worldX) / 44f);

            // Check if tile is water
            GameObject tileObj = _world.Map.GetTile(targetTileX, targetTileY, load: false);
            if (tileObj is Land land)
            {
                return land.TileData.IsWet &&
                       (land.TileData.Name?.ToLower().Contains("water") == true);
            }

            return false;
        }

        /// <summary>
        /// Draws an ellipse that appears as a circle in isometric projection.
        /// The vertical axis is compressed to match the isometric view.
        /// </summary>
        /// <param name="batcher">Renderer to draw with</param>
        /// <param name="center">Center of the ellipse</param>
        /// <param name="radius">Horizontal radius (appears as circle radius in isometric view)</param>
        /// <param name="color">Color of the ellipse</param>
        /// <param name="layerDepth">Layer depth for rendering</param>
        private void DrawEllipse(UltimaBatcher2D batcher, Vector2 center, float radius, Color color, float layerDepth)
        {
            if (radius <= 0) return;

            const int segments = CIRCLE_SEGMENTS;
            float angleStep = (float)(Math.PI * 2.0 / segments);

            float radiusX = radius;
            float radiusY = radius * ISOMETRIC_VERTICAL_SCALE;

            Vector2 prevPoint = center + new Vector2(radiusX, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep;
                Vector2 currentPoint = center + new Vector2(
                    (float)(Math.Cos(angle) * radiusX),
                    (float)(Math.Sin(angle) * radiusY)
                );

                batcher.DrawLine(
                    SolidColorTextureCache.GetTexture(color),
                    prevPoint,
                    currentPoint,
                    Vector3.UnitZ,
                    1, // Line thickness: 1 pixel
                    layerDepth
                );

                prevPoint = currentPoint;
            }
        }

        private struct Ripple
        {
            public float WorldX, WorldY;      // Absolute isometric coordinates
            public float X, Y;                // Viewport-relative coordinates (cached)
            public float LifeTime;            // 0.0 to 1.0 (animation progress)
            public float MaxRadius;           // Maximum expansion radius
            public bool Active;               // Is this ripple currently active
            public uint SeedID;               // Random seed for consistent pattern
        }
    }
}

