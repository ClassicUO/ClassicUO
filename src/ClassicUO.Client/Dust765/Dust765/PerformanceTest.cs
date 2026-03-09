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
using System.Diagnostics;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Dust765.Dust765
{
    internal static class PerformanceTest
    {
        private static bool _isRunning = false;
        private static Stopwatch _testStopwatch = new Stopwatch();
        private static List<float> _testResults = new List<float>();
        
        public static bool IsEnabled => ProfileManager.CurrentProfile?.PerformanceOptimizations ?? true;

        public static void RunPerformanceTest()
        {
            if (!IsEnabled || _isRunning)
                return;

            _isRunning = true;
            _testStopwatch.Restart();
            _testResults.Clear();

            GameActions.Print("Starting Performance Test...", 63);
            
            // Test 1: Frustum Culling Performance
            TestFrustumCulling();
            
            // Test 2: Batch Optimization Performance  
            TestBatchOptimization();
            
            // Test 3: LOD System Performance
            TestLODSystem();
            
            // Test 4: Overall Performance
            TestOverallPerformance();

            _testStopwatch.Stop();
            _isRunning = false;

            // Display results
            DisplayTestResults();
        }

        private static void TestFrustumCulling()
        {
            var testObjects = new List<Entity>();
            
            // Create test objects at various distances
            for (int i = 0; i < 1000; i++)
            {
                var mobile = new Mobile();
                mobile.RealScreenPosition = new Point(i * 10, i * 10);
                testObjects.Add(mobile);
            }

            var stopwatch = Stopwatch.StartNew();
            
            // Test without frustum culling
            PerformanceOptimizations.SetFrustumCulling(false);
            int visibleCount1 = 0;
            foreach (var obj in testObjects)
            {
                if (PerformanceOptimizations.IsObjectVisible(obj, Client.Game.Scene.Camera))
                    visibleCount1++;
            }

            // Test with frustum culling
            PerformanceOptimizations.SetFrustumCulling(true);
            int visibleCount2 = 0;
            foreach (var obj in testObjects)
            {
                if (PerformanceOptimizations.IsObjectVisible(obj, Client.Game.Scene.Camera))
                    visibleCount2++;
            }

            stopwatch.Stop();
            
            var optimization = visibleCount1 > 0 ? (float)(visibleCount1 - visibleCount2) / visibleCount1 * 100 : 0;
            _testResults.Add(optimization);
            
            GameActions.Print($"Frustum Culling Test: {optimization:F1}% optimization, {stopwatch.ElapsedMilliseconds}ms", 63);
        }

        private static void TestBatchOptimization()
        {
            var testObjects = new List<Entity>();
            
            // Create test objects of different types
            for (int i = 0; i < 500; i++)
            {
                var mobile = new Mobile();
                mobile.Graphic = (ushort)(i % 10 + 400);
                mobile.Hue = (ushort)(i % 5 + 1);
                testObjects.Add(mobile);
            }

            var stopwatch = Stopwatch.StartNew();
            
            // Test batch optimization
            var grouped = PerformanceOptimizations.GroupObjectsByType(testObjects);
            
            stopwatch.Stop();
            
            var batchCount = grouped.Count;
            var avgGroupSize = testObjects.Count / (float)batchCount;
            
            _testResults.Add(avgGroupSize);
            
            GameActions.Print($"Batch Optimization Test: {batchCount} groups, avg size {avgGroupSize:F1}, {stopwatch.ElapsedMilliseconds}ms", 63);
        }

        private static void TestLODSystem()
        {
            var testObjects = new List<Entity>();
            
            // Create test objects at various distances
            for (int i = 0; i < 1000; i++)
            {
                var mobile = new Mobile();
                mobile.RealScreenPosition = new Point(i * 5, i * 5);
                testObjects.Add(mobile);
            }

            var stopwatch = Stopwatch.StartNew();
            
            // Test LOD system
            int renderedCount = 0;
            foreach (var obj in testObjects)
            {
                if (PerformanceOptimizations.ShouldRenderAtLOD(obj, Client.Game.Scene.Camera))
                    renderedCount++;
            }
            
            stopwatch.Stop();
            
            var optimization = testObjects.Count > 0 ? (float)(testObjects.Count - renderedCount) / testObjects.Count * 100 : 0;
            _testResults.Add(optimization);
            
            GameActions.Print($"LOD System Test: {optimization:F1}% objects culled, {renderedCount} rendered, {stopwatch.ElapsedMilliseconds}ms", 63);
        }

        private static void TestOverallPerformance()
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Simulate a frame update
            PerformanceOptimizations.UpdatePerformanceMetrics();
            PerformanceMonitor.Update();
            
            stopwatch.Stop();
            
            var fps = PerformanceMonitor.GetAverageFPS();
            var ratio = PerformanceMonitor.GetOptimizationRatio();
            
            _testResults.Add(fps);
            
            GameActions.Print($"Overall Performance Test: {fps:F1} FPS, {ratio:P1} objects rendered, {stopwatch.ElapsedMilliseconds}ms", 63);
        }

        private static void DisplayTestResults()
        {
            GameActions.Print("=== PERFORMANCE TEST RESULTS ===", 63);
            GameActions.Print($"Total Test Time: {_testStopwatch.ElapsedMilliseconds}ms", 63);
            GameActions.Print($"Frustum Culling Optimization: {_testResults[0]:F1}%", 63);
            GameActions.Print($"Batch Optimization Efficiency: {_testResults[1]:F1} avg group size", 63);
            GameActions.Print($"LOD System Optimization: {_testResults[2]:F1}% objects culled", 63);
            GameActions.Print($"Overall Performance: {_testResults[3]:F1} FPS", 63);
            GameActions.Print("================================", 63);
        }

        public static void TogglePerformanceStats()
        {
            if (ProfileManager.CurrentProfile != null)
            {
                ProfileManager.CurrentProfile.PerformanceShowStats = !ProfileManager.CurrentProfile.PerformanceShowStats;
                GameActions.Print($"Performance Stats: {(ProfileManager.CurrentProfile.PerformanceShowStats ? "ON" : "OFF")}", 63);
            }
        }

        public static void SetQualityLevel(int level)
        {
            if (ProfileManager.CurrentProfile != null && level >= 0 && level <= 3)
            {
                ProfileManager.CurrentProfile.PerformanceQualityLevel = level;
                var levelName = level switch
                {
                    0 => "Low",
                    1 => "Medium",
                    2 => "High", 
                    3 => "Ultra",
                    _ => "Custom"
                };
                GameActions.Print($"Performance Quality Level set to: {levelName}", 63);
            }
        }
    }
}
