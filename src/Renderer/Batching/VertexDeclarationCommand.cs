using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VertexDeclarationCommand
    {
        public int Offset;
        public VertexElementFormat Format;
        public VertexElementUsage Usage;
        public int UsageIndex;
    }
}