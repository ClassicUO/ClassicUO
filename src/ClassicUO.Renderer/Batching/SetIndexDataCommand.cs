// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SetIndexDataCommand
    {
        public int type;

        public IntPtr id;
        public IntPtr indices_buffer_ptr;
        public int indices_buffer_length;
    }
}