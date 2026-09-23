using ClassicUO.Game.Effects;
using FluentAssertions;
using Microsoft.Xna.Framework;
using System.Reflection;
using Xunit;

namespace ClassicUO.UnitTests.Game.Effects
{
    public class SplashEffectTests
    {
        /// <summary>
        /// Helper method to get the active particle count using reflection.
        /// </summary>
        private int GetActiveParticleCount(SplashEffect splashEffect)
        {
            var field = typeof(SplashEffect).GetField("_particles", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return 0;

            var particles = field.GetValue(splashEffect);
            if (particles == null) return 0;

            var array = particles as System.Array;
            if (array == null) return 0;

            int count = 0;
            for (int i = 0; i < array.Length; i++)
            {
                var particle = array.GetValue(i);
                if (particle == null) continue;

                var activeField = particle.GetType().GetField("Active");
                if (activeField != null && (bool)activeField.GetValue(particle))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Helper method to get particle data using reflection.
        /// </summary>
        private object GetParticle(SplashEffect splashEffect, int index)
        {
            var field = typeof(SplashEffect).GetField("_particles", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return null;

            var particles = field.GetValue(splashEffect);
            if (particles == null) return null;

            var array = particles as System.Array;
            return array?.GetValue(index);
        }

        /// <summary>
        /// Helper method to get the first active particle.
        /// </summary>
        private object GetFirstActiveParticle(SplashEffect splashEffect)
        {
            var field = typeof(SplashEffect).GetField("_particles", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return null;

            var particles = field.GetValue(splashEffect);
            if (particles == null) return null;

            var array = particles as System.Array;
            if (array == null) return null;

            for (int i = 0; i < array.Length; i++)
            {
                var particle = array.GetValue(i);
                if (particle == null) continue;

                var activeField = particle.GetType().GetField("Active");
                if (activeField != null && (bool)activeField.GetValue(particle))
                {
                    return particle;
                }
            }
            return null;
        }

        [Fact]
        public void CreateSplash_Should_CreateParticle_When_SlotAvailable()
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();

            splashEffect.CreateSplash(100.0f, 200.0f, config);

            int activeCount = GetActiveParticleCount(splashEffect);
            activeCount.Should().Be(1, "exactly one particle should be created");
        }

        [Fact]
        public void CreateSplash_Should_HandleFullPool_Gracefully()
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();

            for (int i = 0; i < 255; i++)
            {
                splashEffect.CreateSplash(i * 10.0f, i * 10.0f, config);
            }

            int activeCount = GetActiveParticleCount(splashEffect);
            activeCount.Should().Be(255, "all 255 particle slots should be filled");

            splashEffect.CreateSplash(1000.0f, 1000.0f, config);
            
            activeCount = GetActiveParticleCount(splashEffect);
            activeCount.Should().Be(255, "pool should remain full when attempting to create beyond capacity");
        }

        [Fact]
        public void CreateSplash_Should_StoreWorldCoordinates_When_UseWorldCoordinatesIsTrue()
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();
            config.UseWorldCoordinates = true;
            config.RiseSpeed = 0.0f;
            float worldX = 150.0f;
            float worldY = 250.0f;

            splashEffect.CreateSplash(worldX, worldY, config);

            var particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should exist");

            var worldXField = particle.GetType().GetField("WorldX");
            var worldYField = particle.GetType().GetField("WorldY");
            
            float storedWorldX = (float)worldXField.GetValue(particle);
            float storedWorldY = (float)worldYField.GetValue(particle);
            
            storedWorldX.Should().BeApproximately(worldX, 0.01f, "WorldX should be stored correctly");
            storedWorldY.Should().BeApproximately(worldY, 0.01f, "WorldY should be stored correctly");

            int viewportOffsetX = 50;
            int viewportOffsetY = 75;
            splashEffect.Update(0.01f, viewportOffsetX, viewportOffsetY, 1000, 1000);
            
            particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should still exist after update");
            
            var xField = particle.GetType().GetField("X");
            var yField = particle.GetType().GetField("Y");
            float currentWorldX = (float)worldXField.GetValue(particle);
            float currentWorldY = (float)worldYField.GetValue(particle);
            
            float viewportX = (float)xField.GetValue(particle);
            float viewportY = (float)yField.GetValue(particle);
            
            viewportX.Should().BeApproximately(currentWorldX - viewportOffsetX, 0.01f, 
                "X should be converted to viewport coordinates");
            viewportY.Should().BeApproximately(currentWorldY - viewportOffsetY, 0.01f, 
                "Y should be converted to viewport coordinates");
        }

        [Fact]
        public void CreateSplash_Should_StoreViewportCoordinates_When_UseWorldCoordinatesIsFalse()
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();
            config.UseWorldCoordinates = false;
            float viewportX = 100.0f;
            float viewportY = 200.0f;

            splashEffect.CreateSplash(viewportX, viewportY, config);

            var particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should exist");

            var xField = particle.GetType().GetField("X");
            var yField = particle.GetType().GetField("Y");
            var worldXField = particle.GetType().GetField("WorldX");
            var worldYField = particle.GetType().GetField("WorldY");
            
            float storedX = (float)xField.GetValue(particle);
            float storedY = (float)yField.GetValue(particle);
            float storedWorldX = (float)worldXField.GetValue(particle);
            float storedWorldY = (float)worldYField.GetValue(particle);
            
            storedX.Should().BeApproximately(viewportX, 0.01f, "X should be stored directly");
            storedY.Should().BeApproximately(viewportY, 0.01f, "Y should be stored directly");
            storedWorldX.Should().Be(0f, "WorldX should be 0 in viewport mode");
            storedWorldY.Should().Be(0f, "WorldY should be 0 in viewport mode");

            splashEffect.Update(0.01f, 50, 75, 1000, 1000);
            
            particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should still exist after update");
            
            float updatedX = (float)xField.GetValue(particle);
            float updatedY = (float)yField.GetValue(particle);
            
            updatedX.Should().BeApproximately(viewportX, 0.01f, 
                "X should remain unchanged in viewport mode");
            updatedY.Should().BeApproximately(viewportY, 0.01f, 
                "Y should remain unchanged in viewport mode");
        }

        [Theory]
        [InlineData(0.01f)]
        [InlineData(0.05f)]
        [InlineData(0.1f)]
        public void Update_Should_IncreaseLifetime(float deltaTime)
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();
            config.Duration = 0.15f;
            splashEffect.CreateSplash(100.0f, 200.0f, config);

            var particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should exist");
            
            var lifetimeField = particle.GetType().GetField("LifeTime");
            float initialLifetime = (float)lifetimeField.GetValue(particle);
            initialLifetime.Should().Be(0f, "initial lifetime should be 0");

            splashEffect.Update(deltaTime, 0, 0, 1000, 1000);

            // Lifetime should have increased
            particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should still exist after update");
            
            float updatedLifetime = (float)lifetimeField.GetValue(particle);
            updatedLifetime.Should().BeGreaterThan(initialLifetime, 
                $"lifetime should increase after update with deltaTime={deltaTime}");
            
            float expectedLifetime = initialLifetime + (deltaTime / config.Duration);
            updatedLifetime.Should().BeApproximately(expectedLifetime, 0.001f, 
                "lifetime should increase by deltaTime / Duration");

            int activeCount = GetActiveParticleCount(splashEffect);
            activeCount.Should().Be(1, "particle should still be active");
        }

