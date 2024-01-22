using NUnit.Framework;
using System.Linq;

namespace FontStashSharp.Tests
{
	[TestFixture]
	public class StaticSpriteFontTests
	{
		[Test]
		public void Load()
		{
			var assembly = TestsEnvironment.Assembly;
			var data = assembly.ReadResourceAsString("Resources.arial64.fnt");

			var font = StaticSpriteFont.FromBMFont(data, fileName => assembly.OpenResourceStream("Resources." + fileName), TestsEnvironment.GraphicsDevice);

			Assert.AreEqual(font.FontSize, 63);
			Assert.AreEqual(font.Glyphs.Count, 191);

			var texture = font.Glyphs.First().Value.Texture;

			Assert.NotNull(texture);
			Assert.AreEqual(texture.Width, 512);
			Assert.AreEqual(texture.Height, 512);
		}
	}
}
