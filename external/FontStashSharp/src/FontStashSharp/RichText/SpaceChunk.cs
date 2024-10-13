#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using System.Drawing;
using System.Numerics;
using Color = FontStashSharp.FSColor;
#endif

namespace FontStashSharp.RichText
{
	public class SpaceChunk : BaseChunk
	{
		private readonly int _width;

		public override Point Size => new Point(_width, 0);

		public SpaceChunk(int width)
		{
			_width = width;
		}

		public override void Draw(FSRenderContext context, Vector2 position, Color color)
		{
		}
	}
}
