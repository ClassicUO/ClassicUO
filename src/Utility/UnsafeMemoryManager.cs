#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.Utility
{
    public static unsafe class UnsafeMemoryManager
    {
        static UnsafeMemoryManager()
        {
            Console.WriteLine("Platform: {0}", PlatformHelper.IsMonoRuntime ? "Mono" : ".NET");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AsPointer<T>(ref T v)
        {
            TypedReference t = __makeref(v);

            return (void*) *((IntPtr*) &t + (PlatformHelper.IsMonoRuntime ? 1 : 0));
        }

        public static T ToStruct<T>(IntPtr ptr)
        {
            return ToStruct<T>(ptr, SizeOf<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToStruct<T>(IntPtr ptr, int size)
        {
            byte* str = (byte*) ptr;

            T result = default;
            byte* resultPtr = (byte*) AsPointer(ref result);
            Buffer.MemoryCopy(str, resultPtr, size, size);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T As<T>(object v)
        {
            int size = SizeOf<T>();

            return Reinterpret<object, T>(v, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            DoubleStruct<T> doubleStruct = DoubleStruct<T>.Value;
            TypedReference tRef0 = __makeref(doubleStruct.First);
            TypedReference tRef1 = __makeref(doubleStruct.Second);
            IntPtr ptrToT0, ptrToT1;

            if (PlatformHelper.IsMonoRuntime)
            {
                ptrToT0 = *((IntPtr*) &tRef0 + 1);
                ptrToT1 = *((IntPtr*) &tRef1 + 1);
            }
            else
            {
                ptrToT0 = *(IntPtr*) &tRef0;
                ptrToT1 = *(IntPtr*) &tRef1;
            }

            return (int) ((byte*) ptrToT1 - (byte*) ptrToT0);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Reinterpret<TIn, TOut>(TIn curValue, int sizeBytes) //where TIn : struct where TOut : struct
        {
            TOut result = default;

            //SingleStruct<TIn> inS = SingleStruct<TIn>.Value;
            //SingleStruct<TOut> outS = SingleStruct<TOut>.Value;

            TypedReference resultRef = __makeref(result);
            TypedReference curValueRef = __makeref(curValue);


            int offset = PlatformHelper.IsMonoRuntime ? 1 : 0;

            byte* resultPtr = (byte*) *((IntPtr*) &resultRef + offset);
            byte* curValuePtr = (byte*) *((IntPtr*) &curValueRef + offset);

            //for (int i = 0; i < sizeBytes; ++i)
            //    resultPtr[i] = curValuePtr[i];

            Buffer.MemoryCopy(curValuePtr, resultPtr, sizeBytes, sizeBytes);

            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DoubleStruct<T>
        {
            public T First;
            public T Second;
            public static readonly DoubleStruct<T> Value;
        }

        //[StructLayout(LayoutKind.Sequential, Pack = 1)]
        //private struct DoubleStruct<T> //where T : struct
        //{
        //    public T First;
        //    public T Second;
        //    public static readonly DoubleStruct<T> Value;
        //}

        //[StructLayout(LayoutKind.Sequential, Pack = 1)]
        //private struct SingleStruct<T> //where T : struct
        //{
        //    public T First;
        //    public static readonly SingleStruct<T> Value;
        //}
    }
}