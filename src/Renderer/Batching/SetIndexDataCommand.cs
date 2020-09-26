using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SetIndexDataCommand
    {
        public int type;

        public IntPtr id;
        public IntPtr indices_buffer_ptr;
        public int indices_buffer_length;
    }
}