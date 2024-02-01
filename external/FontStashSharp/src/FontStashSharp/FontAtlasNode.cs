using System.Runtime.InteropServices;

namespace FontStashSharp
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct FontAtlasNode
	{
		public int X;
		public int Y;
		public int Width;
	}
}
