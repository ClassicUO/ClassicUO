// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Resources;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeCharacterStatus()
        {
            EventSink.CharacterStatusReceived += OnCharacterStatusReceived;
        }

        private void UnsubscribeCharacterStatus()
        {
            EventSink.CharacterStatusReceived -= OnCharacterStatusReceived;
        }

        private void OnCharacterStatusReceived(CharacterStatusReceivedArgs e)
        {
            if (Player == null) return;

            Entity entity = Get(e.Serial);
            if (entity == null) return;

            string oldName = entity.Name;
            entity.Name = e.Name;
            entity.Hits = e.Hits;
            entity.HitsMax = e.HitsMax;

            if (entity.HitsRequest == HitsRequestStatus.Pending)
            {
                entity.HitsRequest = HitsRequestStatus.Received;
            }

            if (!SerialHelper.IsMobile(e.Serial)) return;

            if (!(entity is Mobile mobile)) return;

            mobile.IsRenamable = e.IsRenamable;

            if (e.HasExtendedStats)
            {
                mobile.IsFemale = e.IsFemale;

                if (mobile == Player)
                {
                    if (!string.IsNullOrEmpty(Player.Name) && oldName != Player.Name)
                    {
                        Client.Game.SetWindowTitle(Player.Name);
                    }

                    if (Player.Strength != 0
                        && ProfileManager.CurrentProfile != null
                        && ProfileManager.CurrentProfile.ShowStatsChangedMessage)
                    {
                        ushort currentStr = Player.Strength;
                        ushort currentDex = Player.Dexterity;
                        ushort currentInt = Player.Intelligence;

                        int deltaStr = e.Strength - currentStr;
                        int deltaDex = e.Dexterity - currentDex;
                        int deltaInt = e.Intelligence - currentInt;

                        if (deltaStr != 0)
                        {
                            GameActions.Print(
                                this,
                                string.Format(
                                    ResGeneral.Your0HasChangedBy1ItIsNow2,
                                    ResGeneral.Strength,
                                    deltaStr,
                                    e.Strength
                                ),
                                0x0170,
                                MessageType.System,
                                3,
                                false
                            );
                        }

                        if (deltaDex != 0)
                        {
                            GameActions.Print(
                                this,
                                string.Format(
                                    ResGeneral.Your0HasChangedBy1ItIsNow2,
                                    ResGeneral.Dexterity,
                                    deltaDex,
                                    e.Dexterity
                                ),
                                0x0170,
                                MessageType.System,
                                3,
                                false
                            );
                        }

                        if (deltaInt != 0)
                        {
                            GameActions.Print(
                                this,
                                string.Format(
                                    ResGeneral.Your0HasChangedBy1ItIsNow2,
                                    ResGeneral.Intelligence,
                                    deltaInt,
                                    e.Intelligence
                                ),
                                0x0170,
                                MessageType.System,
                                3,
                                false
                            );
                        }
                    }

                    Player.Strength = e.Strength;
                    Player.Dexterity = e.Dexterity;
                    Player.Intelligence = e.Intelligence;
                    Player.Stamina = e.Stamina;
                    Player.StaminaMax = e.StaminaMax;
                    Player.Mana = e.Mana;
                    Player.ManaMax = e.ManaMax;
                    Player.Gold = e.Gold;
                    Player.PhysicalResistance = e.PhysicalResistance;
                    Player.Weight = e.Weight;

                    if (e.Type >= 5)
                    {
                        Player.WeightMax = e.WeightMax;
                        byte race = e.Race;
                        if (race == 0) race = 1;
                        Player.Race = (RaceType)race;
                    }
                    else
                    {
                        if (Client.Game.UO.Version >= Utility.ClientVersion.CV_500A)
                        {
                            Player.WeightMax = (ushort)(7 * (Player.Strength >> 1) + 40);
                        }
                        else
                        {
                            Player.WeightMax = (ushort)(Player.Strength * 4 + 25);
                        }
                    }

                    if (e.HasRenaissanceStats)
                    {
                        Player.StatsCap = e.StatsCap;
                        Player.Followers = e.Followers;
                        Player.FollowersMax = e.FollowersMax;
                    }

                    if (e.HasAosStats)
                    {
                        Player.FireResistance = e.FireResistance;
                        Player.ColdResistance = e.ColdResistance;
                        Player.PoisonResistance = e.PoisonResistance;
                        Player.EnergyResistance = e.EnergyResistance;
                        Player.Luck = e.Luck;
                        Player.DamageMin = e.DamageMin;
                        Player.DamageMax = e.DamageMax;
                        Player.TithingPoints = e.TithingPoints;
                    }

                    if (e.HasKrSaStats)
                    {
                        Player.MaxPhysicResistence = e.MaxPhysicResistence;
                        Player.MaxFireResistence = e.MaxFireResistence;
                        Player.MaxColdResistence = e.MaxColdResistence;
                        Player.MaxPoisonResistence = e.MaxPoisonResistence;
                        Player.MaxEnergyResistence = e.MaxEnergyResistence;
                        Player.DefenseChanceIncrease = e.DefenseChanceIncrease;
                        Player.MaxDefenseChanceIncrease = e.MaxDefenseChanceIncrease;
                        Player.HitChanceIncrease = e.HitChanceIncrease;
                        Player.SwingSpeedIncrease = e.SwingSpeedIncrease;
                        Player.DamageIncrease = e.DamageIncrease;
                        Player.LowerReagentCost = e.LowerReagentCost;
                        Player.SpellDamageIncrease = e.SpellDamageIncrease;
                        Player.FasterCastRecovery = e.FasterCastRecovery;
                        Player.FasterCasting = e.FasterCasting;
                        Player.LowerManaCost = e.LowerManaCost;
                    }
                }
            }

            if (mobile == Player)
            {
                UoAssist.SignalHits();
                UoAssist.SignalStamina();
                UoAssist.SignalMana();
            }
        }
    }
}
