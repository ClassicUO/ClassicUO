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
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace ClassicUO.Platforms.Windows
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Message
    {
        public uint Id;
        public IntPtr WParam;
        public IntPtr LParam;
        public Point Point;

        public Message(uint id, IntPtr wParam, IntPtr lParam)
        {
            Id = id;
            WParam = wParam;
            LParam = lParam;
            Point = new Point(LowWord(lParam), HighWord(lParam));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Message)) return false;

            Message msg = (Message) obj;

            return msg.Id == Id && msg.LParam == LParam && msg.Point == Point && msg.WParam == WParam;
        }

        public override int GetHashCode() => (int) Id;

        public static bool operator ==(Message m1, Message m2) => m1.Equals(m2);

        public static bool operator !=(Message m1, Message m2) => !m1.Equals(m2);

        public static int HighWord(int n) => (n >> 0x10) & 0xffff;

        public static int HighWord(IntPtr n) => HighWord((int) (long) n);

        public static int LowWord(int n) => n & 0xffff;

        public static int LowWord(IntPtr n) => LowWord((int) (long) n);

        public static int MakeLong(int low, int high) => (high << 0x10) | (low & 0xffff);

        public static IntPtr MakeLParam(int low, int high) => (IntPtr) ((high << 0x10) | (low & 0xffff));

        public static int SignedHighWord(int n) => (short) ((n >> 0x10) & 0xffff);

        public static int SignedHighWord(IntPtr n) => SignedHighWord((int) (long) n);

        public static int SignedLowWord(int n) => (short) (n & 0xffff);

        public static int SignedLowWord(IntPtr n) => SignedLowWord((int) (long) n);
    }
}