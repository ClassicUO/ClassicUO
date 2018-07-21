using ClassicUO.Game.Map;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassicUO.Game.WorldObjects
{
    public enum Ability : ushort
    {
        None = 0,
        ArmorIgnore = 1,
        BleedAttack = 2,
        ConcussionBlow = 3,
        CrushingBlow = 4,
        Disarm = 5,
        Dismount = 6,
        DoubleStrike = 7,
        InfectiousStrike = 8,
        MortalStrike = 9,
        MovingShot = 10,
        ParalyzingBlow = 11,
        ShadowStrike = 12,
        WhirlwindAttack = 13,
        RidingSwipe = 14,
        FrenziedWhirlwind = 15,
        Block = 16,
        DefenseMastery = 17,
        NerveStrike = 18,
        TalonStrike = 19,
        Feint = 20,
        DualWield = 21,
        DoubleShot = 22,
        ArmorPierce = 23,
        Bladeweave = 24,
        ForceArrow = 25,
        LightningArrow = 26,
        PsychicAttack = 27,
        SerpentArrow = 28,
        ForceOfNature = 29,
        InfusedThrow = 30,
        MysticArc = 31,

        Invalid
    }

    public class PlayerMobile : Mobile
    {
        private ushort _strength;
        private ushort _intelligence;
        private ushort _dexterity;
        private ushort _weight;
        private ushort _weightMax;
        private uint _gold;
        private ushort _resistPhysical;
        private ushort _resistFire;
        private ushort _resistCold;
        private ushort _resistPoison;
        private ushort _resistEnergy;
        private byte _followers;
        private byte _followersMax;
        private ushort _luck;
        private uint _tithingPoints;
        private ushort _damageMin;
        private ushort _damageMax;
        private bool _female;
        private readonly Ability[] _ability = new Ability[2] { Ability.None, Ability.None };
        private List<Skill> _sklls;


        public event EventHandler StatsChanged, SkillsChanged;


        public PlayerMobile(Serial serial) : base(serial)
        {
            _sklls = new List<Skill>();
        }



        public IReadOnlyList<Skill> Skills => _sklls;

        public override bool InWarMode { get; set; }

        public ushort Strength
        {
            get { return _strength; }
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
            get { return _intelligence; }
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
            get { return _dexterity; }
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
            get { return _weight; }
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
            get { return _weightMax; }
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
            get { return _gold; }
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
            get { return _resistPhysical; }
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
            get { return _resistFire; }
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
            get { return _resistCold; }
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
            get { return _resistPoison; }
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
            get { return _resistEnergy; }
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
            get { return _followers; }
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
            get { return _followersMax; }
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
            get { return _luck; }
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
            get { return _tithingPoints; }
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
            get { return _damageMin; }
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
            get { return _damageMax; }
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
            get { return _female; }
            set
            {
                if (_female != value)
                {
                    _female = value;
                    _delta |= Delta.Stats;
                }
            }
        }

        public Ability PrimaryAbility { get => _ability[0]; set => _ability[0] = value; }
        public Ability SecondaryAbility { get => _ability[1]; set => _ability[1] = value; }

        public void UpdateSkill(int id, ushort realValue, ushort baseValue, SkillLock skillLock, ushort cap)
        {
            if (id < Skills.Count)
            {
                Skill skill = Skills[id];
                skill.ValueFixed = realValue;
                skill.BaseFixed = baseValue;
                skill.Lock = skillLock;
                skill.CapFixed = cap;
                _delta |= Delta.Skills;
            }
        }

        public void UpdateSkillLock(int id, SkillLock skillLock)
        {
            if (id < Skills.Count)
            {
                Skill skill = Skills[id];
                skill.Lock = skillLock;
                _delta |= Delta.Skills;
            }
        }

        public void UpdateAbilities()
        {
            Item right = GetItemAtLayer(Layer.RightHand);
            Item left = GetItemAtLayer(Layer.LeftHand);

            _ability[0] = Ability.None;
            _ability[1] = Ability.None;

            if (right == null && left == null)
                return;

            Graphic[] graphics = { 0x00, 0x00 };

            if (right == null && left != null)
                graphics[0] = left.Graphic;
            else if (right != null && left == null)
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
                    case 0x0DF5: // Wands Type A-D
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
                    case 0x406D:// Dual Pointed Spear
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
                    default:
                        break;
                }    
            }

            done:
            ;
        }



        protected override void OnProcessDelta(Delta d)
        {
            base.OnProcessDelta(d);
            if (d.HasFlag(Delta.Stats))
                StatsChanged.Raise(this);

            if (d.HasFlag(Delta.Skills))
                SkillsChanged.Raise(this);
        }

        protected override void OnPositionChanged(object sender, EventArgs e)
        {
            if (World.Map != null)
            {
                if (World.Map.Center == Point.Zero)
                World.Map.Center = new Point((short)Position.X, (short)Position.Y);
                base.OnPositionChanged(sender, e);
            }
        }


        protected override bool NoIterateAnimIndex()
        {
            return false;
        }

        public override bool IsWalking => LastStepTime > (World.Ticks - WALKING_DELAY);

        private readonly Deque<Step> _requestedSteps = new Deque<Step>();
        private long _lastStepRequestedTime;
        public byte SequenceNumber { get; set; }

        public bool Walk(Direction direction, bool run)
        {
            if (_lastStepRequestedTime > World.Ticks || _requestedSteps.Count >= MAX_STEP_COUNT)
                return false;

            int x = 0, y = 0;
            sbyte z = 0;
            Direction oldDirection = Direction.NONE;

            if (_requestedSteps.Count <= 0)
            {
                GetEndPosition(ref x, ref y, ref z, ref oldDirection);
            }
            else
            {
                Step step1 = _requestedSteps.Back();
                x = step1.X; y = step1.Y; z = step1.Z; oldDirection = (Direction)step1.Direction;
            }

            oldDirection = (oldDirection & Direction.Up);
            direction = (direction & Direction.Up);

            ushort walkTime;
            Direction newDirection = direction;
            int newX = x;
            int newY = y;
            sbyte newZ = z;

            if (oldDirection == newDirection)
            {

                if (!Pathfinder.CanWalk(this, ref newX, ref newY, ref newZ, ref newDirection))
                {
                    return false;
                }

                if (newDirection != direction)
                {
                    direction = newDirection;
                    walkTime = TURN_DELAY;
                }
                else
                {
                    direction = newDirection;
                    x = newX; y = newY; z = newZ;
                    walkTime = (ushort)MovementSpeed.TimeToCompleteMovement(this, run);
                }
            }
            else
            {
                if (oldDirection == newDirection)
                {
                    direction = newDirection;
                    x = newX; y = newY; z = newZ;
                    walkTime = (ushort)MovementSpeed.TimeToCompleteMovement(this, run);
                }
                else
                {
                    direction = newDirection;
                    walkTime = TURN_DELAY;
                }
            }

            if (run)
                direction |= Direction.Running;

            Step step = new Step()
            {
                X = x,
                Y = y,
                Z = z,
                Direction = (byte)direction,
                Run = run,
                Seq = SequenceNumber
            };


            if (_movementState == PlayerMovementState.ANIMATE_IMMEDIATELY)
            {
                for (int i = 0; i < _requestedSteps.Count; i++)
                {
                    var s = _requestedSteps[i];
                    if (!s.Anim)
                    {
                        s.Anim = true;
                        _requestedSteps[i] = s;
                        EnqueueStep(new Position((ushort)s.X, (ushort)s.Y, s.Z), (Direction)s.Direction, s.Run);

                    }
                }

                step.Anim = true;
                EnqueueStep(new Position((ushort)step.X, (ushort)step.Y, step.Z), (Direction)step.Direction, step.Run);
            }


            _requestedSteps.AddToBack(step);
            new Network.PWalkRequest(direction, SequenceNumber).SendToServer();

            if (SequenceNumber == 0xFF)
                SequenceNumber = 1;
            else
                SequenceNumber++;

            _lastStepRequestedTime = World.Ticks + walkTime;

            GetAnimationGroup();
            return true;
        }

        public void ConfirmWalk(in byte seq)
        {
            if (_requestedSteps.Count <= 0)
                return;

            Step step = _requestedSteps.Front();
            if (step.Seq != seq)
                return;

            _requestedSteps.RemoveFromFront();

            if (!step.Anim)
            {
                int endX = 0, endY = 0;
                sbyte endZ = 0;
                Direction endDir = Direction.NONE;

                GetEndPosition(ref endX, ref endY, ref endZ, ref endDir);

                if (step.Direction == (byte)endDir)
                {
                    if (_movementState == PlayerMovementState.ANIMATE_ON_CONFIRM)
                        _movementState = PlayerMovementState.ANIMATE_ON_CONFIRM;
                }

                EnqueueStep(new Position((ushort)step.X, (ushort)step.Y, step.Z), (Direction)step.Direction, step.Run);
            }
        }

        public void DenyWalk(in byte seq, in Direction dir, in Position position)
        {
            foreach (Step step in _requestedSteps)
            {
                if (step.Seq == seq)
                {
                    ResetSteps();
                    Position = position;
                    Direction = dir;

                    ProcessDelta();

                    break;
                }
            }
        }

        public void ResetSteps()
        {
            _requestedSteps.Clear();
            SequenceNumber = 0;
            _lastStepRequestedTime = 0;

            Offset = Vector3.Zero;
        }

        public void ResetRequestedSteps() => _requestedSteps.Clear();




        private PlayerMovementState _movementState = PlayerMovementState.ANIMATE_IMMEDIATELY;

        enum  PlayerMovementState
        {
            ANIMATE_IMMEDIATELY = 0,
            ANIMATE_ON_CONFIRM,
        }

    }
}
