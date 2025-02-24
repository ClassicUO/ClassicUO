// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CreateBasicEffectCommand
    {
        public int type;

        public IntPtr id;
        public Matrix world;
        public Matrix view;
        public Matrix projection;
        public bool texture_enabled;
        public IntPtr texture_id;
        public bool vertex_color_enabled;
    }
}