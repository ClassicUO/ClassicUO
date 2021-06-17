using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.IO;
using ClassicUO.Utility;
using Xunit;

namespace ClassicUO.UnitTests.IO
{
    public class StackDataReaderTest
    {
        [Fact]
        public unsafe void Check_For_ASCII_Garbage_Cleanup()
        {
            Span<byte> data = stackalloc byte[]
            {
                (byte) 'C',
                (byte) 'l',
                (byte) 'a',
                (byte) 's',
                (byte) 's',
                (byte) 'i',
                (byte) 'c',
                (byte) 'U',
                (byte) 'O',
                (byte) '\0'
            };


            Span<byte> dataWithGarbage = stackalloc byte[]
            {
                (byte)'C',
                (byte)'l',
                (byte)'a',
                (byte)'s',
                (byte)'s',
                (byte)'i',
                (byte)'c',
                (byte)'\0',
                (byte)'U',
                (byte)'O'
            };

            StackDataReader reader = new StackDataReader(data);
            StackDataReader reader2 = new StackDataReader(dataWithGarbage);

            var s00 = reader.ReadASCII(data.Length);
            var s01 = reader2.ReadASCII(dataWithGarbage.Length);

            Assert.Equal(s00, s01);

            var s0 = Encoding.ASCII.GetString((byte*) UnsafeMemoryManager.AsPointer(ref MemoryMarshal.GetReference(data)), data.Length);
            var s1 = Encoding.ASCII.GetString((byte*) UnsafeMemoryManager.AsPointer(ref MemoryMarshal.GetReference(dataWithGarbage)), dataWithGarbage.Length);

            Assert.NotEqual(s0, s1);

            reader2.Release();
            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "ClassicUO")]
        [InlineData("Classic\0UO", "ClassicUO")]
        [InlineData("Classic\0UO\0", "ClassicUO")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "ClassicUO")]

        public void Check_For_ASCII_With_Length_Specified(string str, string result)
        {
            Span<byte> data = Encoding.ASCII.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadASCII(str.Length);

            Assert.Equal(s, result);

            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Check_For_ASCII_Specified(string str, string result)
        {
            Span<byte> data = Encoding.ASCII.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadASCII();

            Assert.Equal(s, result);

            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "ClassicUO")]
        [InlineData("Classic\0UO", "ClassicUO")]
        [InlineData("Classic\0UO\0", "ClassicUO")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "ClassicUO")]

        public void Check_For_Unicode_With_Length_Specified_LittleEndian(string str, string result)
        {
            Span<byte> data = Encoding.Unicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUnicodeLE(str.Length);

            Assert.Equal(s, result);

            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Check_For_Unicode_Specified_LittleEndian(string str, string result)
        {
            Span<byte> data = Encoding.Unicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUnicodeLE();

            Assert.Equal(s, result);

            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "ClassicUO")]
        [InlineData("Classic\0UO", "ClassicUO")]
        [InlineData("Classic\0UO\0", "ClassicUO")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "ClassicUO")]

        public void Check_For_Unicode_With_Length_Specified_BigEndian(string str, string result)
        {
            Span<byte> data = Encoding.BigEndianUnicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUnicodeBE(str.Length);

            Assert.Equal(s, result);

            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Check_For_Unicode_Specified_BigEndian(string str, string result)
        {
            Span<byte> data = Encoding.BigEndianUnicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUnicodeBE();

            Assert.Equal(s, result);

            reader.Release();
        }
    }
}
