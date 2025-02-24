// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IndexedPrimitiveDataCommand
    {
        public int type;

        public IntPtr texture_id;
        public PrimitiveType PrimitiveType;
        public int BaseVertex;
        public int MinVertexIndex;
        public int NumVertices;
        public int StartIndex;
        public int PrimitiveCount;
    }
}