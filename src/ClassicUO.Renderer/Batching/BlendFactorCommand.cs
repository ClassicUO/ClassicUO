// SPDX-License-Identifier: BSD-2-Clause

using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BlendFactorCommand
    {
        public int type;

        public Color color;
    }
}