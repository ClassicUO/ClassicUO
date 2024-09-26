using System;
using System.Collections.Generic;
using System.Text;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using System.Drawing;
using Color = FontStashSharp.FSColor;
#endif

namespace FontStashSharp.RichText
{
	internal class LayoutBuilder
	{
        private readonly RichTextSettings _richTextSettings;
        public const int NewLineWidth = 0;
		public const string Commands = "cefistv";

		private string _text;
		private SpriteFontBase _font;
		private bool _measureRun;

		private readonly List<TextLine> _lines = new List<TextLine>();
		private TextLine _currentLine;
		private int lineTop, lineBottom;
		private int? width;
		private int _lineCount;
		private int _currentLineWidth;
		private int _currentLineChunks;

		private readonly StringBuilder _stringBuilder = new StringBuilder();
		private Color? _currentColor;
		private SpriteFontBase _currentFont;
		private int _currentVerticalOffset;
		private TextStyle _currentTextStyle;
		private FontSystemEffect _currentEffect;
		private int _currentEffectAmount = 0;

		public List<TextLine> Lines => _lines;

		public int VerticalSpacing { get; set; }
		public bool SupportsCommands { get; set; } = true;
		public bool CalculateGlyphs { get; set; }
		public bool ShiftByTop { get; set; } = true;
		public char CommandPrefix { get; set; } = '/';
        
        public LayoutBuilder(RichTextSettings richTextSettings)
        {
            _richTextSettings = richTextSettings;
        }

        private bool HasIntegerParam(int i)
		{
			if (char.IsDigit(_text[i]) ||
				(_text[i] == '-' && i < _text.Length - 1 && char.IsDigit(_text[i + 1])))
			{
				return true;
			}

			if (_text[i] == '[' && i < _text.Length - 2)
			{
				for (var j = i + 1; j < _text.Length; ++j)
				{
					if (_text[j] == ']')
					{
						// Found enclosing 'j'
						return true;
					}
				}
			}

			return false;
		}

		private bool IsCommand(int i)
		{
			if (!SupportsCommands ||
				i >= _text.Length - 2 ||
				_text[i] != CommandPrefix ||
				Commands.IndexOf(_text[i + 1]) == -1)
			{
				return false;
			}

			++i;

			var command = _text[i];
			if (command == 'e')
			{
				switch (_text[i + 1])
				{
					case 'b':
					case 's':
					case 'd':
						break;
					default:
						return false;
				}
			}
			else if (command == 't')
			{
				switch (_text[i + 1])
				{
					case 's':
					case 'u':
					case 'd':
						break;
					default:
						return false;
				}
			}
			else if (_text[i + 1] == 'd')
			{
				switch (command)
				{
					case 'c':
					case 'f':
					case 'v':
						break;
					default:
						return false;
				}
			}
			else if (command == 's' || command == 'v')
			{
				return HasIntegerParam(i + 1);
			}
			else
			{
				if (_text[i + 1] != '[')
				{
					return false;
				}

				// Find end
				var startPos = i + 2;
				int j;
				var foundEnclosing = false;
				for (j = startPos; j < _text.Length; ++j)
				{
					if (_text[j] == ']')
					{
						// Found enclosing 'j'
						foundEnclosing = true;
						break;
					}
					else if (_text[j] == '[')
					{
						break;
					}
				}

				if (!foundEnclosing)
				{
					return false;
				}
			}

			return true;
		}

		private int? ProcessIntegerParam(ref int i)
		{
			int? startPos = null;
			int endPos = 0;
			if (char.IsDigit(_text[i]) || _text[i] == '-')
			{
				startPos = i;
				do
				{
					++i;
				}
				while (i < _text.Length && char.IsDigit(_text[i]));
				endPos = i;
			}

			if (_text[i] == '[' && i < _text.Length - 2)
			{
				for (var j = i + 1; j < _text.Length; ++j)
				{
					if (_text[j] == ']')
					{
						// Found enclosing 'j'
						startPos = i + 1;
						endPos = j;
						i = j + 1;
						break;
					}
				}
			}

			if (startPos == null)
			{
				return null;
			}

			var parameters = _text.Substring(startPos.Value, endPos - startPos.Value);
			return int.Parse(parameters);
		}

