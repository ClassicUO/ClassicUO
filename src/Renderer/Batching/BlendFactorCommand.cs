using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct BlendFactorCommand
    {
        public int type;

        public Color color;
    }
}