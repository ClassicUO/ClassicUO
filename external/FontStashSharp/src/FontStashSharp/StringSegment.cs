using System;

namespace FontStashSharp
{
	public readonly ref struct StringSegment
	{
		public readonly string String;
		public readonly int Offset;
		public readonly int Length;

		public char this[int index] => String[Offset + index];

		public bool IsNullOrEmpty
		{
			get
			{
				if (String == null) return true;

				return Offset >= String.Length;
			}
		}

		public StringSegment(string value)
		{
			String = value;
			Offset = 0;
			Length = value != null ? value.Length : 0;
		}

		public StringSegment(string value, int offset)
		{
			String = value;
			Offset = offset;
			Length = value != null ? value.Length - offset : 0;
		}

		public StringSegment(string value, int offset, int length)
		{
			String = value;
			Offset = offset;
			Length = length;
		}

		public bool Equals(StringSegment b) => String == b.String && Offset == b.Offset && Length == b.Length;

		public override bool Equals(object obj) => throw new NotSupportedException();

		public override int GetHashCode() => IsNullOrEmpty ? 0 : String.GetHashCode() ^ Offset ^ Length;

		public override string ToString() => IsNullOrEmpty ? string.Empty : String.Substring(Offset, Length);

		public static bool operator ==(StringSegment a, StringSegment b) => a.Equals(b);

		public static bool operator !=(StringSegment a, StringSegment b) => !a.Equals(b);
	}
}