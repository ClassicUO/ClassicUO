#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using System.Drawing;
#endif

namespace FontStashSharp.RichText
{
	public struct TextChunkGlyph
	{
		public int Index;
		public int Codepoint;
		public Rectangle Bounds;
		public int XAdvance;
		public int LineTop;
		public TextChunk TextChunk;
	}
}
