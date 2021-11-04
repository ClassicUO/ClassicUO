using ClassicUO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
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

            Assert.True(MemoryMarshal.AsBytes(span).SequenceEqual(writer.Buffer.Slice(0, writer.BytesWritten)));

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

            Assert.True(MemoryMarshal.AsBytes(span).SequenceEqual(writer.Buffer.Slice(0, writer.BytesWritten)));

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

            Assert.True(MemoryMarshal.AsBytes(span).SequenceEqual(writer.Buffer.Slice(0, writer.BytesWritten)));

            writer.Dispose();
        }

        [Fact]
        public void Write_Ascii_String_No_Fixed_size()
        {
            StackDataWriter writer = new StackDataWriter(32);

            string str = new string('a', 128);

            writer.WriteASCII(str);
            str += '\0'; //WriteASCII with no length null-terminates by design, even if the string already has a null at the end, so we have to append it here.
            
            Span<char> span = stackalloc char[str.Length + 1]; // '\0'
            str.AsSpan().CopyTo(span);
            byte[] cp1252Bytes = ClassicUO.Utility.StringHelper.StringToCp1252Bytes(str);

            var writtenBytes = writer.Buffer.Slice(0, writer.BytesWritten);
            
            writtenBytes.ToArray().Should().Equal(cp1252Bytes);

            writer.Dispose();
        }

        [Fact]
        public void Write_Ascii_String_Greater_Fixed_Size_Than_Real_String()
        {
            StackDataWriter writer = new StackDataWriter(32);

            string str = "aaaa";
            int size = 256;

            writer.WriteASCII(str, size);

            Span<byte> span = stackalloc byte[size];
            byte[] cp1252Bytes = ClassicUO.Utility.StringHelper.StringToCp1252Bytes(str);
            cp1252Bytes.CopyTo(span);

            var writtenBytes = writer.Buffer.Slice(0, writer.BytesWritten);
            var byteArray = writtenBytes.ToArray();
            byteArray.Should().Equal(span.ToArray());

            writer.Dispose();
        }

        [Fact]
        public void Write_Ascii_String_Lesser_Fixed_Size_Than_Real_String()
        {
            StackDataWriter writer = new StackDataWriter(32);

            string str = new string('a', 256);
            int size = 192;

            writer.WriteASCII(str, size);

            Span<char> span = stackalloc char[size];
            str.AsSpan(0, size).CopyTo(span);

            byte[] cp1252Bytes = ClassicUO.Utility.StringHelper.StringToCp1252Bytes(str);

            var writtenBytes = writer.Buffer.Slice(0, writer.BytesWritten);
            var byteArray = writtenBytes.ToArray();
            
            byteArray.Should().BeSubsetOf(cp1252Bytes);
            byteArray.Should().HaveCount(size);
            

            writer.Dispose();
        }
    }
}
