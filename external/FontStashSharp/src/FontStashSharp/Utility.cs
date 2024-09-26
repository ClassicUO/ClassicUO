// Some code had been borrowed from https://www.monogame.net/

using System;
using System.IO;
using System.Runtime.InteropServices;
using FontStashSharp.Interfaces;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Core.Mathematics;
using Stride.Graphics;
using Texture2D = Stride.Graphics.Texture;
#else
using System.Numerics;
using System.Drawing;
using Matrix = System.Numerics.Matrix3x2;
using Texture2D = System.Object;
using Color = FontStashSharp.FSColor;
#endif


namespace FontStashSharp
{
	internal static class Utility
	{
		public static readonly Point PointZero = new Point(0, 0);
		public static readonly Vector2 Vector2Zero = new Vector2(0, 0);
		public static readonly Vector2 DefaultScale = new Vector2(1.0f, 1.0f);
		public static readonly Vector2 DefaultOrigin = new Vector2(0.0f, 0.0f);

		[StructLayout(LayoutKind.Explicit)]
		private struct FloatToInt
		{
			[FieldOffset(0)] public float f;
			[FieldOffset(0)] public int i;
		}

		public static int FloatAsInt(this float f)
		{
			return new FloatToInt { f = f }.i;
		}

		/// <summary>
		/// Restricts a value to be within a specified range.
		/// </summary>
		/// <param name="value">The value to clamp.</param>
		/// <param name="min">The minimum value. If <c>value</c> is less than <c>min</c>, <c>min</c> will be returned.</param>
		/// <param name="max">The maximum value. If <c>value</c> is greater than <c>max</c>, <c>max</c> will be returned.</param>
		/// <returns>The clamped value.</returns>
		public static int Clamp(int value, int min, int max)
		{
			value = (value > max) ? max : value;
			value = (value < min) ? min : value;
			return value;
		}

		/// <summary>
		/// Restricts a value to be within a specified range.
		/// </summary>
		/// <param name="value">The value to clamp.</param>
		/// <param name="min">The minimum value. If <c>value</c> is less than <c>min</c>, <c>min</c> will be returned.</param>
		/// <param name="max">The maximum value. If <c>value</c> is greater than <c>max</c>, <c>max</c> will be returned.</param>
		/// <returns>The clamped value.</returns>
		public static float Clamp(float value, float min, float max)
		{
			// First we check to see if we're greater than the max
			value = (value > max) ? max : value;

			// Then we check to see if we're less than the min.
			value = (value < min) ? min : value;

			// There's no check to see if min > max.
			return value;
		}

		/// <summary>
		/// Linearly interpolates between two values.
		/// </summary>
		/// <param name="value1">Source value.</param>
		/// <param name="value2">Destination value.</param>
		/// <param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
		/// <returns>Interpolated value.</returns> 
		/// <remarks>This method performs the linear interpolation based on the following formula:
		/// <code>value1 + (value2 - value1) * amount</code>.
		/// Passing amount a value of 0 will cause value1 to be returned, a value of 1 will cause value2 to be returned.
		/// See <see cref="MathHelper.LerpPrecise"/> for a less efficient version with more precision around edge cases.
		/// </remarks>
		public static float Lerp(float value1, float value2, float amount)
		{
			return value1 + (value2 - value1) * amount;
		}

		/// <summary>
		/// Linearly interpolates between two values.
		/// This method is a less efficient, more precise version of <see cref="MathHelper.Lerp"/>.
		/// See remarks for more info.
		/// </summary>
		/// <param name="value1">Source value.</param>
		/// <param name="value2">Destination value.</param>
		/// <param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
		/// <returns>Interpolated value.</returns>
		/// <remarks>This method performs the linear interpolation based on the following formula:
		/// <code>((1 - amount) * value1) + (value2 * amount)</code>.
		/// Passing amount a value of 0 will cause value1 to be returned, a value of 1 will cause value2 to be returned.
		/// This method does not have the floating point precision issue that <see cref="MathHelper.Lerp"/> has.
		/// i.e. If there is a big gap between value1 and value2 in magnitude (e.g. value1=10000000000000000, value2=1),
		/// right at the edge of the interpolation range (amount=1), <see cref="MathHelper.Lerp"/> will return 0 (whereas it should return 1).
		/// This also holds for value1=10^17, value2=10; value1=10^18,value2=10^2... so on.
		/// For an in depth explanation of the issue, see below references:
		/// Relevant Wikipedia Article: https://en.wikipedia.org/wiki/Linear_interpolation#Programming_language_support
		/// Relevant StackOverflow Answer: http://stackoverflow.com/questions/4353525/floating-point-linear-interpolation#answer-23716956
		/// </remarks>
		public static float LerpPrecise(float value1, float value2, float amount)
		{
			return ((1 - amount) * value1) + (value2 * amount);
		}

