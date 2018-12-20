#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System;
using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{   
    public class PlayerMobile : Mobile
    {
        private readonly Ability[] _ability = new Ability[2]
        {
            Ability.None, Ability.None
        };
        private readonly Dictionary<Graphic, BuffIcon> _buffIcons = new Dictionary<Graphic, BuffIcon>();
        private readonly Skill[] _sklls;
        private ushort _damageIncrease;
        private ushort _damageMax;
        private ushort _damageMin;
        private ushort _defenseChanceInc;
        private ushort _dexterity;
        private ushort _dexterityInc;
        private ushort _enhancePotions;
        private ushort _fasterCasting;
        private ushort _fasterCastRecovery;
        private bool _female;
        private byte _followers;
        private byte _followersMax;
        private uint _gold;
        private ushort _hitChanceInc;
        private ushort _hitPointsInc;
        private ushort _hitPointsRegen;
        private ushort _intelligence;
        private ushort _intelligenceInc;
        private ushort _lowerManaCost;
        private ushort _lowerReagentCost;
        private ushort _luck;
        private ushort _manaInc;
        private ushort _manaRegen;
        private ushort _maxColdcRes;
        private ushort _maxDefChance;
        private ushort _maxEnergRes;
        private ushort _maxFireRes;
        private ushort _maximumHitPointsInc;
        private ushort _maximumManaInc;
        private ushort _maximumStaminaInc;
        private ushort _maxPhysicRes;
        private ushort _maxPoisResUshort;
        private ushort _reflectPhysicalDamage;
        private ushort _resistCold;
        private ushort _resistEnergy;
        private ushort _resistFire;
        private ushort _resistPhysical;
        private ushort _resistPoison;
        private ushort _spellDamageInc;
        private ushort _staminaInc;
        private ushort _staminaRegen;
        private ushort _statscap;
        private ushort _strength;
        private ushort _strengthInc;
        private ushort _swingSpeedInc;
        private uint _tithingPoints;
        private ushort _weight;
        private ushort _weightMax;

      

        public PlayerMobile(Serial serial) : base(serial)
        {
            _sklls = new Skill[IO.Resources.Skills.SkillsCount];

            for (int i = 0; i < _sklls.Length; i++)
            {
                SkillEntry skill = IO.Resources.Skills.GetSkill(i);
                _sklls[i] = new Skill(skill.Name, skill.Index, skill.HasButton);
            }

            if (serial.IsValid)
                NetClient.Socket.Send(new PSkillsRequest(this));
        }

        public IReadOnlyList<Skill> Skills => _sklls;

        public override bool InWarMode { get; set; }

        public Deque<Step> RequestedSteps { get; } = new Deque<Step>();

        public IReadOnlyDictionary<Graphic, BuffIcon> BuffIcons => _buffIcons;

        public ushort Strength
        {
            get => _strength;
            set
            {
                if (_strength != value)
                {
                    _strength = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort Intelligence
        {
            get => _intelligence;
            set
            {
                if (_intelligence != value)
                {
                    _intelligence = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort Dexterity
        {
            get => _dexterity;
            set
            {
                if (_dexterity != value)
                {
                    _dexterity = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort Weight
        {
            get => _weight;
            set
            {
                if (_weight != value)
                {
                    _weight = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort WeightMax
        {
            get => _weightMax;
            set
            {
                if (_weightMax != value)
                {
                    _weightMax = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public uint Gold
        {
            get => _gold;
            set
            {
                if (_gold != value)
                {
                    _gold = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort ResistPhysical
        {
            get => _resistPhysical;
            set
            {
                if (_resistPhysical != value)
                {
                    _resistPhysical = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort ResistFire
        {
            get => _resistFire;
            set
            {
                if (_resistFire != value)
                {
                    _resistFire = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort ResistCold
        {
            get => _resistCold;
            set
            {
                if (_resistCold != value)
                {
                    _resistCold = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort ResistPoison
        {
            get => _resistPoison;
            set
            {
                if (_resistPoison != value)
                {
                    _resistPoison = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort ResistEnergy
        {
            get => _resistEnergy;
            set
            {
                if (_resistEnergy != value)
                {
                    _resistEnergy = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public byte Followers
        {
            get => _followers;
            set
            {
                if (_followers != value)
                {
                    _followers = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public byte FollowersMax
        {
            get => _followersMax;
            set
            {
                if (_followersMax != value)
                {
                    _followersMax = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort Luck
        {
            get => _luck;
            set
            {
                if (_luck != value)
                {
                    _luck = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public uint TithingPoints
        {
            get => _tithingPoints;
            set
            {
                if (_tithingPoints != value)
                {
                    _tithingPoints = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort DamageMin
        {
            get => _damageMin;
            set
            {
                if (_damageMin != value)
                {
                    _damageMin = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort DamageMax
        {
            get => _damageMax;
            set
            {
                if (_damageMax != value)
                {
                    _damageMax = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public bool Female
        {
            get => _female;
            set
            {
                if (_female != value)
                {
                    _female = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort StatsCap
        {
            get => _statscap;
            set
            {
                if (_statscap != value)
                {
                    _statscap = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort HitChanceInc
        {
            get => _hitChanceInc;
            set
            {
                if (_hitChanceInc != value)
                {
                    _hitChanceInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort SwingSpeedInc
        {
            get => _swingSpeedInc;
            set
            {
                if (_swingSpeedInc != value)
                {
                    _swingSpeedInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort DamageIncrease
        {
            get => _damageIncrease;
            set
            {
                if (_damageIncrease != value)
                {
                    _damageIncrease = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort LowerReagentCost
        {
            get => _lowerReagentCost;
            set
            {
                if (_lowerReagentCost != value)
                {
                    _lowerReagentCost = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort HitPointsRegen
        {
            get => _hitPointsRegen;
            set
            {
                if (_hitPointsRegen != value)
                {
                    _hitPointsRegen = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort StaminaRegen
        {
            get => _staminaRegen;
            set
            {
                if (_staminaRegen != value)
                {
                    _staminaRegen = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort ManaRegen
        {
            get => _manaRegen;
            set
            {
                if (_manaRegen != value)
                {
                    _manaRegen = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort MaxPhysicRes
        {
            get => _maxPhysicRes;
            set
            {
                if (_maxPhysicRes != value)
                {
                    _maxPhysicRes = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort MaxFireRes
        {
            get => _maxFireRes;
            set
            {
                if (_maxFireRes != value)
                {
                    _maxFireRes = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort MaxColdRes
        {
            get => _maxColdcRes;
            set
            {
                if (_maxColdcRes != value)
                {
                    _maxColdcRes = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort MaxPoisonRes
        {
            get => _maxPoisResUshort;
            set
            {
                if (_maxPoisResUshort != value)
                {
                    _maxPoisResUshort = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort MaxEnergyRes
        {
            get => _maxEnergRes;
            set
            {
                if (_maxEnergRes != value)
                {
                    _maxEnergRes = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort MaxDefChance
        {
            get => _maxDefChance;
            set
            {
                if (_maxDefChance != value)
                {
                    _maxDefChance = value;
                    _delta |= Delta.Attributes;
                }
            }
        }

        //====================================================
        public ushort ReflectPhysicalDamage
        {
            get => _reflectPhysicalDamage;
            set
            {
                if (_reflectPhysicalDamage != value)
                {
                    _reflectPhysicalDamage = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort EnhancePotions
        {
            get => _enhancePotions;
            set
            {
                if (_enhancePotions != value)
                {
                    _enhancePotions = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort DefenseChanceInc
        {
            get => _defenseChanceInc;
            set
            {
                if (_defenseChanceInc != value)
                {
                    _defenseChanceInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort SpellDamageInc
        {
            get => _spellDamageInc;
            set
            {
                if (_spellDamageInc != value)
                {
                    _spellDamageInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort FasterCastRecovery
        {
            get => _fasterCastRecovery;
            set
            {
                if (_fasterCastRecovery != value)
                {
                    _fasterCastRecovery = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort FasterCasting
        {
            get => _fasterCasting;
            set
            {
                if (_fasterCasting != value)
                {
                    _fasterCasting = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort LowerManaCost
        {
            get => _lowerManaCost;
            set
            {
                if (_lowerManaCost != value)
                {
                    _lowerManaCost = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort StrengthInc
        {
            get => _strengthInc;
            set
            {
                if (_strengthInc != value)
                {
                    _strengthInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort DexterityInc
        {
            get => _dexterityInc;
            set
            {
                if (_dexterityInc != value)
                {
                    _dexterityInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort IntelligenceInc
        {
            get => _intelligenceInc;
            set
            {
                if (_intelligenceInc != value)
                {
                    _intelligenceInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort HitPointsInc
        {
            get => _hitPointsInc;
            set
            {
                if (_hitPointsInc != value)
                {
                    _hitPointsInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort StaminaInc
        {
            get => _staminaInc;
            set
            {
                if (_staminaInc != value)
                {
                    _staminaInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort ManaInc
        {
            get => _manaInc;
            set
            {
                if (_manaInc != value)
                {
                    _manaInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort MaximumHitPointsInc
        {
            get => _maximumHitPointsInc;
            set
            {
                if (_maximumHitPointsInc != value)
                {
                    _maximumHitPointsInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort MaximumStaminaInc
        {
            get => _maximumStaminaInc;
            set
            {
                if (_maximumStaminaInc != value)
                {
                    _maximumStaminaInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public ushort MaximumManaInc
        {
            get => _maximumManaInc;
            set
            {
                if (_maximumManaInc != value)
                {
                    _maximumManaInc = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public Ability PrimaryAbility
        {
            get => _ability[0];
            set => _ability[0] = value;
        }

        public Ability SecondaryAbility
        {
            get => _ability[1];
            set => _ability[1] = value;
        }

        public Lock StrLock { get; set; }

        public Lock DexLock { get; set; }

        public Lock IntLock { get; set; }

        protected override bool IsWalking => LastStepTime > Engine.Ticks - Constants.PLAYER_WALKING_DELAY;

        public void AddBuff(Graphic graphic, uint time, string text)
        {
            _buffIcons[graphic] = new BuffIcon(graphic, time, text);
        }

        public void RemoveBuff(Graphic graphic)
        {
            _buffIcons.Remove(graphic);
        }

        public event EventHandler StatsChanged, SkillsChanged;

        public void UpdateSkill(int id, ushort realValue, ushort baseValue, Lock @lock, ushort cap)
        {
            if (id < _sklls.Length)
            {
                Skill skill = _sklls[id];
                skill.ValueFixed = realValue;
                skill.BaseFixed = baseValue;
                skill.Lock = @lock;
                skill.CapFixed = cap;
                _delta |= Delta.Skills;
            }
        }

        public void UpdateSkillLock(int id, Lock @lock)
        {
            if (id < _sklls.Length)
            {
                Skill skill = _sklls[id];
                skill.Lock = @lock;
                _delta |= Delta.Skills;
            }
        }

        public void UpdateAbilities()
        {
            Item right = Equipment[(int) Layer.OneHanded];
            Item left = Equipment[(int) Layer.TwoHanded];
            _ability[0] = Ability.None;
            _ability[1] = Ability.None;

            if (right == null && left == null) return;

            Graphic[] graphics =
            {
                0x00, 0x00
            };

            if (right == null)
                graphics[0] = left.Graphic;
            else if (left == null)
                graphics[1] = right.Graphic;
            else
            {
                graphics[0] = left.Graphic;
                graphics[1] = right.Graphic;
            }

            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic g = graphics[i];

                switch (g)
                {
                    case 0x0901: // Gargish Cyclone
                        _ability[0] = Ability.MovingShot;
                        _ability[1] = Ability.InfusedThrow;

                        goto done;
                    case 0x0902: // Gargish Dagger
                        _ability[0] = Ability.InfectiousStrike;
                        _ability[1] = Ability.ShadowStrike;

                        goto done;
                    case 0x0905: // Glass Staff
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x0906: // serpentstone staff
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x090C: // Glass Sword
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x0DF0:
                    case 0x0DF1: // Black Staves
                        _ability[0] = Ability.WhirlwindAttack;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x0DF2:
                    case 0x0DF3:
                    case 0x0DF4:
                    case 0x0DF5: // Wands BookType A-D
                        _ability[0] = Ability.Dismount;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x0E81:
                    case 0x0E82: // Shepherd's Crooks
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x0E85:
                    case 0x0E86: // Pickaxes
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x0E87:
                    case 0x0E88: // Pitchforks
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x0E89:
                    case 0x0E8A: // Quarter Staves
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x0EC2:
                    case 0x0EC3: // Cleavers
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.InfectiousStrike;

                        goto done;
                    case 0x0EC4:
                    case 0x0EC5: // Skinning Knives
                        _ability[0] = Ability.ShadowStrike;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x0F43:
                    case 0x0F44: // Hatchets
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x0F45:
                    case 0x0F46: // Double Axes
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x0F47:
                    case 0x0F48: // Battle Axes
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x0F49:
                    case 0x0F4A: // Axes
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x0F4B:
                    case 0x0F4C:
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.WhirlwindAttack;

                        goto done;
                    case 0x0F4D:
                    case 0x0F4E: // Bardiches
                        _ability[0] = Ability.ParalyzingBlow;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x0F4F:
                    case 0x0F50: // Crossbows
                        _ability[0] = Ability.ConcussionBlow;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x0F51:
                    case 0x0F52: // Daggers
                        _ability[0] = Ability.InfectiousStrike;
                        _ability[1] = Ability.ShadowStrike;

                        goto done;
                    case 0x0F5C:
                    case 0x0F5D: // Maces
                        _ability[0] = Ability.ConcussionBlow;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x0F5E:
                    case 0x0F5F: // Broadswords
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.ArmorIgnore;

                        goto done;
                    case 0x0F60:
                    case 0x0F61: // Longswords
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x0F62:
                    case 0x0F63: // Spears
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x0FB5:
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.ShadowStrike;

                        goto done;
                    case 0x13AF:
                    case 0x13B0: // War Axes
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.BleedAttack;

                        goto done;
                    case 0x13B1:
                    case 0x13B2: // Bows
                        _ability[0] = Ability.ParalyzingBlow;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x13B3:
                    case 0x13B4: // Clubs
                        _ability[0] = Ability.ShadowStrike;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x13B7:
                    case 0x13B8: // Scimitars
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x13B9:
                    case 0x13BA: // Viking Swords
                        _ability[0] = Ability.ParalyzingBlow;
                        _ability[1] = Ability.CrushingBlow;

                        goto done;
                    case 0x13FD: // Heavy Crossbows
                        _ability[0] = Ability.MovingShot;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x13E3: // Smith's Hammers
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.ShadowStrike;

                        goto done;
                    case 0x13F6: // Butcher Knives
                        _ability[0] = Ability.InfectiousStrike;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x13F8: // Gnarled Staves
                        _ability[0] = Ability.ConcussionBlow;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x13FB: // Large Battle Axes
                        _ability[0] = Ability.WhirlwindAttack;
                        _ability[1] = Ability.BleedAttack;

                        goto done;
                    case 0x13FF: // Katana
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.ArmorIgnore;

                        goto done;
                    case 0x1401: // Kryss
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.InfectiousStrike;

                        goto done;
                    case 0x1402:
                    case 0x1403: // Short Spears
                        _ability[0] = Ability.ShadowStrike;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x1404:
                    case 0x1405: // War Forks
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x1406:
                    case 0x1407: // War Maces
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.BleedAttack;

                        goto done;
                    case 0x1438:
                    case 0x1439: // War Hammers
                        _ability[0] = Ability.WhirlwindAttack;
                        _ability[1] = Ability.CrushingBlow;

                        goto done;
                    case 0x143A:
                    case 0x143B: // Mauls
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x143C:
                    case 0x143D: // Hammer Picks
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x143E:
                    case 0x143F: // Halberds
                        _ability[0] = Ability.WhirlwindAttack;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x1440:
                    case 0x1441: // Cutlasses
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.ShadowStrike;

                        goto done;
                    case 0x1442:
                    case 0x1443: // Two Handed Axes
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.ShadowStrike;

                        goto done;
                    case 0x26BA: // Scythes
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x26BB: // Bone Harvesters
                        _ability[0] = Ability.ParalyzingBlow;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x26BC: // Scepters
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x26BD: // Bladed Staves
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x26BE: // Pikes
                        _ability[0] = Ability.ParalyzingBlow;
                        _ability[1] = Ability.InfectiousStrike;

                        goto done;
                    case 0x26BF: // Double Bladed Staff
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.InfectiousStrike;

                        goto done;
                    case 0x26C0: // Lances
                        _ability[0] = Ability.Dismount;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x26C1: // Crescent Blades
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x26C2: // Composite Bows
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.MovingShot;

                        goto done;
                    case 0x26C3: // Repeating Crossbows
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.MovingShot;

                        goto done;
                    case 0x26C4: // also Scythes
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x26C5: // also Bone Harvesters
                        _ability[0] = Ability.ParalyzingBlow;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x26C6: // also Scepters
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x26C7: // also Bladed Staves
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x26C8: // also Pikes
                        _ability[0] = Ability.ParalyzingBlow;
                        _ability[1] = Ability.InfectiousStrike;

                        goto done;
                    case 0x26C9: // also Double Bladed Staff
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.InfectiousStrike;

                        goto done;
                    case 0x26CA: // also Lances
                        _ability[0] = Ability.Dismount;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x26CB: // also Crescent Blades
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x26CC: // also Composite Bows
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.MovingShot;

                        goto done;
                    case 0x26CD: // also Repeating Crossbows
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.MovingShot;

                        goto done;
                    case 0x27A2: // No-Dachi
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.RidingSwipe;

                        goto done;
                    case 0x27A3: // Tessen
                        _ability[0] = Ability.Feint;
                        _ability[1] = Ability.Block;

                        goto done;
                    case 0x27A4: // Wakizashi
                        _ability[0] = Ability.FrenziedWhirlwind;
                        _ability[1] = Ability.DoubleShot;

                        goto done;
                    case 0x27A5: // Yumi
                        _ability[0] = Ability.ArmorPierce;
                        _ability[1] = Ability.DoubleShot;

                        goto done;
                    case 0x27A6: // Tetsubo
                        _ability[0] = Ability.FrenziedWhirlwind;
                        _ability[1] = Ability.CrushingBlow;

                        goto done;
                    case 0x27A7: // Lajatang
                        _ability[0] = Ability.DefenseMastery;
                        _ability[1] = Ability.FrenziedWhirlwind;

                        goto done;
                    case 0x27A8: // Bokuto
                        _ability[0] = Ability.Feint;
                        _ability[1] = Ability.NerveStrike;

                        goto done;
                    case 0x27A9: // Daisho
                        _ability[0] = Ability.Feint;
                        _ability[1] = Ability.DoubleShot;

                        goto done;
                    case 0x27AA: // Fukya
                        _ability[0] = Ability.Disarm;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x27AB: // Tekagi
                        _ability[0] = Ability.DualWield;
                        _ability[1] = Ability.TalonStrike;

                        goto done;
                    case 0x27AD: // Kama
                        _ability[0] = Ability.WhirlwindAttack;
                        _ability[1] = Ability.DefenseMastery;

                        goto done;
                    case 0x27AE: // Nunchaku
                        _ability[0] = Ability.Block;
                        _ability[1] = Ability.Feint;

                        goto done;
                    case 0x27AF: // Sai
                        _ability[0] = Ability.Block;
                        _ability[1] = Ability.ArmorPierce;

                        goto done;
                    case 0x27ED: // also No-Dachi
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.RidingSwipe;

                        goto done;
                    case 0x27EE: // also Tessen
                        _ability[0] = Ability.Feint;
                        _ability[1] = Ability.Block;

                        goto done;
                    case 0x27EF: // also Wakizashi
                        _ability[0] = Ability.FrenziedWhirlwind;
                        _ability[1] = Ability.DoubleShot;

                        goto done;
                    case 0x27F0: // also Yumi
                        _ability[0] = Ability.ArmorPierce;
                        _ability[1] = Ability.DoubleShot;

                        goto done;
                    case 0x27F1: // also Tetsubo
                        _ability[0] = Ability.FrenziedWhirlwind;
                        _ability[1] = Ability.CrushingBlow;

                        goto done;
                    case 0x27F2: // also Lajatang
                        _ability[0] = Ability.DefenseMastery;
                        _ability[1] = Ability.FrenziedWhirlwind;

                        goto done;
                    case 0x27F3: // also Bokuto
                        _ability[0] = Ability.Feint;
                        _ability[1] = Ability.NerveStrike;

                        goto done;
                    case 0x27F4: // also Daisho
                        _ability[0] = Ability.Feint;
                        _ability[1] = Ability.DoubleShot;

                        goto done;
                    case 0x27F5: // also Fukya
                        _ability[0] = Ability.Disarm;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x27F6: // also Tekagi
                        _ability[0] = Ability.DualWield;
                        _ability[1] = Ability.TalonStrike;

                        goto done;
                    case 0x27F8: // Kama
                        _ability[0] = Ability.WhirlwindAttack;
                        _ability[1] = Ability.DefenseMastery;

                        goto done;
                    case 0x27F9: // Nunchaku
                        _ability[0] = Ability.Block;
                        _ability[1] = Ability.Feint;

                        goto done;
                    case 0x27FA: // Sai
                        _ability[0] = Ability.Block;
                        _ability[1] = Ability.ArmorPierce;

                        goto done;
                    case 0x2D1E: // Elven Composite Longbows
                        _ability[0] = Ability.ForceArrow;
                        _ability[1] = Ability.SerpentArrow;

                        goto done;
                    case 0x2D1F: // Magical Shortbows
                        _ability[0] = Ability.LightningArrow;
                        _ability[1] = Ability.PsychicAttack;

                        goto done;
                    case 0x2D20: // Elven Spellblades
                        _ability[0] = Ability.PsychicAttack;
                        _ability[1] = Ability.BleedAttack;

                        goto done;
                    case 0x2D21: // Assassin Spikes
                        _ability[0] = Ability.InfectiousStrike;
                        _ability[1] = Ability.ShadowStrike;

                        goto done;
                    case 0x2D22: // Leafblades
                        _ability[0] = Ability.Feint;
                        _ability[1] = Ability.ArmorIgnore;

                        goto done;
                    case 0x2D23: // War Cleavers
                        _ability[0] = Ability.Disarm;
                        _ability[1] = Ability.Bladeweave;

                        goto done;
                    case 0x2D24: // Diamond Maces
                        _ability[0] = Ability.ConcussionBlow;
                        _ability[1] = Ability.CrushingBlow;

                        goto done;
                    case 0x2D25: // Wild Staves
                        _ability[0] = Ability.Block;
                        _ability[1] = Ability.ForceOfNature;

                        goto done;
                    case 0x2D26: // Rune Blades
                        _ability[0] = Ability.Disarm;
                        _ability[1] = Ability.Bladeweave;

                        goto done;
                    case 0x2D27: // Radiant Scimitars
                        _ability[0] = Ability.WhirlwindAttack;
                        _ability[1] = Ability.Bladeweave;

                        goto done;
                    case 0x2D28: // Ornate Axes
                        _ability[0] = Ability.Disarm;
                        _ability[1] = Ability.CrushingBlow;

                        goto done;
                    case 0x2D29: // Elven Machetes
                        _ability[0] = Ability.DefenseMastery;
                        _ability[1] = Ability.Bladeweave;

                        goto done;
                    case 0x2D2A: // also Elven Composite Longbows
                        _ability[0] = Ability.ForceArrow;
                        _ability[1] = Ability.SerpentArrow;

                        goto done;
                    case 0x2D2B: // also Magical Shortbows
                        _ability[0] = Ability.LightningArrow;
                        _ability[1] = Ability.PsychicAttack;

                        goto done;
                    case 0x2D2C: // also Elven Spellblades
                        _ability[0] = Ability.PsychicAttack;
                        _ability[1] = Ability.BleedAttack;

                        goto done;
                    case 0x2D2D: // also Assassin Spikes
                        _ability[0] = Ability.InfectiousStrike;
                        _ability[1] = Ability.ShadowStrike;

                        goto done;
                    case 0x2D2E: // also Leafblades
                        _ability[0] = Ability.Feint;
                        _ability[1] = Ability.ArmorIgnore;

                        goto done;
                    case 0x2D2F: // also War Cleavers
                        _ability[0] = Ability.Disarm;
                        _ability[1] = Ability.Bladeweave;

                        goto done;
                    case 0x2D30: // also Diamond Maces
                        _ability[0] = Ability.ConcussionBlow;
                        _ability[1] = Ability.CrushingBlow;

                        goto done;
                    case 0x2D31: // also Wild Staves
                        _ability[0] = Ability.Block;
                        _ability[1] = Ability.ForceOfNature;

                        goto done;
                    case 0x2D32: // also Rune Blades
                        _ability[0] = Ability.Disarm;
                        _ability[1] = Ability.Bladeweave;

                        goto done;
                    case 0x2D33: // also Radiant Scimitars
                        _ability[0] = Ability.WhirlwindAttack;
                        _ability[1] = Ability.Bladeweave;

                        goto done;
                    case 0x2D34: // also Ornate Axes
                        _ability[0] = Ability.Disarm;
                        _ability[1] = Ability.CrushingBlow;

                        goto done;
                    case 0x2D35: // also Elven Machetes
                        _ability[0] = Ability.DefenseMastery;
                        _ability[1] = Ability.Bladeweave;

                        goto done;
                    case 0x4067: // Boomerang
                        _ability[0] = Ability.MysticArc;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x4068: // Dual Short Axes
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.InfectiousStrike;

                        goto done;
                    case 0x406B: // Soul Glaive
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x406C: // Cyclone
                        _ability[0] = Ability.MovingShot;
                        _ability[1] = Ability.InfusedThrow;

                        goto done;
                    case 0x406D: // Dual Pointed Spear
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x406E: // Disc Mace
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x4072: // Blood Blade
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x4074: // Dread Sword
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x4075: // Gargish Talwar
                        _ability[0] = Ability.WhirlwindAttack;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x4076: // Shortblade
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x48AE: // Gargish Cleaver
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.InfectiousStrike;

                        goto done;
                    case 0x48B0: // Gargish Battle Axe
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x48B2: // Gargish Axe
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x48B4: // Gargish Bardiche
                        _ability[0] = Ability.ParalyzingBlow;
                        _ability[1] = Ability.Dismount;

                        goto done;
                    case 0x48B6: // Gargish Butcher Knife
                        _ability[0] = Ability.InfectiousStrike;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x48B8: // Gargish Gnarled Staff
                        _ability[0] = Ability.ConcussionBlow;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x48BA: // Gargish Katana
                        _ability[0] = Ability.DoubleShot;
                        _ability[1] = Ability.ArmorIgnore;

                        goto done;
                    case 0x48BC: // Gargish Kryss
                        _ability[0] = Ability.ArmorIgnore;
                        _ability[1] = Ability.InfectiousStrike;

                        goto done;
                    case 0x48BE: // Gargish War Fork
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.Disarm;

                        goto done;
                    case 0x48CA: // Gargish Lance
                        _ability[0] = Ability.Dismount;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x48C0: // Gargish War Hammer
                        _ability[0] = Ability.WhirlwindAttack;
                        _ability[1] = Ability.CrushingBlow;

                        goto done;
                    case 0x48C2: // Gargish Maul
                        _ability[0] = Ability.CrushingBlow;
                        _ability[1] = Ability.ConcussionBlow;

                        goto done;
                    case 0x48C4: // Gargish Scyte
                        _ability[0] = Ability.BleedAttack;
                        _ability[1] = Ability.ParalyzingBlow;

                        goto done;
                    case 0x48C6: // Gargish Bone Harvester
                        _ability[0] = Ability.ParalyzingBlow;
                        _ability[1] = Ability.MortalStrike;

                        goto done;
                    case 0x48C8: // Gargish Pike
                        _ability[0] = Ability.ParalyzingBlow;
                        _ability[1] = Ability.InfectiousStrike;

                        goto done;
                    case 0x48CD: // Gargish Tessen
                        _ability[0] = Ability.Feint;
                        _ability[1] = Ability.Block;

                        goto done;
                    case 0x48CE: // Gargish Tekagi
                        _ability[0] = Ability.DualWield;
                        _ability[1] = Ability.TalonStrike;

                        goto done;
                    case 0x48D0: // Gargish Daisho
                        _ability[0] = Ability.Feint;
                        _ability[1] = Ability.DoubleShot;

                        goto done;
                }
            }

            done: ;
        }

        protected override void OnProcessDelta(Delta d)
        {
            base.OnProcessDelta(d);
            if (d.HasFlag(Delta.Stats)) StatsChanged.Raise(this);
            if (d.HasFlag(Delta.Skills)) SkillsChanged.Raise(this);
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();

            if (World.Map != null && World.Map.Index >= 0)
                World.Map.Center = new Point(X, Y);
        }

        public override void Dispose()
        {
            Log.Message(LogTypes.Warning, "PlayerMobile disposed!");
            base.Dispose();
        }



#if !JAEDAN_MOVEMENT_PATCH && !MOVEMENT2
        internal WalkerManager Walker { get; } = new WalkerManager();

        public bool Walk(Direction direction, bool run)
        {
            if (Walker.WalkingFailed || Walker.LastStepRequestTime > Engine.Ticks || Walker.StepsCount >= Constants.MAX_STEP_COUNT)
                return false;

            if (SpeedMode >= CharacterSpeedType.CantRun)
                run = false;

            int x = X;
            int y = Y;
            sbyte z = Z;
            Direction oldDirection = Direction;

            bool emptyStack = Steps.Count == 0;

            if (!emptyStack)
            {
                Step walkStep = Steps.Back();
                x = walkStep.X;
                y = walkStep.Y;
                z = walkStep.Z;
                oldDirection = (Direction)walkStep.Direction;
            }

            sbyte oldZ = z;
            ushort walkTime = Constants.TURN_DELAY;


            if ((oldDirection & Direction.Mask) == (direction & Direction.Mask))
            {
                Direction newDir = direction;
                int newX = x;
                int newY = y;
                sbyte newZ = z;

                if (!Pathfinder.CanWalk(ref newDir, ref newX, ref newY, ref newZ))
                    return false;

                if ((direction & Direction.Mask) != newDir)
                    direction = newDir;
                else
                {
                    direction = newDir;
                    x = newX;
                    y = newY;
                    z = newZ;
                    walkTime = (ushort)MovementSpeed.TimeToCompleteMovement(this, run);
                }
            }
            else
            {
                Direction newDir = direction;
                int newX = x;
                int newY = y;
                sbyte newZ = z;

                if (!Pathfinder.CanWalk(ref newDir, ref newX, ref newY, ref newZ))
                {
                    if ((oldDirection & Direction.Mask) == newDir)
                        return false;
                }

                if ((oldDirection & Direction.Mask) == newDir)
                {
                    x = newX;
                    y = newY;
                    z = newZ;
                    walkTime = (ushort)MovementSpeed.TimeToCompleteMovement(this, run);
                }

                direction = newDir;
            }

            if (emptyStack)
            {
                if (!IsWalking)
                    SetAnimation(0xFF);
                LastStepTime = Engine.Ticks;
            }

            ref var step = ref Walker.StepInfos[Walker.StepsCount];
        
            step.Sequence = Walker.WalkSequence;
            step.Accepted = false;
            step.Running = run;
            step.OldDirection = (byte)(oldDirection & Direction.Mask);
            step.Direction = (byte)direction;
            step.Timer = Engine.Ticks;
            step.X = (ushort)x;
            step.Y = (ushort)y;
            step.Z = z;
            step.NoRotation = ((step.OldDirection == (byte)direction) && (oldZ - z >= 11));

            Walker.StepsCount++;

            Steps.AddToBack(new Step()
            {
                X = x,
                Y = y,
                Z = z,
                Direction = (byte)direction,
                Run = run
            });

            //EnqueueStep(x, y, z, direction, run);

            byte sequence = Walker.WalkSequence;
            NetClient.Socket.Send(new PWalkRequest(direction, sequence, run, Walker.FastWalkStack.GetValue()));

            if (Walker.WalkSequence == 0xFF)
                Walker.WalkSequence = 1;
            else
                Walker.WalkSequence++;

            Walker.UnacceptedPacketsCount++;


            int nowDelta = 0;

            if (_lastDir == (int)direction && _lastMount == IsMounted && _lastRun == run)
            {
                nowDelta = (int)((Engine.Ticks - _lastStepTime) - walkTime + _lastDelta);

                if (Math.Abs(nowDelta) > 70)
                    nowDelta = 0;
                _lastDelta = nowDelta;
            }
            else
            {
                _lastDelta = 0;
            }

            _lastStepTime = (int)Engine.Ticks;
            _lastRun = run;
            _lastMount = IsMounted;
            _lastDir = (int)direction;


            Walker.LastStepRequestTime = Engine.Ticks + walkTime - nowDelta;
            GetGroupForAnimation(this);

            return true;
        }

        private bool _lastRun, _lastMount;
        private int _lastDir, _lastDelta, _lastStepTime;

#elif !MOVEMENT2
        private int _movementX, _movementY;
        private sbyte _movementZ;
        private int _stepsOutstanding;
        private Direction _movementDirection = Direction.North;
        private byte _sequenceNumber;
        private int _resynchronizing;
        private long _nextAllowedStepTime;

        public bool IsWaitingNextMovement => _nextAllowedStepTime > Engine.Ticks;

        public bool Walk(Direction direction, bool run)
        {
            if (_nextAllowedStepTime > Engine.Ticks || IsParalyzed)
            {
                return false;
            }

            if (_stepsOutstanding > Constants.MAX_STEP_COUNT)
            {
                if (_nextAllowedStepTime + 1000 > Engine.Ticks)
                    Resynchronize();
                return false;
            }

       

            if (SpeedMode >= CharacterSpeedType.CantRun)
                run = false;
            // else ALWASY RUN CHECK

            ushort walkTime;
            Direction newDirection = direction;
            int newX = _movementX;
            int newY = _movementY;
            sbyte newZ = _movementZ;

            if (_movementDirection == newDirection)
            {
                if (!Pathfinder.CanWalk(ref newDirection, ref newX, ref newY, ref newZ))
                    return false;

                if (newDirection != direction)
                {
                    direction = newDirection;
                    walkTime = Constants.TURN_DELAY;
                }
                else
                {
                    direction = newDirection;
                    _movementX = newX;
                    _movementY = newY;
                    _movementZ = newZ;
                    walkTime = (ushort)MovementSpeed.TimeToCompleteMovement(this, run);
                }
            }
            else
            {
                if (!Pathfinder.CanWalk(ref newDirection, ref newX, ref newY, ref newZ))
                {
                    if (newDirection == _movementDirection)
                        return false;
                }

                if (_movementDirection == newDirection)
                {
                    direction = newDirection;
                    _movementX = newX;
                    _movementY = newY;
                    _movementZ = newZ;
                    walkTime = (ushort)MovementSpeed.TimeToCompleteMovement(this, run);
                }
                else
                {
                    direction = newDirection;
                    walkTime = Constants.TURN_DELAY;
                }
            }

            _movementDirection = direction;

            EnqueueStep(_movementX, _movementY, _movementZ, _movementDirection, run);

            Log.Message(LogTypes.Panic, "SEND");
            NetClient.Socket.Send(new PWalkRequest(direction, _sequenceNumber, run));
            //Log.Message(LogTypes.Trace, $"Walk request - SEQUENCE: {_sequenceNumber}");

            if (_sequenceNumber == 0xFF)
                _sequenceNumber = 1;
            else
                _sequenceNumber++;
            _nextAllowedStepTime = Engine.Ticks + walkTime;
            _stepsOutstanding++;
            GetGroupForAnimation(this);

            return true;
        }
#else
        public long LastStepRequestedTime { get; set; }
        public byte SequenceNumber { get; set; }

        private PlayerMovementState _movementState;

        enum PlayerMovementState
        {
            ANIMATE_IMMEDIATELY = 0,
            ANIMATE_ON_CONFIRM,
        }
        
        public bool Walk(Direction direction, bool run)
        {
            if (LastStepRequestedTime > Engine.Ticks)
                return false;

            if (RequestedSteps.Count >= Constants.MAX_STEP_COUNT)
                return false;

            if (SpeedMode >= CharacterSpeedType.CantRun)
                run = false;
            // else ALWASY RUN CHECK


            int x, y;
            sbyte z;
            Direction oldDirection;

            if (RequestedSteps.Count == 0)
                GetEndPosition(out x, out y, out  z, out oldDirection);
            else
            {
                Step step = RequestedSteps.Back();

                x = step.X;
                y = step.Y;
                z = step.Z;
                oldDirection = (Direction) step.Direction;
            }

            bool onMount = IsMounted;
            ushort walkTime;

            Direction newDirection = direction;
            int newX = x;
            int newY = y;
            sbyte newZ = z;

            if (oldDirection == newDirection)
            {
                if (!Pathfinder.CanWalk(ref newDirection, ref newX, ref newY, ref newZ))
                    return false;

                if (newDirection != direction)
                {
                    direction = newDirection;
                    walkTime = Constants.TURN_DELAY;
                }
                else
                {
                    direction = newDirection;
                    x = newX;
                    y = newY;
                    z = newZ;
                    walkTime = (ushort) MovementSpeed.TimeToCompleteMovement(this, run);
                }
            }
            else
            {
                if (!Pathfinder.CanWalk(ref newDirection, ref newX, ref newY, ref newZ))
                {
                    if (newDirection == oldDirection)
                        return false;
                }

                if (oldDirection == newDirection)
                {
                    direction = newDirection;
                    x = newX;
                    y = newY;
                    z = newZ;
                    walkTime = (ushort)MovementSpeed.TimeToCompleteMovement(this, run);
                }
                else
                {
                    direction = newDirection;
                    walkTime = Constants.TURN_DELAY;
                }
            }

            Step step1 = new Step()
            {
                X = x,
                Y = y,
                Z = z,
                Direction = (byte) direction,
                Run = run,
                Rej = 0,
                Seq = SequenceNumber
            };

            if (_movementState == PlayerMovementState.ANIMATE_IMMEDIATELY)
            {
                for (int i = 0; i < RequestedSteps.Count; i++)
                {
                    var s = RequestedSteps[i];

                    if (!s.Anim)
                    {
                        s.Anim = true;
                        RequestedSteps[i] = s;
                        EnqueueStep(s.X, s.Y, s.Z, (Direction) s.Direction, s.Run);
                    }
                }
            
                step1.Anim = true;

                EnqueueStep(step1.X, step1.Y, step1.Z, (Direction) step1.Direction, step1.Run);
            }
            RequestedSteps.AddToBack(step1);

            NetClient.Socket.Send(new PWalkRequest(direction, SequenceNumber, run, 0));

            if (SequenceNumber == 0xFF)
                SequenceNumber = 1;
            else SequenceNumber++;

            LastStepRequestedTime = Engine.Ticks + walkTime;

            GetGroupForAnimation(this);

            return true;
        }
#endif

        public void ConfirmWalk(byte seq)
        {
#if MOVEMENT2

            if (RequestedSteps.Count == 0)
            {
                NetClient.Socket.Send(new PResend());
                return;
            }

            Step step = RequestedSteps.RemoveFromFront();

            if (step.Seq != seq)
            {
                NetClient.Socket.Send(new PResend());
                return;
            }

            if (!step.Anim)
            {
                GetEndPosition(out int x, out int y, out sbyte z, out Direction dir);

                if (step.Direction == (byte) dir)
                {
                    if (_movementState == PlayerMovementState.ANIMATE_ON_CONFIRM)
                    {
                        _movementState = PlayerMovementState.ANIMATE_IMMEDIATELY;
                    }
                }

                EnqueueStep(step.X, step.Y, step.Z, (Direction) step.Direction, step.Run);
            }

#elif JAEDAN_MOVEMENT_PATCH

            if (_stepsOutstanding == 0)
            {
                Log.Message(LogTypes.Warning, $"Resync needed after confirmwalk packet - SEQUENCE: {_sequenceNumber}");
                Resynchronize();
            }
            else
            {
                //Log.Message(LogTypes.Trace, $"Step accepted - SEQUENCE: {_sequenceNumber}");
                _stepsOutstanding--;
            }
#else
     Walker.ConfirmWalk(seq);
#endif
        }

#if JAEDAN_MOVEMENT_PATCH

        public override void ForcePosition(ushort x, ushort y, sbyte z, Direction dir)
        {

            //Log.Message(LogTypes.Warning, $"Forced position. - SEQUENCE: {_sequenceNumber}");

            _nextAllowedStepTime = Engine.Ticks;
            _sequenceNumber = 0;
            _stepsOutstanding = 0;
            _movementX = x;
            _movementY = y;
            _movementZ = z;
            _movementDirection = dir;
            _resynchronizing = 0;

            base.ForcePosition(x, y, z, dir);
        }

        internal void Resynchronize()
        {
            if (_resynchronizing > 0)
            {
                if (_nextAllowedStepTime + (_resynchronizing * 1000) > Engine.Ticks)
                    return;
            }

            _resynchronizing++;
            NetClient.Socket.Send(new PResend());
            Log.Message(LogTypes.Trace, $"Resync request num: {_resynchronizing}");
        }
#elif MOVEMENT2
        public void DenyWalk(byte seq, Direction dir, ushort x, ushort y, sbyte z)
        {
            if (RequestedSteps.Count == 0)
            {
                NetClient.Socket.Send(new PResend());
                return;
            }

            Step step = RequestedSteps.RemoveFromFront();

            if (step.Rej == 0)
            {
                ResetSteps();
                ForcePosition(x, y , z ,dir);

                if (step.Seq != seq)
                {
                    NetClient.Socket.Send(new PResend());

                }
            }
            else
            {
                
            }
        }

        public void ResetSteps()
        {
            for (int i = 0; i < RequestedSteps.Count; i++)
            {
                var s = RequestedSteps[i];
                s.Rej = 1;
                RequestedSteps[i] = s;
            }

            SequenceNumber = 0;
            LastStepRequestedTime = 0;
        }
#endif
    }
}