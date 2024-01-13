using FontStashSharp.Interfaces;
using System;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Core.Mathematics;
using Stride.Graphics;
#else
using System.Drawing;
using System.Numerics;
#endif

namespace FontStashSharp
{
	public partial class DynamicSpriteFont : SpriteFontBase
	{
		private class GlyphStorage
		{
			public Int32Map<DynamicFontGlyph> Glyphs = new Int32Map<DynamicFontGlyph>();
			public FontSystemEffect Effect;
			public int EffectAmount;
		}

		private readonly Int32Map<GlyphStorage> _storages = new Int32Map<GlyphStorage>();
		private GlyphStorage _lastStorage;
		private readonly Int32Map<int> Kernings = new Int32Map<int>();
		private FontMetrics[] IndexedMetrics;

		public FontSystem FontSystem { get; private set; }

		internal DynamicSpriteFont(FontSystem system, float size, int lineHeight) : base(size, lineHeight)
		{
			if (system == null)
			{
				throw new ArgumentNullException(nameof(system));
			}

			FontSystem = system;
			RenderFontSizeMultiplicator = FontSystem.FontResolutionFactor;
		}

		internal Int32Map<DynamicFontGlyph> GetGlyphs(FontSystemEffect effect, int effectAmount)
		{
			if (_lastStorage != null && _lastStorage.Effect == effect && _lastStorage.EffectAmount == effectAmount)
			{
				return _lastStorage.Glyphs;
			}

			var key = (int)effect << 16 | effectAmount;

			GlyphStorage result;
			if (!_storages.TryGetValue(key, out result))
			{
				result = new GlyphStorage
				{
					Effect = effect,
					EffectAmount = effectAmount
				};

				_storages[key] = result;
			}

			_lastStorage = result;

			return result.Glyphs;
		}

		private DynamicFontGlyph GetGlyphWithoutBitmap(int codepoint, FontSystemEffect effect, int effectAmount)
		{
			if (effect == FontSystemEffect.None)
			{
				effect = FontSystem.Effect;
				effectAmount = FontSystem.EffectAmount;
			}
			else if (effectAmount == 0)
			{
				effect = FontSystemEffect.None;
			}

			var storage = GetGlyphs(effect, effectAmount);

			DynamicFontGlyph glyph;
			if (storage.TryGetValue(codepoint, out glyph))
			{
				return glyph;
			}

			int fontSourceIndex;
			var g = FontSystem.GetCodepointIndex(codepoint, out fontSourceIndex);
			if (g == null)
			{
				storage[codepoint] = null;
				return null;
			}

			var fontSize = FontSize * FontSystem.FontResolutionFactor;
			var font = FontSystem.FontSources[fontSourceIndex];

			int advance, x0, y0, x1, y1;
			font.GetGlyphMetrics(g.Value, fontSize, out advance, out x0, out y0, out x1, out y1);

			var gw = x1 - x0 + effectAmount * 2;
			var gh = y1 - y0 + effectAmount * 2;

			glyph = new DynamicFontGlyph
			{
				Codepoint = codepoint,
				Id = g.Value,
				FontSize = fontSize,
				FontSourceIndex = fontSourceIndex,
				RenderOffset = new Point(x0, y0),
				Size = new Point(gw, gh),
				XAdvance = advance,
				Effect = effect,
				EffectAmount = effectAmount
			};

			storage[codepoint] = glyph;

			return glyph;
		}

#if MONOGAME || FNA || STRIDE
		private DynamicFontGlyph GetGlyphInternal(GraphicsDevice device, int codepoint, FontSystemEffect effect, int effectAmount)
#else
		private DynamicFontGlyph GetGlyphInternal(ITexture2DManager device, int codepoint, FontSystemEffect effect, int effectAmount)
#endif
		{
			var glyph = GetGlyphWithoutBitmap(codepoint, effect, effectAmount);
			if (glyph == null)
			{
				return null;
			}

			if (device == null || glyph.Texture != null)
				return glyph;

			FontSystem.RenderGlyphOnAtlas(device, glyph);

			return glyph;
		}

#if MONOGAME || FNA || STRIDE
		private DynamicFontGlyph GetDynamicGlyph(GraphicsDevice device, int codepoint, FontSystemEffect effect, int effectAmount)
#else
		private DynamicFontGlyph GetDynamicGlyph(ITexture2DManager device, int codepoint, FontSystemEffect effect, int effectAmount)
#endif
		{
			var result = GetGlyphInternal(device, codepoint, effect, effectAmount);
			if (result == null && FontSystem.DefaultCharacter != null)
			{
				result = GetGlyphInternal(device, FontSystem.DefaultCharacter.Value, effect, effectAmount);
			}

			return result;
		}

#if MONOGAME || FNA || STRIDE
		protected internal override FontGlyph GetGlyph(GraphicsDevice device, int codepoint, FontSystemEffect effect, int effectAmount)
#else
		protected internal override FontGlyph GetGlyph(ITexture2DManager device, int codepoint, FontSystemEffect effect, int effectAmount)
#endif
		{
			return GetDynamicGlyph(device, codepoint, effect, effectAmount);
		}

