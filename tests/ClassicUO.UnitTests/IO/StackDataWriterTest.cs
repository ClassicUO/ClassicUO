using ClassicUO.IO;
using FluentAssertions;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace ClassicUO.UnitTests.IO
{
    public class StackDataWriterTest
    {
        [Fact]
        public void Write_BigEndian_String_No_Fixed_Size()
        {
            StackDataWriter writer = new StackDataWriter(32);

            string str = new string('a', 128);

            if (BitConverter.IsLittleEndian)
            {
                writer.WriteUnicodeLE(str);
            }
            else
            {
                writer.WriteUnicodeBE(str);
            }

            Span<char> span = stackalloc char[str.Length + 1]; // '\0'
            str.AsSpan().CopyTo(span);

            MemoryMarshal.AsBytes(span).SequenceEqual(writer.Buffer.Slice(0, writer.BytesWritten))
                .Should().BeTrue();

            writer.Dispose();
        }

        [Fact]
        public void Write_BigEndian_String_Greater_Fixed_Size_Than_RealString()
        {
            StackDataWriter writer = new StackDataWriter(32);

            string str = "aaaa";
            int size = 256;

            if (BitConverter.IsLittleEndian)
            {
                writer.WriteUnicodeLE(str, size);
            }
            else
            {
                writer.WriteUnicodeBE(str, size);
            }

            Span<char> span = stackalloc char[size];
            str.AsSpan().CopyTo(span);

            MemoryMarshal.AsBytes(span).SequenceEqual(writer.Buffer.Slice(0, writer.BytesWritten))
                .Should().BeTrue();

            writer.Dispose();
        }

        [Fact]
        public void Write_BigEndian_String_Less_Fixed_Size_Than_RealString()
        {
            StackDataWriter writer = new StackDataWriter(32);

            string str = new string('a', 255);
            int size = 239;

            if (BitConverter.IsLittleEndian)
            {
                writer.WriteUnicodeLE(str, size);
            }
            else
            {
                writer.WriteUnicodeBE(str, size);
            }

            Span<char> span = stackalloc char[size];
            str.AsSpan(0, size).CopyTo(span);

            MemoryMarshal.AsBytes(span).SequenceEqual(writer.Buffer.Slice(0, writer.BytesWritten))
                .Should().BeTrue();

            writer.Dispose();
        }

        [Theory]
        [InlineData("ClassicUO", new byte[] { 0x43, 0x6C, 0x61, 0x73, 0x73, 0x69, 0x63, 0x55, 0x4F, 0x00 })]
        [InlineData("ÀÁÂÃÄÅ", new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0x00 })]
        public void Write_CP1252String(string a, byte[] b)
        {
            StackDataWriter writer = new StackDataWriter();

            writer.WriteASCII(a);

            b.AsSpan().SequenceEqual(writer.Buffer.Slice(0, writer.BytesWritten))
                .Should().BeTrue();

            writer.Dispose();
        }

        [Theory]
        [InlineData((byte)0x00)]
        [InlineData((byte)0x7F)]
        [InlineData((byte)0xFF)]
        public void WriteUInt8_WritesCorrectByte(byte value)
        {
            StackDataWriter writer = new StackDataWriter(8);

            writer.WriteUInt8(value);

            writer.BytesWritten.Should().Be(1);
            writer.Buffer[0].Should().Be(value);

            writer.Dispose();
        }

        [Theory]
        [InlineData(true, (byte)0x01)]
        [InlineData(false, (byte)0x00)]
        public void WriteBool_WritesCorrectByte(bool value, byte expected)
        {
            StackDataWriter writer = new StackDataWriter(8);

            writer.WriteBool(value);

            writer.BytesWritten.Should().Be(1);
            writer.Buffer[0].Should().Be(expected);

            writer.Dispose();
        }

        [Theory]
        [InlineData((ushort)0x0102)]
        [InlineData((ushort)0xFF00)]
        [InlineData((ushort)0x0000)]
        public void WriteUInt16LE_WritesLittleEndian(ushort value)
        {
            StackDataWriter writer = new StackDataWriter(8);

            writer.WriteUInt16LE(value);

            writer.BytesWritten.Should().Be(2);
            writer.Buffer[0].Should().Be((byte)(value & 0xFF));
            writer.Buffer[1].Should().Be((byte)(value >> 8));

            writer.Dispose();
        }

        [Theory]
        [InlineData((ushort)0x0102)]
        [InlineData((ushort)0xFF00)]
        [InlineData((ushort)0x0000)]
        public void WriteUInt16BE_WritesBigEndian(ushort value)
        {
            StackDataWriter writer = new StackDataWriter(8);

            writer.WriteUInt16BE(value);

            writer.BytesWritten.Should().Be(2);
            writer.Buffer[0].Should().Be((byte)(value >> 8));
            writer.Buffer[1].Should().Be((byte)(value & 0xFF));

            writer.Dispose();
        }

        [Theory]
        [InlineData(0x01020304u)]
        [InlineData(0xFF000000u)]
        [InlineData(0x00000000u)]
        public void WriteUInt32LE_WritesLittleEndian(uint value)
        {
            StackDataWriter writer = new StackDataWriter(8);

            writer.WriteUInt32LE(value);

            writer.BytesWritten.Should().Be(4);
            writer.Buffer[0].Should().Be((byte)(value & 0xFF));
            writer.Buffer[1].Should().Be((byte)((value >> 8) & 0xFF));
            writer.Buffer[2].Should().Be((byte)((value >> 16) & 0xFF));
            writer.Buffer[3].Should().Be((byte)((value >> 24) & 0xFF));

            writer.Dispose();
        }

        [Theory]
        [InlineData(0x01020304u)]
        [InlineData(0xFF000000u)]
        [InlineData(0x00000000u)]
        public void WriteUInt32BE_WritesBigEndian(uint value)
        {
            StackDataWriter writer = new StackDataWriter(8);

            writer.WriteUInt32BE(value);

            writer.BytesWritten.Should().Be(4);
            writer.Buffer[0].Should().Be((byte)((value >> 24) & 0xFF));
            writer.Buffer[1].Should().Be((byte)((value >> 16) & 0xFF));
            writer.Buffer[2].Should().Be((byte)((value >> 8) & 0xFF));
            writer.Buffer[3].Should().Be((byte)(value & 0xFF));

            writer.Dispose();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(16)]
        public void WriteZero_WritesCorrectNumberOfZeros(int count)
        {
            StackDataWriter writer = new StackDataWriter(32);

            writer.WriteZero(count);

            writer.BytesWritten.Should().Be(count);
            for (int i = 0; i < count; i++)
            {
                writer.Buffer[i].Should().Be(0);
            }

            writer.Dispose();
        }

        [Fact]
        public void WriteASCII_EmptyString_WritesOnlyNullTerminator()
        {
            StackDataWriter writer = new StackDataWriter(8);

            writer.WriteASCII("");

            writer.BytesWritten.Should().Be(1);
            writer.Buffer[0].Should().Be(0x00);

            writer.Dispose();
        }

        [Fact]
        public void WriteASCII_FixedLength_PadsWithZeros()
        {
            StackDataWriter writer = new StackDataWriter(32);

            writer.WriteASCII("AB", 5);

            writer.BytesWritten.Should().Be(5);
            writer.Buffer[0].Should().Be(0x41); // 'A'
            writer.Buffer[1].Should().Be(0x42); // 'B'
            writer.Buffer[2].Should().Be(0x00);
            writer.Buffer[3].Should().Be(0x00);
            writer.Buffer[4].Should().Be(0x00);

            writer.Dispose();
        }

        [Theory]
        [InlineData((ushort)0x1234)]
        [InlineData((ushort)0x0000)]
        [InlineData((ushort)0xFFFF)]
        public void RoundTrip_UInt16LE(ushort value)
        {
            StackDataWriter writer = new StackDataWriter(8);
            writer.WriteUInt16LE(value);

            var reader = new StackDataReader(writer.Buffer);
            reader.ReadUInt16LE().Should().Be(value);

            writer.Dispose();
        }

        [Theory]
        [InlineData(0x12345678u)]
        [InlineData(0x00000000u)]
        [InlineData(0xFFFFFFFFu)]
        public void RoundTrip_UInt32LE(uint value)
        {
            StackDataWriter writer = new StackDataWriter(8);
            writer.WriteUInt32LE(value);

            var reader = new StackDataReader(writer.Buffer);
            reader.ReadUInt32LE().Should().Be(value);

            writer.Dispose();
        }

        [Theory]
        [InlineData((ushort)0x1234)]
        [InlineData((ushort)0x0000)]
        [InlineData((ushort)0xFFFF)]
        public void RoundTrip_UInt16BE(ushort value)
        {
            StackDataWriter writer = new StackDataWriter(8);
            writer.WriteUInt16BE(value);

            var reader = new StackDataReader(writer.Buffer);
            reader.ReadUInt16BE().Should().Be(value);

            writer.Dispose();
        }

        [Theory]
        [InlineData(0x12345678u)]
        [InlineData(0x00000000u)]
        [InlineData(0xFFFFFFFFu)]
        public void RoundTrip_UInt32BE(uint value)
        {
            StackDataWriter writer = new StackDataWriter(8);
            writer.WriteUInt32BE(value);

            var reader = new StackDataReader(writer.Buffer);
            reader.ReadUInt32BE().Should().Be(value);

            writer.Dispose();
        }
    }
}
