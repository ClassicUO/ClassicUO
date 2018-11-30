#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TOut Reinterpret<TIn, TOut>(TIn curValue, int sizeBytes) where TIn : struct where TOut : struct
        {
            TOut result = default;
            TypedReference resultRef = __makeref(result);
            byte* resultPtr = (byte*) *(IntPtr*) &resultRef;
            TypedReference curValueRef = __makeref(curValue);
            byte* curValuePtr = (byte*) *(IntPtr*) &curValueRef;
            for (int i = 0; i < sizeBytes; ++i) resultPtr[i] = curValuePtr[i];

            return result;
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