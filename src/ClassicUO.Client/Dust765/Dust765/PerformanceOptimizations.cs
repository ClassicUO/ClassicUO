#region license

// Copyright (C) 2020 project dust765
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
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765.Dust765
{
    internal static class PerformanceOptimizations
    {
        private static bool _frustumCullingEnabled = true;
        private static bool _batchOptimizationEnabled = true;
        private static bool _lodSystemEnabled = true;
        private static bool _textureStreamingEnabled = true;
        private static bool _occlusionCullingEnabled = false;
        
        private static readonly Dictionary<uint, bool> _visibilityCache = new Dictionary<uint, bool>();
        private static readonly Dictionary<uint, int> _lodLevelCache = new Dictionary<uint, int>();
        private static uint _lastFrameUpdate = 0;
        
        public static bool IsEnabled => ProfileManager.CurrentProfile?.PerformanceOptimizations ?? true;
        
        public static bool IsFrustumCullingEnabled => ProfileManager.CurrentProfile?.PerformanceFrustumCulling ?? true;
        public static bool IsBatchOptimizationEnabled => ProfileManager.CurrentProfile?.PerformanceBatchOptimization ?? true;
        public static bool IsLODSystemEnabled => ProfileManager.CurrentProfile?.PerformanceLODSystem ?? true;
        public static bool IsTextureStreamingEnabled => ProfileManager.CurrentProfile?.PerformanceTextureStreaming ?? true;
        public static bool IsOcclusionCullingEnabled => ProfileManager.CurrentProfile?.PerformanceOcclusionCulling ?? false;
        public static int QualityLevel => ProfileManager.CurrentProfile?.PerformanceQualityLevel ?? 2;

        // FRUSTUM CULLING
        public static bool IsObjectVisible(Entity obj, Camera camera)
        {
            if (!IsFrustumCullingEnabled || !IsEnabled)
                return true;

            // Cache visibility for this frame
            if (_lastFrameUpdate != Time.Ticks)
            {
                _visibilityCache.Clear();
                _lastFrameUpdate = Time.Ticks;
            }

            if (_visibilityCache.TryGetValue(obj.Serial, out bool cached))
                return cached;

            var screenPos = camera.WorldToScreen(obj.RealScreenPosition);
            var screenBounds = GetScreenBounds();
            
            bool isVisible = screenPos.X >= -100 && screenPos.X <= screenBounds.Width + 100 &&
                           screenPos.Y >= -100 && screenPos.Y <= screenBounds.Height + 100;

            // Check for occlusion if enabled
            if (isVisible && IsOcclusionCullingEnabled)
            {
                isVisible = !IsObjectOccluded(obj, camera);
            }

            _visibilityCache[obj.Serial] = isVisible;
            return isVisible;
        }

        // BATCH OPTIMIZATION
        public static Dictionary<string, List<Entity>> GroupObjectsByType(IEnumerable<Entity> objects)
        {
            if (!IsBatchOptimizationEnabled || !IsEnabled)
                return new Dictionary<string, List<Entity>> { { "default", objects.ToList() } };

            var grouped = new Dictionary<string, List<Entity>>();
            
            foreach (var obj in objects)
            {
                string key = GetObjectBatchKey(obj);
                if (!grouped.ContainsKey(key))
                    grouped[key] = new List<Entity>();
                grouped[key].Add(obj);
            }

            return grouped;
        }

        private static string GetObjectBatchKey(Entity obj)
        {
            return obj switch
            {
                Mobile mobile => $"mobile_{mobile.Graphic}_{mobile.Hue}",
                Item item => $"item_{item.Graphic}_{item.Hue}",
                _ => $"unknown_{obj.GetType().Name}"
            };
        }

        // LOD SYSTEM
        public static int GetLODLevel(Entity obj, Camera camera)
        {
            if (!IsLODSystemEnabled || !IsEnabled)
                return 0;

            if (_lodLevelCache.TryGetValue(obj.Serial, out int cached))
                return cached;

            float distance = Vector2.Distance(new Vector2(obj.RealScreenPosition.X, obj.RealScreenPosition.Y), camera.Offset);
            int lodLevel = distance switch
            {
                < 50 => 0,    // High detail
                < 100 => 1,   // Medium detail
                < 200 => 2,   // Low detail
                _ => 3        // Very low detail
            };

            _lodLevelCache[obj.Serial] = lodLevel;
            return lodLevel;
        }

        public static bool ShouldRenderAtLOD(Entity obj, Camera camera)
        {
            int lodLevel = GetLODLevel(obj, camera);
            
            return obj switch
            {
                Mobile mobile => lodLevel < 3, // Always render mobiles unless very far
                Item item => lodLevel < 2,     // Items disappear at medium distance
                _ => lodLevel < 2
            };
        }

        // TEXTURE STREAMING
        public static void OptimizeTextureLoading()
        {
            if (!IsTextureStreamingEnabled || !IsEnabled)
                return;

            // Clean up unused textures from memory
            var unusedTextures = GetUnusedTextures();
            foreach (var texture in unusedTextures)
            {
                texture?.Dispose();
            }
        }

        private static List<Texture2D> GetUnusedTextures()
        {
            // This would need to be implemented based on your texture management system
            // For now, return empty list
            return new List<Texture2D>();
        }

        // OCCLUSION CULLING
        public static bool IsObjectOccluded(Entity obj, Camera camera)
        {
            if (!IsOcclusionCullingEnabled || !IsEnabled)
                return false;

            // Simple occlusion test - check if object is behind other objects
            // This is a basic implementation - can be enhanced with more sophisticated algorithms
            var objPos = new Vector2(obj.RealScreenPosition.X, obj.RealScreenPosition.Y);
            var cameraPos = camera.Offset;
            
            // Check if there are objects between camera and target
            var direction = Vector2.Normalize(objPos - cameraPos);
            var distance = Vector2.Distance(objPos, cameraPos);
            
            // Sample points along the line to check for occlusion
            for (int i = 1; i < 5; i++)
            {
                var samplePos = cameraPos + direction * (distance * i / 5);
                // Check if there's a solid object at this position
                // This would need to be implemented based on your world collision system
            }
            
            return false; // For now, assume no occlusion
        }

        // PERFORMANCE MONITORING
        public static void UpdatePerformanceMetrics()
        {
            if (!IsEnabled)
                return;

            // Update performance counters
            var frameTime = Time.Ticks - _lastFrameUpdate;
            
            // Adjust optimization levels based on performance and quality level
            switch (QualityLevel)
            {
                case 0: // Low Quality
                    _frustumCullingEnabled = true;
                    _batchOptimizationEnabled = true;
                    _lodSystemEnabled = true;
                    _textureStreamingEnabled = true;
                    _occlusionCullingEnabled = false;
                    break;
                case 1: // Medium Quality
                    _frustumCullingEnabled = true;
                    _batchOptimizationEnabled = true;
                    _lodSystemEnabled = true;
                    _textureStreamingEnabled = true;
                    _occlusionCullingEnabled = false;
                    break;
                case 2: // High Quality
                    _frustumCullingEnabled = IsFrustumCullingEnabled;
                    _batchOptimizationEnabled = IsBatchOptimizationEnabled;
                    _lodSystemEnabled = IsLODSystemEnabled;
                    _textureStreamingEnabled = IsTextureStreamingEnabled;
                    _occlusionCullingEnabled = false;
                    break;
                case 3: // Ultra Quality
                    _frustumCullingEnabled = IsFrustumCullingEnabled;
                    _batchOptimizationEnabled = IsBatchOptimizationEnabled;
                    _lodSystemEnabled = IsLODSystemEnabled;
                    _textureStreamingEnabled = IsTextureStreamingEnabled;
                    _occlusionCullingEnabled = IsOcclusionCullingEnabled;
                    break;
            }
            
            // Dynamic adjustment based on frame time
            if (frameTime > 20) // More than 50 FPS target
            {
                // Reduce quality for better performance
                _frustumCullingEnabled = true;
                _batchOptimizationEnabled = true;
                _lodSystemEnabled = true;
            }
        }

        // UTILITY METHODS
        private static Rectangle GetScreenBounds()
        {
            return new Rectangle(0, 0, Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height);
        }

        // CLEANUP
        public static void ClearCaches()
        {
            _visibilityCache.Clear();
            _lodLevelCache.Clear();
        }

        // CONFIGURATION
        public static void SetFrustumCulling(bool enabled) => _frustumCullingEnabled = enabled;
        public static void SetBatchOptimization(bool enabled) => _batchOptimizationEnabled = enabled;
        public static void SetLODSystem(bool enabled) => _lodSystemEnabled = enabled;
        public static void SetTextureStreaming(bool enabled) => _textureStreamingEnabled = enabled;
    }
}
