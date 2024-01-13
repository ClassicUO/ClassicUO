using System.Text;

namespace FontStashSharp
{
  internal ref struct TextSource
  {
	public StringSegment StringText;
	public StringBuilder StringBuilderText;
	private int Position;

	public TextSource(string text)
	{
	  StringText = new StringSegment(text);
	  StringBuilderText = null;
	  Position = 0;
	}

	public TextSource(StringSegment text)
	{
	  StringText = text;
	  StringBuilderText = null;
	  Position = 0;
	}

	public TextSource(StringBuilder text)
	{
	  StringText = new StringSegment();
	  StringBuilderText = text;
	  Position = 0;
	}

	public bool IsNull => StringText.IsNullOrEmpty && StringBuilderText == null;

	public bool GetNextCodepoint(out int result)
	{
	  result = 0;

	  if (!StringText.IsNullOrEmpty)
	  {
		if (Position >= StringText.Length)
		{
		  return false;
		}

		result = char.ConvertToUtf32(StringText.String, StringText.Offset + Position);
		Position += char.IsSurrogatePair(StringText.String, StringText.Offset + Position) ? 2 : 1;
		return true;
	  }

	  if (StringBuilderText != null)
	  {
		if (Position >= StringBuilderText.Length)
		{
		  return false;
		}

		result = StringBuilderConvertToUtf32(StringBuilderText, Position);
		Position += StringBuilderIsSurrogatePair(StringBuilderText, Position) ? 2 : 1;
		return true;
	  }

	  return false;
	}

	public void Reset()
	{
	  Position = 0;
	}

	private static bool StringBuilderIsSurrogatePair(StringBuilder sb, int index)
	{
	  if (index + 1 < sb.Length)
		return char.IsSurrogatePair(sb[index], sb[index + 1]);
	  return false;
	}

	private static int StringBuilderConvertToUtf32(StringBuilder sb, int index)
	{
	  if (!char.IsHighSurrogate(sb[index]))
		return sb[index];

	  return char.ConvertToUtf32(sb[index], sb[index + 1]);
	}

	public static int CalculateLength(string text)
	{
	  if (string.IsNullOrEmpty(text))
	  {
		return 0;
	  }

	  var pos = 0;
	  var result = 0;
	  while (pos < text.Length)
	  {
		pos += char.IsSurrogatePair(text, pos) ? 2 : 1;
		++result;
	  }

	  return result;
	}
  }
}