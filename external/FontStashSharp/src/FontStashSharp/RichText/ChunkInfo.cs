#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using System.Drawing;
#endif

namespace FontStashSharp.RichText
{
	internal enum ChunkInfoType
	{
		Text,
		Space,
		Image
	}

	internal struct ChunkInfo
	{
		public ChunkInfoType Type;
		public int X;
		public int Y;
		public bool LineEnd;
		public int StartIndex, EndIndex;
		public IRenderable Renderable;

		public int Width
		{
			get
			{
				if (Type == ChunkInfoType.Image)
				{
					return Renderable.Size.X;
				}

				return X;
			}
		}

		public int Height
		{
			get
			{
				if (Type == ChunkInfoType.Image)
				{
					return Renderable.Size.Y;
				}

				return Y;
			}
		}
	}
}
