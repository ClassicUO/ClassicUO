#if MONOGAME || FNA
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Core.Mathematics;
using Stride.Graphics;
using Texture2D = Stride.Graphics.Texture;
#else
using System.Numerics;
using Texture2D = System.Object;
using System.Runtime.InteropServices;
#endif

namespace FontStashSharp.Interfaces
{
#if PLATFORM_AGNOSTIC
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct VertexPositionColorTexture
	{
		/// <summary>
		/// Position
		/// </summary>
		public Vector3 Position;

		/// <summary>
		/// Color
		/// </summary>
		public FSColor Color;

		/// <summary>
		/// Texture Coordinate
		/// </summary>
		public Vector2 TextureCoordinate;
	}
#endif

	public interface IFontStashRenderer2
	{
#if MONOGAME || FNA || STRIDE
		GraphicsDevice GraphicsDevice { get; }
#else
		ITexture2DManager TextureManager { get; }
#endif

		void DrawQuad(Texture2D texture, ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight, ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight);
	}
}
