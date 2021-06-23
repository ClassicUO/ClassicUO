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
        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Read_ASCII_With_FixedLength(string str, string result)
        {
            Span<byte> data = Encoding.ASCII.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadASCII(str.Length);

            Assert.Equal(s, result);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Read_ASCII(string str, string result)
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
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Read_Unicode_LittleEndian_With_FixedLength(string str, string result)
        {
            Span<byte> data = Encoding.Unicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUnicodeLE(str.Length);

            Assert.Equal(s, result);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Read_Unicode_LittleEndian(string str, string result)
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
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Read_Unicode_BigEndian_With_FixedLength(string str, string result)
        {
            Span<byte> data = Encoding.BigEndianUnicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUnicodeBE(str.Length);

            Assert.Equal(s, result);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Read_Unicode_BigEndian(string str, string result)
        {
            Span<byte> data = Encoding.BigEndianUnicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUnicodeBE();

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

        public void Read_UTF8_With_FixedLength(string str, string result)
        {
            Span<byte> data = Encoding.UTF8.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUTF8(str.Length);

            Assert.Equal(s, result);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Read_UTF8(string str, string result)
        {
            Span<byte> data = Encoding.UTF8.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUTF8();

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

        public void Read_UTF8_With_FixedLength_Safe(string str, string result)
        {
            Span<byte> data = Encoding.UTF8.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUTF8(str.Length, true);

            Assert.Equal(s, result);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("ClassicUO", "ClassicUO")]
        [InlineData("ClassicUO\0", "ClassicUO")]
        [InlineData("\0ClassicUO", "")]
        [InlineData("Classic\0UO", "Classic")]
        [InlineData("Classic\0UO\0", "Classic")]
        [InlineData("Cla\0ssic\0UO\0\0\0\0\0", "Cla")]

        public void Read_UTF8_Safe(string str, string result)
        {
            Span<byte> data = Encoding.UTF8.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUTF8(true);

            Assert.Equal(s, result);

            reader.Release();
        }


        [Theory]
        [InlineData("classicuo\0abc", 3)]
        [InlineData("classicuoabc", 0)]
        [InlineData("classicuoabc\0", 0)]
        public void Check_Data_Remaining(string str, int remains)
        {
            Span<byte> data = Encoding.ASCII.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            reader.ReadASCII();
            Assert.Equal(remains, reader.Remaining);

            reader.ReadASCII();
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("classicuo\0abc", 0)]
        [InlineData("classicuoabc", 0)]
        [InlineData("classicuoabc\0", 0)]
        public void Check_Data_Remaining_FixedLength(string str, int remains)
        {
            Span<byte> data = Encoding.ASCII.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            reader.ReadASCII(str.Length);
            Assert.Equal(reader.Remaining, remains);

            reader.ReadASCII(remains);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("classicuo\0abc", 6)]
        [InlineData("classicuoabc", 0)]
        [InlineData("classicuoabc\0", 0)]
        public void Check_Data_Remaining_Unicode_BigEndian(string str, int remains)
        {
            Span<byte> data = Encoding.BigEndianUnicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            reader.ReadUnicodeBE();
            Assert.Equal(remains, reader.Remaining);

            reader.ReadUnicodeBE();
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("classicuo\0abc", 0)]
        [InlineData("classicuoabc", 0)]
        [InlineData("classicuoabc\0", 0)]
        public void Check_Data_Remaining_FixedLength_Unicode_BigEndian(string str, int remains)
        {
            Span<byte> data = Encoding.BigEndianUnicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            reader.ReadUnicodeBE(str.Length);
            Assert.Equal(reader.Remaining, remains);

            reader.ReadUnicodeBE(remains);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("classicuo\0abc", 6)]
        [InlineData("classicuoabc", 0)]
        [InlineData("classicuoabc\0", 0)]
        public void Check_Data_Remaining_Unicode_LittleEndian(string str, int remains)
        {
            Span<byte> data = Encoding.Unicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            reader.ReadUnicodeLE();
            Assert.Equal(remains, reader.Remaining);

            reader.ReadUnicodeLE();
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("classicuo\0abc", 0)]
        [InlineData("classicuoabc", 0)]
        [InlineData("classicuoabc\0", 0)]
        public void Check_Data_Remaining_FixedLength_Unicode_LittleEndian(string str, int remains)
        {
            Span<byte> data = Encoding.Unicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            reader.ReadUnicodeLE(str.Length);
            Assert.Equal(reader.Remaining, remains);

            reader.ReadUnicodeLE(remains);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("classicuo\0abc", 3)]
        [InlineData("classicuoabc", 0)]
        [InlineData("classicuoabc\0", 0)]
        public void Check_Data_Remaining_UTF8(string str, int remains)
        {
            Span<byte> data = Encoding.UTF8.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            reader.ReadUTF8();
            Assert.Equal(remains, reader.Remaining);

            reader.ReadUTF8();
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("classicuo\0abc", 0)]
        [InlineData("classicuoabc", 0)]
        [InlineData("classicuoabc\0", 0)]
        public void Check_Data_Remaining_FixedLength_UTF8(string str, int remains)
        {
            Span<byte> data = Encoding.UTF8.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            reader.ReadUTF8(str.Length);
            Assert.Equal(reader.Remaining, remains);

            reader.ReadUTF8(remains);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("classicuo\0abc", 3)]
        [InlineData("classicuoabc", 0)]
        [InlineData("classicuoabc\0", 0)]
        public void Check_Data_Remaining_Unicode_UTF8_Safe(string str, int remains)
        {
            Span<byte> data = Encoding.UTF8.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            reader.ReadUTF8(true);
            Assert.Equal(remains, reader.Remaining);

            reader.ReadUTF8(true);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }

        [Theory]
        [InlineData("classicuo\0abc", 0)]
        [InlineData("classicuoabc", 0)]
        [InlineData("classicuoabc\0", 0)]
        public void Check_Data_Remaining_FixedLength_UTF8_Safe(string str, int remains)
        {
            Span<byte> data = Encoding.UTF8.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            reader.ReadUTF8(str.Length, true);
            Assert.Equal(reader.Remaining, remains);

            reader.ReadUTF8(remains, true);
            Assert.Equal(0, reader.Remaining);

            reader.Release();
        }



        [Theory]
        [InlineData("this is a very long text", 1000)]
        public void Read_More_Data_Than_Remains_ASCII(string str, int length)
        {
            Span<byte> data = Encoding.ASCII.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadASCII(length);

            Assert.Equal(str, s);

            reader.Release();
        }

        [Theory]
        [InlineData("this is a very long text", 1000)]
        public void Read_More_Data_Than_Remains_Unicode(string str, int length)
        {
            Span<byte> data = Encoding.BigEndianUnicode.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUnicodeBE(length);

            Assert.Equal(str, s);

            reader.Release();
        }
        [Theory]
        [InlineData("this is a very long text", 1000)]
        public void Read_More_Data_Than_Remains_UTF8(string str, int length)
        {
            Span<byte> data = Encoding.UTF8.GetBytes(str);

            StackDataReader reader = new StackDataReader(data);

            string s = reader.ReadUTF8(length);

            Assert.Equal(str, s);

            reader.Release();
        }
    }
}
