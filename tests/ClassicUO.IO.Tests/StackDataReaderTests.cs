using System;
using System.Text;
using ClassicUO.IO;
using FluentAssertions;
using Xunit;

namespace ClassicUO.IO.Tests
{
    public class StackDataReaderTests
    {
        [Fact]
        public void ReadUInt8_ReadsSingleByte()
        {
            byte[] data = { 0xAB };
            var reader = new StackDataReader(data);

            byte result = reader.ReadUInt8();

            result.Should().Be(0xAB);
        }

        [Fact]
        public void ReadInt8_ReadsSignedByte()
        {
            byte[] data = { 0xFF }; // -1 as sbyte
            var reader = new StackDataReader(data);

            sbyte result = reader.ReadInt8();

            result.Should().Be(-1);
        }

        [Fact]
        public void ReadInt8_ReadsPositiveValue()
        {
            byte[] data = { 0x7F }; // 127
            var reader = new StackDataReader(data);

            sbyte result = reader.ReadInt8();

            result.Should().Be(127);
        }

        [Fact]
        public void ReadBool_ReturnsTrue_ForNonZero()
        {
            byte[] data = { 0x01 };
            var reader = new StackDataReader(data);

            bool result = reader.ReadBool();

            result.Should().BeTrue();
        }

        [Fact]
        public void ReadBool_ReturnsFalse_ForZero()
        {
            byte[] data = { 0x00 };
            var reader = new StackDataReader(data);

            bool result = reader.ReadBool();

            result.Should().BeFalse();
        }

        [Fact]
        public void ReadUInt16LE_ReadsLittleEndian()
        {
            byte[] data = { 0x34, 0x12 }; // 0x1234 in LE
            var reader = new StackDataReader(data);

            ushort result = reader.ReadUInt16LE();

            result.Should().Be(0x1234);
        }

        [Fact]
        public void ReadUInt16BE_ReadsBigEndian()
        {
            byte[] data = { 0x12, 0x34 }; // 0x1234 in BE
            var reader = new StackDataReader(data);

            ushort result = reader.ReadUInt16BE();

            result.Should().Be(0x1234);
        }

        [Fact]
        public void ReadUInt32LE_ReadsLittleEndian()
        {
            byte[] data = { 0x78, 0x56, 0x34, 0x12 }; // 0x12345678 in LE
            var reader = new StackDataReader(data);

            uint result = reader.ReadUInt32LE();

            result.Should().Be(0x12345678);
        }

        [Fact]
        public void ReadUInt32BE_ReadsBigEndian()
        {
            byte[] data = { 0x12, 0x34, 0x56, 0x78 }; // 0x12345678 in BE
            var reader = new StackDataReader(data);

            uint result = reader.ReadUInt32BE();

            result.Should().Be(0x12345678);
        }

        [Fact]
        public void ReadInt32LE_ReadsSignedLittleEndian()
        {
            byte[] data = new byte[4];
            BitConverter.TryWriteBytes(data, -12345);
            var reader = new StackDataReader(data);

            int result = reader.ReadInt32LE();

            result.Should().Be(-12345);
        }

        [Fact]
        public void ReadUInt64LE_ReadsLittleEndian()
        {
            byte[] data = new byte[8];
            BitConverter.TryWriteBytes(data, 0x123456789ABCDEF0UL);
            var reader = new StackDataReader(data);

            ulong result = reader.ReadUInt64LE();

            result.Should().Be(0x123456789ABCDEF0UL);
        }

