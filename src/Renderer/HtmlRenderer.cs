using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Renderer
{
    class HtmlRenderer : IDisposable
    {
        struct HtmlStyle
        {
            public bool IsUnicode;
            public byte FontIndex;
            public bool Bold, Italic, Underline, Border;
            public uint Color;
            public bool HasIndentation;
            public Rectangle Margins;
            public uint BackgroundColor;
            public uint LinkColor, VisitedLinkColor;
        }

        private readonly Majestic12.HTMLparser _htmlParser = new Majestic12.HTMLparser();


        public void Draw(UOFontRenderer fontRenderer, Vector2 position, string htmlText)
        {
            if (string.IsNullOrEmpty(htmlText))
            {
                return;
            }

            _htmlParser.Init(htmlText);

            FontSettings fontSettings = new FontSettings();

            while (_htmlParser.HasData)
            {
                var chunk = _htmlParser.ParseNext();

                ParseChunk(chunk, ref fontSettings);
            }
        }


        private void ParseChunk(Majestic12.HTMLchunk chunk, ref FontSettings fontSettings)
        {
            switch (chunk.oType)
            {
                case Majestic12.HTMLchunkType.OpenTag:
                    ParseTag(chunk, ref fontSettings);
                    break;

                case Majestic12.HTMLchunkType.CloseTag:
                    break;

                case Majestic12.HTMLchunkType.Text:
                    break;

                default:
                    Log.Warn($"html perser - unsupported type {chunk.oType} ");
                    break;
            }
        }

        private void ParseTag(Majestic12.HTMLchunk chunk, ref FontSettings fontSettings)
        {
            switch (chunk.sTag)
            {
                case "a": //href
                    break;
                case "body":
                    break;
                case "center":
                    break;
                case "left":
                    break;
                case "right":
                    break;
                case "div":
                    break;
                case "span":
                    break;
                case "font":
                    break;
                case "p": // indent
                    break;
                case "b":
                    fontSettings.Bold = true;
                    break;
                case "i":
                    fontSettings.Italic = true;
                    break;
                case "u":
                    fontSettings.Underline = true;
                    break;
                case "outline":
                    fontSettings.Border = true;
                    break;
                case "bq":
                    break;
                case "basefont":
                    break;
                case "big":
                    fontSettings.FontIndex = 0;
                    break;
                case "medium":
                    fontSettings.FontIndex = 1;
                    break;
                case "small":
                    fontSettings.FontIndex = 2;
                    break;
                case "br":
                    break;
                case "gumpimg":
                    break;
                case "itemimg":
                    break;
                case "h1":
                    fontSettings.FontIndex = 0;
                    fontSettings.Underline = true;
                    fontSettings.Bold = true;
                    break;
                case "h2":
                    fontSettings.FontIndex = 0;
                    fontSettings.Bold = true;
                    break;
                case "h3":
                    fontSettings.FontIndex = 0;
                    break;
                case "h4":
                    fontSettings.FontIndex = 2;
                    fontSettings.Bold = true;
                    break;
                case "h5":
                    fontSettings.FontIndex = 2;
                    fontSettings.Italic = true;
                    break;
                case "h6":
                    fontSettings.FontIndex = 2;
                    break;

                default:
                    break;
            }

            foreach (var param in chunk.oParams)
            {
                var key = param.Key;
                var value = param.Value;

                if (value.StartsWith("0x"))
                {

                }

                if (value.EndsWith("/"))
                {

                }


                switch (key)
                {
                    case "href":

                        fontSettings.Underline = true;

                        break;

                    case "color":
                    case "hovercolor":
                    case "activecolor":
                        break;

                    case "src":
                    case "hoversrc":
                    case "activesrc":
                        break;

                    case "width":
                        break;

                    case "height":
                        break;

                    case "style":
                        break;

                    case "text":
                        break;

                    case "bgcolor":
                        break;

                    case "link":
                        break;
                    case "vlink":
                        break;
                    case "leftmargin":
                        break;
                    case "rightmargin":
                        break;
                    case "topmargin":
                        break;
                    case "bottommargin":
                        break;

                    default:
                        break;

                }
            }
        }

        public void Dispose()
        {
            _htmlParser?.Dispose();
        }
    }
}