		private bool ProcessCommand(ref int i, ref ChunkInfo r, out bool chunkFilled)
		{
			chunkFilled = false;
			if (!IsCommand(i))
			{
				return false;
			}

			++i;

			var command = _text[i];

			if (command == 'e')
			{
				switch (_text[i + 1])
				{
					case 'b':
						_currentEffect = FontSystemEffect.Blurry;
						_currentEffectAmount = 1;
						break;
					case 's':
						_currentEffect = FontSystemEffect.Stroked;
						_currentEffectAmount = 1;
						break;
					case 'd':
						_currentEffect = FontSystemEffect.None;
						_currentEffectAmount = 0;
						break;
				}

				i += 2;

				var p = ProcessIntegerParam(ref i);
				if (p != null)
				{
					if (p.Value < 0)
					{
						throw new Exception($"Effect amount couldn't be negative {p.Value}");
					}

					_currentEffectAmount = p.Value;
				}
			}
			else if (command == 't')
			{
				switch (_text[i + 1])
				{
					case 's':
						_currentTextStyle = TextStyle.Strikethrough;
						break;
					case 'u':
						_currentTextStyle = TextStyle.Underline;
						break;
					case 'd':
						_currentTextStyle = TextStyle.None;
						break;
				}

				i += 2;
			}
			else if (_text[i + 1] == 'd')
			{
				switch (command)
				{
					case 'c':
						_currentColor = null;
						break;
					case 'f':
						// Switch to default font
						_currentFont = _font;
						break;
					case 'v':
						_currentVerticalOffset = 0;
						break;
				}

				i += 2;
			}
			else if (command == 's')
			{
				++i;
				var p = ProcessIntegerParam(ref i);
				r.Type = ChunkInfoType.Space;
				r.X = p.Value;
				r.Y = 0;
				r.LineEnd = false;
				chunkFilled = true;
			}
			else if (command == 'v')
			{
				++i;
				var p = ProcessIntegerParam(ref i);
				_currentVerticalOffset = p.Value;
			}
			else
			{
				// Find end
				var startPos = i + 2;
				int j;
				for (j = startPos; j < _text.Length; ++j)
				{
					if (_text[j] == ']')
					{
						// Found enclosing ']'
						break;
					}
				}

				var parameters = _text.Substring(startPos, j - startPos);
				switch (command)
				{
					case 'c':
						_currentColor = ColorStorage.FromName(parameters);
						break;

					case 'f':
						if (_richTextSettings.FontResolver == null)
						{
							throw new Exception($"FontResolver isnt set");
						}

						_currentFont = _richTextSettings.FontResolver(parameters);
						break;
					case 'i':
						if (_richTextSettings.ImageResolver == null)
						{
							throw new Exception($"ImageResolver isnt set");
						}

						var renderable = _richTextSettings.ImageResolver(parameters);
						r.Type = ChunkInfoType.Image;
						r.Renderable = renderable;

						r.LineEnd = false;
						chunkFilled = true;

						break;
				}

				i = j + 1;
			}

			return true;
		}

		private ChunkInfo GetNextChunk(ref int i, int? remainingWidth)
		{
			var r = new ChunkInfo
			{
				LineEnd = true
			};

			// Process commands at the beginning of the chunk
			bool chunkFilled;
			while (ProcessCommand(ref i, ref r, out chunkFilled))
			{
				if (chunkFilled)
				{
					// Content chunk(image or space) is filled, return it
					return r;
				}
			}

			_stringBuilder.Clear();

			r.StartIndex = r.EndIndex = i;

			Point? lastBreakMeasure = null;
			var lastBreakIndex = i;

			for (; i < _text.Length; ++i, ++r.EndIndex)
			{
				var c = _text[i];

				if (char.IsHighSurrogate(c))
				{
					_stringBuilder.Append(c);
					continue;
				}

				if (SupportsCommands &&
					c == CommandPrefix &&
					i < _text.Length - 1)
				{
					if (_text[i + 1] == 'n')
					{
						var sz2 = new Point(r.X + NewLineWidth, Math.Max(r.Y, _currentFont.LineHeight));

						// Break right here
						r.X = sz2.X;
						r.Y = sz2.Y;
						i += 2;
						break;
					}

					if (i < _text.Length - 1 && _text[i + 1] == CommandPrefix)
					{
						// Two '\' means one
						++i; ++r.EndIndex;
					}
					else if (IsCommand(i))
					{
						// Return right here, so the command
						// would be processed in the next chunk
						r.LineEnd = false;
						break;
					}
				}

				_stringBuilder.Append(c);

				Point sz;
				if (c != '\n')
				{
					var v = _currentFont.MeasureString(_stringBuilder);
					sz = new Point((int)v.X, _font.LineHeight);
				}
				else
				{
					sz = new Point(r.X + NewLineWidth, Math.Max(r.Y, _font.LineHeight));

					// Break right here
					++r.EndIndex;
					++i;
					r.X = sz.X;
					r.Y = sz.Y;
					break;
				}

				if (remainingWidth != null && sz.X > remainingWidth.Value && i > r.StartIndex &&
					(lastBreakMeasure != null || _currentLineChunks == 0))
				{
					if (lastBreakMeasure != null)
					{
						r.X = lastBreakMeasure.Value.X;
						r.Y = lastBreakMeasure.Value.Y;
						r.EndIndex = i = lastBreakIndex;
					}

					break;
				}

				if (char.IsWhiteSpace(c) || c == '.')
				{
					lastBreakMeasure = sz;
					lastBreakIndex = i + 1;
				}

				r.X = sz.X;
				r.Y = sz.Y;
			}

			return r;
		}

