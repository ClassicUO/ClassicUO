#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System.Collections.Generic;
using System.Linq;
using System.Xml;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class InfoBarGump : Gump
    {
        private readonly AlphaBlendControl _background;
        private long _refreshTime;

        private readonly List<InfoBarControl> _infobarControls = new List<InfoBarControl>();

        public InfoBarGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            CanCloseWithRightClick = false;
            Height = 20;

            Add(_background = new AlphaBlendControl(0.3f) { Width = Width, Height = Height });

            ResetItems();
        }

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_INFOBAR;

        public void ResetItems()
        {
            foreach (InfoBarControl c in _infobarControls)
            {
                c.Dispose();
            }

            _infobarControls.Clear();

            List<InfoBarItem> infoBarItems = Client.Game.GetScene<GameScene>().InfoBars.GetInfoBars();

            for (int i = 0; i < infoBarItems.Count; i++)
            {
                var info = new InfoBarControl(infoBarItems[i].label, infoBarItems[i].var, infoBarItems[i].hue);
                _infobarControls.Add(info);
                Add(info);
            }
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            //writer.WriteStartElement("controls");

            //foreach (InfoBarControl co in _infobarControls)
            //{
            //    writer.WriteStartElement("control");
            //    writer.WriteAttributeString("label", co.Text);
            //    writer.WriteAttributeString("var", ((int) co.Var).ToString());
            //    writer.WriteAttributeString("hue", co.Hue.ToString());
            //    writer.WriteEndElement();
            //}
            //writer.WriteEndElement();
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            //XmlElement controlsXml = xml["controls"];
            //_infobarControls.Clear();

            //if (controlsXml != null)
            //{
            //    foreach (XmlElement controlXml in controlsXml.GetElementsByTagName("control"))
            //    {
            //        InfoBarControl control = new InfoBarControl(controlXml.GetAttribute("label"),
            //                                                    (InfoBarVars) int.Parse(controlXml.GetAttribute("var")),
            //                                                    ushort.Parse(controlXml.GetAttribute("hue")));

            //        Add(control);
            //        _infobarControls.Add(control);
            //    }
            //}
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (_refreshTime < totalMS)
            {
                _refreshTime = (long) totalMS + 125;

                int x = 5;

                foreach (InfoBarControl c in _infobarControls)
                {
                    c.X = x;
                    x += c.Width + 5;
                }
            }

            base.Update(totalMS, frameMS);

            Control last = Children.LastOrDefault();

            if (last != null)
            {
                Width = last.Bounds.Right;
            }

            _background.Width = Width;
        }
    }


    class InfoBarControl : Control
    {
        private Label _label;
        private Label _data;
        private InfoBarVars _var;
        private ushort _warningLinesHue;
        protected long _refreshTime;

        public InfoBarControl(string label, InfoBarVars var, ushort hue)
        {
            AcceptMouseInput = false;
            WantUpdateSize = true;
            CanMove = false;

            _label = new Label(label, true, 999) { Height = 20, Hue = hue };
            _var = var;

            _data = new Label("", true, 999) { Height = 20, X = _label.Width, Hue = 0x0481 };
            Add(_label);
            Add(_data);
        }

        public string Text => _label.Text;
        public InfoBarVars Var => _var;
        public ushort Hue => _label.Hue;

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (_refreshTime < totalMS)
            {
                _refreshTime = (long) totalMS + 125;

                _data.Text = GetVarData(_var);
                
                if (ProfileManager.Current.InfoBarHighlightType == 0 || _var == InfoBarVars.NameNotoriety)
                {
                    _data.Hue = GetVarHue(_var);
                }
                else
                {
                    _data.Hue =  0x0481;
                    _warningLinesHue = GetVarHue(_var);
                }
                
                _data.WantUpdateSize = true;
            }

            WantUpdateSize = true;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            ResetHueVector();

            if (_var != InfoBarVars.NameNotoriety && ProfileManager.Current.InfoBarHighlightType == 1 && _warningLinesHue != 0x0481)
            {
                ShaderHuesTraslator.GetHueVector(ref _hueVector, _warningLinesHue);
                batcher.Draw2D(Texture2DCache.GetTexture(Color.White), _data.ScreenCoordinateX, _data.ScreenCoordinateY, _data.Width, 2, ref _hueVector);
                batcher.Draw2D(Texture2DCache.GetTexture(Color.White), _data.ScreenCoordinateX, _data.ScreenCoordinateY + Parent.Height - 2, _data.Width, 2, ref _hueVector);
            }

            return true;
        }

        private string GetVarData(InfoBarVars var)
        {
            switch (var)
            {
                case InfoBarVars.HP:
                    return World.Player.Hits + "/" + World.Player.HitsMax;
                case InfoBarVars.Mana:
                    return World.Player.Mana + "/" + World.Player.ManaMax;
                case InfoBarVars.Stamina:
                    return World.Player.Stamina + "/" + World.Player.StaminaMax;
                case InfoBarVars.Weight:
                    return World.Player.Weight + "/" + World.Player.WeightMax;
                case InfoBarVars.Followers:
                    return World.Player.Followers + "/" + World.Player.FollowersMax;
                case InfoBarVars.Gold:
                    return World.Player.Gold.ToString();
                case InfoBarVars.Damage:
                    return World.Player.DamageMin + "-" + World.Player.DamageMax;
                case InfoBarVars.Armor:
                    return World.Player.PhysicalResistance.ToString();
                case InfoBarVars.Luck:
                    return World.Player.Luck.ToString();
                case InfoBarVars.FireResist:
                    return World.Player.FireResistance.ToString();
                case InfoBarVars.ColdResist:
                    return World.Player.ColdResistance.ToString();
                case InfoBarVars.PoisonResist:
                    return World.Player.PoisonResistance.ToString();
                case InfoBarVars.EnergyResist:
                    return World.Player.EnergyResistance.ToString();
                case InfoBarVars.LowerReagentCost:
                    return World.Player.LowerReagentCost.ToString();
                case InfoBarVars.SpellDamageInc:
                    return World.Player.SpellDamageIncrease.ToString();
                case InfoBarVars.FasterCasting:
                    return World.Player.FasterCasting.ToString();
                case InfoBarVars.FasterCastRecovery:
                    return World.Player.FasterCastRecovery.ToString();
                case InfoBarVars.HitChanceInc:
                    return World.Player.HitChanceIncrease.ToString();
                case InfoBarVars.DefenseChanceInc:
                    return World.Player.DefenseChanceIncrease.ToString();
                case InfoBarVars.LowerManaCost:
                    return World.Player.LowerManaCost.ToString();
                case InfoBarVars.DamageChanceInc:
                    return World.Player.DamageIncrease.ToString();
                case InfoBarVars.SwingSpeedInc:
                    return World.Player.SwingSpeedIncrease.ToString();
                case InfoBarVars.StatsCap:
                    return World.Player.StatsCap.ToString();
                case InfoBarVars.NameNotoriety:
                    return World.Player.Name;
                default:
                    return "";
            }
        }

        private ushort GetVarHue(InfoBarVars var)
        {
            float percent;
            switch (var)
            {
                case InfoBarVars.HP:
                    percent = World.Player.Hits / (float) World.Player.HitsMax;
                    if (percent <= 0.25)
                        return 0x0021;
                    else if (percent <= 0.5)
                        return 0x0030;
                    else if (percent <= 0.75)
                        return 0x0035;
                    else
                        return 0x0481;
                case InfoBarVars.Mana:
                    percent = World.Player.Mana / (float) World.Player.ManaMax;
                    if (percent <= 0.25)
                        return 0x0021;
                    else if (percent <= 0.5)
                        return 0x0030;
                    else if (percent <= 0.75)
                        return 0x0035;
                    else
                        return 0x0481;
                case InfoBarVars.Stamina:
                    percent = World.Player.Stamina / (float) World.Player.StaminaMax;
                    if (percent <= 0.25)
                        return 0x0021;
                    else if (percent <= 0.5)
                        return 0x0030;
                    else if (percent <= 0.75)
                        return 0x0035;
                    else
                        return 0x0481;
                case InfoBarVars.Weight:
                    percent = World.Player.Weight / (float) World.Player.WeightMax;
                    if (percent >= 1)
                        return 0x0021;
                    else if (percent >= 0.75)
                        return 0x0030;
                    else if (percent >= 0.5)
                        return 0x0035;
                    else
                        return 0x0481;
                case InfoBarVars.NameNotoriety:
                    return Notoriety.GetHue(World.Player.NotorietyFlag);
                default:
                    return 0x0481;
            }
        }

    }
}
