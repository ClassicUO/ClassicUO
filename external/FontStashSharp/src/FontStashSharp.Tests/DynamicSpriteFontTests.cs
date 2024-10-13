using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NUnit.Framework;

namespace FontStashSharp.Tests
{
	[TestFixture]
	public class DynamicSpriteFontTests
	{
		[Test]
		public void CacheNull()
		{
			var fontSystem = TestsEnvironment.DefaultFontSystem;

			var font = fontSystem.GetFont(32);

			// Such symbol doesnt exist in ttf
			var codePoint = 12345678;

			// Shouldn't exist
			var glyphs = font.GetGlyphs(FontSystemEffect.None, 0);
			DynamicFontGlyph glyph;
			Assert.IsFalse(glyphs.TryGetValue(codePoint, out glyph));

			glyph = (DynamicFontGlyph)font.GetGlyph(TestsEnvironment.GraphicsDevice, codePoint, FontSystemEffect.None, 0);

			// Now it should exist
			Assert.IsTrue(glyphs.TryGetValue(codePoint, out glyph));

			// And should be equal to null too
			Assert.AreEqual(glyph, null);
		}

		[Test]
		public void ExistingTexture()
		{
			Texture2D existingTexture;
			using (var stream = TestsEnvironment.Assembly.OpenResourceStream("Resources.default_ui_skin.png"))
			{
				existingTexture = Texture2D.FromStream(TestsEnvironment.GraphicsDevice, stream);
			}

			var settings = new FontSystemSettings
			{
				ExistingTexture = existingTexture,
				ExistingTextureUsedSpace = new Rectangle(0, 0, existingTexture.Width, 160)
			};

			var fontSystem = TestsEnvironment.CreateDefaultFontSystem(settings);

			var atlasFull = false;
			fontSystem.CurrentAtlasFull += (s, a) => atlasFull = true;

			for (var size = 64; size < 128; ++size)
			{
				var font = fontSystem.GetFont(size);
				for (var codePoint = (int)'a'; codePoint < 'z'; ++codePoint)
				{
					var glyph = (DynamicFontGlyph)font.GetGlyph(TestsEnvironment.GraphicsDevice, codePoint, FontSystemEffect.None, 0);

					// Make sure glyph doesnt intersects with the used space
					if (!atlasFull)
					{
						Assert.IsFalse(settings.ExistingTextureUsedSpace.Intersects(glyph.TextureRectangle));
					}
					else
					{
						// If we've moved to the next atlas
						// The new glyph should override old existing texture used space
						Assert.IsTrue(settings.ExistingTextureUsedSpace.Intersects(glyph.TextureRectangle));

						// This concludes the testing
						goto finish;
					}
				}
			}

		finish:
			;
		}

		private class RendererCall
		{
			public Texture2D Texture;
			public Vector2 Pos;
			public Rectangle? Source;
			public Color Color;
			public float Rotation;
			public Vector2 Scale;
			public float Depth;
		}

		private class Renderer : IFontStashRenderer
		{
			public GraphicsDevice GraphicsDevice => TestsEnvironment.GraphicsDevice;
			public readonly System.Collections.Generic.List<RendererCall> Calls = new System.Collections.Generic.List<RendererCall>();

			public void Draw(Texture2D texture, Vector2 pos, Rectangle? src, Color color, float rotation, Vector2 scale, float depth)
			{
				Calls.Add(new RendererCall
				{
					Texture = texture,
					Pos = pos,
					Source = src,
					Color = color,
					Rotation = rotation,
					Scale = scale,
					Depth = depth
				});
			}
		}

		[TestCase("Tuesday", 45, 4, true, new int[] { 2, 9, 22, 17, 43, 18, 63, 17, 86, 10, 109, 17, 132, 18 })]
		[TestCase("Tuesday", 45, 4, false, new int[] { 2, 9, 24, 17, 45, 18, 65, 17, 88, 10, 111, 17, 134, 18 })]
		[TestCase("Tuesday", 45.5f, 4, true, new int[] { 2, 10, 22, 18, 43, 19, 63, 18, 87, 11, 110, 18, 133, 19 })]
		public void DrawText(string text, float size, int characterSpacing, bool useKernings, int[] glyphPos)
		{
			var settings = new FontSystemSettings();
			var fontSystem = new FontSystem(settings)
			{
				UseKernings = useKernings
			};
			fontSystem.AddFont(TestsEnvironment.Assembly.ReadResourceAsBytes("Resources.Komika.ttf"));

			var renderer = new Renderer();
			var font = fontSystem.GetFont(size);

			font.DrawText(renderer, text, Vector2.Zero, Color.White, characterSpacing: characterSpacing);

			Assert.AreEqual(glyphPos.Length, renderer.Calls.Count * 2);
			for (var i = 0; i < renderer.Calls.Count; i++)
			{
				Assert.AreEqual(glyphPos[i * 2], (int)renderer.Calls[i].Pos.X);
				Assert.AreEqual(glyphPos[i * 2 + 1], (int)renderer.Calls[i].Pos.Y);
			}
		}
	}
}
