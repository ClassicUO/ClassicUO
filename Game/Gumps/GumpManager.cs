using ClassicUO.Game.Renderer;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.Gumps
{
    public static class GumpManager
    {
        private static readonly List<GumpControl> _gumps = new List<GumpControl>();


        public static GumpControl Create(in Serial sender, in Serial gumpID, in int x, in int y, in string layout, in string[] lines)
        {
            List<string> pieces = new List<string>();
            int index = 0;
            GumpControl gump = new GumpControl()
            {
                LocalSerial = sender,
                ServerSerial = gumpID,
                X = x,
                Y = y,
            };

            while (index < layout.Length)
            {
                if (layout.Substring(index) == "\0")
                {
                    break;
                }

                int begin = layout.IndexOf("{", index);
                int end = layout.IndexOf("}", index + 1);

                if (begin != -1 && end != -1)
                {
                    string sub = layout.Substring(begin + 1, end - begin - 1).Trim();
                    pieces.Add(sub);
                    index = end;

                    string[] gparams = Regex.Split(sub, @"\s+");

                    switch (gparams[0].ToLower())
                    {
                        case "button":
                            gump.AddChildren(new Button(gparams));
                            break;
                        case "buttontileart":
                            gump.AddChildren(new Button(gparams));
                            gump.AddChildren(new StaticPic(Graphic.Parse(gparams[8]), Hue.Parse(gparams[9]))
                            {
                                X = int.Parse(gparams[1]) + int.Parse(gparams[10]),
                                Y = int.Parse(gparams[2]) + int.Parse(gparams[11])
                            });
                            break;
                        case "checkertrans":
                            gump.AddChildren(new CheckerTrans(gparams));
                            break;
                        case "croppedtext":
                            gump.AddChildren(new CroppedText(gparams, lines));
                            break;
                        case "gumppic":
                            gump.AddChildren(new GumpPic(gparams));
                            break;
                        case "gumppictiled":
                            gump.AddChildren(new GumpPicTiled(gparams));
                            break;
                        case "htmlgump":
                            gump.AddChildren(new HtmlGump(gparams, lines));
                            break;
                        case "page":
                            break;
                        case "resizepic":
                            break;
                        case "text":
                            break;
                        case "textentry":
                            break;
                        case "textentrylimited":
                            break;
                        case "tilepic":
                            break;
                        case "tilepichue":
                            break;
                        case "noclose":
                            break;
                        case "nodispose":
                            break;
                        case "nomove":
                            break;
                        case "group":
                            break;
                        case "endgroup":
                            break;
                        case "radio":
                            break;
                        case "checkbox":
                            break;
                        case "xmfhtmlgump":
                            break;
                        case "xmfhtmlgumpcolor":
                            break;
                        case "xmfhtmltok":
                            break;
                        case "tooltip":
                            break;
                        case "noresize":
                            break;
                        default:
                            break;
                    }

                }
                else
                {
                    break;
                }
            }


            _gumps.Add(gump);
            return gump;
        }

        public static void Update(in double ms)
        {
            for (int i = 0; i < _gumps.Count; i++)
            {
                _gumps[i].Update(ms);
            }
        }

        public static void Render(in SpriteBatch3D spriteBatch)
        {
            for (int i = 0; i < _gumps.Count; i++)
            {
                var g = _gumps[i];
                g.Draw(spriteBatch,
                    new Microsoft.Xna.Framework.Vector3(g.X, g.Y, 0));
            }
        }
    }
}
