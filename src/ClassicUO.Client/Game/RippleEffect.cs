// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game
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

        public void Draw(UltimaBatcher2D batcher, int x, int y, float layerDepth)
        {
            if (_world.Player == null) return;

            // Calculate viewport offset (same as weather system)
            Point winsize = new Point(
                Client.Game.Scene.Camera.Bounds.Width,
                Client.Game.Scene.Camera.Bounds.Height
            );

            int tileOffX = _world.Player.X;
            int tileOffY = _world.Player.Y;
            int winGameCenterX = (winsize.X >> 1) + (_world.Player.Z << 2);
            int winGameCenterY = (winsize.Y >> 1) + (_world.Player.Z << 2);
            winGameCenterX -= (int)_world.Player.Offset.X;
            winGameCenterY -= (int)(_world.Player.Offset.Y - _world.Player.Offset.Z);

            int viewportOffsetX = (tileOffX - tileOffY) * 22 - winGameCenterX;
            int viewportOffsetY = (tileOffX + tileOffY) * 22 - winGameCenterY;

            // Calculate time delta
            uint currentTick = Time.Ticks;
            uint deltaTicks = currentTick - _lastTick;
            _lastTick = currentTick;

            // Cap delta to prevent large jumps (e.g., when game resumes)
            if (deltaTicks > 7000)
            {
                deltaTicks = 25; // Approximate one frame at 60 FPS
            }

            float deltaTime = deltaTicks / 1000f; // Convert to seconds

            // Update and render each active ripple
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
                int visibleRangeX = winsize.X * 2;
                int visibleRangeY = winsize.Y * 2;
                if (ripple.X < -visibleRangeX || ripple.X > visibleRangeX ||
                    ripple.Y < -visibleRangeY || ripple.Y > visibleRangeY)
                {
                    ripple.Active = false; // Deactivate off-screen ripples
                    continue;
                }

                // Calculate ripple properties
                float progress = ripple.LifeTime;
                float currentRadius = ripple.MaxRadius * progress; // Expand over time
                float alpha = (1.0f - progress) * RIPPLE_ALPHA_MULTIPLIER; // Fade out

                // Draw expanding ripple circle
                Color rippleColor = Color.Lerp(Color.LightBlue, Color.Transparent, progress) * alpha;

                DrawCircle(
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

                    DrawCircle(
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

            // Convert absolute isometric coordinates directly to tile coordinates
            // Forward formula: isoX = (tileX - tileY) * 22, isoY = (tileX + tileY) * 22 - (tileZ * 4)
            // Reverse formula (ignoring Z for ground tile lookup):
            //   tileX = (isoX + isoY) / 44
            //   tileY = (isoY - isoX) / 44
            
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

        private void DrawCircle(UltimaBatcher2D batcher, Vector2 center, float radius, Color color, float layerDepth)
        {
            if (radius <= 0) return;

            const int segments = CIRCLE_SEGMENTS;
            float angleStep = (float)(Math.PI * 2.0 / segments);

            Vector2 prevPoint = center + new Vector2(radius, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep;
                Vector2 currentPoint = center + new Vector2(
                    (float)(Math.Cos(angle) * radius),
                    (float)(Math.Sin(angle) * radius)
                );

                // Draw line segment (creates circle outline)
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

