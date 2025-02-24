// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CreateRasterizerStateCommand
    {
        public int type;

        public IntPtr id;
        public CullMode CullMode;
        public FillMode FillMode;
        public float DepthBias;
        public bool MultiSample;
        public bool ScissorTestEnabled;
        public float SlopeScaleDepthBias;
    }
}