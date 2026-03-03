// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.ECS;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal class InfoBarGump : Gump
    {
        private readonly AlphaBlendControl _background;

        private readonly List<InfoBarControl> _infobarControls = new List<InfoBarControl>();
        private long _refreshTime;

        public InfoBarGump(World world) : base(world, 0, 0)
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

            List<InfoBarItem> infoBarItems = World.InfoBars.GetInfoBars();

            for (int i = 0; i < infoBarItems.Count; i++)
            {
                InfoBarControl info = new InfoBarControl(this, infoBarItems[i].label, infoBarItems[i].var, infoBarItems[i].hue);

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

        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_refreshTime < Time.Ticks)
            {
                _refreshTime = (long)Time.Ticks + 125;

                int x = 5;

                foreach (InfoBarControl c in _infobarControls)
                {
                    c.X = x;
                    x += c.Width + 5;
                }
            }

            base.Update();

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
        private readonly InfoBarGump _gump;
        private readonly Label _data;
        private readonly Label _label;
        private ushort _warningLinesHue;

        public InfoBarControl(InfoBarGump gump, string label, InfoBarVars var, ushort hue)
        {
            _gump = gump;
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

        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_refreshTime < Time.Ticks)
            {
                _refreshTime = (long)Time.Ticks + 125;

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

            base.Update();
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
            float layerDepth = layerDepthRef;

            if (Var != InfoBarVars.NameNotoriety && ProfileManager.CurrentProfile.InfoBarHighlightType == 1 && _warningLinesHue != 0x0481)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(_warningLinesHue);

                renderLists.AddGumpNoAtlas(
                    batcher =>
                    {
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
                            hueVector,
                            layerDepth
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
                            hueVector,
                            layerDepth
                        );
                        return true;
                    }
                );
            }

            return true;
        }

        private string GetVarData(InfoBarVars var)
        {
            var ecs = Client.Game?.UO?.EcsRuntime;
            bool useEcs = ecs != null && ecs.GetCutoverFlags().UseEcsUiData;

            if (useEcs)
            {
                var snap = ecs.GetStatusSnapshot();
                if (!snap.IsValid) return "";
                return var switch
                {
                    InfoBarVars.HP => $"{snap.Vitals.Hits}/{snap.Vitals.HitsMax}",
                    InfoBarVars.Mana => $"{snap.Vitals.Mana}/{snap.Vitals.ManaMax}",
                    InfoBarVars.Stamina => $"{snap.Vitals.Stamina}/{snap.Vitals.StaminaMax}",
                    InfoBarVars.Weight => $"{snap.Stats.Weight}/{snap.Stats.WeightMax}",
                    InfoBarVars.Followers => $"{snap.Stats.Followers}/{snap.Stats.FollowersMax}",
                    InfoBarVars.Gold => snap.Stats.Gold.ToString(),
                    InfoBarVars.Damage => $"{snap.Stats.DamageMin}-{snap.Stats.DamageMax}",
                    InfoBarVars.Armor => snap.Stats.PhysResist.ToString(),
                    InfoBarVars.Luck => snap.Stats.Luck.ToString(),
                    InfoBarVars.FireResist => snap.Stats.FireResist.ToString(),
                    InfoBarVars.ColdResist => snap.Stats.ColdResist.ToString(),
                    InfoBarVars.PoisonResist => snap.Stats.PoisonResist.ToString(),
                    InfoBarVars.EnergyResist => snap.Stats.EnergyResist.ToString(),
                    InfoBarVars.LowerReagentCost => snap.Stats.LowerReagentCost.ToString(),
                    InfoBarVars.SpellDamageInc => snap.Stats.SpellDamageInc.ToString(),
                    InfoBarVars.FasterCasting => snap.Stats.FasterCasting.ToString(),
                    InfoBarVars.FasterCastRecovery => snap.Stats.FasterCastRecovery.ToString(),
                    InfoBarVars.HitChanceInc => snap.Stats.HitChanceInc.ToString(),
                    InfoBarVars.DefenseChanceInc => snap.Stats.DefenseChanceInc.ToString(),
                    InfoBarVars.LowerManaCost => snap.Stats.LowerManaCost.ToString(),
                    InfoBarVars.DamageChanceInc => snap.Stats.DamageInc.ToString(),
                    InfoBarVars.SwingSpeedInc => snap.Stats.SwingSpeedInc.ToString(),
                    InfoBarVars.StatsCap => snap.Stats.StatsCap.ToString(),
                    InfoBarVars.NameNotoriety => ecs.GetEntityName(snap.Serial),
                    InfoBarVars.TithingPoints => snap.Stats.TithingPoints.ToString(),
                    _ => ""
                };
            }

            return var switch
            {
                InfoBarVars.HP => $"{_gump.World.Player.Hits}/{_gump.World.Player.HitsMax}",
                InfoBarVars.Mana => $"{_gump.World.Player.Mana}/{_gump.World.Player.ManaMax}",
                InfoBarVars.Stamina => $"{_gump.World.Player.Stamina}/{_gump.World.Player.StaminaMax}",
                InfoBarVars.Weight => $"{_gump.World.Player.Weight}/{_gump.World.Player.WeightMax}",
                InfoBarVars.Followers => $"{_gump.World.Player.Followers}/{_gump.World.Player.FollowersMax}",
                InfoBarVars.Gold => _gump.World.Player.Gold.ToString(),
                InfoBarVars.Damage => $"{_gump.World.Player.DamageMin}-{_gump.World.Player.DamageMax}",
                InfoBarVars.Armor => _gump.World.Player.PhysicalResistance.ToString(),
                InfoBarVars.Luck => _gump.World.Player.Luck.ToString(),
                InfoBarVars.FireResist => _gump.World.Player.FireResistance.ToString(),
                InfoBarVars.ColdResist => _gump.World.Player.ColdResistance.ToString(),
                InfoBarVars.PoisonResist => _gump.World.Player.PoisonResistance.ToString(),
                InfoBarVars.EnergyResist => _gump.World.Player.EnergyResistance.ToString(),
                InfoBarVars.LowerReagentCost => _gump.World.Player.LowerReagentCost.ToString(),
                InfoBarVars.SpellDamageInc => _gump.World.Player.SpellDamageIncrease.ToString(),
                InfoBarVars.FasterCasting => _gump.World.Player.FasterCasting.ToString(),
                InfoBarVars.FasterCastRecovery => _gump.World.Player.FasterCastRecovery.ToString(),
                InfoBarVars.HitChanceInc => _gump.World.Player.HitChanceIncrease.ToString(),
                InfoBarVars.DefenseChanceInc => _gump.World.Player.DefenseChanceIncrease.ToString(),
                InfoBarVars.LowerManaCost => _gump.World.Player.LowerManaCost.ToString(),
                InfoBarVars.DamageChanceInc => _gump.World.Player.DamageIncrease.ToString(),
                InfoBarVars.SwingSpeedInc => _gump.World.Player.SwingSpeedIncrease.ToString(),
                InfoBarVars.StatsCap => _gump.World.Player.StatsCap.ToString(),
                InfoBarVars.NameNotoriety => _gump.World.Player.Name,
                InfoBarVars.TithingPoints => _gump.World.Player.TithingPoints.ToString(),
                _ => ""
            };
        }

        private ushort GetVarHue(InfoBarVars var)
        {
            float percent;

            var ecs = Client.Game?.UO?.EcsRuntime;
            bool useEcs = ecs != null && ecs.GetCutoverFlags().UseEcsUiData;

            if (useEcs)
            {
                var snap = ecs.GetStatusSnapshot();
                if (!snap.IsValid) return 0x0481;

                switch (var)
                {
                    case InfoBarVars.HP:
                        percent = snap.Vitals.HitsMax > 0 ? snap.Vitals.Hits / (float)snap.Vitals.HitsMax : 1f;
                        return percent <= 0.25f ? (ushort)0x0021 : percent <= 0.5f ? (ushort)0x0030 : percent <= 0.75f ? (ushort)0x0035 : (ushort)0x0481;
                    case InfoBarVars.Mana:
                        percent = snap.Vitals.ManaMax > 0 ? snap.Vitals.Mana / (float)snap.Vitals.ManaMax : 1f;
                        return percent <= 0.25f ? (ushort)0x0021 : percent <= 0.5f ? (ushort)0x0030 : percent <= 0.75f ? (ushort)0x0035 : (ushort)0x0481;
                    case InfoBarVars.Stamina:
                        percent = snap.Vitals.StaminaMax > 0 ? snap.Vitals.Stamina / (float)snap.Vitals.StaminaMax : 1f;
                        return percent <= 0.25f ? (ushort)0x0021 : percent <= 0.5f ? (ushort)0x0030 : percent <= 0.75f ? (ushort)0x0035 : (ushort)0x0481;
                    case InfoBarVars.Weight:
                        percent = snap.Stats.WeightMax > 0 ? snap.Stats.Weight / (float)snap.Stats.WeightMax : 0f;
                        return percent >= 1f ? (ushort)0x0021 : percent >= 0.75f ? (ushort)0x0030 : percent >= 0.5f ? (ushort)0x0035 : (ushort)0x0481;
                    case InfoBarVars.NameNotoriety:
                        return Notoriety.GetHue((NotorietyFlag)ecs.GetNotoriety(snap.Serial));
                    default: return 0x0481;
                }
            }

            switch (var)
            {
                case InfoBarVars.HP:
                    percent = _gump.World.Player.Hits / (float) _gump.World.Player.HitsMax;

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
                    percent = _gump.World.Player.Mana / (float) _gump.World.Player.ManaMax;

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
                    percent = _gump.World.Player.Stamina / (float)_gump.World.Player.StaminaMax;

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
                    percent = _gump.World.Player.Weight / (float)_gump.World.Player.WeightMax;

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

                case InfoBarVars.NameNotoriety: return Notoriety.GetHue(_gump.World.Player.NotorietyFlag);

                default: return 0x0481;
            }
        }
    }
}