#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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

        private readonly List<InfoBarControl> _infobarControls = new List<InfoBarControl>();
        private long _refreshTime;

        public InfoBarGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            CanCloseWithRightClick = false;
            Height = 20;

            Add(_background = new AlphaBlendControl(0.7f) { Width = Width, Height = Height });

            ResetItems();
        }

        public override GumpType GumpType => GumpType.InfoBar;

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
                InfoBarControl info = new InfoBarControl(infoBarItems[i].label, infoBarItems[i].var, infoBarItems[i].hue);

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

        public override void Update(double totalTime, double frameTime)
        {
            if (IsDisposed)
            {
                return;
            }

            if (_refreshTime < totalTime)
            {
                _refreshTime = (long) totalTime + 125;

                int x = 5;

                foreach (InfoBarControl c in _infobarControls)
                {
                    c.X = x;
                    x += c.Width + 5;
                }
            }

            base.Update(totalTime, frameTime);

            Control last = Children.LastOrDefault();

            if (last != null)
            {
                Width = last.Bounds.Right;
            }

            _background.Width = Width;
        }
    }


    internal class InfoBarControl : Control
    {
        private readonly Label _data;
        private readonly Label _label;
        private ushort _warningLinesHue;

        public InfoBarControl(string label, InfoBarVars var, ushort hue)
        {
            AcceptMouseInput = false;
            WantUpdateSize = true;
            CanMove = false;

            _label = new Label(label, true, 999) { Height = 20, Hue = hue };
            Var = var;

            _data = new Label("", true, 999) { Height = 20, X = _label.Width, Hue = 0x0481 };
            Add(_label);
            Add(_data);
        }

        public string Text => _label.Text;
        public InfoBarVars Var { get; }

        public ushort Hue => _label.Hue;
        protected long _refreshTime;

        public override void Update(double totalTime, double frameTime)
        {
            if (IsDisposed)
            {
                return;
            }

            if (_refreshTime < totalTime)
            {
                _refreshTime = (long) totalTime + 125;

                _data.Text = GetVarData(Var);

                if (ProfileManager.CurrentProfile.InfoBarHighlightType == 0 || Var == InfoBarVars.NameNotoriety)
                {
                    _data.Hue = GetVarHue(Var);
                }
                else
                {
                    _data.Hue = 0x0481;
                    _warningLinesHue = GetVarHue(Var);
                }

                _data.WantUpdateSize = true;
            }

            WantUpdateSize = true;

            base.Update(totalTime, frameTime);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (Var != InfoBarVars.NameNotoriety && ProfileManager.CurrentProfile.InfoBarHighlightType == 1 && _warningLinesHue != 0x0481)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(_warningLinesHue);

                batcher.Draw
                (
                    SolidColorTextureCache.GetTexture(Color.White),
                    new Rectangle
                    (
                        _data.ScreenCoordinateX,
                        _data.ScreenCoordinateY,
                        _data.Width,
                        2
                    ),
                    hueVector
                );

                batcher.Draw
                (
                    SolidColorTextureCache.GetTexture(Color.White),
                    new Rectangle
                    (
                        _data.ScreenCoordinateX,
                        _data.ScreenCoordinateY + Parent.Height - 2,
                        _data.Width,
                        2
                    ),
                    hueVector
                );
            }

            return true;
        }

        private string GetVarData(InfoBarVars var)
        {
            switch (var)
            {
                case InfoBarVars.HP: return $"{World.Player.Hits}/{World.Player.HitsMax}";

                case InfoBarVars.Mana: return $"{World.Player.Mana}/{World.Player.ManaMax}";

                case InfoBarVars.Stamina: return $"{World.Player.Stamina}/{World.Player.StaminaMax}";

                case InfoBarVars.Weight: return $"{World.Player.Weight}/{World.Player.WeightMax}";

                case InfoBarVars.Followers: return $"{World.Player.Followers}/{World.Player.FollowersMax}";

                case InfoBarVars.Gold: return World.Player.Gold.ToString();

                case InfoBarVars.Damage: return $"{World.Player.DamageMin}-{World.Player.DamageMax}";

                case InfoBarVars.Armor: return World.Player.PhysicalResistance.ToString();

                case InfoBarVars.Luck: return World.Player.Luck.ToString();

                case InfoBarVars.FireResist: return World.Player.FireResistance.ToString();

                case InfoBarVars.ColdResist: return World.Player.ColdResistance.ToString();

                case InfoBarVars.PoisonResist: return World.Player.PoisonResistance.ToString();

                case InfoBarVars.EnergyResist: return World.Player.EnergyResistance.ToString();

                case InfoBarVars.LowerReagentCost: return World.Player.LowerReagentCost.ToString();

                case InfoBarVars.SpellDamageInc: return World.Player.SpellDamageIncrease.ToString();

                case InfoBarVars.FasterCasting: return World.Player.FasterCasting.ToString();

                case InfoBarVars.FasterCastRecovery: return World.Player.FasterCastRecovery.ToString();

                case InfoBarVars.HitChanceInc: return World.Player.HitChanceIncrease.ToString();

                case InfoBarVars.DefenseChanceInc: return World.Player.DefenseChanceIncrease.ToString();

                case InfoBarVars.LowerManaCost: return World.Player.LowerManaCost.ToString();

                case InfoBarVars.DamageChanceInc: return World.Player.DamageIncrease.ToString();

                case InfoBarVars.SwingSpeedInc: return World.Player.SwingSpeedIncrease.ToString();

                case InfoBarVars.StatsCap: return World.Player.StatsCap.ToString();

                case InfoBarVars.NameNotoriety: return World.Player.Name;

                case InfoBarVars.TithingPoints: return World.Player.TithingPoints.ToString();

                default: return "";
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
                    {
                        return 0x0021;
                    }
                    else if (percent <= 0.5)
                    {
                        return 0x0030;
                    }
                    else if (percent <= 0.75)
                    {
                        return 0x0035;
                    }
                    else
                    {
                        return 0x0481;
                    }

                case InfoBarVars.Mana:
                    percent = World.Player.Mana / (float) World.Player.ManaMax;

                    if (percent <= 0.25)
                    {
                        return 0x0021;
                    }
                    else if (percent <= 0.5)
                    {
                        return 0x0030;
                    }
                    else if (percent <= 0.75)
                    {
                        return 0x0035;
                    }
                    else
                    {
                        return 0x0481;
                    }

                case InfoBarVars.Stamina:
                    percent = World.Player.Stamina / (float) World.Player.StaminaMax;

                    if (percent <= 0.25)
                    {
                        return 0x0021;
                    }
                    else if (percent <= 0.5)
                    {
                        return 0x0030;
                    }
                    else if (percent <= 0.75)
                    {
                        return 0x0035;
                    }
                    else
                    {
                        return 0x0481;
                    }

                case InfoBarVars.Weight:
                    percent = World.Player.Weight / (float) World.Player.WeightMax;

                    if (percent >= 1)
                    {
                        return 0x0021;
                    }
                    else if (percent >= 0.75)
                    {
                        return 0x0030;
                    }
                    else if (percent >= 0.5)
                    {
                        return 0x0035;
                    }
                    else
                    {
                        return 0x0481;
                    }

                case InfoBarVars.NameNotoriety: return Notoriety.GetHue(World.Player.NotorietyFlag);

                default: return 0x0481;
            }
        }
    }
}