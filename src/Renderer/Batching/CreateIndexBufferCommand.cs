using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CreateIndexBufferCommand
    {
        public int type;

        public IntPtr id;
        public IndexElementSize IndexElementSize;
        public int IndexCount;
        public BufferUsage BufferUsage;
        public bool IsDynamic;
    }
}