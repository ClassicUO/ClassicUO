using System;
using System.IO;
using System.Text;
using ClassicUO.IO;
using FluentAssertions;
using Xunit;

namespace ClassicUO.IO.Tests
{
    public class StackDataWriterTests
    {
        [Fact]
        public void WriteUInt8_WritesSingleByte()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUInt8(0xAB);

            writer.Position.Should().Be(1);
            writer.Buffer[0].Should().Be(0xAB);

            writer.Dispose();
        }

        [Fact]
        public void WriteInt8_WritesSignedByte()
        {
            var writer = new StackDataWriter(16);

            writer.WriteInt8(-1);

            writer.Position.Should().Be(1);
            writer.Buffer[0].Should().Be(0xFF);

            writer.Dispose();
        }

        [Fact]
        public void WriteBool_True_Writes0x01()
        {
            var writer = new StackDataWriter(16);

            writer.WriteBool(true);

            writer.Position.Should().Be(1);
            writer.Buffer[0].Should().Be(0x01);

            writer.Dispose();
        }

        [Fact]
        public void WriteBool_False_Writes0x00()
        {
            var writer = new StackDataWriter(16);

            writer.WriteBool(false);

            writer.Position.Should().Be(1);
            writer.Buffer[0].Should().Be(0x00);

            writer.Dispose();
        }

        [Fact]
        public void WriteUInt16LE_WritesLittleEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUInt16LE(0x1234);

            writer.Position.Should().Be(2);
            writer.Buffer[0].Should().Be(0x34);
            writer.Buffer[1].Should().Be(0x12);

