// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CreateVertexBufferCommand
    {
        public int type;

        public IntPtr id;
        public int VertexElementsCount;
        public int Size;
        public int DeclarationCount;
        public unsafe VertexDeclarationCommand* Declarations;
        public BufferUsage BufferUsage;
        public bool IsDynamic;
    }
}