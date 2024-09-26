using System;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Graphics;
using Stride.Core.Mathematics;
using Texture2D = Stride.Graphics.Texture;
#endif

namespace FontStashSharp
{
	internal static class Texture2DManager
	{
		public static Texture2D CreateTexture(GraphicsDevice device, int width, int height)
		{
#if MONOGAME || FNA
			var texture2d = new Texture2D(device, width, height);
#elif STRIDE
			var texture2d = Texture2D.New2D(device, width, height, false, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
#endif

			return texture2d;
		}

		public static void SetTextureData(Texture2D texture, Rectangle bounds, byte[] data)
		{
#if MONOGAME || FNA
			texture.SetData(0, bounds, data, 0, bounds.Width * bounds.Height * 4);
#elif STRIDE
			var size = bounds.Width * bounds.Height * 4;
			byte[] temp;
			if (size == data.Length)
			{
				temp = data;
			}
			else
			{
				// Since Stride requres buffer size to match exactly, copy data in the temporary buffer
				temp = new byte[bounds.Width * bounds.Height * 4];
				Array.Copy(data, temp, temp.Length);
			}

			var context = new GraphicsContext(texture.GraphicsDevice);
			texture.SetData(context.CommandList, temp, 0, 0, new ResourceRegion(bounds.Left, bounds.Top, 0, bounds.Right, bounds.Bottom, 1));
#endif
		}
	}
}