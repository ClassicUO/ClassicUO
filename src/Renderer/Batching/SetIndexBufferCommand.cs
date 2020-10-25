using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SetIndexBufferCommand
    {
        public int type;

        public IntPtr id;
    }
}