using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Utility
{
    public static class UnsafeMemoryManager
    {
        public static unsafe int SizeOf<T>() where T : struct
        {
            DoubleStruct<T> doubleStruct = DoubleStruct<T>.Value;
            TypedReference tRef0 = __makeref(doubleStruct.First);
            TypedReference tRef1 = __makeref(doubleStruct.Second);
            IntPtr ptrToT0 = *(IntPtr*) &tRef0;
            IntPtr ptrToT1 = *(IntPtr*) &tRef1;

            return (int) ((byte*) ptrToT1 - (byte*) ptrToT0);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DoubleStruct<T> where T : struct
        {
            public T First;
            public T Second;
            public static readonly DoubleStruct<T> Value;
        }
    }
}