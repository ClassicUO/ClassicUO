// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CreateBlendStateCommand
    {
        public int Type;

        public IntPtr id;
        public BlendFunction AlphaBlendFunc;
        public Blend AlphaDestBlend;
        public Blend AlphaSrcBlend;
        public BlendFunction ColorBlendFunc;
        public Blend ColorDestBlend;
        public Blend ColorSrcBlend;
        public ColorWriteChannels ColorWriteChannels0;
        public ColorWriteChannels ColorWriteChannels1;
        public ColorWriteChannels ColorWriteChannels2;
        public ColorWriteChannels ColorWriteChannels3;
        public Color BlendFactor;
        public int MultipleSampleMask;
    }
}