using ClassicUO.IO;
using FluentAssertions;
using Xunit;

namespace ClassicUO.IO.Tests
{
    public class UOFileIndexTests
    {
        [Fact]
        public void Constructor_SetsAllFieldsCorrectly()
        {
            var index = new UOFileIndex(
                file: null,
                offset: 100,
                length: 200,
                decompressed: 300,
                compressionFlag: CompressionType.Zlib,
                width: 64,
                height: 128,
                hue: 1234
            );

            index.File.Should().BeNull();
            index.Offset.Should().Be(100);
            index.Length.Should().Be(200);
            index.DecompressedLength.Should().Be(300);
            index.CompressionFlag.Should().Be(CompressionType.Zlib);
            index.Width.Should().Be(64);
            index.Height.Should().Be(128);
            index.Hue.Should().Be(1234);
            index.AnimOffset.Should().Be(0);
        }

        [Fact]
        public void Invalid_Static_HasSentinelValues()
        {
            var invalid = UOFileIndex.Invalid;

            invalid.File.Should().BeNull();
            invalid.Offset.Should().Be(0);
            invalid.Length.Should().Be(0);
            invalid.DecompressedLength.Should().Be(0);
        }

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            var a = new UOFileIndex(null, 100, 200, 300);
            var b = new UOFileIndex(null, 100, 200, 300);

            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentOffset_ReturnsFalse()
        {
            var a = new UOFileIndex(null, 100, 200, 300);
            var b = new UOFileIndex(null, 999, 200, 300);

            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentLength_ReturnsFalse()
        {
            var a = new UOFileIndex(null, 100, 200, 300);
            var b = new UOFileIndex(null, 100, 999, 300);

            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentDecompressedLength_ReturnsFalse()
        {
            var a = new UOFileIndex(null, 100, 200, 300);
            var b = new UOFileIndex(null, 100, 200, 999);

            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Default_IsNotValid_MatchesInvalid()
        {
            var defaultIndex = default(UOFileIndex);

            defaultIndex.File.Should().BeNull();
            defaultIndex.Offset.Should().Be(0);
            defaultIndex.Length.Should().Be(0);
            defaultIndex.DecompressedLength.Should().Be(0);
            defaultIndex.Equals(UOFileIndex.Invalid).Should().BeTrue();
        }

        [Fact]
        public void Constructor_DefaultOptionalParams_SetsZeros()
        {
            var index = new UOFileIndex(null, 10, 20, 30);

            index.CompressionFlag.Should().Be(CompressionType.None);
            index.Width.Should().Be(0);
            index.Height.Should().Be(0);
            index.Hue.Should().Be(0);
            index.AnimOffset.Should().Be(0);
        }

        [Fact]
        public void Equals_SameWidthHeightHueDifferent_StillEqualsByKeyFields()
        {
            // Equals only compares File, Offset, Length, DecompressedLength
            var a = new UOFileIndex(null, 100, 200, 300, width: 10, height: 20, hue: 30);
            var b = new UOFileIndex(null, 100, 200, 300, width: 99, height: 99, hue: 99);

            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void AnimOffset_CanBeSet()
        {
            var index = new UOFileIndex(null, 0, 0, 0);

            index.AnimOffset = -5;

            index.AnimOffset.Should().Be(-5);
        }

        [Fact]
        public void UOFileIndex5D_Constructor_SetsAllFields()
        {
            var index5d = new UOFileIndex5D(1, 2, 3, 4, 5);

            index5d.FileID.Should().Be(1u);
            index5d.BlockID.Should().Be(2u);
            index5d.Position.Should().Be(3u);
            index5d.Length.Should().Be(4u);
            index5d.GumpData.Should().Be(5u);
        }

        [Fact]
        public void UOFileIndex5D_DefaultExtra_IsZero()
        {
            var index5d = new UOFileIndex5D(1, 2, 3, 4);

            index5d.GumpData.Should().Be(0u);
        }
    }
}