		public static byte[] ToByteArray(this Stream stream)
		{
			byte[] bytes;

			// Rewind stream if it is at end
			if (stream.CanSeek && stream.Length == stream.Position)
			{
				stream.Seek(0, SeekOrigin.Begin);
			}

			// Copy it's data to memory
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				bytes = ms.ToArray();
			}

			return bytes;
		}

		public static Vector2 Transform(this Vector2 v, ref Matrix matrix)
		{
#if MONOGAME || FNA
			Vector2 result;
			Vector2.Transform(ref v, ref matrix, out result);
			return result;
#elif STRIDE
			Vector4 result;
			Vector2.Transform(ref v, ref matrix, out result);
			return new Vector2(result.X, result.Y);
#else
			return Vector2.Transform(v, matrix);
#endif
		}

		public static Vector3 TransformToVector3(this Vector2 v, ref Matrix matrix, float z)
		{
			var result = v.Transform(ref matrix);
			return new Vector3(result.X, result.Y, z);
		}

		public static int Length(this string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return 0;
			}

			return s.Length;
		}

		public static void BuildTransform(Vector2 position, Vector2 scale, float rotation, Vector2 origin, out Matrix transformation)
		{
			// This code had been borrowed from MonoGame's SpriteBatch.DrawString
			transformation = Matrix.Identity;

			float offsetX, offsetY;
			if (rotation == 0)
			{
				transformation.M11 = scale.X;
				transformation.M22 = scale.Y;
				offsetX = position.X - (origin.X * transformation.M11);
				offsetY = position.Y - (origin.Y * transformation.M22);
			}
			else
			{
				var cos = (float)Math.Cos(rotation);
				var sin = (float)Math.Sin(rotation);
				transformation.M11 = scale.X * cos;
				transformation.M12 = scale.X * sin;
				transformation.M21 = scale.Y * -sin;
				transformation.M22 = scale.Y * cos;
				offsetX = position.X - (origin.X * transformation.M11) - (origin.Y * transformation.M21);
				offsetY = position.Y - (origin.X * transformation.M12) - (origin.Y * transformation.M22);
			}

#if MONOGAME || FNA || STRIDE
			transformation.M41 = offsetX;
			transformation.M42 = offsetY;
#else
			transformation.M31 = offsetX;
			transformation.M32 = offsetY;
#endif
		}

		public static void DrawQuad(this IFontStashRenderer2 renderer,
			Texture2D texture, Color color,
			Vector2 baseOffset, ref Matrix transformation, float layerDepth,
			Vector2 size, Rectangle textureRectangle,
			ref VertexPositionColorTexture topLeft, ref VertexPositionColorTexture topRight,
			ref VertexPositionColorTexture bottomLeft, ref VertexPositionColorTexture bottomRight)
		{
#if MONOGAME || FNA || STRIDE
			var textureSize = new Point(texture.Width, texture.Height);
#else
			var textureSize = renderer.TextureManager.GetTextureSize(texture);
#endif

			topLeft.Position = baseOffset.TransformToVector3(ref transformation, layerDepth);
			topLeft.TextureCoordinate = new Vector2((float)textureRectangle.X / textureSize.X,
													(float)textureRectangle.Y / textureSize.Y);
			topLeft.Color = color;

			topRight.Position = (baseOffset + new Vector2(size.X, 0)).TransformToVector3(ref transformation, layerDepth);
			topRight.TextureCoordinate = new Vector2((float)textureRectangle.Right / textureSize.X,
												 (float)textureRectangle.Y / textureSize.Y);
			topRight.Color = color;

			bottomLeft.Position = (baseOffset + new Vector2(0, size.Y)).TransformToVector3(ref transformation, layerDepth);
			bottomLeft.TextureCoordinate = new Vector2((float)textureRectangle.Left / textureSize.X,
														 (float)textureRectangle.Bottom / textureSize.Y);
			bottomLeft.Color = color;

			bottomRight.Position = (baseOffset + new Vector2(size.X, size.Y)).TransformToVector3(ref transformation, layerDepth);
			bottomRight.TextureCoordinate = new Vector2((float)textureRectangle.Right / textureSize.X,
														(float)textureRectangle.Bottom / textureSize.Y);
			bottomRight.Color = color;

			renderer.DrawQuad(texture, ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
		}
	}
}