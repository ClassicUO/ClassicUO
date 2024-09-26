#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Core.Mathematics;
using Texture2D = Stride.Graphics.Texture;
#else
using System.Drawing;
using Texture2D = System.Object;
#endif

namespace FontStashSharp
{
	public class FontGlyph
	{
		public int Codepoint;
		public int Id;
		public int XAdvance;
		public Texture2D Texture;
		public Point RenderOffset;
		public Point TextureOffset;
		public Point Size;

		public bool IsEmpty
		{
			get
			{
				return Size.X == 0 || Size.Y == 0;
			}
		}

		public Rectangle TextureRectangle => new Rectangle(TextureOffset.X, TextureOffset.Y, Size.X, Size.Y);
		public Rectangle RenderRectangle => new Rectangle(RenderOffset.X, RenderOffset.Y, Size.X, Size.Y);
	}

	public class DynamicFontGlyph : FontGlyph
	{
		public float FontSize;
		public int FontSourceIndex;
		public FontSystemEffect Effect;
		public int EffectAmount;
	}
}
