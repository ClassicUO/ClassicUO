using System;
using FluentAssertions;
using Xunit;
using ZLibNative;

namespace ClassicUO.Utility.Tests.ZLib
{
    public class ZLibHeaderTests
    {
        [Fact]
        public void DecodeHeader_ValidDeflateStream_SetsPropertiesCorrectly()
        {
            // CMF = 0x78 (CompressionInfo=7, CompressionMethod=8)
            // FLG = 0x9C (Default compression)
            var header = ZLibHeader.DecodeHeader(0x78, 0x9C);

            header.CompressionMethod.Should().Be(8);
            header.CompressionInfo.Should().Be(7);
            header.IsSupportedZLibStream.Should().BeTrue();
        }

        [Fact]
        public void DecodeHeader_ValidStream_FDictIsFalse()
        {
            var header = ZLibHeader.DecodeHeader(0x78, 0x9C);

            header.FDict.Should().BeFalse();
        }

        [Fact]
        public void IsSupportedZLibStream_ForValidDeflateStream_IsTrue()
        {
            // 0x78 0x01 is another valid combination (fastest compression)
            var header = ZLibHeader.DecodeHeader(0x78, 0x01);

            header.IsSupportedZLibStream.Should().BeTrue();
            header.CompressionMethod.Should().Be(8);
        }

        [Fact]
        public void IsSupportedZLibStream_ForInvalidMethod_IsFalse()
        {
            // CMF with method=5 instead of 8
            var header = ZLibHeader.DecodeHeader(0x75, 0x00);

            header.CompressionMethod.Should().Be(5);
            header.IsSupportedZLibStream.Should().BeFalse();
        }

        [Fact]
        public void CompressionMethod_ShouldBe8_ForDeflate()
        {
            var header = ZLibHeader.DecodeHeader(0x78, 0x9C);

            header.CompressionMethod.Should().Be(8);
        }

        [Fact]
        public void EncodeZlibHeader_RoundTrips()
        {
            var original = new ZLibHeader
            {
                CompressionMethod = 8,
                CompressionInfo = 7,
                FDict = false,
                FLevel = FLevel.Default
            };

            byte[] encoded = original.EncodeZlibHeader();

            encoded.Should().HaveCount(2);

            var decoded = ZLibHeader.DecodeHeader(encoded[0], encoded[1]);

            decoded.CompressionMethod.Should().Be(original.CompressionMethod);
            decoded.CompressionInfo.Should().Be(original.CompressionInfo);
            decoded.FDict.Should().Be(original.FDict);
            decoded.FLevel.Should().Be(original.FLevel);
            decoded.IsSupportedZLibStream.Should().BeTrue();
        }

        [Fact]
        public void EncodeZlibHeader_ProducesValidChecksum()
        {
            var header = new ZLibHeader
            {
                CompressionMethod = 8,
                CompressionInfo = 7,
                FDict = false,
                FLevel = FLevel.Default
            };

            byte[] encoded = header.EncodeZlibHeader();

            // The two-byte header must satisfy (CMF * 256 + FLG) % 31 == 0
            int check = (encoded[0] * 256 + encoded[1]) % 31;
            check.Should().Be(0);
        }

        [Fact]
        public void DecodeHeader_MasksToBytes()
        {
            // Pass values larger than a byte; the method should mask them
            var header = ZLibHeader.DecodeHeader(0x178, 0x19C);

            header.CompressionMethod.Should().Be(8);
            header.CompressionInfo.Should().Be(7);
        }

        [Fact]
        public void CompressionMethod_GreaterThan15_ThrowsArgumentOutOfRange()
        {
            var header = new ZLibHeader();

            var act = () => header.CompressionMethod = 16;

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void CompressionInfo_GreaterThan15_ThrowsArgumentOutOfRange()
        {
            var header = new ZLibHeader();

            var act = () => header.CompressionInfo = 16;

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void FCheck_GreaterThan31_ThrowsArgumentOutOfRange()
        {
            var header = new ZLibHeader();

            var act = () => header.FCheck = 32;

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void FLevel_Values_DecodedCorrectly()
        {
            // 0x78 0x01: Faster
            var faster = ZLibHeader.DecodeHeader(0x78, 0x01);
            faster.FLevel.Should().Be(FLevel.Faster);

            // 0x78 0x9C: Default
            var defaultLevel = ZLibHeader.DecodeHeader(0x78, 0x9C);
            defaultLevel.FLevel.Should().Be(FLevel.Default);

            // 0x78 0xDA: Optimal
            var optimal = ZLibHeader.DecodeHeader(0x78, 0xDA);
            optimal.FLevel.Should().Be(FLevel.Optimal);
        }
    }
}
