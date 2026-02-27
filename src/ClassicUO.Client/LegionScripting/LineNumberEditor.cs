using System;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    internal class LineNumberEditor : Control
    {
        public const int GutterWidth = 44;
        private const int LINENUM_WIDTH = GutterWidth;
        private const int FONT_SIZE = 20;
        private readonly TextBox _lineNumbers;
        private readonly TTFTextInputField _editor;

        public TTFTextInputField Editor => _editor;
        public string Text { get => _editor.Text; set => _editor.SetText(value ?? ""); }
        public event EventHandler TextChanged { add { _editor.TextChanged += value; } remove { _editor.TextChanged -= value; } }

        public LineNumberEditor(int editorWidth, int editorHeight, string text = "")
        {
            _lineNumbers = new TextBox("1", TrueTypeLoader.EMBEDDED_FONT, FONT_SIZE, LINENUM_WIDTH - 4, 0x8080, FontStashSharp.RichText.TextHorizontalAlignment.Right, strokeEffect: false, supportsCommands: false) { X = 4, Y = 4, AcceptMouseInput = false, MultiLine = true };
            _editor = new TTFTextInputField(editorWidth, editorHeight, text: text ?? "", multiline: true, convertHtmlColors: false) { X = LINENUM_WIDTH, Y = 4 };
            _editor.TextChanged += (s, e) => UpdateLineNumbers();
            Add(_lineNumbers);
            Add(_editor);
            UpdateLineNumbers();
        }

        public void SetText(string text)
        {
            _editor.SetText(text ?? "");
            UpdateLineNumbers();
        }

        public void UpdateSize(int editorWidth, int editorHeight)
        {
            _editor.UpdateSize(editorWidth, editorHeight);
            int lineCount = Math.Max(1, (_editor.Text ?? "").Count(c => c == '\n') + 1);
            int lineH = 24;
            int gutterH = Math.Max(editorHeight, lineCount * lineH);
            _lineNumbers.Height = gutterH;
            _lineNumbers.Width = LINENUM_WIDTH - 8;
            Width = LINENUM_WIDTH + editorWidth;
            Height = Math.Max(_editor.TextBox.TotalHeight, gutterH);
        }

        private void UpdateLineNumbers()
        {
            string t = _editor.Text ?? "";
            int lineCount = Math.Max(1, t.Count(c => c == '\n') + 1);
            _lineNumbers.Text = string.Join("\n", Enumerable.Range(1, lineCount).Select(i => i.ToString()));
        }
    }
}
