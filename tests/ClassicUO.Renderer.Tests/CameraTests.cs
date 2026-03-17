using ClassicUO.Renderer;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.Renderer.Tests
{
    public class CameraTests
    {
        // --- Constructor defaults ---

        [Fact]
        public void Constructor_DefaultValues()
        {
            var camera = new Camera();

            camera.ZoomMin.Should().Be(1f);
            camera.ZoomMax.Should().Be(1f);
            camera.ZoomStep.Should().Be(0.1f);
            camera.Zoom.Should().Be(1f);
        }

        [Fact]
        public void Constructor_CustomZoomRange()
        {
            var camera = new Camera(minZoomValue: 0.5f, maxZoomValue: 3.0f, zoomStep: 0.25f);

            camera.ZoomMin.Should().Be(0.5f);
            camera.ZoomMax.Should().Be(3.0f);
            camera.ZoomStep.Should().Be(0.25f);
            camera.Zoom.Should().Be(1f);
        }

        // --- Zoom clamping ---

        [Fact]
        public void Zoom_ClampedToMin()
        {
            var camera = new Camera(minZoomValue: 0.5f, maxZoomValue: 2f);

            camera.Zoom = 0.1f;

            camera.Zoom.Should().Be(0.5f);
        }

        [Fact]
        public void Zoom_ClampedToMax()
        {
            var camera = new Camera(minZoomValue: 0.5f, maxZoomValue: 2f);

            camera.Zoom = 5f;

            camera.Zoom.Should().Be(2f);
        }

        [Fact]
        public void Zoom_ValueWithinRange_Accepted()
        {
            var camera = new Camera(minZoomValue: 0.5f, maxZoomValue: 2f);

            camera.Zoom = 1.5f;

            camera.Zoom.Should().Be(1.5f);
        }

        // --- ZoomIn / ZoomOut ---

        [Fact]
        public void ZoomIn_DecreasesZoomByStep()
        {
            var camera = new Camera(minZoomValue: 0.5f, maxZoomValue: 2f, zoomStep: 0.1f);
            camera.Zoom = 1f;

            camera.ZoomIn();

            camera.Zoom.Should().BeApproximately(0.9f, 0.001f);
        }

        [Fact]
        public void ZoomOut_IncreasesZoomByStep()
        {
            var camera = new Camera(minZoomValue: 0.5f, maxZoomValue: 2f, zoomStep: 0.1f);
            camera.Zoom = 1f;

            camera.ZoomOut();

            camera.Zoom.Should().BeApproximately(1.1f, 0.001f);
        }

        [Fact]
        public void ZoomIn_ClampedAtMin()
        {
            var camera = new Camera(minZoomValue: 1f, maxZoomValue: 2f, zoomStep: 0.5f);
            camera.Zoom = 1f;

            camera.ZoomIn();

            camera.Zoom.Should().Be(1f); // can't go below min
        }

        [Fact]
        public void ZoomOut_ClampedAtMax()
        {
            var camera = new Camera(minZoomValue: 1f, maxZoomValue: 1f, zoomStep: 0.5f);

            camera.ZoomOut();

            camera.Zoom.Should().Be(1f); // can't go above max
        }

        // --- Bounds ---

        [Fact]
        public void Bounds_CanBeSetDirectly()
        {
            var camera = new Camera();
            camera.Bounds = new Rectangle(10, 20, 800, 600);

            camera.Bounds.X.Should().Be(10);
            camera.Bounds.Y.Should().Be(20);
            camera.Bounds.Width.Should().Be(800);
            camera.Bounds.Height.Should().Be(600);
        }

        // --- Offset initial value ---

        [Fact]
        public void Offset_InitiallyZero()
        {
            var camera = new Camera();

            camera.Offset.Should().Be(Vector2.Zero);
        }

        // --- Peeking flags ---

        [Fact]
        public void PeekingFlags_DefaultFalse()
        {
            var camera = new Camera();

            camera.PeekingToMouse.Should().BeFalse();
            camera.PeekBackwards.Should().BeFalse();
        }

        // --- ScreenToWorld / WorldToScreen round-trip at zoom=1 ---

        [Fact]
        public void ScreenToWorld_AtDefaultZoom_ReturnsSamePoint()
        {
            var camera = new Camera();
            camera.Bounds = new Rectangle(0, 0, 800, 600);
            camera.Update(true, 0f, Point.Zero);

            var input = new Point(400, 300); // center
            var world = camera.ScreenToWorld(input);

            // At zoom=1 with no offset, screen == world
            world.Should().Be(input);
        }

        [Fact]
        public void WorldToScreen_AtDefaultZoom_ReturnsSamePoint()
        {
            var camera = new Camera();
            camera.Bounds = new Rectangle(0, 0, 800, 600);
            camera.Update(true, 0f, Point.Zero);

            var input = new Point(400, 300);
            var screen = camera.WorldToScreen(input);

            screen.Should().Be(input);
        }

        [Fact]
        public void ScreenToWorld_WorldToScreen_RoundTrip()
        {
            var camera = new Camera(minZoomValue: 0.5f, maxZoomValue: 3f);
            camera.Bounds = new Rectangle(0, 0, 800, 600);
            camera.Zoom = 2f;
            camera.Update(true, 0f, Point.Zero);

            var original = new Point(100, 200);
            var world = camera.ScreenToWorld(original);
            var backToScreen = camera.WorldToScreen(world);

            // Due to integer truncation there may be small error
            backToScreen.X.Should().BeCloseTo(original.X, 1);
            backToScreen.Y.Should().BeCloseTo(original.Y, 1);
        }

        // --- GetViewport ---

        [Fact]
        public void GetViewport_MatchesBounds()
        {
            var camera = new Camera();
            camera.Bounds = new Rectangle(10, 20, 640, 480);

            var vp = camera.GetViewport();

            vp.X.Should().Be(10);
            vp.Y.Should().Be(20);
            vp.Width.Should().Be(640);
            vp.Height.Should().Be(480);
        }
    }
}