		private void ResetCurrents()
		{
			_currentColor = null;
			_currentFont = _font;
			_currentVerticalOffset = 0;
			_currentTextStyle = TextStyle.None;
			_currentEffect = FontSystemEffect.None;
			_currentEffectAmount = 0;
		}

		private void StartLine(int startIndex, int? rowWidth)
		{
			if (!_measureRun)
			{
				_currentLine = new TextLine
				{
					TextStartIndex = startIndex
				};
			}

			lineTop = 0;
			lineBottom = 0;
			_currentLineWidth = 0;
			_currentLineChunks = 0;
			width = rowWidth;
		}

		private void EndLine(ref Point size)
		{
			var lineHeight = lineBottom - lineTop;
			++_lineCount;

			if (_currentLineWidth > size.X)
			{
				size.X = _currentLineWidth;
			}
			size.Y += lineHeight;

			if (!_measureRun)
			{
				if (ShiftByTop)
				{
					// Shift all chunks top by lineTop
					foreach (var lineChunk in _currentLine.Chunks)
					{
						lineChunk.VerticalOffset -= lineTop;
					}
				}

				_currentLine.Size.Y = lineHeight;

				// New line
				_lines.Add(_currentLine);
			}
		}

		public Point Layout(string text, SpriteFontBase font, int? rowWidth, bool measureRun = false)
		{
			if (!measureRun)
			{
				_lines.Clear();
			}

			_lineCount = 0;
			var size = Utility.PointZero;

			if (string.IsNullOrEmpty(text))
			{
				return size;
			}

			_text = text;
			_font = font;
			_measureRun = measureRun;

			ResetCurrents();

			var i = 0;

			StartLine(0, rowWidth);
			while (i < _text.Length)
			{
				var c = GetNextChunk(ref i, width);

				if (width != null && c.Width > width.Value && _currentLineChunks > 0)
				{
					// New chunk doesn't fit in the line
					// Hence move it to the second
					EndLine(ref size);
					StartLine(i, rowWidth);
					c.LineEnd = false;
				}

				width -= c.Width;
				if (_currentVerticalOffset < lineTop)
				{
					lineTop = _currentVerticalOffset;
				}

				if (_currentVerticalOffset + c.Height > lineBottom)
				{
					lineBottom = _currentVerticalOffset + c.Height;
				}

				_currentLineWidth += c.Width;

				if (!_measureRun)
				{
					Point? startPos = null;
					if (CalculateGlyphs)
					{
						startPos = new Point(_currentLine.Size.X, size.Y);
					}
					_currentLine.Size.X += c.Width;

					BaseChunk chunk = null;
					switch (c.Type)
					{
						case ChunkInfoType.Text:
							var t = _text.Substring(c.StartIndex, c.EndIndex - c.StartIndex);
							if (SupportsCommands)
							{
								t = t.Replace("//", "/");
							}
							var textChunk = new TextChunk(_currentFont, t, new Point(c.X, c.Y), startPos)
							{
								Style = _currentTextStyle,
								Effect = _currentEffect,
								EffectAmount = _currentEffectAmount,
							};
							chunk = textChunk;
							break;
						case ChunkInfoType.Space:
							chunk = new SpaceChunk(c.X);
							break;
						case ChunkInfoType.Image:
							chunk = new ImageChunk(c.Renderable);
							break;
					}

					chunk.Color = _currentColor;
					chunk.VerticalOffset = _currentVerticalOffset;

					var asText = chunk as TextChunk;
					if (asText != null)
					{
						_currentLine.Count += asText.Count;
					}

					_currentLine.Chunks.Add(chunk);
				}

				++_currentLineChunks;

				if (c.LineEnd)
				{
					EndLine(ref size);
					StartLine(i, rowWidth);
				}
			}

			// Add last line if it isnt empty
			if (_currentLineChunks > 0)
			{
				EndLine(ref size);
			}

			// If text ends with '\n', then add additional line
			if (_text[_text.Length - 1] == '\n')
			{
				var lineSize = _currentFont.MeasureString(" ");
				if (!_measureRun)
				{
					var additionalLine = new TextLine
					{
						TextStartIndex = _text.Length
					};

					additionalLine.Size.Y = (int)lineSize.Y;

					_lines.Add(additionalLine);
				}

				size.Y += (int)lineSize.Y;
			}

			// Index lines and chunks
			if (!_measureRun)
			{
				for (i = 0; i < _lines.Count; ++i)
				{
					_currentLine = _lines[i];
					_currentLine.LineIndex = i;

					for (var j = 0; j < _currentLine.Chunks.Count; ++j)
					{
						var chunk = _currentLine.Chunks[j];
						chunk.LineIndex = _currentLine.LineIndex;
						chunk.ChunkIndex = j;
					}
				}
			}

			size.Y += (_lineCount - 1) * VerticalSpacing;

			return size;
		}
	}
}
