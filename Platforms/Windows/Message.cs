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