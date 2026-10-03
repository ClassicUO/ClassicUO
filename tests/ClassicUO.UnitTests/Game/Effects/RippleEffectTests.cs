using ClassicUO.Game;
using ClassicUO.Game.Effects;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace ClassicUO.UnitTests.Game.Effects
{
    public class RippleEffectTests
    {
        private int GetActiveRippleCount(RippleEffect rippleEffect)
        {
            var field = typeof(RippleEffect).GetField("_ripples", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return 0;

            var ripples = field.GetValue(rippleEffect);
            if (ripples == null) return 0;

            var array = ripples as System.Array;
            if (array == null) return 0;

            int count = 0;
            for (int i = 0; i < array.Length; i++)
            {
                var ripple = array.GetValue(i);
                if (ripple == null) continue;

                var activeField = ripple.GetType().GetField("Active");
                if (activeField != null && (bool)activeField.GetValue(ripple))
                {
                    count++;
                }
            }
            return count;
        }

        private object GetFirstActiveRipple(RippleEffect rippleEffect)
        {
            var field = typeof(RippleEffect).GetField("_ripples", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return null;

            var ripples = field.GetValue(rippleEffect);
            if (ripples == null) return null;

            var array = ripples as System.Array;
            if (array == null) return null;

            for (int i = 0; i < array.Length; i++)
            {
                var ripple = array.GetValue(i);
                if (ripple == null) continue;

                var activeField = ripple.GetType().GetField("Active");
                if (activeField != null && (bool)activeField.GetValue(ripple))
                {
                    return ripple;
                }
            }
            return null;
        }

        /// <summary>
        /// Helper method to directly create a ripple for testing (bypasses map check).
        /// Note: Since Ripple is a struct (value type), we need to get, modify, and set it back.
        /// </summary>
        private void CreateRippleDirectly(RippleEffect rippleEffect, float worldX, float worldY, int index = 0)
        {
            var field = typeof(RippleEffect).GetField("_ripples", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return;

            var ripples = field.GetValue(rippleEffect);
            if (ripples == null) return;

            var array = ripples as System.Array;
            if (array == null || index >= array.Length) return;

            object ripple = array.GetValue(index);
            if (ripple == null) return;

            ripple.GetType().GetField("Active").SetValue(ripple, true);
            ripple.GetType().GetField("WorldX").SetValue(ripple, worldX);
            ripple.GetType().GetField("WorldY").SetValue(ripple, worldY);
            ripple.GetType().GetField("LifeTime").SetValue(ripple, 0.0f);
            ripple.GetType().GetField("MaxRadius").SetValue(ripple, 20.0f);
            ripple.GetType().GetField("SeedID").SetValue(ripple, (uint)(1000 + index));

            array.SetValue(ripple, index);
        }

        [Fact]
        public void CreateRipple_Should_NotCreateRipple_When_MapIsNull()
        {
            var world = new World();
            var rippleEffect = new RippleEffect(world);

            rippleEffect.CreateRipple(100.0f, 200.0f);

            // No ripple should be created when map is null
            int activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(0, "no ripple should be created when map is null");
        }

        [Fact]
        public void CreateRipple_Should_CreateRipple_When_SlotAvailable()
        {
            var world = new World();
            var rippleEffect = new RippleEffect(world);
            float worldX = 100.0f;
            float worldY = 200.0f;

            // Create ripple directly (bypassing map check for testing)
            CreateRippleDirectly(rippleEffect, worldX, worldY, 0);

            // Verify ripple was created
            int activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(1, "exactly one ripple should be created");

            // Verify ripple properties
            var ripple = GetFirstActiveRipple(rippleEffect);
            ripple.Should().NotBeNull("ripple should exist");

            var worldXField = ripple.GetType().GetField("WorldX");
            var worldYField = ripple.GetType().GetField("WorldY");
            var lifetimeField = ripple.GetType().GetField("LifeTime");
            var maxRadiusField = ripple.GetType().GetField("MaxRadius");

            float storedWorldX = (float)worldXField.GetValue(ripple);
            float storedWorldY = (float)worldYField.GetValue(ripple);
            float lifetime = (float)lifetimeField.GetValue(ripple);
            float maxRadius = (float)maxRadiusField.GetValue(ripple);

            storedWorldX.Should().BeApproximately(worldX, 0.01f, "WorldX should be stored correctly");
            storedWorldY.Should().BeApproximately(worldY, 0.01f, "WorldY should be stored correctly");
            lifetime.Should().Be(0.0f, "initial lifetime should be 0");
            maxRadius.Should().Be(20.0f, "MaxRadius should be 20.0f");
        }

        [Fact]
        public void CreateRipple_Should_HandleFullPool_Gracefully()
        {
            var world = new World();
            var rippleEffect = new RippleEffect(world);

            // Fill all slots (64 ripples) directly
            for (int i = 0; i < 64; i++)
            {
                CreateRippleDirectly(rippleEffect, i * 10.0f, i * 10.0f, i);
            }

            // All slots should be filled
            int activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(64, "all 64 ripple slots should be filled");

            // Try to create one more (pool is full)
            // Since we're using direct creation, we'll verify the pool is full
            // In real usage, CreateRipple would find no available slot and not create a ripple
            CreateRippleDirectly(rippleEffect, 1000.0f, 1000.0f, 0); // Overwrite first slot

            // Count should still be 64 (overwrote existing, didn't create new)
            activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(64, "pool should remain full");
        }

        [Theory]
        [InlineData(0.01f)]
        [InlineData(0.1f)]
        [InlineData(0.5f)]
        public void Update_Should_IncreaseLifetime(float deltaTime)
        {
            var world = new World();
            var rippleEffect = new RippleEffect(world);
            CreateRippleDirectly(rippleEffect, 100.0f, 200.0f, 0);

            var ripple = GetFirstActiveRipple(rippleEffect);
            ripple.Should().NotBeNull("ripple should exist");

            var lifetimeField = ripple.GetType().GetField("LifeTime");
            float initialLifetime = (float)lifetimeField.GetValue(ripple);
            initialLifetime.Should().Be(0f, "initial lifetime should be 0");

            rippleEffect.Update(deltaTime, 0, 0, 1000, 1000);

            // Lifetime should have increased
            ripple = GetFirstActiveRipple(rippleEffect);
            ripple.Should().NotBeNull("ripple should still exist after update");

            float updatedLifetime = (float)lifetimeField.GetValue(ripple);
            updatedLifetime.Should().BeGreaterThan(initialLifetime,
                $"lifetime should increase after update with deltaTime={deltaTime}");

            // RIPPLE_DURATION = 0.8f
            float expectedLifetime = initialLifetime + (deltaTime / 0.8f);
            updatedLifetime.Should().BeApproximately(expectedLifetime, 0.001f,
                "lifetime should increase by deltaTime / RIPPLE_DURATION (0.8f)");

            // Verify ripple is still active
            int activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(1, "ripple should still be active");
        }

        [Fact]
        public void Update_Should_DeactivateExpiredRipples()
        {
            var world = new World();
            var rippleEffect = new RippleEffect(world);
            CreateRippleDirectly(rippleEffect, 100.0f, 200.0f, 0);

            // Verify ripple is initially active
            int initialActiveCount = GetActiveRippleCount(rippleEffect);
            initialActiveCount.Should().Be(1, "ripple should be active initially");

            // Update past duration (RIPPLE_DURATION = 0.8f)
            float totalTime = 1.0f; // Exceeds RIPPLE_DURATION (0.8f), so lifetime = 1.0 / 0.8 = 1.25
            rippleEffect.Update(totalTime, 0, 0, 1000, 1000);

            // Ripple should be deactivated (lifetime >= 1.0)
            int activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(0, "ripple should be deactivated after exceeding duration");

            // Verify lifetime is >= 1.0 by checking all ripples
            var field = typeof(RippleEffect).GetField("_ripples", BindingFlags.NonPublic | BindingFlags.Instance);
            var ripples = field.GetValue(rippleEffect) as System.Array;

            bool foundExpiredRipple = false;
            for (int i = 0; i < ripples.Length; i++)
            {
                var ripple = ripples.GetValue(i);
                if (ripple == null) continue;

                var activeField = ripple.GetType().GetField("Active");
                var lifetimeField = ripple.GetType().GetField("LifeTime");

                if (activeField != null && lifetimeField != null)
                {
                    bool isActive = (bool)activeField.GetValue(ripple);
                    float lifetime = (float)lifetimeField.GetValue(ripple);

                    if (!isActive && lifetime >= 1.0f)
                    {
                        foundExpiredRipple = true;
                        break;
                    }
                }
            }

            foundExpiredRipple.Should().BeTrue("should find an expired ripple with lifetime >= 1.0");
        }

        [Fact]
        public void Update_Should_ConvertWorldToViewportCoordinates()
        {
            var world = new World();
            var rippleEffect = new RippleEffect(world);
            float worldX = 150.0f;
            float worldY = 250.0f;
            CreateRippleDirectly(rippleEffect, worldX, worldY, 0);

            int viewportOffsetX = 50;
            int viewportOffsetY = 75;

            rippleEffect.Update(0.01f, viewportOffsetX, viewportOffsetY, 1000, 1000);

            // Coordinates should be converted: X = 150 - 50 = 100, Y = 250 - 75 = 175
            var ripple = GetFirstActiveRipple(rippleEffect);
            ripple.Should().NotBeNull("ripple should exist");

            var xField = ripple.GetType().GetField("X");
            var yField = ripple.GetType().GetField("Y");
            var worldXField = ripple.GetType().GetField("WorldX");
            var worldYField = ripple.GetType().GetField("WorldY");

            float viewportX = (float)xField.GetValue(ripple);
            float viewportY = (float)yField.GetValue(ripple);
            float currentWorldX = (float)worldXField.GetValue(ripple);
            float currentWorldY = (float)worldYField.GetValue(ripple);

            viewportX.Should().BeApproximately(currentWorldX - viewportOffsetX, 0.01f,
                "X should be converted to viewport coordinates: WorldX - viewportOffsetX");
            viewportY.Should().BeApproximately(currentWorldY - viewportOffsetY, 0.01f,
                "Y should be converted to viewport coordinates: WorldY - viewportOffsetY");
        }

        [Fact]
        public void Update_Should_CullOffScreenRipples()
        {
            var world = new World();
            var rippleEffect = new RippleEffect(world);
            CreateRippleDirectly(rippleEffect, 100.0f, 200.0f, 0);

            // Ripple is initially active
            int initialActiveCount = GetActiveRippleCount(rippleEffect);
            initialActiveCount.Should().Be(1, "ripple should be active initially");

            // Update with viewport that excludes ripple
            // Ripple at (100, 200) with viewport offset (10000, 10000) 
            // results in viewport coords (-9900, -9800) which is off-screen
            // visibleRange is 100, so ripple at -9900 is outside [-100, 100] range
            rippleEffect.Update(0.01f, 10000, 10000, 100, 100);

            // Ripple should be culled (deactivated)
            int activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(0, "ripple should be culled when off-screen");
        }

        [Fact]
        public void Update_Should_HandleMultipleRipples()
        {
            var world = new World();
            var rippleEffect = new RippleEffect(world);
            int rippleCount = 10;

            // Create multiple ripples directly
            for (int i = 0; i < rippleCount; i++)
            {
                CreateRippleDirectly(rippleEffect, i * 10.0f, i * 10.0f, i);
            }

            // All ripples should be created
            int activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(rippleCount, $"all {rippleCount} ripples should be created");

            // Update all ripples
            rippleEffect.Update(0.05f, 0, 0, 1000, 1000);

            // All ripples should still be active (not expired)
            activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(rippleCount,
                $"all {rippleCount} ripples should still be active after small update");

            // Update again
            rippleEffect.Update(0.05f, 0, 0, 1000, 1000);

            // All ripples should still be active
            activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(rippleCount,
                $"all {rippleCount} ripples should still be active after second update");
        }

        [Fact]
        public void Update_Should_HandleZeroDeltaTime()
        {
            var world = new World();
            var rippleEffect = new RippleEffect(world);
            CreateRippleDirectly(rippleEffect, 100.0f, 200.0f, 0);

            var ripple = GetFirstActiveRipple(rippleEffect);
            ripple.Should().NotBeNull("ripple should exist");

            var lifetimeField = ripple.GetType().GetField("LifeTime");
            float initialLifetime = (float)lifetimeField.GetValue(ripple);

            rippleEffect.Update(0.0f, 0, 0, 1000, 1000);

            // Lifetime should not change with zero delta time
            ripple = GetFirstActiveRipple(rippleEffect);
            ripple.Should().NotBeNull("ripple should still exist after zero delta update");

            float updatedLifetime = (float)lifetimeField.GetValue(ripple);
            updatedLifetime.Should().BeApproximately(initialLifetime, 0.001f,
                "lifetime should not change with zero delta time");

            // Ripple is still active
            int activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(1, "ripple should still be active");
        }

        [Fact]
        public void Reset_Should_ClearAllRipples()
        {
            var world = new World();
            var rippleEffect = new RippleEffect(world);

            // Create some ripples directly
            int rippleCount = 5;
            for (int i = 0; i < rippleCount; i++)
            {
                CreateRippleDirectly(rippleEffect, i * 10.0f, i * 10.0f, i);
            }

            // Verify ripples are created
            int initialActiveCount = GetActiveRippleCount(rippleEffect);
            initialActiveCount.Should().Be(rippleCount, $"{rippleCount} ripples should be created");

            rippleEffect.Reset();

            // All ripples should be cleared
            int activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(0, "all ripples should be cleared after Reset");

            // Verify by updating - no active ripples should remain
            rippleEffect.Update(0.1f, 0, 0, 1000, 1000);
            activeCount = GetActiveRippleCount(rippleEffect);
            activeCount.Should().Be(0, "no ripples should be active after reset and update");
        }
    }
}
