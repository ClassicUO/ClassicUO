using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.Gumps
{
    public static class GumpManager
    {
        public static GumpControl Create(in string layout, in string[] lines)
        {
            List<string> pieces = new List<string>();
            int index = 0;
            GumpControl gump = new GumpControl(null);

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
                            new Button(gump, gparams);
                            break;
                        case "buttontileart":
                            new Button(gump, gparams);
                            new StaticPic(gump, Graphic.Parse(gparams[8]), Hue.Parse(gparams[9]))
                            {
                                X = int.Parse(gparams[1]) + int.Parse(gparams[10]),
                                Y = int.Parse(gparams[2]) + int.Parse(gparams[11])
                            };
                            break;
                        case "checkertrans":
                            new CheckerTrans(gump, gparams);
                            break;
                        case "croppedtext":
                            new CroppedText(gump, gparams, lines);
                            break;
                        case "gumppic":
                            break;
                        case "gumppictiled":
                            break;
                        case "htmlgump":
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



            return gump;
        }
    }
}
