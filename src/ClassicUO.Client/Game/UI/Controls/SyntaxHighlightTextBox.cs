#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.

#endregion

using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.UI.Controls
{
    public class SyntaxHighlightTextBox : StbTextBox
    {
        private static readonly string[] Keywords = new[]
        {
            "if", "else", "for", "while", "return", "var", "void", "int", "string", "bool",
            "true", "false", "null", "new", "class", "public", "private", "static", "using",
            "namespace", "this", "base", "override", "virtual", "async", "await", "try",
            "catch", "finally", "throw", "in", "out", "ref", "const", "readonly"
        };

        private static readonly HashSet<string> KeywordSet = new HashSet<string>(Keywords, StringComparer.Ordinal);

        private static readonly Regex TokenRegex = new Regex(
            @"(//[^\n]*)|(""(?:[^""\\]|\\.)*"")|('(?:[^'\\]|\\.)*')|(\b\d+\.?\d*\b)|(\b\w+\b)|(.)",
            RegexOptions.Compiled
        );

        private const ushort HueDefault = 0x0386;
        private const ushort HueKeyword = 0x0059;
        private const ushort HueString = 0x0026;
        private const ushort HueComment = 0x0035;
        private const ushort HueNumber = 0x0025;

        public SyntaxHighlightTextBox(
            int maxCharCount = -1,
            int maxWidth = 0,
            bool multiline = true)
            : base(1, maxCharCount, maxWidth, true, FontStyle.None, HueDefault)
        {
            Multiline = multiline;
            AllowTAB = true;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (batcher.ClipBegin(x, y, Width, Height))
            {
                base.Draw(batcher, x, y);
                DrawSelection(batcher, x, y);
                DrawSyntaxHighlightedText(batcher, x, y);
                DrawCaret(batcher, x, y);
                batcher.ClipEnd();
            }
            return true;
        }

        private void DrawSyntaxHighlightedText(UltimaBatcher2D batcher, int x, int y)
        {
            string text = Text;
            if (string.IsNullOrEmpty(text))
                return;

            MultilinesFontInfo info = _rendererText.GetInfo();
            if (info == null)
                return;

            int drawY = 1;
            int charStart = 0;

            while (info != null)
            {
                int charEnd = charStart + info.CharCount;
                if (charStart < text.Length)
                {
                    int len = Math.Min(info.CharCount, text.Length - charStart);
                    string lineText = text.Substring(charStart, len);
                    DrawHighlightedLine(batcher, x, y + drawY, lineText);
                }
                charStart = charEnd;
                drawY += info.MaxHeight;
                info = info.Next;
            }
        }

        private void DrawHighlightedLine(UltimaBatcher2D batcher, int baseX, int baseY, string line)
        {
            int drawX = 0;
            var matches = TokenRegex.Matches(line);

            foreach (Match m in matches)
            {
                if (!m.Success || m.Length == 0)
                    continue;

                string token = m.Value;
                ushort hue = GetTokenHue(token, m.Groups);
                var rt = RenderedText.Create(
                    token,
                    hue,
                    _rendererText.Font,
                    _rendererText.IsUnicode,
                    _rendererText.FontStyle,
                    TEXT_ALIGN_TYPE.TS_LEFT,
                    0
                );
                rt.Draw(batcher, baseX + drawX, baseY);
                drawX += GetTokenWidth(token);
                rt.Destroy();
            }
        }

        private static ushort GetTokenHue(string token, GroupCollection groups)
        {
            if (groups[1].Success)
                return HueComment;
            if (groups[2].Success || groups[3].Success)
                return HueString;
            if (groups[4].Success)
                return HueNumber;
            if (groups[5].Success && KeywordSet.Contains(token))
                return HueKeyword;
            return HueDefault;
        }

        private int GetTokenWidth(string token)
        {
            if (string.IsNullOrEmpty(token))
                return 0;
            return _rendererText.IsUnicode
                ? FontsLoader.Instance.GetWidthUnicode(_rendererText.Font, token)
                : FontsLoader.Instance.GetWidthASCII(_rendererText.Font, token);
        }
    }
}
