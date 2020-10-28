using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ScissorCommand
    {
        public int type;

        public int x;
        public int y;
        public int w;
        public int h;
    }
}