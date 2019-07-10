#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

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

        [MethodImpl(256)]
        public static void* AsPointer<T>(ref T v)
        {
            TypedReference t = __makeref(v);

            return (void*) *((IntPtr*) &t + (PlatformHelper.IsMonoRuntime ? 1 : 0));
        }

        public static T ToStruct<T>(IntPtr ptr)
        {
            return ToStruct<T>(ptr, SizeOf<T>());
        }

        [MethodImpl(256)]
        public static T ToStruct<T>(IntPtr ptr, int size)
        {
            byte* str = (byte*) ptr;

            T result = default;
            TypedReference resultRef = __makeref(result);
            byte* resultPtr = (byte*) *((IntPtr*) &resultRef + (PlatformHelper.IsMonoRuntime ? 1 : 0));

            int sizeOf = size;

            for (int i = 0; i < sizeOf; i++) resultPtr[i] = str[i];

            return result;
        }

        [MethodImpl(256)]
        public static T As<T>(object v)
        {
            int size = SizeOf<T>();

            return Reinterpret<object, T>(v, size);
        }

        [MethodImpl(256)]
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


        [MethodImpl(256)]
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