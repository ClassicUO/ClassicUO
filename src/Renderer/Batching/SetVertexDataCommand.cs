using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SetVertexDataCommand
    {
        public int type;

        public IntPtr id;
        public IntPtr vertex_buffer_ptr;
        public int vertex_buffer_length;
    }
}