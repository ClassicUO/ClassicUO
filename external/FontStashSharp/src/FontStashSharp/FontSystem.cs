using FontStashSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using FontStashSharp.Rasterizers.StbTrueTypeSharp;
using System.Runtime.InteropServices;

#if MONOGAME || FNA
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
using Stride.Graphics;
using Texture2D = Stride.Graphics.Texture;
#else
using System.Drawing;
using Texture2D = System.Object;
#endif

namespace FontStashSharp
{
	public class FontSystem : IDisposable
	{
		public const int GlyphPad = 2;

		private readonly List<IFontSource> _fontSources = new List<IFontSource>();
		private readonly Int32Map<DynamicSpriteFont> _fonts = new Int32Map<DynamicSpriteFont>();
		private readonly FontSystemSettings _settings;

		private FontAtlas _currentAtlas;

		public FontSystemEffect Effect => _settings.Effect;
		public int EffectAmount => _settings.EffectAmount;

		public int TextureWidth => _settings.TextureWidth;
		public int TextureHeight => _settings.TextureHeight;

		public bool PremultiplyAlpha => _settings.PremultiplyAlpha;

		public float FontResolutionFactor => _settings.FontResolutionFactor;

		public int KernelWidth => _settings.KernelWidth;
		public int KernelHeight => _settings.KernelHeight;

		public Texture2D ExistingTexture => _settings.ExistingTexture;
		public Rectangle ExistingTextureUsedSpace => _settings.ExistingTextureUsedSpace;

		public bool UseKernings { get; set; } = true;
		public int? DefaultCharacter { get; set; } = ' ';

		internal List<IFontSource> FontSources => _fontSources;

		public List<FontAtlas> Atlases { get; } = new List<FontAtlas>();

		public event EventHandler CurrentAtlasFull;
		private readonly IFontLoader _fontLoader;

		public FontSystem(FontSystemSettings settings)
		{
			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			_settings = settings.Clone();

			if (_settings.FontLoader == null)
			{
				var loaderSettings = new StbTrueTypeSharpSettings
				{
					KernelWidth = _settings.KernelWidth,
					KernelHeight = _settings.KernelHeight
				};
				_fontLoader = new StbTrueTypeSharpLoader(loaderSettings);
			}
			else
			{
				_fontLoader = _settings.FontLoader;
			}

			UseKernings = FontSystemDefaults.UseKernings;
			DefaultCharacter = FontSystemDefaults.DefaultCharacter;
		}

		public FontSystem() : this(new FontSystemSettings())
		{
		}

		public void Dispose()
		{
			if (_fontSources != null)
			{
				foreach (var font in _fontSources)
					font.Dispose();
				_fontSources.Clear();
			}

			Atlases?.Clear();
			_currentAtlas = null;
			_fonts.Clear();
		}

		public void AddFont(byte[] data)
		{
			var fontSource = _fontLoader.Load(data);
			_fontSources.Add(fontSource);
		}

		public void AddFont(Stream stream)
		{
			AddFont(stream.ToByteArray());
		}

		public DynamicSpriteFont GetFont(float fontSize)
		{
			var intSize = fontSize.FloatAsInt();
			DynamicSpriteFont result;
			if (_fonts.TryGetValue(intSize, out result))
			{
				return result;
			}

			if (_fontSources.Count == 0)
			{
				throw new Exception("Could not create a font without a single font source. Use AddFont to add at least one font source.");
			}

			var fontSource = _fontSources[0];

			int ascent, descent, lineHeight;
			fontSource.GetMetricsForSize(fontSize, out ascent, out descent, out lineHeight);

			result = new DynamicSpriteFont(this, fontSize, lineHeight);
			_fonts[intSize] = result;
			return result;
		}

		public void Reset()
		{
			Atlases.Clear();
			_fonts.Clear();
			_currentAtlas = null;
		}

		internal int? GetCodepointIndex(int codepoint, out int fontSourceIndex)
		{
			fontSourceIndex = 0;
			var g = default(int?);

			for (var i = 0; i < _fontSources.Count; ++i)
			{
				var f = _fontSources[i];
				g = f.GetGlyphId(codepoint);
				if (g != null)
				{
					fontSourceIndex = i;
					break;
				}
			}

			return g;
		}

#if MONOGAME || FNA || STRIDE
		private FontAtlas GetCurrentAtlas(GraphicsDevice device, int textureWidth, int textureHeight)
#else
		private FontAtlas GetCurrentAtlas(ITexture2DManager device, int textureWidth, int textureHeight)
#endif
		{
			if (_currentAtlas == null)
			{
				Texture2D existingTexture = null;
				if (ExistingTexture != null && Atlases.Count == 0)
				{
					existingTexture = ExistingTexture;
				}

				_currentAtlas = new FontAtlas(textureWidth, textureHeight, 256, existingTexture);

				// If existing texture is used, mark existing used rect as used
				if (existingTexture != null && !ExistingTextureUsedSpace.IsEmpty)
				{
					if (!_currentAtlas.AddSkylineLevel(0, ExistingTextureUsedSpace.X, ExistingTextureUsedSpace.Y, ExistingTextureUsedSpace.Width, ExistingTextureUsedSpace.Height))
					{
						throw new Exception(string.Format("Unable to specify existing texture used space: {0}", ExistingTextureUsedSpace));
					}

					// TODO: Clear remaining space
				}

				Atlases.Add(_currentAtlas);
			}

			return _currentAtlas;
		}

#if MONOGAME || FNA || STRIDE
		internal void RenderGlyphOnAtlas(GraphicsDevice device, DynamicFontGlyph glyph)
#else
		internal void RenderGlyphOnAtlas(ITexture2DManager device, DynamicFontGlyph glyph)
#endif
		{
			var textureSize = new Point(TextureWidth, TextureHeight);

			if (ExistingTexture != null)
			{
#if MONOGAME || FNA || STRIDE
				textureSize = new Point(ExistingTexture.Width, ExistingTexture.Height);
#else
				textureSize = device.GetTextureSize(ExistingTexture);
#endif
			}

			int gx = 0, gy = 0;
			var gw = glyph.Size.X + GlyphPad * 2;
			var gh = glyph.Size.Y + GlyphPad * 2;

			var currentAtlas = GetCurrentAtlas(device, textureSize.X, textureSize.Y);
			if (!currentAtlas.AddRect(gw, gh, ref gx, ref gy))
			{
				CurrentAtlasFull?.Invoke(this, EventArgs.Empty);

				// This code will force creation of new atlas
				_currentAtlas = null;
				currentAtlas = GetCurrentAtlas(device, textureSize.X, textureSize.Y);

				// Try to add again
				if (!currentAtlas.AddRect(gw, gh, ref gx, ref gy))
				{
					throw new Exception(string.Format("Could not add rect to the newly created atlas. gw={0}, gh={1}", gw, gh));
				}
			}

			glyph.TextureOffset.X = gx + GlyphPad;
			glyph.TextureOffset.Y = gy + GlyphPad;

			currentAtlas.RenderGlyph(device, glyph, FontSources[glyph.FontSourceIndex], PremultiplyAlpha, KernelWidth, KernelHeight);

			glyph.Texture = currentAtlas.Texture;
		}
	}
}