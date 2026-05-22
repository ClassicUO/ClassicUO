// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Resources;

namespace ClassicUO.Game.Entities.Players
{
    internal sealed class CharacterStatusHandler : IEventListener
    {
        private readonly World _world;

        public CharacterStatusHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.CharacterStatusReceived += OnCharacterStatusReceived;
        }

        public void Unsubscribe()
        {
            EventSink.CharacterStatusReceived -= OnCharacterStatusReceived;
        }

        private void OnCharacterStatusReceived(CharacterStatusReceivedArgs e)
        {
            if (_world.Player == null) return;

            Entity entity = _world.Get(e.Serial);
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

                if (mobile == _world.Player)
                {
                    if (!string.IsNullOrEmpty(_world.Player.Name) && oldName != _world.Player.Name)
                    {
                        Client.Game.SetWindowTitle(_world.Player.Name);
                    }

                    if (_world.Player.Strength != 0
                        && ProfileManager.CurrentProfile != null
                        && ProfileManager.CurrentProfile.ShowStatsChangedMessage)
                    {
                        ushort currentStr = _world.Player.Strength;
                        ushort currentDex = _world.Player.Dexterity;
                        ushort currentInt = _world.Player.Intelligence;

                        int deltaStr = e.Strength - currentStr;
                        int deltaDex = e.Dexterity - currentDex;
                        int deltaInt = e.Intelligence - currentInt;

                        if (deltaStr != 0)
                        {
                            GameActions.Print(
                                _world,
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
                                _world,
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
                                _world,
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

                    _world.Player.Strength = e.Strength;
                    _world.Player.Dexterity = e.Dexterity;
                    _world.Player.Intelligence = e.Intelligence;
                    _world.Player.Stamina = e.Stamina;
                    _world.Player.StaminaMax = e.StaminaMax;
                    _world.Player.Mana = e.Mana;
                    _world.Player.ManaMax = e.ManaMax;
                    _world.Player.Gold = e.Gold;
                    _world.Player.PhysicalResistance = e.PhysicalResistance;
                    _world.Player.Weight = e.Weight;

                    if (e.Type >= 5)
                    {
                        _world.Player.WeightMax = e.WeightMax;
                        byte race = e.Race;
                        if (race == 0) race = 1;
                        _world.Player.Race = (RaceType)race;
                    }
                    else
                    {
                        if (Client.Game.UO.Version >= Utility.ClientVersion.CV_500A)
                        {
                            _world.Player.WeightMax = (ushort)(7 * (_world.Player.Strength >> 1) + 40);
                        }
                        else
                        {
                            _world.Player.WeightMax = (ushort)(_world.Player.Strength * 4 + 25);
                        }
                    }

                    if (e.HasRenaissanceStats)
                    {
                        _world.Player.StatsCap = e.StatsCap;
                        _world.Player.Followers = e.Followers;
                        _world.Player.FollowersMax = e.FollowersMax;
                    }

                    if (e.HasAosStats)
                    {
                        _world.Player.FireResistance = e.FireResistance;
                        _world.Player.ColdResistance = e.ColdResistance;
                        _world.Player.PoisonResistance = e.PoisonResistance;
                        _world.Player.EnergyResistance = e.EnergyResistance;
                        _world.Player.Luck = e.Luck;
                        _world.Player.DamageMin = e.DamageMin;
                        _world.Player.DamageMax = e.DamageMax;
                        _world.Player.TithingPoints = e.TithingPoints;
                    }

                    if (e.HasKrSaStats)
                    {
                        _world.Player.MaxPhysicResistence = e.MaxPhysicResistence;
                        _world.Player.MaxFireResistence = e.MaxFireResistence;
                        _world.Player.MaxColdResistence = e.MaxColdResistence;
                        _world.Player.MaxPoisonResistence = e.MaxPoisonResistence;
                        _world.Player.MaxEnergyResistence = e.MaxEnergyResistence;
                        _world.Player.DefenseChanceIncrease = e.DefenseChanceIncrease;
                        _world.Player.MaxDefenseChanceIncrease = e.MaxDefenseChanceIncrease;
                        _world.Player.HitChanceIncrease = e.HitChanceIncrease;
                        _world.Player.SwingSpeedIncrease = e.SwingSpeedIncrease;
                        _world.Player.DamageIncrease = e.DamageIncrease;
                        _world.Player.LowerReagentCost = e.LowerReagentCost;
                        _world.Player.SpellDamageIncrease = e.SpellDamageIncrease;
                        _world.Player.FasterCastRecovery = e.FasterCastRecovery;
                        _world.Player.FasterCasting = e.FasterCasting;
                        _world.Player.LowerManaCost = e.LowerManaCost;
                    }
                }
            }

            if (mobile == _world.Player)
            {
                _world.UoAssist.SignalHits();
                _world.UoAssist.SignalStamina();
                _world.UoAssist.SignalMana();
            }
        }
    }
}
