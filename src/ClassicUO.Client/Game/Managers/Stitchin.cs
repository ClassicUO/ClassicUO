using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.Game.Managers
{
    sealed class Stitchin
    {
        private readonly static char[] _split = new char[] { ' ', '\t' };

        public void Read()
        {
            var filePath = UOFileManager.GetUOFilePath("stitchin.def");

            if (!File.Exists(filePath))
            {
                return;
            }

            const string END_DEF = "# enddef";

            using (var reader = new StreamReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                var list = new List<string>();
                var started = false;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    {
                        continue;
                    }

                    if (!started && line.StartsWith("# ") && !line.StartsWith(END_DEF))
                    {
                        started = true;
                    }

                    if (started)
                    {
                        list.Add(line);
                    }

                    if (started && line.StartsWith(END_DEF))
                    {
                        Work(list);

                        list.Clear();
                    }
                }
            }
        }

        private void Work(List<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return;
            }

            foreach (var line in list)
            {
                var args = line.Split(_split, StringSplitOptions.RemoveEmptyEntries);

                BuildCommand(args);
            }
        }

        private void BuildCommand(string[] arguments)
        {
            if (arguments== null || arguments.Length == 0)
            {
                return;
            }

            var cmd = arguments[0];

            switch (cmd)
            {
                // coveredBy LAYER0 LAYER1 ....
                case "coveredBy":

                    for (int i = 1; i < arguments.Length; ++i)
                    {
                        var layer = GetLayer(arguments[i]);
                    }

                    break;

                // covers LAYER0 LAYER1 ....
                case "covers":

                    for (int i = 1; i < arguments.Length; ++i)
                    {
                        var layer = GetLayer(arguments[i]);
                    }

                    break;

                // replace XXX with YYY
                case "replace" when arguments.Length == 4:
                    var item0 = arguments[1];
                    var item1 = arguments[3];
                    break;

                // remove XXX YYY ZZZ ...
                case "remove":

                    break;
            }
        }

        private Layer? GetLayer(string layerName)
        {
            switch (layerName.ToUpper())
            {
                case "HEAD": return Layer.Helmet;
                case "FACE": return Layer.Face;
                case "EARS": return Layer.Earrings;
                case "NECK": return Layer.Necklace;
                case "TORSO": return Layer.Torso;
                case "UPPER_ARMS_TOP": return Layer.Invalid;
                case "UPPER_ARMS_BOTTOM": return Layer.Invalid;
                case "LOWER_ARMS_TOP": return Layer.Invalid;
                case "LOWER_ARMS_BOTTOM": return Layer.Invalid;
                case "HANDS": return Layer.Gloves;
                case "PELVIS": return Layer.Waist;
                case "UPPER_LEGS_TOP": return Layer.Invalid;
                case "UPPER_LEGS_BOTTOM": return Layer.Invalid;
                case "LOWER_LEGS_TOP": return Layer.Invalid;
                case "LOWER_LEGS_BOTTOM": return Layer.Invalid;
                case "FEET": return Layer.Shoes;   
            }

            Log.Warn($"layer not handled: {layerName}");

            return null;
        }
    }
}
