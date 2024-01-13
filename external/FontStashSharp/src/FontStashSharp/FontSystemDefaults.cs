using System;
using FontStashSharp.Interfaces;

namespace FontStashSharp
{
	public static class FontSystemDefaults
	{
		private static int _textureWidth = 1024, _textureHeight = 1024;
		private static float _fontResolutionFactor = 1.0f;
		private static int _kernelWidth = 0, _kernelHeight = 0;

		public static int TextureWidth
		{
			get => _textureWidth;

			set
			{
				if (value <= 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value));

				}

				_textureWidth = value;
			}
		}

		public static int TextureHeight
		{
			get => _textureHeight;

			set
			{
				if (value <= 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value));

				}

				_textureHeight = value;
			}
		}

		public static bool PremultiplyAlpha { get; set; } = true;

		public static float FontResolutionFactor
		{
			get => _fontResolutionFactor;
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, "This cannot be smaller than 0");
				}

				_fontResolutionFactor = value;
			}
		}

		public static int KernelWidth
		{
			get => _kernelWidth;

			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, "This cannot be smaller than 0");
				}

				_kernelWidth = value;
			}
		}

		public static int KernelHeight
		{
			get => _kernelHeight;

			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, "This cannot be smaller than 0");
				}

				_kernelHeight = value;
			}
		}

		/// <summary>
		/// Font Rasterizer. If set to null then default rasterizer(StbTrueTypeSharp) is used.
		/// </summary>
		public static IFontLoader FontLoader { get; set; }

		public static bool UseKernings { get; set; } = true;
		public static int? DefaultCharacter { get; set; } = ' ';

		public static int TextStyleLineHeight { get; set; } = 2;
	}
}