        [Fact]
        public void Update_Should_DeactivateExpiredParticles()
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();
            config.Duration = 0.15f;
            splashEffect.CreateSplash(100.0f, 200.0f, config);

            int initialActiveCount = GetActiveParticleCount(splashEffect);
            initialActiveCount.Should().Be(1, "particle should be active initially");

            // Update past duration
            float totalTime = config.Duration + 0.1f;
            splashEffect.Update(totalTime, 0, 0, 1000, 1000);

            // Particle should be deactivated (lifetime >= 1.0)
            int activeCount = GetActiveParticleCount(splashEffect);
            activeCount.Should().Be(0, "particle should be deactivated after exceeding duration");

            // Verify lifetime is >= 1.0 by checking all particles
            var field = typeof(SplashEffect).GetField("_particles", BindingFlags.NonPublic | BindingFlags.Instance);
            var particles = field.GetValue(splashEffect) as System.Array;
            
            bool foundExpiredParticle = false;
            for (int i = 0; i < particles.Length; i++)
            {
                var particle = particles.GetValue(i);
                if (particle == null) continue;
                
                var activeField = particle.GetType().GetField("Active");
                var lifetimeField = particle.GetType().GetField("LifeTime");
                
                if (activeField != null && lifetimeField != null)
                {
                    bool isActive = (bool)activeField.GetValue(particle);
                    float lifetime = (float)lifetimeField.GetValue(particle);
                    
                    if (!isActive && lifetime >= 1.0f)
                    {
                        foundExpiredParticle = true;
                        break;
                    }
                }
            }
            
            foundExpiredParticle.Should().BeTrue("should find an expired particle with lifetime >= 1.0");
        }

        [Fact]
        public void Update_Should_CullOffScreenParticles()
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();
            config.UseWorldCoordinates = true;
            splashEffect.CreateSplash(100.0f, 200.0f, config);

            // Verify particle is initially active
            int initialActiveCount = GetActiveParticleCount(splashEffect);
            initialActiveCount.Should().Be(1, "particle should be active initially");

            // Update with viewport that excludes particle
            // Particle at (100, 200) with viewport offset (10000, 10000) 
            // results in viewport coords (-9900, -9800) which is off-screen
            // visibleRange is 100, so particle at -9900 is outside [-100, 200] range
            splashEffect.Update(0.01f, 10000, 10000, 100, 100);

