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
        public unsafe void Check_For_ASCII_Garbage()
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

            StackDataReader reader = new StackDataReader((byte*)UnsafeMemoryManager.AsPointer(ref MemoryMarshal.GetReference(data)), data.Length);
            StackDataReader reader2 = new StackDataReader((byte*)UnsafeMemoryManager.AsPointer(ref MemoryMarshal.GetReference(dataWithGarbage)), dataWithGarbage.Length);

            var s00 = reader.ReadASCII(data.Length);
            var s01 = reader2.ReadASCII(dataWithGarbage.Length);

            Assert.Equal(s00, s01);

            var s0 = Encoding.ASCII.GetString((byte*) UnsafeMemoryManager.AsPointer(ref MemoryMarshal.GetReference(data)), data.Length);
            var s1 = Encoding.ASCII.GetString((byte*) UnsafeMemoryManager.AsPointer(ref MemoryMarshal.GetReference(dataWithGarbage)), dataWithGarbage.Length);

            Assert.NotEqual(s0, s1);

            reader2.Release();
            reader.Release();

        }
    }
}
