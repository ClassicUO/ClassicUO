using System;
using System.Collections.Generic;

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
  public class TextChunk : BaseChunk
  {
	private readonly Point _size;

	public List<TextChunkGlyph> Glyphs { get; } = new List<TextChunkGlyph>();

	public int Count { get; }
	public string Text { get; internal set; }
	public override Point Size => _size;

	public SpriteFontBase Font { get; }
	public TextStyle Style { get; set; }
	public FontSystemEffect Effect { get; set; }
	public int EffectAmount { get; set; }

	public TextChunk(SpriteFontBase font, string text, Point size, Point? startPos)
	{
	  if (font == null)
	  {
		throw new ArgumentNullException("font");
	  }

	  Font = font;
	  Text = text;
	  _size = size;
	  Count = TextSource.CalculateLength(text);

	  if (startPos != null)
	  {
		CalculateGlyphs(startPos.Value);
	  }
	}

	private void CalculateGlyphs(Point startPos)
	{
	  if (string.IsNullOrEmpty(Text))
	  {
		return;
	  }

	  var glyphs = Font.GetGlyphs(Text, Vector2.Zero);

	  Glyphs.Clear();
	  for (var i = 0; i < glyphs.Count; ++i)
	  {
		var glyph = glyphs[i];
		var bounds = glyph.Bounds;
		bounds.Offset(startPos);
		Glyphs.Add(new TextChunkGlyph
		{
		  TextChunk = this,
		  LineTop = startPos.Y,
		  Index = glyph.Index,
		  Codepoint = glyph.Codepoint,
		  Bounds = bounds,
		  XAdvance = glyph.XAdvance
		});
	  }
	}

	public TextChunkGlyph? GetGlyphInfoByIndex(int index)
	{
	  if (string.IsNullOrEmpty(Text) || index < 0 || index >= Text.Length)
	  {
		return null;
	  }

	  return Glyphs[index];
	}

	public int? GetGlyphIndexByX(int x)
	{
	  if (Glyphs.Count == 0 || x < 0)
	  {
		return null;
	  }

	  var i = 0;
	  for (; i < Glyphs.Count; ++i)
	  {
		var glyph = Glyphs[i];
		var width = glyph.XAdvance;
		var right = glyph.Bounds.X + width;

		if (glyph.Bounds.X <= x && x <= right)
		{
		  if (x - glyph.Bounds.X >= width / 2)
		  {
			++i;
		  }

		  break;
		}
	  }

	  if (i - 1 >= 0 && i - 1 < Glyphs.Count && Glyphs[i - 1].Codepoint == '\n')
	  {
		--i;
	  }

	  return i;
	}

	public override void Draw(FSRenderContext context, Vector2 position, Color color)
	{
	  context.DrawText(Text, Font, position, color, Style, Effect, EffectAmount);
	}
  }
}