            // Particle should be culled (deactivated)
            int activeCount = GetActiveParticleCount(splashEffect);
            activeCount.Should().Be(0, "particle should be culled when off-screen");
        }

        [Fact]
        public void Update_Should_ApplyPhysics_When_UseWorldCoordinatesIsTrue()
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();
            config.UseWorldCoordinates = true;
            config.RiseSpeed = -3.0f; // Nagative = Upward velocity
            float initialWorldY = 200.0f;
            splashEffect.CreateSplash(100.0f, initialWorldY, config);

            var particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should exist");
            
            var worldYField = particle.GetType().GetField("WorldY");
            var velocityYField = particle.GetType().GetField("VelocityY");
            float initialY = (float)worldYField.GetValue(particle);
            float velocityY = (float)velocityYField.GetValue(particle);
            
            initialY.Should().BeApproximately(initialWorldY, 0.01f, "initial WorldY should match input");
            velocityY.Should().BeApproximately(config.RiseSpeed, 0.01f, "VelocityY should match config");

            float deltaTime = 0.1f;
            splashEffect.Update(deltaTime, 0, 0, 1000, 1000);

            // Physics should be applied (WorldY should change)
            // Get particle again after update to ensure we read updated values
            particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should still exist after update");
            
            float updatedY = (float)worldYField.GetValue(particle);
            float expectedY = initialY + (config.RiseSpeed * deltaTime * 37.0f);
            
            updatedY.Should().BeApproximately(expectedY, 0.01f, 
                "WorldY should change according to physics: WorldY += RiseSpeed * deltaTime * 37.0");
            updatedY.Should().BeLessThan(initialY, 
                "WorldY should decrease (move upward) with negative RiseSpeed");
        }

        [Fact]
        public void Update_Should_NotApplyPhysics_When_UseWorldCoordinatesIsFalse()
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();
            config.UseWorldCoordinates = false;
            config.RiseSpeed = -3.0f;
            float initialY = 200.0f;
            splashEffect.CreateSplash(100.0f, initialY, config);

            var particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should exist");
            
            var yField = particle.GetType().GetField("Y");
            var worldYField = particle.GetType().GetField("WorldY");
            
            float initialViewportY = (float)yField.GetValue(particle);
            float initialWorldY = (float)worldYField.GetValue(particle);

            splashEffect.Update(0.1f, 0, 0, 1000, 1000);

            // No physics applied in viewport mode
            // Get particle again after update
            particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should still exist after update");
            
            float updatedViewportY = (float)yField.GetValue(particle);
            float updatedWorldY = (float)worldYField.GetValue(particle);
            
            updatedViewportY.Should().BeApproximately(initialViewportY, 0.01f, 
                "viewport Y should not change (no physics in viewport mode)");
            updatedWorldY.Should().Be(initialWorldY, 
                "WorldY should remain 0 in viewport mode");
        }

        [Fact]
        public void Update_Should_ConvertWorldToViewportCoordinates()
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();
            config.UseWorldCoordinates = true;
            config.RiseSpeed = 0.0f;
            float worldX = 150.0f;
            float worldY = 250.0f;
            splashEffect.CreateSplash(worldX, worldY, config);

            int viewportOffsetX = 50;
            int viewportOffsetY = 75;

            splashEffect.Update(0.01f, viewportOffsetX, viewportOffsetY, 1000, 1000);

            // Coordinates should be converted: X = 150 - 50 = 100, Y = 250 - 75 = 175
            var particle = GetFirstActiveParticle(splashEffect);
            particle.Should().NotBeNull("particle should exist");
            
            var xField = particle.GetType().GetField("X");
            var yField = particle.GetType().GetField("Y");
            var worldXField = particle.GetType().GetField("WorldX");
            var worldYField = particle.GetType().GetField("WorldY");
            
            float viewportX = (float)xField.GetValue(particle);
            float viewportY = (float)yField.GetValue(particle);
            float currentWorldX = (float)worldXField.GetValue(particle);
            float currentWorldY = (float)worldYField.GetValue(particle);
            
            viewportX.Should().BeApproximately(currentWorldX - viewportOffsetX, 0.01f, 
                "X should be converted to viewport coordinates: WorldX - viewportOffsetX");
            viewportY.Should().BeApproximately(currentWorldY - viewportOffsetY, 0.01f, 
                "Y should be converted to viewport coordinates: WorldY - viewportOffsetY");
        }

        [Fact]
        public void Update_Should_HandleMultipleParticles()
        {
            var splashEffect = new SplashEffect();
            var config = SplashConfig.WaterSplash();
            int particleCount = 10;

            // Create multiple particles
            for (int i = 0; i < particleCount; i++)
            {
                splashEffect.CreateSplash(i * 10.0f, i * 10.0f, config);
            }

            // All particles should be created
            int activeCount = GetActiveParticleCount(splashEffect);
            activeCount.Should().Be(particleCount, $"all {particleCount} particles should be created");

            // Update all particles
            splashEffect.Update(0.05f, 0, 0, 1000, 1000);

            // All particles should still be active
            activeCount = GetActiveParticleCount(splashEffect);
            activeCount.Should().Be(particleCount, 
                $"all {particleCount} particles should still be active after small update");

            // Update again
            splashEffect.Update(0.05f, 0, 0, 1000, 1000);

            // All particles should still be active
            activeCount = GetActiveParticleCount(splashEffect);
            activeCount.Should().Be(particleCount, 
                $"all {particleCount} particles should still be active after second update");
        }
    }
}
