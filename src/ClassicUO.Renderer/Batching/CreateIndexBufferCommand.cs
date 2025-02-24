// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CreateIndexBufferCommand
    {
        public int type;

        public IntPtr id;
        public IndexElementSize IndexElementSize;
        public int IndexCount;
        public BufferUsage BufferUsage;
        public bool IsDynamic;
    }
}