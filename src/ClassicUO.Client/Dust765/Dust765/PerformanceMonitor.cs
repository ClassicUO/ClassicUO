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
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765.Dust765
{
    internal static class PerformanceMonitor
    {
        private static readonly List<float> _frameTimes = new List<float>();
        private static readonly List<int> _objectCounts = new List<int>();
        private static readonly List<int> _renderedObjects = new List<int>();
        
        private static uint _lastUpdate = 0;
        private static int _frameCount = 0;
        private static float _averageFPS = 0;
        private static int _totalObjects = 0;
        private static int _renderedObjectsCount = 0;
        
        public static bool IsEnabled => ProfileManager.CurrentProfile?.PerformanceOptimizations ?? true;
        public static bool ShowStats => ProfileManager.CurrentProfile?.PerformanceShowStats ?? false;

        public static void Update()
        {
            if (!IsEnabled)
                return;

            _frameCount++;
            
            // Update every second
            if (Time.Ticks - _lastUpdate >= 1000)
            {
                _averageFPS = _frameCount;
                _frameCount = 0;
                _lastUpdate = Time.Ticks;
                
                // Keep only last 10 seconds of data
                if (_frameTimes.Count > 10)
                    _frameTimes.RemoveAt(0);
                if (_objectCounts.Count > 10)
                    _objectCounts.RemoveAt(0);
                if (_renderedObjects.Count > 10)
                    _renderedObjects.RemoveAt(0);
                
                _frameTimes.Add(_averageFPS);
                _objectCounts.Add(_totalObjects);
                _renderedObjects.Add(_renderedObjectsCount);
            }
        }

        public static void RecordObjectCount(int total, int rendered)
        {
            _totalObjects = total;
            _renderedObjectsCount = rendered;
        }

        public static void Draw(UltimaBatcher2D batcher)
        {
            if (!ShowStats || !IsEnabled)
                return;

            // Draw performance stats in top-left corner
            var stats = new List<string>
            {
                $"FPS: {_averageFPS:F1}",
                $"Objects: {_renderedObjectsCount}/{_totalObjects}",
                $"Optimization: {GetOptimizationStatus()}",
                $"Quality: {GetQualityLevelName()}"
            };

            int y = 10;
            foreach (var stat in stats)
            {
                var text = RenderedText.Create(stat, 0x0481, style: FontStyle.BlackBorder);
                text.Draw(batcher, 10, y);
                y += 20;
            }
        }

        private static string GetOptimizationStatus()
        {
            var optimizations = new List<string>();
            
            if (PerformanceOptimizations.IsFrustumCullingEnabled)
                optimizations.Add("FC");
            if (PerformanceOptimizations.IsBatchOptimizationEnabled)
                optimizations.Add("BO");
            if (PerformanceOptimizations.IsLODSystemEnabled)
                optimizations.Add("LOD");
            if (PerformanceOptimizations.IsTextureStreamingEnabled)
                optimizations.Add("TS");
            if (PerformanceOptimizations.IsOcclusionCullingEnabled)
                optimizations.Add("OC");

            return optimizations.Count > 0 ? string.Join(",", optimizations) : "None";
        }

        private static string GetQualityLevelName()
        {
            return PerformanceOptimizations.QualityLevel switch
            {
                0 => "Low",
                1 => "Medium", 
                2 => "High",
                3 => "Ultra",
                _ => "Custom"
            };
        }

        public static float GetAverageFPS()
        {
            return _averageFPS;
        }

        public static float GetOptimizationRatio()
        {
            if (_totalObjects == 0)
                return 0;
            
            return (float)_renderedObjectsCount / _totalObjects;
        }

        public static void Reset()
        {
            _frameTimes.Clear();
            _objectCounts.Clear();
            _renderedObjects.Clear();
            _frameCount = 0;
            _averageFPS = 0;
            _totalObjects = 0;
            _renderedObjectsCount = 0;
        }
    }
}