        [Fact]
        public void ReadUInt64BE_ReadsBigEndian()
        {
            byte[] data = { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
            var reader = new StackDataReader(data);

            ulong result = reader.ReadUInt64BE();

            result.Should().Be(0x123456789ABCDEF0UL);
        }

        [Fact]
        public void ReadInt64LE_ReadsSignedLittleEndian()
        {
            byte[] data = new byte[8];
            BitConverter.TryWriteBytes(data, -9876543210L);
            var reader = new StackDataReader(data);

            long result = reader.ReadInt64LE();

            result.Should().Be(-9876543210L);
        }

        [Fact]
        public void ReadASCII_NullTerminated_ReadsUntilNull()
        {
            byte[] data = { 0x48, 0x69, 0x00, 0x58 }; // "Hi\0X"
            var reader = new StackDataReader(data);

            string result = reader.ReadASCII();

            result.Should().Be("Hi");
        }

        [Fact]
        public void ReadASCII_FixedLength_ReadsExactBytes()
        {
            byte[] data = { 0x41, 0x42, 0x43, 0x44, 0x45 }; // "ABCDE"
            var reader = new StackDataReader(data);

            string result = reader.ReadASCII(3);

            result.Should().Be("ABC");
        }

        [Fact]
        public void ReadASCII_FixedLength_WithNullInMiddle_ReadsUpToNull()
        {
            byte[] data = { 0x41, 0x00, 0x43, 0x44 }; // "A\0CD"
            var reader = new StackDataReader(data);

            string result = reader.ReadASCII(4);

            result.Should().Be("A");
        }

        [Fact]
        public void ReadUnicodeLE_ReadsUnicodeString()
        {
            string expected = "Hi";
            byte[] data = new byte[6]; // 2 chars * 2 bytes + 2 null bytes
            Encoding.Unicode.GetBytes(expected, 0, expected.Length, data, 0);
            // null terminator
            data[4] = 0;
            data[5] = 0;
            var reader = new StackDataReader(data);

            string result = reader.ReadUnicodeLE();

            result.Should().Be("Hi");
        }

        [Fact]
        public void ReadUnicodeBE_ReadsBigEndianUnicode()
        {
            string expected = "Hi";
            byte[] data = new byte[6];
            Encoding.BigEndianUnicode.GetBytes(expected, 0, expected.Length, data, 0);
            data[4] = 0;
            data[5] = 0;
            var reader = new StackDataReader(data);

            string result = reader.ReadUnicodeBE();

            result.Should().Be("Hi");
        }

        [Fact]
        public void ReadUTF8_ReadsUtf8String()
        {
            byte[] data = { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00 }; // "Hello\0"
            var reader = new StackDataReader(data);

            string result = reader.ReadUTF8();

            result.Should().Be("Hello");
        }

        [Fact]
        public void Position_AdvancesCorrectly_AfterReads()
        {
            byte[] data = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
            var reader = new StackDataReader(data);

            reader.Position.Should().Be(0);

            reader.ReadUInt8();
            reader.Position.Should().Be(1);

            reader.ReadUInt16LE();
            reader.Position.Should().Be(3);

            reader.ReadUInt32LE();
            reader.Position.Should().Be(7);
        }

        [Fact]
        public void Remaining_DecreasesCorrectly()
        {
            byte[] data = { 0x01, 0x02, 0x03, 0x04 };
            var reader = new StackDataReader(data);

            reader.Remaining.Should().Be(4);

            reader.ReadUInt8();
            reader.Remaining.Should().Be(3);

            reader.ReadUInt16LE();
            reader.Remaining.Should().Be(1);
        }

        [Fact]
        public void Seek_SetsPosition()
        {
            byte[] data = { 0x01, 0x02, 0x03, 0x04 };
            var reader = new StackDataReader(data);

            reader.Seek(2);

            reader.Position.Should().Be(2);
            reader.ReadUInt8().Should().Be(0x03);
        }

        [Fact]
        public void Skip_AdvancesPosition()
        {
            byte[] data = { 0x01, 0x02, 0x03, 0x04 };
            var reader = new StackDataReader(data);

            reader.Skip(2);

            reader.Position.Should().Be(2);
            reader.ReadUInt8().Should().Be(0x03);
        }

        [Fact]
        public void ReadArray_ReturnsCorrectBytes()
        {
            byte[] data = { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var reader = new StackDataReader(data);

            byte[] result = reader.ReadArray(3);

            result.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
            reader.Position.Should().Be(3);
        }

        [Fact]
        public void ReadArray_PastEnd_ReturnsEmptyArray()
        {
            byte[] data = { 0x01, 0x02 };
            var reader = new StackDataReader(data);

            byte[] result = reader.ReadArray(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public void Length_ReturnsBufferLength()
        {
            byte[] data = { 0x01, 0x02, 0x03 };
            var reader = new StackDataReader(data);

            reader.Length.Should().Be(3);
        }

        [Fact]
        public void ReadUInt8_PastEnd_ReturnsZero()
        {
            byte[] data = { 0x01 };
            var reader = new StackDataReader(data);

            reader.ReadUInt8(); // consume the only byte
            byte result = reader.ReadUInt8(); // past end

            result.Should().Be(0);
        }

        [Fact]
        public void ReadUInt16LE_PastEnd_ReturnsZero()
        {
            byte[] data = { 0x01 }; // only 1 byte, need 2
            var reader = new StackDataReader(data);

            ushort result = reader.ReadUInt16LE();

            result.Should().Be(0);
        }

        [Fact]
        public void ReadUInt32LE_PastEnd_ReturnsZero()
        {
            byte[] data = { 0x01, 0x02 }; // only 2 bytes, need 4
            var reader = new StackDataReader(data);

            uint result = reader.ReadUInt32LE();

            result.Should().Be(0u);
        }

        [Fact]
        public void ReadUInt64LE_PastEnd_ReturnsZero()
        {
            byte[] data = { 0x01, 0x02, 0x03, 0x04 }; // only 4 bytes, need 8
            var reader = new StackDataReader(data);

            ulong result = reader.ReadUInt64LE();

            result.Should().Be(0UL);
        }

        [Fact]
        public void ReadInt8_PastEnd_ReturnsZero()
        {
            byte[] data = Array.Empty<byte>();
            var reader = new StackDataReader(data);

            sbyte result = reader.ReadInt8();

            result.Should().Be(0);
        }

        [Fact]
        public void ReadInt16LE_ReadsCorrectly()
        {
            byte[] data = new byte[2];
            BitConverter.TryWriteBytes(data, (short)-1234);
            var reader = new StackDataReader(data);

            short result = reader.ReadInt16LE();

            result.Should().Be(-1234);
        }

        [Fact]
        public void ReadInt16BE_ReadsCorrectly()
        {
            // 0x1234 as big endian short
            byte[] data = { 0x12, 0x34 };
            var reader = new StackDataReader(data);

            short result = reader.ReadInt16BE();

            result.Should().Be(0x1234);
        }

        [Fact]
        public void ReadInt32BE_ReadsCorrectly()
        {
            byte[] data = { 0x12, 0x34, 0x56, 0x78 };
            var reader = new StackDataReader(data);

            int result = reader.ReadInt32BE();

            result.Should().Be(0x12345678);
        }

        [Fact]
        public void ReadInt64BE_ReadsCorrectly()
        {
            byte[] data = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
            var reader = new StackDataReader(data);

            long result = reader.ReadInt64BE();

            result.Should().Be(1L);
        }

        [Fact]
        public void Read_GenericInt_ReadsCorrectly()
        {
            byte[] data = new byte[4];
            BitConverter.TryWriteBytes(data, 42);
            var reader = new StackDataReader(data);

            int result = reader.Read<int>();

            result.Should().Be(42);
        }

        [Fact]
        public void Read_GenericFloat_ReadsCorrectly()
        {
            byte[] data = new byte[4];
            BitConverter.TryWriteBytes(data, 3.14f);
            var reader = new StackDataReader(data);

            float result = reader.Read<float>();

            result.Should().BeApproximately(3.14f, 0.001f);
        }

        [Fact]
        public void Indexer_ReturnsCorrectByte()
        {
            byte[] data = { 0xAA, 0xBB, 0xCC };
            var reader = new StackDataReader(data);

            reader[0].Should().Be(0xAA);
            reader[1].Should().Be(0xBB);
            reader[2].Should().Be(0xCC);
        }

        [Fact]
        public void Buffer_ReturnsOriginalData()
        {
            byte[] data = { 0x01, 0x02, 0x03 };
            var reader = new StackDataReader(data);

            reader.Buffer.ToArray().Should().BeEquivalentTo(data);
        }

        [Fact]
        public void Release_DoesNotThrow()
        {
            byte[] data = { 0x01 };
            var reader = new StackDataReader(data);

            // Release is a no-op currently but should not throw
            reader.Release();
        }

        [Fact]
        public void MultipleSequentialReads_AdvancePositionCorrectly()
        {
            byte[] data = new byte[15];
            data[0] = 0xFF;                                     // uint8
            BitConverter.TryWriteBytes(data.AsSpan(1), (ushort)0x1234); // uint16 LE
            BitConverter.TryWriteBytes(data.AsSpan(3), 0x56789ABCu);    // uint32 LE
            BitConverter.TryWriteBytes(data.AsSpan(7), 0x0102030405060708UL); // uint64 LE

            var reader = new StackDataReader(data);

            reader.ReadUInt8().Should().Be(0xFF);
            reader.Position.Should().Be(1);

            reader.ReadUInt16LE().Should().Be(0x1234);
            reader.Position.Should().Be(3);

            reader.ReadUInt32LE().Should().Be(0x56789ABCu);
            reader.Position.Should().Be(7);

            reader.ReadUInt64LE().Should().Be(0x0102030405060708UL);
            reader.Position.Should().Be(15);
        }

        [Fact]
        public void ReadASCII_EmptyString_ReturnsEmpty()
        {
            byte[] data = { 0x00 }; // null terminator immediately
            var reader = new StackDataReader(data);

            string result = reader.ReadASCII();

            result.Should().BeEmpty();
        }

        [Fact]
        public void ReadUTF8_FixedLength_ReadsCorrectly()
        {
            byte[] data = Encoding.UTF8.GetBytes("Hello World");
            var reader = new StackDataReader(data);

            string result = reader.ReadUTF8(5);

            result.Should().Be("Hello");
        }

        [Fact]
        public void Read_IntoSpan_CopiesCorrectBytes()
        {
            byte[] data = { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var reader = new StackDataReader(data);

            byte[] bufferArr = new byte[3];
            int bytesRead = reader.Read(bufferArr);

            bytesRead.Should().Be(3);
            bufferArr[0].Should().Be(0x01);
            bufferArr[1].Should().Be(0x02);
            bufferArr[2].Should().Be(0x03);
            reader.Position.Should().Be(3);
        }

        [Fact]
        public void Read_IntoSpan_PastEnd_ReturnsNegativeOne()
        {
            byte[] data = { 0x01 };
            var reader = new StackDataReader(data);

            byte[] bufferArr = new byte[5];
            int bytesRead = reader.Read(bufferArr);

            bytesRead.Should().Be(-1);
        }

        [Fact]
        public void Constructor_EmptySpan_HasZeroLength()
        {
            var reader = new StackDataReader(ReadOnlySpan<byte>.Empty);

            reader.Length.Should().Be(0);
            reader.Position.Should().Be(0);
            reader.Remaining.Should().Be(0);
        }
    }
}
