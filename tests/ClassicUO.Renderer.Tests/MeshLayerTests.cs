using ClassicUO.Renderer;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Renderer.Tests
{
    public class MeshLayerTests
    {
        // --- Initial state ---

        [Fact]
        public void NewMeshLayer_HasZeroCounts()
        {
            var layer = new MeshLayer();

            layer.Count.Should().Be(0);
            layer.VisibleSpriteCount.Should().Be(0);
            layer.VisibleRunCount.Should().Be(0);
        }

        // --- EnsureCapacity ---

        [Fact]
        public void EnsureCapacity_BelowCurrent_DoesNotShrink()
        {
            var layer = new MeshLayer();
            int originalLength = layer.Vertices.Length;

            layer.EnsureCapacity(1);

            layer.Vertices.Length.Should().Be(originalLength);
        }

        [Fact]
        public void EnsureCapacity_AboveCurrent_GrowsArrays()
        {
            var layer = new MeshLayer();
            int originalLength = layer.Vertices.Length;

            layer.EnsureCapacity(originalLength + 1);

            layer.Vertices.Length.Should().BeGreaterThanOrEqualTo(originalLength + 1);
            layer.Textures.Length.Should().Be(layer.Vertices.Length);
            layer.Visible.Length.Should().Be(layer.Vertices.Length);
        }

        [Fact]
        public void EnsureCapacity_GrowsByDoublingPowerOf2()
        {
            var layer = new MeshLayer();
            // Initial capacity is 64

            layer.EnsureCapacity(100);

            layer.Vertices.Length.Should().Be(128); // 64*2
        }

        [Fact]
        public void EnsureCapacity_LargeValue_DoublesSufficientTimes()
        {
            var layer = new MeshLayer();

            layer.EnsureCapacity(500);

            layer.Vertices.Length.Should().Be(512); // 64 -> 128 -> 256 -> 512
        }

        // --- ResetVisibility ---

        [Fact]
        public void ResetVisibility_ClearsVisibleFlags()
        {
            var layer = new MeshLayer();
            layer.Count = 3;
            layer.Visible[0] = true;
            layer.Visible[1] = true;
            layer.Visible[2] = true;

            layer.ResetVisibility();

            layer.Visible[0].Should().BeFalse();
            layer.Visible[1].Should().BeFalse();
            layer.Visible[2].Should().BeFalse();
        }

        [Fact]
        public void ResetVisibility_ZeroCount_DoesNotThrow()
        {
            var layer = new MeshLayer();
            layer.Count = 0;

            var act = () => layer.ResetVisibility();

            act.Should().NotThrow();
        }

        // --- Reset ---

        [Fact]
        public void Reset_ClearsAllCounts()
        {
            var layer = new MeshLayer();
            layer.Count = 10;
            layer.VisibleSpriteCount = 5;
            layer.VisibleRunCount = 2;

            layer.Reset();

            layer.Count.Should().Be(0);
            layer.VisibleSpriteCount.Should().Be(0);
            layer.VisibleRunCount.Should().Be(0);
        }

        // --- SoftReset ---

        [Fact]
        public void SoftReset_ClearsCountsAndTextureReferences()
        {
            var layer = new MeshLayer();
            layer.Count = 5;
            layer.VisibleSpriteCount = 3;
            layer.VisibleRunCount = 1;

            layer.SoftReset();

            layer.Count.Should().Be(0);
            layer.VisibleSpriteCount.Should().Be(0);
            layer.VisibleRunCount.Should().Be(0);

            // Texture references should be cleared
            for (int i = 0; i < layer.Textures.Length; i++)
            {
                layer.Textures[i].Should().BeNull();
            }
        }

        // --- BuildVisibleIndices with no GPU (null textures) ---

        [Fact]
        public void BuildVisibleIndices_ZeroCount_ReturnsFalse()
        {
            var layer = new MeshLayer();
            layer.Count = 0;

            bool result = layer.BuildVisibleIndices();

            result.Should().BeFalse();
            layer.VisibleSpriteCount.Should().Be(0);
            layer.VisibleRunCount.Should().Be(0);
        }

        [Fact]
        public void BuildVisibleIndices_NoneVisible_ReturnsFalse()
        {
            var layer = new MeshLayer();
            layer.Count = 3;
            // All Visible[] default to false

            bool result = layer.BuildVisibleIndices();

            result.Should().BeFalse();
            layer.VisibleSpriteCount.Should().Be(0);
        }

        [Fact]
        public void BuildVisibleIndices_SomeVisible_CountsCorrectly()
        {
            var layer = new MeshLayer();
            layer.Count = 4;
            layer.Visible[0] = true;
            layer.Visible[1] = false;
            layer.Visible[2] = true;
            layer.Visible[3] = false;

            // Textures are null but that's fine for counting logic
            bool result = layer.BuildVisibleIndices();

            result.Should().BeTrue();
            layer.VisibleSpriteCount.Should().Be(2);
        }

        // --- SetVisible alpha logic ---

        [Fact]
        public void SetVisible_Alpha0_TreatedAsOpaque_NoVertexChange()
        {
            var layer = new MeshLayer();
            layer.Count = 1;
            layer.Vertices[0].Hue0.Z = 1f;

            layer.SetVisible(0, 0);

            layer.Visible[0].Should().BeTrue();
            layer.Vertices[0].Hue0.Z.Should().Be(1f); // unchanged
        }

        [Fact]
        public void SetVisible_Alpha255_FullyOpaque_NoVertexChange()
        {
            var layer = new MeshLayer();
            layer.Count = 1;
            layer.Vertices[0].Hue0.Z = 1f;

            layer.SetVisible(0, 0xFF);

            layer.Visible[0].Should().BeTrue();
            layer.Vertices[0].Hue0.Z.Should().Be(1f); // unchanged
        }

        [Fact]
        public void SetVisible_AlphaMiddle_SetsVertexAlpha()
        {
            var layer = new MeshLayer();
            layer.Count = 1;
            layer.Vertices[0].Hue0.Z = 1f;

            layer.SetVisible(0, 128);

            layer.Visible[0].Should().BeTrue();
            float expected = 128f / 255f;
            layer.Vertices[0].Hue0.Z.Should().BeApproximately(expected, 0.001f);
            layer.Vertices[0].Hue1.Z.Should().BeApproximately(expected, 0.001f);
            layer.Vertices[0].Hue2.Z.Should().BeApproximately(expected, 0.001f);
            layer.Vertices[0].Hue3.Z.Should().BeApproximately(expected, 0.001f);
        }

        [Fact]
        public void SetVisible_CircleTrans_AlphaGreaterThanOne()
        {
            var layer = new MeshLayer();
            layer.Count = 1;

            layer.SetVisible(0, 0, circletrans: true);

            layer.Visible[0].Should().BeTrue();
            // alphaHue=0 with circletrans => alpha = 1f + 1f = 2f
            layer.Vertices[0].Hue0.Z.Should().Be(2f);
        }

        [Fact]
        public void SetVisible_CircleTrans_WithAlpha_AddsOne()
        {
            var layer = new MeshLayer();
            layer.Count = 1;

            layer.SetVisible(0, 128, circletrans: true);

            // alpha = (128/255) + 1
            float expected = (128f / 255f) + 1f;
            layer.Vertices[0].Hue0.Z.Should().BeApproximately(expected, 0.001f);
        }

        // --- ResetAlpha ---

        [Fact]
        public void ResetAlpha_RestoresFullOpacity()
        {
            var layer = new MeshLayer();
            layer.Count = 1;
            layer.SetVisible(0, 100); // sets partial alpha

            layer.ResetAlpha();

            layer.Vertices[0].Hue0.Z.Should().Be(1f);
            layer.Vertices[0].Hue1.Z.Should().Be(1f);
            layer.Vertices[0].Hue2.Z.Should().Be(1f);
            layer.Vertices[0].Hue3.Z.Should().Be(1f);
        }

        [Fact]
        public void ResetAlpha_NothingDirty_DoesNotThrow()
        {
            var layer = new MeshLayer();
            layer.Count = 1;

            var act = () => layer.ResetAlpha();

            act.Should().NotThrow();
        }

        // --- SetHue ---

        [Fact]
        public void SetHue_SetsAllFourVertexHues()
        {
            var layer = new MeshLayer();
            layer.Count = 1;
            layer.Vertices[0].Hue0.Z = 0.5f; // existing alpha

            layer.SetHue(0, 10f, 2f);

            layer.Vertices[0].Hue0.X.Should().Be(10f);
            layer.Vertices[0].Hue0.Y.Should().Be(2f);
            layer.Vertices[0].Hue0.Z.Should().Be(0.5f); // alpha preserved
            layer.Vertices[0].Hue1.X.Should().Be(10f);
            layer.Vertices[0].Hue2.X.Should().Be(10f);
            layer.Vertices[0].Hue3.X.Should().Be(10f);
        }

        [Fact]
        public void SetHue_SameValue_DoesNotChange()
        {
            var layer = new MeshLayer();
            layer.Count = 1;
            layer.Vertices[0].Hue0.X = 5f;
            layer.Vertices[0].Hue0.Y = 1f;
            layer.Vertices[0].Hue0.Z = 0.8f;

            // Setting same hue values should be a no-op
            layer.SetHue(0, 5f, 1f);

            layer.Vertices[0].Hue0.Z.Should().Be(0.8f); // alpha unchanged
        }

        // --- TextureRun struct ---

        [Fact]
        public void TextureRun_DefaultValues()
        {
            var run = new TextureRun();

            run.Texture.Should().BeNull();
            run.Start.Should().Be(0);
            run.Count.Should().Be(0);
        }
    }
}
