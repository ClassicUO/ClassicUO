// SPDX-License-Identifier: BSD-2-Clause

using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ViewportCommand
    {
        public int Type;

        public int X;
        public int y;
        public int w;
        public int h;
    }
}