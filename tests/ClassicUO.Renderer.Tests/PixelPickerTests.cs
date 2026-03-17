using ClassicUO.Renderer;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Renderer.Tests
{
    public class PixelPickerTests
    {
        // --- Get on unregistered texture ---

        [Fact]
        public void Get_UnregisteredTexture_ReturnsFalse()
        {
            var picker = new PixelPicker();
            picker.Get(999, 0, 0).Should().BeFalse();
        }

        // --- Boundary checks ---

        [Fact]
        public void Get_OutOfBoundsNegativeX_ReturnsFalse()
        {
            var picker = new PixelPicker();
            uint[] pixels = { 0xFFFFFFFF };
            picker.Set(1, 1, 1, pixels);

            picker.Get(1, -1, 0).Should().BeFalse();
        }

        [Fact]
        public void Get_OutOfBoundsNegativeY_ReturnsFalse()
        {
            var picker = new PixelPicker();
            uint[] pixels = { 0xFFFFFFFF };
            picker.Set(1, 1, 1, pixels);

            picker.Get(1, 0, -1).Should().BeFalse();
        }

        [Fact]
        public void Get_OutOfBoundsLargeX_ReturnsFalse()
        {
            var picker = new PixelPicker();
            uint[] pixels = { 0xFFFFFFFF };
            picker.Set(1, 1, 1, pixels);

            picker.Get(1, 5, 0).Should().BeFalse();
        }

        [Fact]
        public void Get_OutOfBoundsLargeY_ReturnsFalse()
        {
            var picker = new PixelPicker();
            uint[] pixels = { 0xFFFFFFFF };
            picker.Set(1, 1, 1, pixels);

            picker.Get(1, 0, 5).Should().BeFalse();
        }

        // --- Transparent pixel ---

        [Fact]
        public void Set_ThenGet_TransparentPixel_ReturnsFalse()
        {
            var picker = new PixelPicker();
            uint[] pixels = { 0x00000000 }; // 1x1 transparent
            picker.Set(2, 1, 1, pixels);

            picker.Get(2, 0, 0).Should().BeFalse();
        }

        // --- All opaque image: position 0 is not detected, but other positions are ---

        [Fact]
        public void AllOpaque_8x1_PositionsAfterFirst_ReturnTrue()
        {
            var picker = new PixelPicker();
            uint[] pixels = new uint[8];
            for (int i = 0; i < 8; i++) pixels[i] = 0xFF000000;
            picker.Set(10, 8, 1, pixels);

            // The RLE algorithm starts with transparent span of length 0.
            // Position 0 (target=0): loop never enters, returns false.
            // Positions 1+ fall strictly inside the opaque span.
            picker.Get(10, 1, 0).Should().BeTrue();
            picker.Get(10, 4, 0).Should().BeTrue();
            picker.Get(10, 7, 0).Should().BeTrue();
        }

        [Fact]
        public void AllOpaque_8x8_InternalPositions_ReturnTrue()
        {
            var picker = new PixelPicker();
            int w = 8, h = 8;
            uint[] pixels = new uint[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = 0xFF000000;
            picker.Set(50, w, h, pixels);

            picker.Get(50, 1, 0).Should().BeTrue();
            picker.Get(50, 0, 1).Should().BeTrue(); // target=8
            picker.Get(50, 4, 4).Should().BeTrue();
            picker.Get(50, 7, 7).Should().BeTrue();
        }

        // --- Transparent prefix then opaque run ---

        [Fact]
        public void TransparentThenOpaque_SkipsFirstOpaquePixel()
        {
            var picker = new PixelPicker();
            // 1 transparent + 7 opaque
            uint[] pixels = { 0, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu };
            picker.Set(20, 8, 1, pixels);

            picker.Get(20, 0, 0).Should().BeFalse(); // transparent
            picker.Get(20, 1, 0).Should().BeFalse(); // first opaque pixel at span boundary
            picker.Get(20, 2, 0).Should().BeTrue();  // inside opaque span
            picker.Get(20, 7, 0).Should().BeTrue();
        }

        [Fact]
        public void TwoTransparentThenOpaque_ThirdPositionInsideOpaque()
        {
            var picker = new PixelPicker();
            // 2 transparent + 6 opaque
            uint[] pixels = { 0, 0, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu };
            picker.Set(21, 8, 1, pixels);

            picker.Get(21, 0, 0).Should().BeFalse();
            picker.Get(21, 1, 0).Should().BeFalse();
            picker.Get(21, 2, 0).Should().BeFalse(); // first opaque at boundary
            picker.Get(21, 3, 0).Should().BeTrue();  // inside opaque span
            picker.Get(21, 7, 0).Should().BeTrue();
        }

        // --- Opaque span followed by transparent ---

        [Fact]
        public void OpaqueFollowedByTransparent_DetectsTransparent()
        {
            var picker = new PixelPicker();
            // 5 opaque + 3 transparent
            uint[] pixels = { 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0xFFu, 0, 0, 0 };
            picker.Set(22, 8, 1, pixels);

            picker.Get(22, 1, 0).Should().BeTrue();  // inside opaque
            picker.Get(22, 4, 0).Should().BeTrue();  // last opaque
            picker.Get(22, 5, 0).Should().BeFalse(); // first transparent at boundary
            picker.Get(22, 6, 0).Should().BeFalse(); // transparent
            picker.Get(22, 7, 0).Should().BeFalse(); // transparent
        }

        // --- Alternating opaque/transparent with wide spans ---

        [Fact]
        public void WideOpaqueSpans_DetectedCorrectly()
        {
            var picker = new PixelPicker();
            // 3 opaque, 2 transparent, 3 opaque
            uint[] pixels = { 0xFFu, 0xFFu, 0xFFu, 0, 0, 0xFFu, 0xFFu, 0xFFu };
            picker.Set(23, 8, 1, pixels);

            picker.Get(23, 1, 0).Should().BeTrue();   // inside first opaque span
            picker.Get(23, 2, 0).Should().BeTrue();   // inside first opaque span
            picker.Get(23, 3, 0).Should().BeFalse();  // transparent boundary
            picker.Get(23, 4, 0).Should().BeFalse();  // transparent
            picker.Get(23, 6, 0).Should().BeTrue();   // inside second opaque span
            picker.Get(23, 7, 0).Should().BeTrue();   // inside second opaque span
        }

        // --- Set is idempotent (does not overwrite) ---

        [Fact]
        public void Set_CalledTwice_DoesNotOverwrite()
        {
            var picker = new PixelPicker();
            // Use 8x1, test position 2 which is inside the opaque span
            uint[] pixels1 = new uint[8];
            for (int i = 0; i < 8; i++) pixels1[i] = 0xFFu;
            uint[] pixels2 = new uint[8]; // all transparent

            picker.Set(1, 8, 1, pixels1);
            picker.Set(1, 8, 1, pixels2); // should be ignored

            picker.Get(1, 2, 0).Should().BeTrue(); // still opaque from first set
        }

        // --- GetDimensions ---

        [Fact]
        public void GetDimensions_UnregisteredTexture_ReturnsZero()
        {
            var picker = new PixelPicker();
            picker.GetDimensions(999, out int w, out int h);

            w.Should().Be(0);
            h.Should().Be(0);
        }

        [Fact]
        public void GetDimensions_RegisteredTexture_ReturnsCorrectDimensions()
        {
            var picker = new PixelPicker();
            uint[] pixels = new uint[12]; // 4x3
            picker.Set(5, 4, 3, pixels);

            picker.GetDimensions(5, out int w, out int h);
            w.Should().Be(4);
            h.Should().Be(3);
        }

        // --- Multiple textures ---

        [Fact]
        public void MultipleTextures_IndependentDimensions()
        {
            var picker = new PixelPicker();

            uint[] pixels2x3 = new uint[6];
            uint[] pixels4x5 = new uint[20];

            picker.Set(100, 2, 3, pixels2x3);
            picker.Set(200, 4, 5, pixels4x5);

            picker.GetDimensions(100, out int w1, out int h1);
            picker.GetDimensions(200, out int w2, out int h2);

            w1.Should().Be(2);
            h1.Should().Be(3);
            w2.Should().Be(4);
            h2.Should().Be(5);
        }

        [Fact]
        public void MultipleTextures_IndependentHitData()
        {
            var picker = new PixelPicker();

            // texture 100: 8x1 all opaque
            uint[] pixels1 = new uint[8];
            for (int i = 0; i < 8; i++) pixels1[i] = 0xFFu;
            // texture 200: 8x1 all transparent
            uint[] pixels2 = new uint[8];

            picker.Set(100, 8, 1, pixels1);
            picker.Set(200, 8, 1, pixels2);

            picker.Get(100, 3, 0).Should().BeTrue();
            picker.Get(200, 3, 0).Should().BeFalse();
        }

        // --- All transparent ---

        [Fact]
        public void Set_LargerImage_AllTransparent_GetReturnsFalse()
        {
            var picker = new PixelPicker();
            int w = 8, h = 8;
            uint[] pixels = new uint[w * h]; // all zeros

            picker.Set(51, w, h, pixels);

            picker.Get(51, 4, 4).Should().BeFalse();
            picker.Get(51, 7, 7).Should().BeFalse();
        }

        // --- Varint encoding round-trip for large dimensions ---

        [Fact]
        public void GetDimensions_LargeWidth_EncodesCorrectly()
        {
            var picker = new PixelPicker();
            int w = 300, h = 200;
            uint[] pixels = new uint[w * h];
            picker.Set(60, w, h, pixels);

            picker.GetDimensions(60, out int rw, out int rh);
            rw.Should().Be(300);
            rh.Should().Be(200);
        }

        [Fact]
        public void GetDimensions_VeryLargeValues_RoundTrip()
        {
            var picker = new PixelPicker();
            int w = 1024, h = 768;
            uint[] pixels = new uint[w * h];
            picker.Set(70, w, h, pixels);

            picker.GetDimensions(70, out int rw, out int rh);
            rw.Should().Be(1024);
            rh.Should().Be(768);
        }
    }
}