            writer.Dispose();
        }

        [Fact]
        public void WriteUInt16BE_WritesBigEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUInt16BE(0x1234);

            writer.Position.Should().Be(2);
            writer.Buffer[0].Should().Be(0x12);
            writer.Buffer[1].Should().Be(0x34);

            writer.Dispose();
        }

        [Fact]
        public void WriteInt16LE_WritesLittleEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteInt16LE(-1234);

            writer.Position.Should().Be(2);
            short readBack = BitConverter.ToInt16(writer.Buffer);
            readBack.Should().Be(-1234);

            writer.Dispose();
        }

        [Fact]
        public void WriteInt16BE_WritesBigEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteInt16BE(0x1234);

            writer.Position.Should().Be(2);
            writer.Buffer[0].Should().Be(0x12);
            writer.Buffer[1].Should().Be(0x34);

            writer.Dispose();
        }

        [Fact]
        public void WriteUInt32LE_WritesLittleEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUInt32LE(0x12345678);

            writer.Position.Should().Be(4);
            writer.Buffer[0].Should().Be(0x78);
            writer.Buffer[1].Should().Be(0x56);
            writer.Buffer[2].Should().Be(0x34);
            writer.Buffer[3].Should().Be(0x12);

            writer.Dispose();
        }

        [Fact]
        public void WriteUInt32BE_WritesBigEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUInt32BE(0x12345678);

            writer.Position.Should().Be(4);
            writer.Buffer[0].Should().Be(0x12);
            writer.Buffer[1].Should().Be(0x34);
            writer.Buffer[2].Should().Be(0x56);
            writer.Buffer[3].Should().Be(0x78);

            writer.Dispose();
        }

        [Fact]
        public void WriteInt32LE_WritesLittleEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteInt32LE(-12345);

            writer.Position.Should().Be(4);
            int readBack = BitConverter.ToInt32(writer.Buffer);
            readBack.Should().Be(-12345);

            writer.Dispose();
        }

        [Fact]
        public void WriteInt32BE_WritesBigEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteInt32BE(0x12345678);

            writer.Position.Should().Be(4);
            writer.Buffer[0].Should().Be(0x12);
            writer.Buffer[1].Should().Be(0x34);
            writer.Buffer[2].Should().Be(0x56);
            writer.Buffer[3].Should().Be(0x78);

            writer.Dispose();
        }

        [Fact]
        public void WriteUInt64LE_WritesLittleEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUInt64LE(0x0102030405060708UL);

            writer.Position.Should().Be(8);
            ulong readBack = BitConverter.ToUInt64(writer.Buffer);
            readBack.Should().Be(0x0102030405060708UL);

            writer.Dispose();
        }

        [Fact]
        public void WriteUInt64BE_WritesBigEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUInt64BE(0x0102030405060708UL);

            writer.Position.Should().Be(8);
            writer.Buffer[0].Should().Be(0x01);
            writer.Buffer[1].Should().Be(0x02);
            writer.Buffer[2].Should().Be(0x03);
            writer.Buffer[3].Should().Be(0x04);
            writer.Buffer[4].Should().Be(0x05);
            writer.Buffer[5].Should().Be(0x06);
            writer.Buffer[6].Should().Be(0x07);
            writer.Buffer[7].Should().Be(0x08);

            writer.Dispose();
        }

        [Fact]
        public void WriteInt64LE_WritesLittleEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteInt64LE(-9876543210L);

            writer.Position.Should().Be(8);
            long readBack = BitConverter.ToInt64(writer.Buffer);
            readBack.Should().Be(-9876543210L);

            writer.Dispose();
        }

        [Fact]
        public void WriteInt64BE_WritesBigEndian()
        {
            var writer = new StackDataWriter(16);

            writer.WriteInt64BE(1L);

            writer.Position.Should().Be(8);
            writer.Buffer[0].Should().Be(0x00);
            writer.Buffer[7].Should().Be(0x01);

            writer.Dispose();
        }

        [Fact]
        public void WriteASCII_WritesNullTerminatedString()
        {
            var writer = new StackDataWriter(16);

            writer.WriteASCII("Hi");

            writer.Position.Should().Be(3); // 'H', 'i', '\0'
            writer.Buffer[0].Should().Be((byte)'H');
            writer.Buffer[1].Should().Be((byte)'i');
            writer.Buffer[2].Should().Be(0x00);

            writer.Dispose();
        }

        [Fact]
        public void WriteASCII_FixedLength_PadsWithZeros()
        {
            var writer = new StackDataWriter(16);

            writer.WriteASCII("Hi", 5);

            writer.Position.Should().Be(5);
            writer.Buffer[0].Should().Be((byte)'H');
            writer.Buffer[1].Should().Be((byte)'i');
            writer.Buffer[2].Should().Be(0x00);
            writer.Buffer[3].Should().Be(0x00);
            writer.Buffer[4].Should().Be(0x00);

            writer.Dispose();
        }

        [Fact]
        public void WriteASCII_EmptyString_WritesNullTerminator()
        {
            var writer = new StackDataWriter(16);

            writer.WriteASCII("");

            writer.Position.Should().Be(1);
            writer.Buffer[0].Should().Be(0x00);

            writer.Dispose();
        }

        [Fact]
        public void WriteUnicodeLE_WritesLittleEndianUnicode()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUnicodeLE("A");

            // 'A' in UTF-16 LE = 0x41, 0x00, then null terminator 0x00, 0x00
            writer.Buffer[0].Should().Be(0x41);
            writer.Buffer[1].Should().Be(0x00);
            writer.Buffer[2].Should().Be(0x00); // null terminator
            writer.Buffer[3].Should().Be(0x00);

            writer.Dispose();
        }

        [Fact]
        public void WriteUnicodeBE_WritesBigEndianUnicode()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUnicodeBE("A");

            // 'A' in UTF-16 BE = 0x00, 0x41, then null terminator 0x00, 0x00
            writer.Buffer[0].Should().Be(0x00);
            writer.Buffer[1].Should().Be(0x41);
            writer.Buffer[2].Should().Be(0x00); // null terminator
            writer.Buffer[3].Should().Be(0x00);

            writer.Dispose();
        }

        [Fact]
        public void WriteUTF8_FixedLength_WritesCorrectly()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUTF8("Hi", 5);

            writer.Position.Should().Be(5);
            writer.Buffer[0].Should().Be((byte)'H');
            writer.Buffer[1].Should().Be((byte)'i');

            writer.Dispose();
        }

        [Fact]
        public void WriteZero_WritesNZeroBytes()
        {
            var writer = new StackDataWriter(16);
            writer.WriteUInt8(0xFF); // write a non-zero byte first

            writer.WriteZero(4);

            writer.Position.Should().Be(5);
            writer.Buffer[1].Should().Be(0x00);
            writer.Buffer[2].Should().Be(0x00);
            writer.Buffer[3].Should().Be(0x00);
            writer.Buffer[4].Should().Be(0x00);

            writer.Dispose();
        }

        [Fact]
        public void WriteZero_ZeroCount_DoesNothing()
        {
            var writer = new StackDataWriter(16);

            writer.WriteZero(0);

            writer.Position.Should().Be(0);

            writer.Dispose();
        }

        [Fact]
        public void Write_ReadOnlySpanByte_WritesCorrectly()
        {
            var writer = new StackDataWriter(16);
            byte[] data = { 0x01, 0x02, 0x03 };

            writer.Write(data);

            writer.Position.Should().Be(3);
            writer.Buffer[0].Should().Be(0x01);
            writer.Buffer[1].Should().Be(0x02);
            writer.Buffer[2].Should().Be(0x03);

            writer.Dispose();
        }

        [Fact]
        public void Position_TracksCorrectly_AfterMultipleWrites()
        {
            var writer = new StackDataWriter(32);

            writer.Position.Should().Be(0);

            writer.WriteUInt8(0x01);
            writer.Position.Should().Be(1);

            writer.WriteUInt16LE(0x1234);
            writer.Position.Should().Be(3);

            writer.WriteUInt32LE(0x12345678);
            writer.Position.Should().Be(7);

            writer.Dispose();
        }

        [Fact]
        public void Buffer_ContainsWrittenData()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUInt8(0xAA);
            writer.WriteUInt8(0xBB);
            writer.WriteUInt8(0xCC);

            ReadOnlySpan<byte> buffer = writer.Buffer;
            buffer.Length.Should().Be(3);
            buffer[0].Should().Be(0xAA);
            buffer[1].Should().Be(0xBB);
            buffer[2].Should().Be(0xCC);

            writer.Dispose();
        }

        [Fact]
        public void Seek_Begin_ChangesPosition()
        {
            var writer = new StackDataWriter(16);
            writer.WriteUInt8(0x01);
            writer.WriteUInt8(0x02);
            writer.WriteUInt8(0x03);

            writer.Seek(1, SeekOrigin.Begin);

            writer.Position.Should().Be(1);

            writer.Dispose();
        }

        [Fact]
        public void Seek_Current_OffsetsFromCurrentPosition()
        {
            var writer = new StackDataWriter(16);
            writer.WriteUInt8(0x01);
            writer.WriteUInt8(0x02);

            writer.Seek(2, SeekOrigin.Current);

            writer.Position.Should().Be(4);

            writer.Dispose();
        }

        [Fact]
        public void Seek_End_OffsetsFromBytesWritten()
        {
            var writer = new StackDataWriter(16);
            writer.WriteUInt8(0x01);
            writer.WriteUInt8(0x02);
            writer.WriteUInt8(0x03);

            writer.Seek(-1, SeekOrigin.End);

            writer.Position.Should().Be(2);

            writer.Dispose();
        }

        [Fact]
        public void Dispose_WorksWithoutError()
        {
            var writer = new StackDataWriter(16);
            writer.WriteUInt8(0x01);

            // Should not throw
            writer.Dispose();
        }

        [Fact]
        public void Dispose_OnDefault_DoesNotThrow()
        {
            var writer = new StackDataWriter();

            // Dispose on default should not throw
            writer.Dispose();
        }

        [Fact]
        public void AutoGrowth_WhenCapacityExceeded()
        {
            var writer = new StackDataWriter(4);

            // Write more than initial capacity
            writer.WriteUInt64LE(0x0102030405060708UL);
            writer.WriteUInt64LE(0x090A0B0C0D0E0F10UL);

            writer.Position.Should().Be(16);
            ulong first = BitConverter.ToUInt64(writer.Buffer);
            first.Should().Be(0x0102030405060708UL);

            writer.Dispose();
        }

        [Fact]
        public void BytesWritten_TracksHighWaterMark()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUInt8(0x01);
            writer.WriteUInt8(0x02);
            writer.WriteUInt8(0x03);

            writer.BytesWritten.Should().Be(3);

            // Seek back and write doesn't reduce BytesWritten
            writer.Seek(0, SeekOrigin.Begin);
            writer.WriteUInt8(0xFF);

            writer.BytesWritten.Should().Be(3);

            writer.Dispose();
        }

        [Fact]
        public void WriteAndRead_RoundTrip()
        {
            var writer = new StackDataWriter(32);
            writer.WriteUInt8(0xAA);
            writer.WriteUInt16LE(0x1234);
            writer.WriteUInt32LE(0xDEADBEEF);

            var reader = new StackDataReader(writer.Buffer);

            reader.ReadUInt8().Should().Be(0xAA);
            reader.ReadUInt16LE().Should().Be(0x1234);
            reader.ReadUInt32LE().Should().Be(0xDEADBEEF);

            writer.Dispose();
        }

        [Fact]
        public void BufferWritten_ReturnsSliceUpToBytesWritten()
        {
            var writer = new StackDataWriter(16);

            writer.WriteUInt8(0xAA);
            writer.WriteUInt8(0xBB);

            Span<byte> written = writer.BufferWritten;
            written.Length.Should().Be(2);
            written[0].Should().Be(0xAA);
            written[1].Should().Be(0xBB);

            writer.Dispose();
        }

        [Fact]
        public void WriteUnicodeLE_FixedLength_PadsWithZeros()
        {
            var writer = new StackDataWriter(32);

            writer.WriteUnicodeLE("A", 3);

            // 'A' takes 2 bytes in UTF-16 LE, then padded to 3 chars = 6 bytes
            writer.Position.Should().Be(6);

            writer.Dispose();
        }

        [Fact]
        public void WriteUnicodeBE_FixedLength_PadsWithZeros()
        {
            var writer = new StackDataWriter(32);

            writer.WriteUnicodeBE("A", 3);

            writer.Position.Should().Be(6);

            writer.Dispose();
        }
    }
}
