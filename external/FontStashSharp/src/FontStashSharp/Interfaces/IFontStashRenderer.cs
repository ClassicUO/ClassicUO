#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Core.Mathematics;
using Stride.Graphics;
using Texture2D = Stride.Graphics.Texture;
#else
using System.Drawing;
using System.Numerics;
using Color = FontStashSharp.FSColor;
using Texture2D = System.Object;
#endif

namespace FontStashSharp.Interfaces
{
	public interface IFontStashRenderer
	{
#if MONOGAME || FNA || STRIDE
		GraphicsDevice GraphicsDevice { get; }
#else
		ITexture2DManager TextureManager { get; }
#endif

		void Draw(Texture2D texture, Vector2 pos, Rectangle? src, Color color, float rotation, Vector2 scale, float depth);
	}
}
