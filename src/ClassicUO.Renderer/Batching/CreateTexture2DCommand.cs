// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CreateTexture2DCommand
    {
        public int type;

        public IntPtr id;
        public SurfaceFormat Format;
        public int Width;
        public int Height;
        public int MipLevels;
        public bool IsRenderTarget;
    }
}