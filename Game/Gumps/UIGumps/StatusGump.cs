using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class StatusGump : Gump
    {

        double _refreshTime;
        int _offset;
        private bool _useOldGump = false;
        private PlayerMobile _mobile = World.Player;
        private Label[] _labels = new Label[(int)MobileStats.Max];

        public StatusGump()
            : base(0, 0)
        {
            CanMove = true;


            switch (FileManager.ClientVersion)
            {
                case var expression when FileManager.ClientVersion >= ClientVersions.CV_308D && !_useOldGump: //ORIGINAL LARGE GUMP
                    _offset = 82;
                    AddChildren(new GumpPic(0, 0, 0x2A6C, 0));
                    AddChildren(_labels[(int)MobileStats.Name] = new Label(1, 997, _mobile.Name) { X = 260 - Width / 2, Y = 48 });
                    //============================================================================================================================
                    AddChildren(_labels[(int)MobileStats.Strength] = new Label(1, 997, _mobile.Strength.ToString()) { X = _offset - 4, Y = 76 });
                    AddChildren(_labels[(int)MobileStats.Dexterity] = new Label(1, 997, _mobile.Dexterity.ToString()) { X = _offset - 4, Y = 104 });
                    AddChildren(_labels[(int)MobileStats.Intelligence] = new Label(1, 997, _mobile.Intelligence.ToString()) { X = _offset - 4, Y = 132 });
                    AddChildren(_labels[(int)MobileStats.HitChanceInc] = new Label(1, 997, _mobile.HitChanceInc.ToString()) { X = _offset - 4, Y = 160 });
                    //============================================================================================================================
                    AddChildren(_labels[(int)MobileStats.HealthCurrent] = new Label(1, 997, _mobile.Hits.ToString()) { X = 2 * _offset - 4, Y = 69 });
                    AddChildren(_labels[(int)MobileStats.HealthMax] = new Label(1, 997, _mobile.HitsMax.ToString()) { X = 2 * _offset - 4, Y = 82 });
                    AddChildren(_labels[(int)MobileStats.StaminaCurrent] = new Label(1, 997, _mobile.Stamina.ToString()) { X = 2 * _offset - 4, Y = 97 });
                    AddChildren(_labels[(int)MobileStats.StaminaMax] = new Label(1, 997, _mobile.StaminaMax.ToString()) { X = 2 * _offset - 4, Y = 110 });
                    AddChildren(_labels[(int)MobileStats.ManaCurrent] = new Label(1, 997, _mobile.Mana.ToString()) { X = 2 * _offset - 4, Y = 125 });
                    AddChildren(_labels[(int)MobileStats.ManaMax] = new Label(1, 997, _mobile.ManaMax.ToString()) { X = 2 * _offset - 4, Y = 138 });
                    AddChildren(_labels[(int)MobileStats.DefenseChanceInc] = new Label(1, 997, _mobile.DefenseChanceInc.ToString()) { X = 2 * _offset - 4, Y = 160 });
                    //============================================================================================================================
                    AddChildren(_labels[(int)MobileStats.StatCap] = new Label(1, 997, _mobile.StatsCap.ToString()) { X = 3 * _offset - 4, Y = 76 });
                    AddChildren(_labels[(int)MobileStats.Luck] = new Label(1, 997, _mobile.Luck.ToString()) { X = 3 * _offset - 4, Y = 102 });
                    AddChildren(_labels[(int)MobileStats.WeightCurrent] = new Label(1, 997, _mobile.Weight.ToString()) { X = 3 * _offset - 4, Y = 125 });
                    AddChildren(_labels[(int)MobileStats.WeightMax] = new Label(1, 997, _mobile.WeightMax.ToString()) { X = 3 * _offset - 4, Y = 138 });
                    AddChildren(_labels[(int)MobileStats.LowerManaCost] = new Label(1, 997, _mobile.LowerManaCost.ToString()) { X = 3 * _offset - 4, Y = 160 });
                    //============================================================================================================================
                    AddChildren(_labels[(int)MobileStats.Damage] = new Label(1, 997, ConcatCurrentMax(_mobile.DamageMin, _mobile.DamageMax)) { X = 4 * _offset - 4, Y = 75 });
                    AddChildren(_labels[(int)MobileStats.DamageChanceInc] = new Label(1, 997, _mobile.DamageChanceInc.ToString()) { X = 4 * _offset - 4, Y = 102 });
                    AddChildren(_labels[(int)MobileStats.Followers] = new Label(1, 997, ConcatCurrentMax(_mobile.Followers, _mobile.FollowersMax)) { X = 4 * _offset - 4, Y = 132 });
                    AddChildren(_labels[(int)MobileStats.SwingSpeedInc] = new Label(1, 997, _mobile.SwingSpeedInc.ToString()) { X = 4 * _offset - 4, Y = 160 });
                    //============================================================================================================================
                    AddChildren(_labels[(int)MobileStats.LowerReagentCost] = new Label(1, 997, _mobile.LowerReagentCost.ToString()) { X = 5 * _offset - 4, Y = 76 });
                    AddChildren(_labels[(int)MobileStats.SpellDamageInc] = new Label(1, 997, _mobile.SpellDamageInc.ToString()) { X = 5 * _offset - 4, Y = 102 });
                    AddChildren(_labels[(int)MobileStats.FasterCasting] = new Label(1, 997, _mobile.FasterCasting.ToString()) { X = 5 * _offset - 4, Y = 132 });
                    AddChildren(_labels[(int)MobileStats.FasterCastRecovery] = new Label(1, 997, _mobile.FasterCastRecovery.ToString()) { X = 5 * _offset - 4, Y = 160 });
                    //============================================================================================================================
                    AddChildren(_labels[(int)MobileStats.AR] = new Label(1, 997, _mobile.ResistPhysical.ToString()) { X = 6 * _offset - 4, Y = 73 });
                    AddChildren(_labels[(int)MobileStats.RF] = new Label(1, 997, _mobile.ResistFire.ToString()) { X = 6 * _offset - 4, Y = 90 });
                    AddChildren(_labels[(int)MobileStats.RC] = new Label(1, 997, _mobile.ResistCold.ToString()) { X = 6 * _offset - 4, Y = 105 });
                    AddChildren(_labels[(int)MobileStats.RP] = new Label(1, 997, _mobile.ResistPoison.ToString()) { X = 6 * _offset - 4, Y = 119 });
                    AddChildren(_labels[(int)MobileStats.RE] = new Label(1, 997, _mobile.ResistEnergy.ToString()) { X = 6 * _offset - 4, Y = 135 });
                    AddChildren(_labels[(int)MobileStats.Gold] = new Label(1, 997, _mobile.Gold.ToString()) { X = 6 * _offset - 4, Y = 160 });
                    break;
                case var expression when FileManager.ClientVersion < ClientVersions.CV_308D | _useOldGump: //OLD GUMP
                    _offset = 84;
                    AddChildren(new GumpPic(0, 0, 0x802, 0));
                    //============================================================================================================================
                    AddChildren(_labels[(int)MobileStats.Name] = new Label(1, 997, _mobile.Name.ToString()) { X = _offset, Y = 42 });
                    AddChildren(_labels[(int)MobileStats.Strength] = new Label(1, 997, _mobile.Strength.ToString()) { X = _offset, Y = 62 });
                    AddChildren(_labels[(int)MobileStats.Dexterity] = new Label(1, 997, _mobile.Dexterity.ToString()) { X = _offset, Y = 74 });
                    AddChildren(_labels[(int)MobileStats.Intelligence] = new Label(1, 997, _mobile.Intelligence.ToString()) { X = _offset, Y = 85 });
                    AddChildren(_labels[(int)MobileStats.Sex] = new Label(1, 997, ((_mobile.Flags & Flags.Female) != 0) ? "F" : "M") { X = _offset, Y = 98 });
                    AddChildren(_labels[(int)MobileStats.AR] = new Label(1, 997, _mobile.ResistPhysical.ToString()) { X = _offset, Y = 110 });
                    //============================================================================================================================
                    AddChildren(_labels[(int)MobileStats.HealthCurrent] = new Label(1, 997, _mobile.Hits.ToString()) { X = 2 * _offset+3, Y = 62 });
                    AddChildren(_labels[(int)MobileStats.ManaCurrent] = new Label(1, 997, _mobile.Mana.ToString()) { X = 2 * _offset+3, Y = 74 });
                    AddChildren(_labels[(int)MobileStats.StaminaCurrent] = new Label(1, 997, _mobile.Stamina.ToString()) { X = 2 * _offset+3, Y = 85 });
                    AddChildren(_labels[(int)MobileStats.Gold] = new Label(1, 997, _mobile.Gold.ToString()) { X = 2 * _offset+3, Y = 98 });
                    AddChildren(_labels[(int)MobileStats.WeightCurrent] = new Label(1, 997, _mobile.Weight.ToString()) { X = 2 * _offset+3, Y = 110 });
                    break;
            }


        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {

            base.Draw(spriteBatch, position, hue);
            switch (FileManager.ClientVersion)
            {
                case var expression when FileManager.ClientVersion >= ClientVersions.CV_308D && !_useOldGump:
                    Texture2D line = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                    line.SetData(new[] { Color.Black });
                    spriteBatch.Draw2D(line, new Rectangle((int)position.X + 2 * 82 - 4, (int)position.Y + 81, 20, 1), RenderExtentions.GetHueVector(0, false, false, false));
                    spriteBatch.Draw2D(line, new Rectangle((int)position.X + 2 * 82 - 4, (int)position.Y + 109, 20, 1), RenderExtentions.GetHueVector(0, false, false, false));
                    spriteBatch.Draw2D(line, new Rectangle((int)position.X + 2 * 82 - 4, (int)position.Y + 137, 20, 1), RenderExtentions.GetHueVector(0, false, false, false));
                    spriteBatch.Draw2D(line, new Rectangle((int)position.X + 3 * 82 - 4, (int)position.Y + 137, 20, 1), RenderExtentions.GetHueVector(0, false, false, false));
                    break;
                default:
                    break;
            }
            return true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_refreshTime + 0.5d < totalMS) //need to update
            {
                _refreshTime = totalMS;

                switch (FileManager.ClientVersion)
                {
                    case var expression when FileManager.ClientVersion >= ClientVersions.CV_308D && !_useOldGump:
                        _labels[(int)MobileStats.Name].Text = _mobile.Name;
                        _labels[(int)MobileStats.Strength].Text = _mobile.Strength.ToString();
                        _labels[(int)MobileStats.Dexterity].Text = _mobile.Dexterity.ToString();
                        _labels[(int)MobileStats.Intelligence].Text = _mobile.Intelligence.ToString();
                        _labels[(int)MobileStats.HealthCurrent].Text = _mobile.Hits.ToString();
                        _labels[(int)MobileStats.HealthMax].Text = _mobile.HitsMax.ToString();
                        _labels[(int)MobileStats.StaminaCurrent].Text = _mobile.Stamina.ToString();
                        _labels[(int)MobileStats.StaminaMax].Text = _mobile.StaminaMax.ToString();
                        _labels[(int)MobileStats.ManaCurrent].Text = _mobile.Mana.ToString();
                        _labels[(int)MobileStats.ManaMax].Text = _mobile.ManaMax.ToString();
                        _labels[(int)MobileStats.Followers].Text = ConcatCurrentMax(_mobile.Followers, _mobile.FollowersMax);
                        _labels[(int)MobileStats.WeightCurrent].Text = _mobile.Weight.ToString();
                        _labels[(int)MobileStats.WeightMax].Text = _mobile.WeightMax.ToString();
                        _labels[(int)MobileStats.StatCap].Text = _mobile.StatsCap.ToString();
                        _labels[(int)MobileStats.Luck].Text = _mobile.Luck.ToString();
                        _labels[(int)MobileStats.Gold].Text = _mobile.Gold.ToString();
                        _labels[(int)MobileStats.AR].Text = _mobile.ResistPhysical.ToString();
                        _labels[(int)MobileStats.RF].Text = _mobile.ResistFire.ToString();
                        _labels[(int)MobileStats.RC].Text = _mobile.ResistCold.ToString();
                        _labels[(int)MobileStats.RP].Text = _mobile.ResistPoison.ToString();
                        _labels[(int)MobileStats.RE].Text = _mobile.ResistEnergy.ToString();
                        _labels[(int)MobileStats.Damage].Text = ConcatCurrentMax(_mobile.DamageMin, _mobile.DamageMax);
                        _labels[(int)MobileStats.LowerReagentCost].Text = _mobile.LowerReagentCost.ToString();
                        _labels[(int)MobileStats.SpellDamageInc].Text = _mobile.SpellDamageInc.ToString();
                        _labels[(int)MobileStats.FasterCasting].Text = _mobile.FasterCasting.ToString();
                        _labels[(int)MobileStats.FasterCastRecovery].Text = _mobile.FasterCastRecovery.ToString();
                        _labels[(int)MobileStats.HitChanceInc].Text = _mobile.HitChanceInc.ToString();
                        _labels[(int)MobileStats.DefenseChanceInc].Text = _mobile.DefenseChanceInc.ToString();
                        _labels[(int)MobileStats.LowerManaCost].Text = _mobile.LowerManaCost.ToString();
                        _labels[(int)MobileStats.DamageChanceInc].Text = _mobile.DamageChanceInc.ToString();
                        _labels[(int)MobileStats.SwingSpeedInc].Text = _mobile.SwingSpeedInc.ToString();
                        break;
                    case var expression when FileManager.ClientVersion < ClientVersions.CV_308D | _useOldGump: //OLD GUMP
                        _labels[(int)MobileStats.Name].Text = _mobile.Name;
                        _labels[(int)MobileStats.Strength].Text = _mobile.Strength.ToString();
                        _labels[(int)MobileStats.Dexterity].Text = _mobile.Dexterity.ToString();
                        _labels[(int)MobileStats.Intelligence].Text = _mobile.Intelligence.ToString();
                        _labels[(int)MobileStats.HealthCurrent].Text = ConcatCurrentMax(_mobile.Hits, _mobile.HitsMax);
                        _labels[(int)MobileStats.StaminaCurrent].Text = ConcatCurrentMax(_mobile.Stamina, _mobile.StaminaMax);
                        _labels[(int)MobileStats.ManaCurrent].Text = ConcatCurrentMax(_mobile.Mana, _mobile.ManaMax);
                        _labels[(int)MobileStats.WeightCurrent].Text = ConcatCurrentMax(_mobile.Weight, _mobile.WeightMax);
                        _labels[(int)MobileStats.Gold].Text = _mobile.Gold.ToString();
                        _labels[(int)MobileStats.AR].Text = _mobile.ResistPhysical.ToString();
                        _labels[(int)MobileStats.Sex].Text = ((_mobile.Flags & Flags.Female) != 0) ? "F" : "M";
                        break;

                }
            }

            base.Update(totalMS, frameMS);
        }

        private string ConcatCurrentMax(int min, int max)
        {
            return string.Format("{0}/{1}", min, max);
        }

        private enum MobileStats
        {
            Name,
            Strength,
            Dexterity,
            Intelligence,
            HealthCurrent,
            HealthMax,
            StaminaCurrent,
            StaminaMax,
            ManaCurrent,
            ManaMax,
            WeightMax,
            Followers,
            WeightCurrent,
            LowerReagentCost,
            SpellDamageInc,
            FasterCasting,
            FasterCastRecovery,
            StatCap,
            HitChanceInc,
            DefenseChanceInc,
            LowerManaCost,
            DamageChanceInc,
            SwingSpeedInc,
            Luck,
            Gold,
            AR,
            RF,
            RC,
            RP,
            RE,
            Damage,
            Sex,
            Max
        }

    }
}
