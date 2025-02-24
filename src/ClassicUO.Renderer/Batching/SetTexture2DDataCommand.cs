// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SetTexture2DDataCommand
    {
        public int type;

        public IntPtr id;
        public SurfaceFormat format;
        public int x;
        public int y;
        public int width;
        public int height;
        public int level;
        public IntPtr data;
        public int data_length;
    }
}