using ClassicUO.Game.UI.Gumps;
using System.Xml;
using System.IO;
using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using System.Collections.Generic;

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

                    foreach (XmlNode node in root.ChildNodes)
                    {
                        switch (node.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (node.Name.ToLower().Equals("text"))
                                {
                                    HandleTextTag(gump, node);
                                    break;
                                }
                                if (node.Name.ToLower().Equals("colorbox"))
                                {
                                    HandleColorBox(gump, node);
                                    break;
                                }
                                if (node.Name.ToLower().Equals("image"))
                                {
                                    HandleImage(gump, node);
                                    break;
                                }
                                break;
                        }
                    }
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

        private static void HandleImage(XmlGump gump,  XmlNode node)
        {
            ushort graphic = 0, hue = 0;

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
                }
            }

            gump.Add(ApplyBasicAttributes(new GumpPic(0, 0, graphic, hue), node));
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
                }
            }
            TextBox t;


            gump.Add(t = new TextBox(FormatText(textNode.InnerText), font, fontSize, width > 0 ? width : null, hue, strokeEffect: false) { X = x, Y = y, AcceptMouseInput = false });

            if(needsUpdates)
            {
                gump.TextBoxUpdates.Add(new Tuple<TextBox, string>(t, textNode.InnerText));
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
        public List<Tuple<TextBox, string>> TextBoxUpdates { get; set; } = new List<Tuple<TextBox, string>>();

        private uint nextUpdate = 0;

        public XmlGump() : base(0, 0)
        {
        }

        public override void Update()
        {
            base.Update();

            if(Time.Ticks >= nextUpdate)
            {
                foreach(var t in TextBoxUpdates)
                {
                    if (t.Item1 != null && !t.Item1.IsDisposed)
                    {
                        string newString = XmlGumpHandler.FormatText(t.Item2);
                        if(t.Item1.Text != newString)
                        {
                            t.Item1.Text = newString;
                        }
                    }
                }

                nextUpdate = Time.Ticks + 500;
            }
        }
    }
}
