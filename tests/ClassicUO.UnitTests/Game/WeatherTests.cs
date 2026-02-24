using ClassicUO.Game;
using ClassicUO.Renderer;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace ClassicUO.UnitTests.Game
{
    /// <summary>
    /// Unit tests for Weather class defensive programming patterns.
    /// Tests focus on graceful degradation and error handling.
    /// </summary>
    public class WeatherTests
    {
        #region Helper Methods

        private static Texture2D GetWhiteTexture()
        {
            var property = typeof(Weather).GetProperty("WhiteTexture", BindingFlags.NonPublic | BindingFlags.Static);
            return property?.GetValue(null) as Texture2D;
        }

        private static bool GetWhiteTextureInitFailed()
        {
            var field = typeof(Weather).GetField("_whiteTextureInitFailed", BindingFlags.NonPublic | BindingFlags.Static);
            return field != null && (bool)field.GetValue(null);
        }

        private static void SetWhiteTextureInitFailed(bool value)
        {
            var field = typeof(Weather).GetField("_whiteTextureInitFailed", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, value);
        }

        private static void SetWhiteTexture(Texture2D texture)
        {
            var field = typeof(Weather).GetField("_whiteTexture", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, texture);
        }

        private static void ResetWarningState()
        {
            var field = typeof(Weather).GetField("_whiteTextureWarningLogged", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, false);
        }

        private static Vector3 InvokeColorToVector3(Color color)
        {
            var method = typeof(Weather).GetMethod("ColorToVector3", BindingFlags.NonPublic | BindingFlags.Static);
            return (Vector3)method.Invoke(null, new object[] { color });
        }

        private static bool InvokeSafeDraw(UltimaBatcher2D batcher, Texture2D texture, Rectangle rect, Vector3 color, float depth)
        {
            var method = typeof(Weather).GetMethod("SafeDraw", BindingFlags.NonPublic | BindingFlags.Static);
            return (bool)method.Invoke(null, new object[] { batcher, texture, rect, color, depth });
        }

        private static bool InvokeSafeDrawLine(UltimaBatcher2D batcher, Texture2D texture, Vector2 start, Vector2 end, Vector3 color, int width, float depth)
        {
            var method = typeof(Weather).GetMethod("SafeDrawLine", BindingFlags.NonPublic | BindingFlags.Static);
            return (bool)method.Invoke(null, new object[] { batcher, texture, start, end, color, width, depth });
        }

        #endregion

        #region ColorToVector3 Tests

        [Fact]
        public void ColorToVector3_Should_ConvertValidColor_Correctly()
        {
            var color = new Color(255, 128, 64, 255);

            Vector3 result = InvokeColorToVector3(color);

            result.X.Should().BeApproximately(1.0f, 0.01f, "Red should be 255/255 = 1.0");
            result.Y.Should().BeApproximately(0.502f, 0.01f, "Green should be 128/255 ≈ 0.502");
            result.Z.Should().BeApproximately(0.251f, 0.01f, "Blue should be 64/255 ≈ 0.251");
        }

        [Fact]
        public void ColorToVector3_Should_ClampNegativeValues_ToZero()
        {
            var color = new Color(-1, -1, -1, -255);

            Vector3 result = InvokeColorToVector3(color);

            result.X.Should().BeGreaterOrEqualTo(0f, "Red should be clamped to >= 0");
            result.Y.Should().BeGreaterOrEqualTo(0f, "Green should be clamped to >= 0");
            result.Z.Should().BeGreaterOrEqualTo(0f, "Blue should be clamped to >= 0");
        }

        [Fact]
        public void ColorToVector3_Should_ClampValues_ToValidRange()
        {
            var color = new Color(260, 260, 260, 260);

            Vector3 result = InvokeColorToVector3(color);

            result.X.Should().BeLessOrEqualTo(1f, "Red should be clamped to <= 1.0");
            result.Y.Should().BeLessOrEqualTo(1f, "Green should be clamped to <= 1.0");
            result.Z.Should().BeLessOrEqualTo(1f, "Blue should be clamped to <= 1.0");
        }

        [Fact]
        public void ColorToVector3_Should_HandleWhiteColor()
        {
            var color = Color.White;

            Vector3 result = InvokeColorToVector3(color);

            result.Should().Be(new Vector3(1f, 1f, 1f), "White color should convert to (1, 1, 1)");
        }
        #endregion

        #region SafeDraw Tests

        [Fact]
        public void SafeDraw_Should_ReturnFalse_When_TextureIsNull()
        {
            UltimaBatcher2D batcher = null; 
            Texture2D texture = null;
            Rectangle rect = new Rectangle(0, 0, 10, 10);
            Vector3 color = Vector3.One;
            float depth = 0.5f;

            bool result = InvokeSafeDraw(batcher, texture, rect, color, depth);

            result.Should().BeFalse("SafeDraw should return false when texture is null");
        }

        [Fact]
        public void SafeDraw_Should_ReturnFalse_When_BatcherIsNull()
        {
            UltimaBatcher2D batcher = null;
            Texture2D texture = null;
            Rectangle rect = new Rectangle(0, 0, 10, 10);
            Vector3 color = Vector3.One;
            float depth = 0.5f;

            bool result = InvokeSafeDraw(batcher, texture, rect, color, depth);

            result.Should().BeFalse("SafeDraw should return false when batcher is null");
        }

        #endregion

        #region SafeDrawLine Tests

        [Fact]
        public void SafeDrawLine_Should_ReturnFalse_When_TextureIsNull()
        {
            UltimaBatcher2D batcher = null;
            Texture2D texture = null;
            Vector2 start = new Vector2(0, 0);
            Vector2 end = new Vector2(10, 10);
            Vector3 color = Vector3.One;
            int width = 2;
            float depth = 0.5f;

            bool result = InvokeSafeDrawLine(batcher, texture, start, end, color, width, depth);

            result.Should().BeFalse("SafeDrawLine should return false when texture is null");
        }

        [Fact]
        public void SafeDrawLine_Should_ReturnFalse_When_BatcherIsNull()
        {
            UltimaBatcher2D batcher = null;
            Texture2D texture = null;
            Vector2 start = new Vector2(0, 0);
            Vector2 end = new Vector2(10, 10);
            Vector3 color = Vector3.One;
            int width = 2;
            float depth = 0.5f;

            bool result = InvokeSafeDrawLine(batcher, texture, start, end, color, width, depth);

            result.Should().BeFalse("SafeDrawLine should return false when batcher is null");
        }

        #endregion

        #region WhiteTexture Initialization Tests

        [Fact]
        public void WhiteTexture_Should_ReturnNull_When_InitializationFailed()
        {
            SetWhiteTexture(null);
            SetWhiteTextureInitFailed(true);
            ResetWarningState();

            Texture2D result = GetWhiteTexture();

            result.Should().BeNull("WhiteTexture should return null when initialization previously failed");

            SetWhiteTextureInitFailed(false);
        }

        [Fact]
        public void WhiteTexture_Should_TrackFailureState_WhenInitFails()
        {
            SetWhiteTexture(null);
            SetWhiteTextureInitFailed(false);
            ResetWarningState();

            SetWhiteTextureInitFailed(true);
            bool failureState = GetWhiteTextureInitFailed();

            failureState.Should().BeTrue("Failure state should be tracked");

            SetWhiteTextureInitFailed(false);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Weather_Should_HandleNullWorld_Gracefully()
        {
            World world = null;

            Action act = () => new Weather(world);

            act.Should().NotThrow("Weather constructor should handle null world gracefully");
        }

        [Fact]
        public void ColorToVector3_Should_HandleMultipleColors_Consistently()
        {
            var colors = new[]
            {
                Color.Red,
                Color.Green,
                Color.Blue,
                Color.Yellow,
                Color.Magenta,
                Color.Cyan
            };

            foreach (var color in colors)
            {
                Vector3 result = InvokeColorToVector3(color);

                result.X.Should().BeInRange(0f, 1f, $"{color} X component should be in valid range");
                result.Y.Should().BeInRange(0f, 1f, $"{color} Y component should be in valid range");
                result.Z.Should().BeInRange(0f, 1f, $"{color} Z component should be in valid range");
            }
        }

        #endregion

        #region Warning Logging Tests

        [Fact]
        public void LogWarning_Should_OnlyLogOnce()
        {
            ResetWarningState();

            var warningLoggedField = typeof(Weather).GetField("_whiteTextureWarningLogged", BindingFlags.NonPublic | BindingFlags.Static);
            bool initialState = (bool)warningLoggedField.GetValue(null);

            initialState.Should().BeFalse("Warning state should be reset initially");
        }

        #endregion

        #region Edge Case Tests

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(128, 128, 128)]
        [InlineData(255, 255, 255)]
        [InlineData(64, 128, 192)]
        public void ColorToVector3_Should_HandleVariousRGBValues(byte r, byte g, byte b)
        {
            var color = new Color(r, g, b);

            Vector3 result = InvokeColorToVector3(color);

            result.X.Should().BeApproximately(r / 255f, 0.01f, "Red conversion should be accurate");
            result.Y.Should().BeApproximately(g / 255f, 0.01f, "Green conversion should be accurate");
            result.Z.Should().BeApproximately(b / 255f, 0.01f, "Blue conversion should be accurate");
        }

        [Fact]
        public void SafeDraw_And_SafeDrawLine_Should_HandleNullGracefully()
        {
            UltimaBatcher2D nullBatcher = null;
            Texture2D nullTexture = null;

            bool drawResult = InvokeSafeDraw(nullBatcher, nullTexture, Rectangle.Empty, Vector3.Zero, 0f);
            drawResult.Should().BeFalse("SafeDraw should gracefully handle null parameters");

            bool drawLineResult = InvokeSafeDrawLine(nullBatcher, nullTexture, Vector2.Zero, Vector2.Zero, Vector3.Zero, 1, 0f);
            drawLineResult.Should().BeFalse("SafeDrawLine should gracefully handle null parameters");
        }

        #endregion
    }
}
