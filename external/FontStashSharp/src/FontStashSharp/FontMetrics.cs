namespace FontStashSharp
{
	internal struct FontMetrics
	{
		public int Ascent { get; private set; }
		public int Descent { get; private set; }
		public int LineHeight { get; private set; }

		public FontMetrics(int ascent, int descent, int lineHeight)
		{
			Ascent = ascent;
			Descent = descent;
			LineHeight = lineHeight;
		}
	}
}