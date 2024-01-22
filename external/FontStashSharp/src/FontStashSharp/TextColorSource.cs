using System.Text;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using Color = FontStashSharp.FSColor;
#endif

namespace FontStashSharp
{
	internal ref struct TextColorSource
	{
		public TextSource TextSource;
		public Color? SingleColor;
		public Color[] Colors;
		public int ColorPosition;

		public TextColorSource(string text, Color color)
		{
			TextSource = new TextSource(text);
			SingleColor = color;
			Colors = null;
			ColorPosition = 0;
		}

		public TextColorSource(string text, Color[] colors)
		{
			TextSource = new TextSource(text);
			SingleColor = null;
			Colors = colors;
			ColorPosition = 0;
		}

		public TextColorSource(StringSegment text, Color color)
		{
			TextSource = new TextSource(text);
			SingleColor = color;
			Colors = null;
			ColorPosition = 0;
		}

		public TextColorSource(StringSegment text, Color[] colors)
		{
			TextSource = new TextSource(text);
			SingleColor = null;
			Colors = colors;
			ColorPosition = 0;
		}

		public TextColorSource(StringBuilder text, Color color)
		{
			TextSource = new TextSource(text);
			SingleColor = color;
			Colors = null;
			ColorPosition = 0;
		}

		public TextColorSource(StringBuilder text, Color[] colors)
		{
			TextSource = new TextSource(text);
			SingleColor = null;
			Colors = colors;
			ColorPosition = 0;
		}

		public bool IsNull => TextSource.IsNull;

		[System.Obsolete("Possible phase out.")]
		public bool GetNextCodepoint(out int codepoint, out Color color)
		{
			color = Color.Transparent;
			if (!TextSource.GetNextCodepoint(out codepoint))
			{
				return false;
			}

			if (SingleColor != null)
			{
				color = SingleColor.Value;
			}
			else
			{
				color = Colors[ColorPosition];
				++ColorPosition;
			}

			return true;
		}
		
		public bool GetNextCodepoint(out int codepoint)
		{
			return TextSource.GetNextCodepoint(out codepoint);
		}
		
		public Color GetNextColor()
		{
			var color = Color.Transparent;
			
			if (SingleColor != null)
			{
				color = SingleColor.Value;
			}
			else
			{
				color = Colors[ColorPosition % Colors.Length];
				++ColorPosition;
			}
			
			return color;
		}
	}
}
