using ClassicUO.Renderer;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.Renderer.Tests
{
    public class GlyphAtlasEntryTests
    {
        [Fact]
        public void Empty_IsNotValid()
        {
            GlyphAtlasEntry.Empty.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Empty_HasNullTexture()
        {
            GlyphAtlasEntry.Empty.Texture.Should().BeNull();
        }

        [Fact]
        public void Empty_HasZeroDimensions()
        {
            GlyphAtlasEntry.Empty.GlyphWidth.Should().Be(0);
            GlyphAtlasEntry.Empty.GlyphHeight.Should().Be(0);
            GlyphAtlasEntry.Empty.BearingX.Should().Be(0);
            GlyphAtlasEntry.Empty.BearingY.Should().Be(0);
            GlyphAtlasEntry.Empty.AdvanceWidth.Should().Be(0);
        }

        [Fact]
        public void DefaultConstructor_IsNotValid()
        {
            var entry = new GlyphAtlasEntry();

            entry.IsValid.Should().BeFalse();
        }

        [Fact]
        public void CanSetBearingAndAdvance()
        {
            var entry = new GlyphAtlasEntry
            {
                BearingX = 3,
                BearingY = 5,
                AdvanceWidth = 10,
                GlyphWidth = 8,
                GlyphHeight = 12,
                UV = new Rectangle(0, 0, 8, 12)
            };

            entry.BearingX.Should().Be(3);
            entry.BearingY.Should().Be(5);
            entry.AdvanceWidth.Should().Be(10);
            entry.GlyphWidth.Should().Be(8);
            entry.GlyphHeight.Should().Be(12);
            entry.UV.Should().Be(new Rectangle(0, 0, 8, 12));
        }
    }
}
