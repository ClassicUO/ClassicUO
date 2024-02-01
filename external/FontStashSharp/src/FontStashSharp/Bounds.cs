#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using System.Numerics;
#endif

namespace FontStashSharp
{
	public struct Bounds
	{
		public static readonly Bounds Empty = new Bounds
		{
			X = 0,
			Y = 0,
			X2 = 0,
			Y2 = 0,
		};

		public float X, Y, X2, Y2;

		public Bounds(float x, float y, float x2, float y2)
		{
			X = x;
			Y = y;
			X2 = x2;
			Y2 = y2;
		}

		public void ApplyScale(Vector2 scale)
		{
			X *= scale.X;
			Y *= scale.Y;
			X2 *= scale.X;
			Y2 *= scale.Y;
		}
	}
}
