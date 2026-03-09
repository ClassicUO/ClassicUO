using System;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    internal class LineNumberEditor : Control
    {
        public const int GutterWidth = 48;
        private const int LINENUM_WIDTH = GutterWidth;
        private readonly UOLabel _lineNumbers;
        private readonly TTFTextInputField _editor;

        public TTFTextInputField Editor => _editor;
        public string Text { get => _editor.Text; set => _editor.SetText(value ?? ""); }
        public event EventHandler TextChanged { add { _editor.TextChanged += value; } remove { _editor.TextChanged -= value; } }

        private static readonly Color GutterBgColor = new Color(37, 37, 38, 255);
        private readonly AlphaBlendControl _gutterBg;

        public LineNumberEditor(int editorWidth, int editorHeight, string text = "")
        {
            _gutterBg = new AlphaBlendControl(1f) { X = 0, Y = 0, Width = LINENUM_WIDTH, Height = editorHeight, BaseColor = GutterBgColor };
            Add(_gutterBg);
            _lineNumbers = new UOLabel("1", 1, 0x0386, ClassicUO.Assets.TEXT_ALIGN_TYPE.TS_RIGHT, LINENUM_WIDTH - 8, ClassicUO.Game.FontStyle.None) { X = 4, Y = 4 };
            _editor = new TTFTextInputField(editorWidth, editorHeight, text: text ?? "", multiline: true, convertHtmlColors: false, codeEditorStyle: true) { X = LINENUM_WIDTH, Y = 4 };
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
            _gutterBg.Height = gutterH;
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
