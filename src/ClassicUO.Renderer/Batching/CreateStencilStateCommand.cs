// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CreateStencilStateCommand
    {
        public int type;

        public IntPtr id;
        public bool DepthBufferEnabled;
        public bool DepthBufferWriteEnabled;
        public CompareFunction DepthBufferFunc;
        public bool StencilEnabled;
        public CompareFunction StencilFunc;
        public StencilOperation StencilPass;
        public StencilOperation StencilFail;
        public StencilOperation StencilDepthBufferFail;
        public bool TwoSidedStencilMode;
        public CompareFunction CounterClockwiseStencilFunc;
        public StencilOperation CounterClockwiseStencilFail;
        public StencilOperation CounterClockwiseStencilPass;
        public StencilOperation CounterClockwiseStencilDepthBufferFail;
        public int StencilMask;
        public int StencilWriteMask;
        public int ReferenceStencil;
    }
}