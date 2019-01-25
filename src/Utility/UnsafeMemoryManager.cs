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
using System.Reflection.Emit;

namespace ClassicUO.Utility
{
    public static unsafe class UnsafeMemoryManager
    {
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void* AsPointer<T>(ref T v)
        //{
        //    TypedReference t = __makeref(v);
        //    return (void*)*((IntPtr*)&t + (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix ? 1 : 0) );
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static T As<T>(object v)
        //{
        //    int size = SizeOf<T>();

        //    return Reinterpret<object, T>(v, size);
        //}


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>() //where T : struct
        {
            return Marshal.SizeOf<T>();

            //DoubleStruct<T> doubleStruct = DoubleStruct<T>.Value;

            //TypedReference tRef0 = __makeref(doubleStruct.First);
            //TypedReference tRef1 = __makeref(doubleStruct.Second);

            //// NB: Mono doesn't like these... LOL
            ////TypedReference* address0 = &tRef0;
            ////TypedReference* address1 = &tRef1;

            //bool use2nd = Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix;

            //IntPtr firstValueAddress0 = *(IntPtr*)&tRef0;
            //IntPtr secondValueAddress0 = *(IntPtr*)&tRef0 + 1;
            //IntPtr firstValueAddress1 = *(IntPtr*)&tRef1;
            //IntPtr secondValueAddress1 = *((IntPtr*)&tRef1 + 1);

            //int size = 0;

            //if (use2nd/* firstValueAddress0 == firstValueAddress1 */ )
            //    size = (int)((byte*)secondValueAddress1 - (byte*)secondValueAddress0);
            //else
            //    size = (int)((byte*)firstValueAddress1 - (byte*)firstValueAddress0);

            //return size;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static TOut Reinterpret<TIn, TOut>(TIn curValue, int sizeBytes) //where TIn : struct where TOut : struct
        //{
        //    TOut result = default;

        //    //SingleStruct<TIn> inS = SingleStruct<TIn>.Value;
        //    //SingleStruct<TOut> outS = SingleStruct<TOut>.Value;

        //    TypedReference resultRef = __makeref(result);
        //    TypedReference curValueRef = __makeref(curValue);


        //    int offset = (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix ? 1 : 0);

        //    byte* resultPtr = (byte*)*((IntPtr*)&resultRef + offset);
        //    byte* curValuePtr = (byte*)*((IntPtr*)&curValueRef + offset);

        //    //for (int i = 0; i < sizeBytes; ++i)
        //    //    resultPtr[i] = curValuePtr[i];

        //    Buffer.MemoryCopy(curValuePtr, resultPtr, sizeBytes, sizeBytes);

        //    return result;
        //}

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