		private void GetMetrics(int fontSourceIndex, out FontMetrics result)
		{
			if (IndexedMetrics == null || IndexedMetrics.Length != FontSystem.FontSources.Count)
			{
				IndexedMetrics = new FontMetrics[FontSystem.FontSources.Count];
				for (var i = 0; i < IndexedMetrics.Length; ++i)
				{
					int ascent, descent, lineHeight;
					FontSystem.FontSources[i].GetMetricsForSize(FontSize * RenderFontSizeMultiplicator, out ascent, out descent, out lineHeight);

					IndexedMetrics[i] = new FontMetrics(ascent, descent, lineHeight);
				}
			}

			result = IndexedMetrics[fontSourceIndex];
		}

		internal override void PreDraw(TextSource source, FontSystemEffect effect, int effectAmount,
			out int ascent, out int lineHeight)
		{
			// Determine ascent and lineHeight from first character
			ascent = 0;
			lineHeight = 0;
			while (true)
			{
				int codepoint;
				if (!source.GetNextCodepoint(out codepoint))
				{
					break;
				}

				var glyph = GetDynamicGlyph(null, codepoint, effect, effectAmount);
				if (glyph == null)
				{
					continue;
				}

				FontMetrics metrics;
				GetMetrics(glyph.FontSourceIndex, out metrics);
				ascent = metrics.Ascent;
				lineHeight = metrics.LineHeight;
				break;
			}

			source.Reset();
		}

		internal override Bounds InternalTextBounds(TextSource source, Vector2 position,
			float characterSpacing, float lineSpacing, FontSystemEffect effect, int effectAmount)
		{
			if (source.IsNull)
				return Bounds.Empty;

			var result = base.InternalTextBounds(source, position, characterSpacing, lineSpacing, effect, effectAmount);
			if (effect != FontSystemEffect.None)
			{
				result.X2 += effectAmount * 2;
			}

			return result;
		}

		private static int GetKerningsKey(int glyph1, int glyph2)
		{
			return ((glyph1 << 16) | (glyph1 >> 16)) ^ glyph2;
		}

		internal override float GetKerning(FontGlyph glyph, FontGlyph prevGlyph)
		{
			if (!FontSystem.UseKernings)
			{
				return 0.0f;
			}


			var dynamicGlyph = (DynamicFontGlyph)glyph;
			var dynamicPrevGlyph = (DynamicFontGlyph)prevGlyph;
			if (dynamicGlyph.FontSourceIndex != dynamicPrevGlyph.FontSourceIndex)
			{
				return 0.0f;
			}

			var key = GetKerningsKey(prevGlyph.Id, dynamicGlyph.Id);
			var result = 0;
			if (!Kernings.TryGetValue(key, out result))
			{
				var fontSource = FontSystem.FontSources[dynamicGlyph.FontSourceIndex];
				result = fontSource.GetGlyphKernAdvance(prevGlyph.Id, dynamicGlyph.Id, dynamicGlyph.FontSize);

				Kernings[key] = result;
			}

			return result;
		}
	}
}