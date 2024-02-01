using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace FontStashSharp.Tests
{
	[TestFixture]
	public class RichTextLayoutTests
	{
		[TestCase("First line./nSecond line.", 2, 1, 149, 64)]
		[TestCase("This is /c[red]colored /c[#00f0fa]ext, /cdcolor could be set either /c[lightGreen]by name or /c[#fa9000ff]by hex code.", 1, 6, 844, 32)]
		[TestCase("/esT/eb[2]e/edxt", 1, 3, 52, 32)]
		public void BasicTests(string text, int linesCount, int chunksInFirstLineCount, int width, int height)
		{
			var fontSystem = TestsEnvironment.DefaultFontSystem;

			var richTextLayout = new RichTextLayout
			{
				Text = text,
				Font = fontSystem.GetFont(32)
			};

			Assert.AreEqual(richTextLayout.Lines.Count, linesCount);
			if (linesCount > 0)
			{
				Assert.AreEqual(richTextLayout.Lines[0].Chunks.Count, chunksInFirstLineCount);
			}
			Assert.AreEqual(richTextLayout.Size, new Point(width, height));
		}

		[Test]
		public void NumericParametersTest()
		{
			const string text = "/v[-8]Test/v4Test/vd/es[2]Test/edTest/eb3Test";

			var fontSystem = TestsEnvironment.DefaultFontSystem;

			var richTextLayout = new RichTextLayout
			{
				Text = text,
				Font = fontSystem.GetFont(32),
				ShiftByTop = false
			};

			Assert.AreEqual(richTextLayout.Lines.Count, 1);
			var chunks = richTextLayout.Lines[0].Chunks;
			Assert.AreEqual(chunks.Count, 5);
			Assert.AreEqual(chunks[0].VerticalOffset, -8);
			Assert.AreEqual(chunks[1].VerticalOffset, 4);

			var textChunk = (TextChunk)chunks[2];
			Assert.AreEqual(textChunk.VerticalOffset, 0);
			Assert.AreEqual(textChunk.Effect, FontSystemEffect.Stroked);
			Assert.AreEqual(textChunk.EffectAmount, 2);

			textChunk = (TextChunk)chunks[3];
			Assert.AreEqual(textChunk.Effect, FontSystemEffect.None);
			Assert.AreEqual(textChunk.EffectAmount, 0);

			textChunk = (TextChunk)chunks[4];
			Assert.AreEqual(textChunk.Effect, FontSystemEffect.Blurry);
			Assert.AreEqual(textChunk.EffectAmount, 3);
		}

		[Test]
		public void WrappingTest()
		{
			const string text = "This is the first line. This is the second line. This is the third line.";

			var fontSystem = TestsEnvironment.DefaultFontSystem;

			var richTextLayout = new RichTextLayout
			{
				Text = text,
				Font = fontSystem.GetFont(32),
				Width = 300
			};

			Assert.AreEqual(richTextLayout.Lines.Count, 3);
		}

		[Test]
		public void MeasureUtf32DoesNotThrow()
		{
			var fontSystem = TestsEnvironment.DefaultFontSystem;

			var richTextLayout = new RichTextLayout
			{
				Font = fontSystem.GetFont(32),
				Text = "🙌h📦e l👏a👏zy"
			};

			var size = Point.Zero;
			Assert.DoesNotThrow(() =>
			{
				size = richTextLayout.Size;
			});

			Assert.GreaterOrEqual(size.X, 0);
			Assert.GreaterOrEqual(size.Y, 0);
		}
	}
}
