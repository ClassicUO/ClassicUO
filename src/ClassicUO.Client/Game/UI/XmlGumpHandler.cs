using ClassicUO.Game.UI.Gumps;
using System.Xml;
using System.IO;
using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.UI
{
    internal class XmlGumpHandler
    {
        public static string XmlGumpPath { get => Path.Combine(CUOEnviroment.ExecutablePath, "Data", "XmlGumps"); }

        public static XmlGump CreateGumpFromFile(string filePath)
        {
            XmlGump gump = new XmlGump();
            gump.CanCloseWithRightClick = true;
            gump.AcceptMouseInput = true;
            gump.CanMove = true;

            if (File.Exists(filePath))
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.LoadXml(File.ReadAllText(filePath));
                }
                catch (Exception e) { GameActions.Print(e.Message); }

                if (xmlDoc.DocumentElement != null)
                {
                    XmlElement root = xmlDoc.DocumentElement;

                    foreach (XmlAttribute attr in root.Attributes)
                    {
                        switch (attr.Name.ToLower())
                        {
                            case "x":
                                int.TryParse(attr.Value, out gump.X);
                                break;
                            case "y":
                                int.TryParse(attr.Value, out gump.Y);
                                break;
                        }
                    }

                    ProcessChildNodes(gump, root);
                }
                gump.ForceSizeUpdate();
            }

            return gump;
        }

        public static string[] GetAllXmlGumps()
        {
            List<string> fileList = new List<string>();

            try
            {
                if (Directory.Exists(XmlGumpPath))
                {
                    string[] allFiles = Directory.GetFiles(XmlGumpPath, "*.xml");
                    foreach (string file in allFiles)
                    {
                        fileList.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
                else
                {
                    Directory.CreateDirectory(XmlGumpPath);
                }
            }
            catch { }

            return fileList.ToArray();
        }

        private static void ProcessChildNodes(XmlGump gump, XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                switch (child.NodeType)
                {
                    case XmlNodeType.Element:
                        if (child.Name.ToLower().Equals("text"))
                        {
                            HandleTextTag(gump, child);
                            break;
                        }
                        if (child.Name.ToLower().Equals("colorbox"))
                        {
                            HandleColorBox(gump, child);
                            break;
                        }
                        if (child.Name.ToLower().Equals("image"))
                        {
                            HandleImage(gump, child);
                            break;
                        }
                        if (child.Name.ToLower().Equals("image_progress_bar"))
                        {
                            HandleImageProgressBar(gump, child);
                            break;
                        }
                        if (child.Name.ToLower().Equals("color_progress_bar"))
                        {
                            HandleColorProgressBar(gump, child);
                            break;
                        }
                        if (child.Name.ToLower().Equals("control"))
                        {
                            HandleControl(gump, child);
                            break;
                        }
                        if (child.Name.ToLower().Equals("simple_border"))
                        {
                            HandleBorder(gump, child);
                            break;
                        }
                        if (child.Name.ToLower().Equals("macro_button_color"))
                        {
                            HandleMacroButton(gump, child);
                            break;
                        }
                        if (child.Name.ToLower().Equals("macro_button_graphic"))
                        {
                            HandleMacroButtonGraphic(gump, child);
                            break;
                        }
                        break;
                }
            }
        }

        private static void HandleMacroButtonGraphic(XmlGump gump, XmlNode node)
        {
            HitBox hb = new HitBox(0, 0, 0, 0, null, 0);
            ApplyBasicAttributes(hb, node);


            GumpPic bg = new GumpPic(0, 0, 0, 0);

            hb.Add(bg);

            ushort hue = 0, graphic = 0;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "id":
                        if(ushort.TryParse(attr.Value, out ushort gra))
                        {
                            bg.Graphic = graphic = gra;
                            if(hb.Width < 1)
                            {
                                hb.ForceSizeUpdate();
                            }
                        }
                        break;
                    case "id_hover":
                        if (ushort.TryParse(attr.Value, out ushort gra_hover))
                        {
                            hb.MouseEnter += (s, e) => { bg.Graphic = gra_hover; };
                            hb.MouseExit += (s, e) => { bg.Graphic = graphic; };
                        }
                        break;
                    case "alpha":
                        if (float.TryParse(attr.Value, out float a))
                        {
                            bg.Alpha = a;
                        }
                        break;
                    case "hue":
                        if (ushort.TryParse(attr.Value, out ushort h))
                        {
                            bg.Hue = hue = h;
                        }
                        break;
                    case "hue_hover":
                        if (ushort.TryParse(attr.Value, out ushort hh))
                        {
                            hb.MouseEnter += (s, e) => { bg.Hue = hh; };
                            hb.MouseExit += (s, e) => { bg.Hue = hue; };
                        }
                        break;
                    case "macro":
                        MacroManager manager = Client.Game.GetScene<GameScene>().Macros;
                        Macro m = manager.FindMacro(attr.Value);
                        if (m != null)
                        {
                            hb.MouseUp += (s, e) => { if (e.Button == Input.MouseButtonType.Left) { manager.SetMacroToExecute(m.Items as MacroObject); } };
                        }
                        break;
                }
            }

            gump.Add(hb);
        }

        private static void HandleMacroButton(XmlGump gump, XmlNode node)
        {
            HitBox hb = new HitBox(0, 0, 0, 0, null, 0);
            ApplyBasicAttributes(hb, node);

            AlphaBlendControl bg = new AlphaBlendControl() { Width = hb.Width, Height = hb.Height };
            hb.Add(bg);

            ushort hue = 0;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {

                    case "alpha":
                        if (float.TryParse(attr.Value, out float a))
                        {
                            bg.Alpha = a;
                        }
                        break;
                    case "hue":
                        if(ushort.TryParse(attr.Value, out ushort h))
                        {
                            bg.Hue = hue = h;
                        }
                        break;
                    case "hue_hover":
                        if (ushort.TryParse(attr.Value, out ushort hh))
                        {
                            hb.MouseEnter += (s, e) => { bg.Hue = hh; };
                            hb.MouseExit += (s, e) => { bg.Hue = hue; };
                        }
                        break;
                    case "macro":
                        MacroManager manager = Client.Game.GetScene<GameScene>().Macros;
                        Macro m = manager.FindMacro(attr.Value);
                        if(m != null){
                            hb.MouseUp += (s, e) => { if (e.Button == Input.MouseButtonType.Left) { manager.SetMacroToExecute(m.Items as MacroObject); } };
                        }
                        break;
                }
            }

            gump.Add(hb);
        }

        private static void HandleBorder(XmlGump gump, XmlNode node)
        {
            ushort hue = 0;
            int width = 0, height = 0;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "hue":
                        ushort.TryParse(attr.Value, out hue);
                        break;
                    case "width":
                        int.TryParse(attr.Value, out width); //Special handling of width and height required for this tag
                        break;
                    case "height":
                        int.TryParse(attr.Value, out height);
                        break;
                }
            }

            gump.Add(ApplyBasicAttributes(new SimpleBorder() { Hue = hue, Width = width, Height = height }, node));
        }

        private static void HandleControl(XmlGump gump, XmlNode node)
        {
            XmlGump newControl = new XmlGump();
            ApplyBasicAttributes(newControl, node);

            ProcessChildNodes(newControl, node);

            if (newControl.Width < 1)
            {
                newControl.ForceSizeUpdate();
            }

            gump.Add(newControl);
        }

        private static void HandleColorProgressBar(XmlGump gump, XmlNode node)
        {
            ushort bg_hue = 0, fg_hue = 0;
            int value = 0, maxval = 0;
            bool needsUpdates = false;
            string originalValue = string.Empty, originalMaxVal = string.Empty;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "background_hue":
                        ushort.TryParse(attr.Value, out bg_hue);
                        break;
                    case "foreground_hue":
                        ushort.TryParse(attr.Value, out fg_hue);
                        break;
                    case "value":
                        originalValue = attr.Value;
                        if (!int.TryParse(attr.Value, out value))
                        {
                            int.TryParse(FormatText(attr.Value), out value);
                        }
                        break;
                    case "max_value":
                        originalMaxVal = attr.Value;
                        if (!int.TryParse(attr.Value, out maxval))
                        {
                            int.TryParse(FormatText(attr.Value), out maxval);
                        }
                        break;
                    case "updates":
                        bool.TryParse(attr.Value, out needsUpdates);
                        break;
                }
            }
            Control c;
            gump.Add(c = ApplyBasicAttributes(new ColorBox(0, 0, bg_hue) { AcceptMouseInput = false }, node));
            int maxWidth = c.Width;
            gump.Add(c = ApplyBasicAttributes(new ColorBox(0, 0, fg_hue) { AcceptMouseInput = false }, node));
            c.Width = (int)(GetPercentage(value, maxval) * c.Width);

            if (needsUpdates)
            {
                gump.ProgressBarUpdates.Add(new Tuple<Tuple<Control, int>, Tuple<string, string>>(new Tuple<Control, int>(c, maxWidth), new Tuple<string, string>(originalValue, originalMaxVal)));
            }
        }

        private static void HandleImageProgressBar(XmlGump gump, XmlNode node)
        {
            ushort bg_graphic = 0, fg_graphic = 0, bg_hue = 0, fg_hue = 0;
            int value = 0, maxval = 0;
            bool needsUpdates = false;
            string originalValue = string.Empty, originalMaxVal = string.Empty;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "background_image":
                        ushort.TryParse(attr.Value, out bg_graphic);
                        break;
                    case "foreground_image":
                        ushort.TryParse(attr.Value, out fg_graphic);
                        break;
                    case "background_hue":
                        ushort.TryParse(attr.Value, out bg_hue);
                        break;
                    case "foreground_hue":
                        ushort.TryParse(attr.Value, out fg_hue);
                        break;
                    case "value":
                        originalValue = attr.Value;
                        if (!int.TryParse(attr.Value, out value))
                        {
                            int.TryParse(FormatText(attr.Value), out value);
                        }
                        break;
                    case "max_value":
                        originalMaxVal = attr.Value;
                        if (!int.TryParse(attr.Value, out maxval))
                        {
                            int.TryParse(FormatText(attr.Value), out maxval);
                        }
                        break;
                    case "updates":
                        bool.TryParse(attr.Value, out needsUpdates);
                        break;
                }
            }
            Control c;
            gump.Add(c = ApplyBasicAttributes(new GumpPic(0, 0, bg_graphic, bg_hue), node));
            int maxWidth = c.Width;
            gump.Add(c = ApplyBasicAttributes(new GumpPicTiled(fg_graphic) { Hue = fg_hue }, node));
            c.Width = (int)(GetPercentage(value, maxval) * c.Width);

            if (needsUpdates)
            {
                gump.ProgressBarUpdates.Add(new Tuple<Tuple<Control, int>, Tuple<string, string>>(new Tuple<Control, int>(c, maxWidth), new Tuple<string, string>(originalValue, originalMaxVal)));
            }
        }

        private static void HandleImage(XmlGump gump, XmlNode node)
        {
            ushort graphic = 0, hue = 0;
            Rectangle picinpic = Rectangle.Empty;
            bool isPicInPic = false;

            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "id":
                        ushort.TryParse(attr.Value, out graphic);
                        break;
                    case "hue":
                        ushort.TryParse(attr.Value, out hue);
                        break;
                    case "rect":
                        string[] parts = attr.Value.Split(';');
                        if (parts.Length == 4)
                        {
                            if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y) && int.TryParse(parts[2], out int w) && int.TryParse(parts[3], out int h))
                            {
                                picinpic = new Rectangle(x, y, w, h);
                                isPicInPic = true;
                            }
                        }
                        break;
                }
            }

            if (isPicInPic)
            {
                gump.Add(ApplyBasicAttributes(new GumpPicInPic(0, 0, graphic, (ushort)picinpic.X, (ushort)picinpic.Y, (ushort)picinpic.Width, (ushort)picinpic.Height), node));
            }
            else
            {
                gump.Add(ApplyBasicAttributes(new GumpPic(0, 0, graphic, hue), node));
            }
        }

        private static void HandleColorBox(XmlGump gump, XmlNode colorNode)
        {
            ushort hue = 0;
            float alpha = 1;

            foreach (XmlAttribute attr in colorNode.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "hue":
                        ushort.TryParse(attr.Value, out hue);
                        break;
                    case "alpha":
                        float.TryParse(attr.Value, out alpha);
                        break;

                }
            }

            gump.Add(ApplyBasicAttributes(new ColorBox(0, 0, hue) { Alpha = alpha, AcceptMouseInput = true, CanCloseWithRightClick = true, CanMove = true }, colorNode));
        }

        private static void HandleTextTag(XmlGump gump, XmlNode textNode)
        {
            string font = TrueTypeLoader.EMBEDDED_FONT;
            int x = 0, y = 0, fontSize = 16, width = 0, hue = 997;
            bool needsUpdates = false;
            FontStashSharp.RichText.TextHorizontalAlignment align = FontStashSharp.RichText.TextHorizontalAlignment.Left;

            foreach (XmlAttribute attr in textNode.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "x":
                        int.TryParse(attr.Value, out x);
                        break;
                    case "y":
                        int.TryParse(attr.Value, out y);
                        break;
                    case "font":
                        font = attr.Value;
                        break;
                    case "size":
                        int.TryParse(attr.Value, out fontSize);
                        break;
                    case "width":
                        int.TryParse(attr.Value, out width);
                        break;
                    case "hue":
                        int.TryParse(attr.Value, out hue);
                        break;
                    case "updates":
                        bool.TryParse(attr.Value, out needsUpdates);
                        break;
                    case "align":
                        switch (attr.Value.ToLower())
                        {
                            case "left":
                                align = FontStashSharp.RichText.TextHorizontalAlignment.Left;
                                break;
                            case "center":
                                align = FontStashSharp.RichText.TextHorizontalAlignment.Center;
                                break;
                            case "right":
                                align = FontStashSharp.RichText.TextHorizontalAlignment.Right;
                                break;
                        }
                        break;
                }
            }
            TextBox t;


            gump.Add(t = new TextBox(FormatText(textNode.InnerText), font, fontSize, width > 0 ? width : null, hue, align, false) { X = x, Y = y, AcceptMouseInput = false });

            if (needsUpdates)
            {
                gump.TextBoxUpdates.Add(new Tuple<TextBox, Tuple<string, int>>(t, new Tuple<string, int>(textNode.InnerText, width)));
            }
        }

        private static Control ApplyBasicAttributes(Control c, XmlNode node)
        {
            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name.ToLower())
                {
                    case "x":
                        int.TryParse(attr.Value, out c.X);
                        break;
                    case "y":
                        int.TryParse(attr.Value, out c.Y);
                        break;
                    case "acceptmouseinput":
                        if (bool.TryParse(attr.Value, out bool b))
                        {
                            c.AcceptMouseInput = b;
                        }
                        break;
                    case "canmove":
                        if (bool.TryParse(attr.Value, out bool cm))
                        {
                            c.CanMove = cm;
                        }
                        break;
                    case "width":
                        int.TryParse(attr.Value, out c.Width);
                        break;
                    case "height":
                        int.TryParse(attr.Value, out c.Height);
                        break;
                }
            }

            return c;
        }

        public static float GetPercentage(double value, double max)
        {
            return (float)(value / max);
        }

        public static string FormatText(string text)
        {
            text = text.Replace("{charname}", World.Player.Name);
            text = text.Replace("{hp}", World.Player.Hits.ToString());
            text = text.Replace("{maxhp}", World.Player.HitsMax.ToString());
            text = text.Replace("{mana}", World.Player.Mana.ToString());
            text = text.Replace("{maxmana}", World.Player.ManaMax.ToString());
            text = text.Replace("{stam}", World.Player.Stamina.ToString());
            text = text.Replace("{maxstam}", World.Player.StaminaMax.ToString());
            text = text.Replace("{weight}", World.Player.Weight.ToString());
            text = text.Replace("{maxweight}", World.Player.WeightMax.ToString());
            text = text.Replace("{str}", World.Player.Strength.ToString());
            text = text.Replace("{dex}", World.Player.Dexterity.ToString());
            text = text.Replace("{int}", World.Player.Intelligence.ToString());
            text = text.Replace("{damagemin}", World.Player.DamageMin.ToString());
            text = text.Replace("{damagemax}", World.Player.DamageMax.ToString());
            text = text.Replace("{hci}", World.Player.HitChanceIncrease.ToString());
            text = text.Replace("{di}", World.Player.DamageIncrease.ToString());
            text = text.Replace("{ssi}", World.Player.SwingSpeedIncrease.ToString());
            text = text.Replace("{defchance}", World.Player.DefenseChanceIncrease.ToString());
            text = text.Replace("{defchancemax}", World.Player.MaxDefenseChanceIncrease.ToString());
            text = text.Replace("{sdi}", World.Player.SpellDamageIncrease.ToString());
            text = text.Replace("{fc}", World.Player.FasterCasting.ToString());
            text = text.Replace("{fcr}", World.Player.FasterCastRecovery.ToString());
            text = text.Replace("{lmc}", World.Player.LowerManaCost.ToString());
            text = text.Replace("{lrc}", World.Player.LowerReagentCost.ToString());
            text = text.Replace("{phyres}", World.Player.PhysicalResistance.ToString());
            text = text.Replace("{phyresmax}", World.Player.MaxPhysicResistence.ToString());
            text = text.Replace("{fireres}", World.Player.FireResistance.ToString());
            text = text.Replace("{fireresmax}", World.Player.MaxFireResistence.ToString());
            text = text.Replace("{coldres}", World.Player.ColdResistance.ToString());
            text = text.Replace("{coldresmax}", World.Player.MaxColdResistence.ToString());
            text = text.Replace("{poisonres}", World.Player.PoisonResistance.ToString());
            text = text.Replace("{poisonresmax}", World.Player.MaxPoisonResistence.ToString());
            text = text.Replace("{energyres}", World.Player.EnergyResistance.ToString());
            text = text.Replace("{energyresmax}", World.Player.MaxEnergyResistence.ToString());
            text = text.Replace("{maxstats}", World.Player.StatsCap.ToString());
            text = text.Replace("{luck}", World.Player.Luck.ToString());
            text = text.Replace("{gold}", World.Player.Gold.ToString());
            text = text.Replace("{pets}", World.Player.Followers.ToString());
            text = text.Replace("{petsmax}", World.Player.FollowersMax.ToString());

            return text;
        }
    }

    internal class XmlGump : Gump
    {
        public List<Tuple<TextBox, Tuple<string, int>>> TextBoxUpdates { get; set; } = new List<Tuple<TextBox, Tuple<string, int>>>();

        /// <summary>
        /// <Control, Max width>
        /// <Val string, max val string>
        /// </summary>
        public List<Tuple<Tuple<Control, int>, Tuple<string, string>>> ProgressBarUpdates { get; set; } = new List<Tuple<Tuple<Control, int>, Tuple<string, string>>>();

        private uint nextUpdate = 0;

        public XmlGump() : base(0, 0)
        {
        }

        public override void Update()
        {
            base.Update();

            if (Time.Ticks >= nextUpdate)
            {
                foreach (var t in TextBoxUpdates)
                {
                    if (t.Item1 != null && !t.Item1.IsDisposed)
                    {
                        string newString = XmlGumpHandler.FormatText(t.Item2.Item1);
                        if (t.Item1.Text != newString)
                        {
                            if (t.Item2.Item2 < 1)
                            {
                                t.Item1.UpdateText(newString);
                            }
                            else
                            {
                                t.Item1.Text = newString;
                            }
                        }
                    }
                }

                foreach (var p in ProgressBarUpdates)
                {
                    if (p.Item1 != null && !p.Item1.Item1.IsDisposed)
                    {
                        if (int.TryParse(XmlGumpHandler.FormatText(p.Item2.Item1), out int val))
                        {
                            if (int.TryParse(XmlGumpHandler.FormatText(p.Item2.Item2), out int max))
                            {
                                p.Item1.Item1.Width = (int)(XmlGumpHandler.GetPercentage(val, max) * p.Item1.Item2);
                            }
                        }
                    }
                }

                nextUpdate = Time.Ticks + 250;
            }
        }
    }
}
