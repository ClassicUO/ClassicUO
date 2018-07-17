using ClassicUO.Game.Map;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
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
                World.Map.Center = new Microsoft.Xna.Framework.Point((short)Position.X, (short)Position.Y);
                base.OnPositionChanged(sender, e);
            }
        }

        struct Step
        {
            public int X, Y;
            public sbyte Z;

            public byte Direction;
            public byte Anim;
            public bool Run;
            public byte Seq;
        }

        const int MAX_STEP_COUNT = 5;
        const int TURN_DELAY = 100;
        const int WALKING_DELAY = 750;

        private readonly Queue<Step> _requestedSteps = new Queue<Step>();
        private readonly Queue<Step> _steps = new Queue<Step>();
        private int _lastStepRequestedTime;
        public byte SequenceNumber { get; set; }

        public bool Walk(Direction direction, bool run)
        {
            if (_lastStepRequestedTime > 0 || _requestedSteps.Count >= MAX_STEP_COUNT)
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
                Step step1 = _requestedSteps.Dequeue();
                x = step1.X; y = step1.Y; z = step1.Z; oldDirection = (Direction)step1.Direction;
            }

            ushort walkTime;

            Direction newDirection = direction;
            int newX = x;
            int newY = y;
            sbyte newZ = z;

            if (oldDirection == newDirection)
            {
                if (newDirection != direction)
                {
                    direction = newDirection;
                    walkTime = TURN_DELAY;
                }
                else
                {
                    direction = newDirection;
                    x = newX; y = newY; z = newZ;
                    walkTime = (ushort)MovementSpeed.TimeToCompleteMovement(this, direction);
                }
            }
            else
            {
                if (oldDirection == newDirection)
                {
                    direction = newDirection;
                    x = newX; y = newY; z = newZ;
                    walkTime = (ushort)MovementSpeed.TimeToCompleteMovement(this, direction);
                }
                else
                {
                    direction = newDirection;
                    walkTime = TURN_DELAY;
                }
            }

            Step step = new Step()
            {
                X = x, Y = y, Z = z, Direction = (byte)direction, Run = run, Seq = SequenceNumber
            };

            _requestedSteps.Enqueue(step);
            new Network.PWalkRequest(direction, SequenceNumber).SendToServer();

            if (SequenceNumber == 0xFF)
                SequenceNumber = 1;
            else
                SequenceNumber++;

            _lastStepRequestedTime = 0 + walkTime;

            GetAnimationGroup();
            return true;
        }

        public void ConfirmWalk(in byte seq)
        {
            if (_requestedSteps.Count <= 0)
                return;

            Step step = _requestedSteps.Peek();
            if (step.Seq != seq)
                return;

            _requestedSteps.Dequeue();
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

                    break;
                }
            }
        }

        public void ResetSteps()
        {
            _requestedSteps.Clear();
            SequenceNumber = 0;
            _lastStepRequestedTime = 0;
        }

        private void GetEndPosition(ref int x, ref int y, ref sbyte z, ref Direction dir)
        {
            if (_requestedSteps.Count <= 0)
            {
                x = Position.X;
                y = Position.Y;
                z = Position.Z;
                dir = Direction;
            }
            else
            {
                Step step = _requestedSteps.Dequeue();
                x = step.X; y = step.Y; z = step.Z; dir = (Direction)step.Direction;
            }
        }

        public byte GetAnimationGroup(in ushort checkGraphic = 0)
        {
            Graphic graphic = checkGraphic;
            if (graphic == 0)
                graphic = GetMountAnimation();

            AssetsLoader.ANIMATION_GROUPS groupIndex = AssetsLoader.Animations.GetGroupIndex(graphic);
            byte result = AnimationGroup;

            if (result != 0xFF && Serial.IsMobile && checkGraphic > 0)
            {

            }

            bool isWalking = IsWalking;
            bool isRun = IsRunning;

            if (_steps.Count > 0)
            {
                isWalking = true;
                isRun = _steps.Peek().Run;
            }

            if ( groupIndex == AssetsLoader.ANIMATION_GROUPS.AG_LOW)
            {
                if (isWalking)
                {
                    if (isRun)
                        result = (byte)AssetsLoader.LOW_ANIMATION_GROUP.LAG_RUN;
                    else
                        result = (byte)AssetsLoader.LOW_ANIMATION_GROUP.LAG_WALK;
                }
                else if (AnimationGroup == 0xFF)
                {
                    result = (byte)AssetsLoader.LOW_ANIMATION_GROUP.LAG_STAND;
                    AnimIndex = 0;
                }
            }
            else if (groupIndex == AssetsLoader.ANIMATION_GROUPS.AG_HIGHT)
            {
                if (isWalking)
                {
                    result = (byte)AssetsLoader.HIGHT_ANIMATION_GROUP.HAG_WALK;
                    if (isRun)
                    {
                        if (AssetsLoader.Animations.AnimationExists(graphic, (byte)AssetsLoader.HIGHT_ANIMATION_GROUP.HAG_FLY))
                            result = (byte)AssetsLoader.HIGHT_ANIMATION_GROUP.HAG_FLY;
                    }
                }
                else if (AnimationGroup == 0xFF)
                {
                    result = (byte)AssetsLoader.HIGHT_ANIMATION_GROUP.HAG_STAND;
                    AnimIndex = 0;
                }

                if (graphic == 151)
                    result++;
            }
            else if (groupIndex == AssetsLoader.ANIMATION_GROUPS.AG_PEOPLE)
            {
                bool inWar = WarMode;

                if (isWalking)
                {
                    if (isRun)
                    {
                        if (GetItemAtLayer(Layer.Mount) != null)
                            result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_FAST;
                        else if (GetItemAtLayer(Layer.LeftHand) != null || GetItemAtLayer(Layer.RightHand) != null)
                            result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_RUN_ARMED;
                        else
                            result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_RUN_UNARMED;

                        //if (!IsHuman && !AssetsLoader.Animations.AnimationExists(graphic, result))
                        //    goto test_walk;

                    }
                    else
                    {

                        if (GetItemAtLayer(Layer.Mount) != null)
                            result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_SLOW;
                        else if ((GetItemAtLayer(Layer.LeftHand) != null || GetItemAtLayer(Layer.RightHand) != null) && !IsDead)
                        {
                            if (inWar)
                                result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                            else
                                result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED;
                        }
                        else if (inWar && !IsDead)
                            result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                        else
                            result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED;

                    }
                }
                else if (AnimationGroup == 0xFF)
                {
                    if (GetItemAtLayer(Layer.Mount) != null)
                        result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_STAND;
                    else if (inWar && !IsDead)
                    {
                        if (GetItemAtLayer(Layer.LeftHand) != null)
                            result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_STAND_ONEHANDED_ATTACK;
                        else if (GetItemAtLayer(Layer.RightHand) != null)
                            result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_STAND_TWOHANDED_ATTACK;
                        else
                            result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_STAND_ONEHANDED_ATTACK;
                    }
                    else
                        result = (byte)AssetsLoader.PEOPLE_ANIMATION_GROUP.PAG_STAND;

                    AnimIndex = 0;
                }

                if (Race == RaceType.GARGOYLE)
                {
                    if (IsFlying)
                    {
                        if (result == 0 || result == 1)
                            result = 62;
                        else if (result == 2 || result == 3)
                            result = 63;
                        else if (result == 4)
                            result = 64;
                        else if (result == 6)
                            result = 66;
                        else if (result == 7 || result == 8)
                            result = 65;
                        else if (result >= 9 && result <= 11)
                        {
                            result = 71;
                        }
                        else if (result >= 12 && result <= 14)
                        {
                            result = 72;
                        }
                        else if (result == 15)
                        {
                            result = 62;
                        }
                        else if (result == 20)
                        {
                            result = 77;
                        }
                        else if (result == 31)
                        {
                            result = 71;
                        }
                        else if (result == 34)
                        {
                            result = 78;
                        }
                        else if (result >= 200 && result <= 259)
                        {
                            result = 75;
                        }
                        else if (result >= 260 && result <= 270)
                        {
                            result = 75;
                        }
                    }
                }
            }

            return result;
        }

        public bool IsWalking => _lastStepRequestedTime > (0 - WALKING_DELAY);
        public byte AnimationGroup { get; set; } = 0xFF;
        public byte AnimIndex { get; set; }

        public Graphic GetMountAnimation()
        {
            ushort g = Graphic;

            switch (g)
            {
                case 0x0192:
                case 0x0193:
                    {
                        g -= 2;
                        break;
                    }
                default:
                    break;
            }

            return g;
        }
























        private Direction _nextDirection = Direction.NONE;

        public void MovementACKReceived(in byte seq)
        {
            MovementHandler.ACKReceived(seq);
        }
        public void MovementRejected(in byte seq, in Position position, in Direction direction)
        {
            Log.Message(LogTypes.Error, "Movement rejected to " + position.ToString() + " - " + direction.ToString());
            MovementHandler.RejectedMovementRequest(seq, out _, out _);
            MoveTo(position, direction);
            MovementHandler.Reset();
        }


        public void MovementStart(in Direction direction) { if (!IsMoving) _nextDirection = direction; }
        public void MovementStop() => _nextDirection = Direction.NONE;


        public void CheckIfNeedToMove()
        {
            if (_nextDirection != Direction.NONE)
            {
                Direction oldDirection = Direction;
                Direction nextDirection = _nextDirection;
                _nextDirection = Direction.NONE;

                (int tileX, int tileY) = OffsetTile(Position, nextDirection);

                bool ok = GetNextTile(this, Position, tileX, tileY, out Direction direction, out int nextTileX, out int nextTileY, out int nextZ);

                if (ok)
                {
                    Log.Message(LogTypes.Warning, "ENQUEED!");

                    if ((Direction & Direction.Up) != (direction & Direction.Up))
                    {
                        EnqueueMovement(Position, (direction & Direction.Up), true);
                    }


                    if ((nextDirection & Direction.Running) != 0)
                        direction |= Direction.Running;
                    else
                        direction &= Direction.Up;

                    if (Position.X != nextTileX || Position.Y != nextTileY || Position.Z != nextZ)
                    {
                        EnqueueMovement(new Position((ushort)nextTileX, (ushort)nextTileY, (sbyte)nextZ), direction, true);                  
                    }
                }

            }
        }


        private static bool GetNextTile(in Mobile m, in Position current, in int goalX, in int goalY, out Direction direction, out int nextX, out int nextY, out int nextZ)
        {
            direction = GetNextDirection(current, goalX, goalY);
            Direction initialDir = direction;

            (nextX, nextY) = OffsetTile(current, direction);
            bool moveIsOK = CheckMovement(m, current, direction, out nextZ);

            if (!moveIsOK)
            {
                direction = (Direction)(((int)direction - 1) & 0x87);
                (nextX, nextY) = OffsetTile(current, direction);
                moveIsOK = CheckMovement(m, current, direction, out nextZ);
            }

            if (!moveIsOK)
            {
                direction = (Direction)(((int)direction + 2) & 0x87);
                (nextX, nextY) = OffsetTile(current, direction);
                moveIsOK = CheckMovement(m, current, direction, out nextZ);
            }

            if (moveIsOK)
            {
                if (m.IsRunning)
                    direction |= Direction.Running;
                return true;
            }
            return false;
        }

        private static Direction GetNextDirection(in Position current, in int goalX, in int goalY)
        {
            Direction direction;

            if (goalX < current.X)
            {
                if (goalY < current.Y)
                    direction = Direction.Up;
                else if (goalY > current.Y)
                    direction = Direction.Left;
                else
                    direction = Direction.West;
            }
            else if (goalX > current.X)
            {
                if (goalY < current.Y)
                    direction = Direction.Right;
                else if (goalY > current.Y)
                    direction = Direction.Down;
                else
                    direction = Direction.East;
            }
            else
            {
                if (goalY < current.Y)
                    direction = Direction.North;
                else if (goalY > current.Y)
                    direction = Direction.South;
                else
                    throw new Exception("Wrong direction");
            }

            return direction;
        }


        private static (int, int) OffsetTile(in Position position, Direction direction)
        {
            int nextX = position.X; int nextY = position.Y;

            switch (direction & Direction.Up)
            {
                case Direction.North:
                    nextY--;
                    break;
                case Direction.South:
                    nextY++;
                    break;
                case Direction.West:
                    nextX--;
                    break;
                case Direction.East:
                    nextX++;
                    break;
                case Direction.Right:
                    nextX++; nextY--;
                    break;
                case Direction.Left:
                    nextX--; nextY++;
                    break;
                case Direction.Down:
                    nextX++; nextY++;
                    break;
                case Direction.Up:
                    nextX--; nextY--;
                    break;
            }

            return (nextX, nextY);
        }

        private static List<Item>[] _poolsItems = { new List<Item>(), new List<Item>(), new List<Item>(), new List<Item>() };
        private static List<Static>[] _poolsStatics = { new List<Static>(), new List<Static>(), new List<Static>(), new List<Static>() };

        private static readonly List<Tile> _tiles = new List<Tile>();

        const long IMPASSABLE_SURFACE = 0x00000040 | 0x00000200;
        const int PERSON_HEIGHT = 16;
        const int STEP_HEIGHT = 2;



        private static int GetNextZ(in Mobile mobile, in Position loc, in Direction d)
        {
            if (CheckMovement(mobile, loc, d, out int newZ, true))
                return newZ;
            return loc.Z;
        }

        // servuo
        private static bool CheckMovement(in Mobile mobile, in Position loc, in Direction d, out int newZ, bool forceOK = false)
        {
            Facet map = World.Map;

            if (map == null)
            {
                newZ = 0;
                return true;
            }


            int xStart = loc.X;
            int yStart = loc.Y;
            int xForward = xStart, yForward = yStart;
            int xRight = xStart, yRight = yStart;
            int xLeft = xStart, yLeft = yStart;

            bool checkDiagonals = ((int)d & 0x1) == 0x1;

            OffsetXY(d, ref xForward, ref yForward);
            OffsetXY((Direction)(((int)d - 1) & 0x7), ref xLeft, ref yLeft);
            OffsetXY((Direction)(((int)d + 1) & 0x7), ref xRight, ref yRight);

            if (xForward < 0 || yForward < 0 || xForward >= AssetsLoader.Map.MapsDefaultSize[map.Index][0] || yForward >= AssetsLoader.Map.MapsDefaultSize[map.Index][1])
            {
                newZ = 0;
                return false;
            }


            List<Item> itemsStart = _poolsItems[0];
            List<Item> itemsForward = _poolsItems[1];
            List<Item> itemsLeft = _poolsItems[2];
            List<Item> itemsRight = _poolsItems[3];

            List<Static> staticStart = _poolsStatics[0];
            List<Static> staticForward = _poolsStatics[1];
            List<Static> staticLeft = _poolsStatics[2];
            List<Static> staticRight = _poolsStatics[3];

            long reqFlags = IMPASSABLE_SURFACE;

            if (checkDiagonals)
            {
                Tile tileStart = map.GetTile(xStart, yStart);
                Tile tileForward = map.GetTile(xForward, yForward);
                Tile tileLeft = map.GetTile(xLeft, yLeft);
                Tile tileRight = map.GetTile(xRight, yRight);

                if ((tileForward == null) || (tileStart == null) || (tileLeft == null) || (tileRight == null))
                {
                    newZ = loc.Z;
                    return false;
                }

                List<Tile> tiles = _tiles;
                tiles.Add(tileStart);
                tiles.Add(tileForward);
                tiles.Add(tileLeft);
                tiles.Add(tileRight);

                for (int i = 0; i < tiles.Count; ++i)
                {
                    Tile tile = tiles[i];

                    for (int j = 0; j < tile.ObjectsOnTiles.Count; ++j)
                    {
                        WorldObject entity = tile.ObjectsOnTiles[j];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is Item item)
                        {
                            if (((long)item.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (tile == tileStart && item.Position.X == xStart && item.Position.Y == yStart && item.Graphic < 0x4000)
                                itemsStart.Add(item);
                            else if (tile == tileForward && item.Position.X == xForward && item.Position.Y == yForward && item.Graphic < 0x4000)
                                itemsForward.Add(item);
                            else if (tile == tileLeft && item.Position.X == xLeft && item.Position.Y == yLeft && item.Graphic < 0x4000)
                                itemsLeft.Add(item);
                            else if (tile == tileRight && item.Position.X == xRight && item.Position.Y == yRight && item.Graphic < 0x4000)
                                itemsRight.Add(item);
                        }
                        else if (entity is Static stat)
                        {                          
                            if (((long)stat.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (tile == tileStart && stat.Position.X == xStart && stat.Position.Y == yStart && stat.TileID < 0x4000)
                                staticStart.Add(stat);
                            else if (tile == tileForward && stat.Position.X == xForward && stat.Position.Y == yForward && stat.TileID < 0x4000)
                                staticForward.Add(stat);
                            else if (tile == tileLeft && stat.Position.X == xLeft && stat.Position.Y == yLeft && stat.TileID < 0x4000)
                                staticLeft.Add(stat);
                            else if (tile == tileRight && stat.Position.X == xRight && stat.Position.Y == yRight && stat.TileID < 0x4000)
                                staticRight.Add(stat);
                        }
                    }


                }
            }
            else
            {
                Tile tileStart = map.GetTile(xStart, yStart);
                Tile tileForward = map.GetTile(xForward, yForward);
                if ((tileForward == null) || (tileStart == null))
                {
                    newZ = loc.Z;
                    return false;
                }

                if (tileStart == tileForward)
                {
                    for(int i = 0; i < tileStart.ObjectsOnTiles.Count; i++)
                    {
                        WorldObject entity = tileStart.ObjectsOnTiles[i];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is Item item)
                        {
                            if (((long)item.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (item.Position.X == xStart && item.Position.Y == yStart && item.Graphic < 0x4000)
                                itemsStart.Add(item);
                            else if (item.Position.X == xForward && item.Position.Y == yForward && item.Graphic < 0x4000)
                                itemsForward.Add(item);
                        }
                        else if (entity is Static stat)
                        {
                            if (((long)stat.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (stat.Position.X == xStart && stat.Position.Y == yStart && stat.TileID < 0x4000)
                                staticStart.Add(stat);
                            else if (stat.Position.X == xForward && stat.Position.Y == yForward && stat.TileID < 0x4000)
                                staticForward.Add(stat);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < tileForward.ObjectsOnTiles.Count; i++)
                    {
                        WorldObject entity = tileForward.ObjectsOnTiles[i];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is Item item)
                        {
                            if (((long)item.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (item.Position.X == xForward && item.Position.Y == yForward && item.Graphic < 0x4000)
                                itemsForward.Add(item);
                        }
                        else if (entity is Static stat)
                        {
                            if (((long)stat.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (stat.Position.X == xForward && stat.Position.Y == yForward && stat.TileID < 0x4000)
                                staticForward.Add(stat);
                        }
                    }

                    for (int i = 0; i < tileStart.ObjectsOnTiles.Count; i++)
                    {
                        WorldObject entity = tileStart.ObjectsOnTiles[i];

                        // if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                        //     continue;

                        if (entity is Item item)
                        {
                            if (((long)item.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (item.Position.X == xStart && item.Position.Y == yStart && item.Graphic < 0x4000)
                                itemsStart.Add(item);
                        }
                        else if (entity is Static stat)
                        {
                            if (((long)stat.ItemData.Flags & reqFlags) == 0)
                                continue;

                            if (stat.Position.X == xStart && stat.Position.Y == yStart && stat.TileID < 0x4000)
                                staticStart.Add(stat);
                        }
                    }
                }
            }

            GetStartZ(mobile, loc, itemsStart, staticStart, out int startZ, out int startTop);

            bool moveIsOk = Check(mobile, itemsForward, staticForward, xForward, yForward, startTop, startZ, out newZ) ||
                            forceOK;

            if (moveIsOk && checkDiagonals)
            {
                if (!Check(mobile, itemsLeft, staticLeft, xLeft, yLeft, startTop, startZ, out int hold) && !Check(mobile, itemsRight, staticRight, xRight, yRight, startTop, startZ, out hold))
                    moveIsOk = false;
            }

            for (int i = 0; i < (checkDiagonals ? 4 : 2); i++)
            {
                if (_poolsItems[i].Count > 0)
                    _poolsItems[i].Clear();
                if (_poolsStatics[i].Count > 0)
                    _poolsStatics[i].Clear();
            }

            if (!moveIsOk)
                newZ = startZ;

            return moveIsOk;
        }

        private static void OffsetXY(in Direction d, ref int x, ref int y)
        {
            switch (d & Direction.Up)
            {
                case Direction.North:
                    --y;
                    break;
                case Direction.South:
                    ++y;
                    break;
                case Direction.West:
                    --x;
                    break;
                case Direction.East:
                    ++x;
                    break;
                case Direction.Right:
                    ++x;
                    --y;
                    break;
                case Direction.Left:
                    --x;
                    ++y;
                    break;
                case Direction.Down:
                    ++x;
                    ++y;
                    break;
                case Direction.Up:
                    --x;
                    --y;
                    break;
            }
        }

        private static void GetStartZ(in WorldObject e, in Position loc, in List<Item> itemList, in List<Static> staticList, out int zLow, out int zTop)
        {
            int xCheck = loc.X, yCheck = loc.Y;

            Tile mapTile = World.Map.GetTile(xCheck, yCheck);
            if (mapTile == null)
            {
                zLow = int.MinValue;
                zTop = int.MinValue;
            }

            bool landBlocks = AssetsLoader.TileData.IsImpassable((long)AssetsLoader.TileData.LandData[mapTile.TileID].Flags);

            int landLow = 0, landTop = 0;
            int landCenter = World.Map.GetAverageZ((short)xCheck, (short)yCheck, ref landLow, ref landTop);

            bool considerLand = !mapTile.IsIgnored;

            int zCenter = zLow = zTop = 0;
            bool isSet = false;

            if (considerLand && !landBlocks && loc.Z >= landCenter)
            {
                zLow = landLow;
                zCenter = landCenter;

                if (!isSet || landTop > zTop)
                    zTop = landTop;

                isSet = true;
            }

            Static[] staticTiles = mapTile.GetWorldObjects<Static>();

            for (int i = 0; i < staticTiles.Length; i++)
            {
                Static tile = staticTiles[i];
                var id = tile.ItemData;

                int calcTop = tile.Position.Z + ((id.Flags & 0x00000400) != 0 ? id.Height / 2 : id.Height);

                if ((!isSet || calcTop >= zCenter) && ((id.Flags & 0x00000200) != 0) && loc.Z >= calcTop)
                {
                    //  || (m.CanSwim && (id.Flags & TileFlag.Wet) != 0)
                    // if (m.CantWalk && (id.Flags & TileFlag.Wet) == 0)
                    //     continue;

                    zLow = tile.Position.Z;
                    zCenter = calcTop;

                    int top = tile.Position.Z + id.Height;

                    if (!isSet || top > zTop)
                        zTop = top;

                    isSet = true;
                }
            }

            for (int i = 0; i < itemList.Count; i++)
            {
                Item item = itemList[i];
                var id = item.ItemData;

                int calcTop = item.Position.Z + ((id.Flags & 0x00000400) != 0 ? id.Height / 2 : id.Height);

                if ((!isSet || calcTop >= zCenter) && ((id.Flags & 0x00000200) != 0) && loc.Z >= calcTop)
                {
                    //  || (m.CanSwim && (id.Flags & TileFlag.Wet) != 0)
                    // if (m.CantWalk && (id.Flags & TileFlag.Wet) == 0)
                    //     continue;

                    zLow = item.Position.Z;
                    zCenter = calcTop;

                    int top = item.Position.Z + id.Height;

                    if (!isSet || top > zTop)
                        zTop = top;

                    isSet = true;
                }
            }

            for (int i = 0; i < staticList.Count; i++)
            {
                Static item = staticList[i];
                var id = item.ItemData;

                int calcTop = item.Position.Z + ((id.Flags & 0x00000400) != 0 ? id.Height / 2 : id.Height);

                if ((!isSet || calcTop >= zCenter) && ((id.Flags & 0x00000200) != 0) && loc.Z >= calcTop)
                {
                    //  || (m.CanSwim && (id.Flags & TileFlag.Wet) != 0)
                    // if (m.CantWalk && (id.Flags & TileFlag.Wet) == 0)
                    //     continue;

                    zLow = item.Position.Z;
                    zCenter = calcTop;

                    int top = item.Position.Z + id.Height;

                    if (!isSet || top > zTop)
                        zTop = top;

                    isSet = true;
                }
            }

            if (!isSet)
                zLow = zTop = loc.Z;
            else if (loc.Z > zTop)
                zTop = loc.Z;
        }

        private static bool IsOK(in bool ignoreDoors, in int ourZ, in int ourTop, in Static[] tiles, in List<Item> items, in List<Static> statics)
        {
            for (int i = 0; i < tiles.Length; ++i)
            {
                Static item = tiles[i];
                if ((item.ItemData.Flags & IMPASSABLE_SURFACE) != 0) // Impassable || Surface
                {
                    int checkZ = item.Position.Z;
                    int checkTop = checkZ + ((item.ItemData.Flags & 0x00000400) != 0 ? item.ItemData.Height / 2 : item.ItemData.Height);

                    if (checkTop > ourZ && ourTop > checkZ)
                        return false;
                }
            }

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = items[i];
                int itemID = item.Graphic & AssetsLoader.FileManager.GraphicMask;
                var itemData = AssetsLoader.TileData.StaticData[itemID];
                ulong flags = itemData.Flags;

                if ((flags & IMPASSABLE_SURFACE) != 0) // Impassable || Surface
                {
                    if (ignoreDoors && (AssetsLoader.TileData.IsDoor((long)flags) || itemID == 0x692 || itemID == 0x846 || itemID == 0x873 || (itemID >= 0x6F5 && itemID <= 0x6F6)))
                        continue;

                    int checkZ = item.Position.Z;
                    int checkTop = checkZ + ((item.ItemData.Flags & 0x00000400) != 0 ? item.ItemData.Height / 2 : item.ItemData.Height);

                    if (checkTop > ourZ && ourTop > checkZ)
                        return false;
                }
            }

            for (int i = 0; i < statics.Count; ++i)
            {
                Static item = statics[i];
                int itemID = item.TileID & AssetsLoader.FileManager.GraphicMask;
                var itemData = AssetsLoader.TileData.StaticData[itemID];
                ulong flags = itemData.Flags;

                if ((flags & IMPASSABLE_SURFACE) != 0) // Impassable || Surface
                {
                    if (ignoreDoors && (AssetsLoader.TileData.IsDoor((long)flags) || itemID == 0x692 || itemID == 0x846 || itemID == 0x873 || (itemID >= 0x6F5 && itemID <= 0x6F6)))
                        continue;

                    int checkZ = item.Position.Z;
                    int checkTop = checkZ + ((item.ItemData.Flags & 0x00000400) != 0 ? item.ItemData.Height / 2 : item.ItemData.Height);

                    if (checkTop > ourZ && ourTop > checkZ)
                        return false;
                }
            }

            return true;
        }

        private static bool Check(in Mobile m, in List<Item> items, in List<Static> statics, in int x, int y, in int startTop, in int startZ, out int newZ)
        {
            newZ = 0;

            Tile mapTile = World.Map.GetTile(x, y);
            if (mapTile == null)
                return false;

            var id = AssetsLoader.TileData.LandData[mapTile.TileID];

            Static[] tiles = mapTile.GetWorldObjects<Static>();
            bool landBlocks = (id.Flags & 0x00000040) != 0;
            bool considerLand = !mapTile.IsIgnored;

            int landLow = 0, landCenter = 0, landTop = 0;
            landCenter = World.Map.GetAverageZ((short)x, (short)y, ref landLow, ref landTop);

            bool moveIsOk = false;

            int stepTop = startTop + STEP_HEIGHT;
            int checkTop = startZ + PERSON_HEIGHT;

            bool ignoreDoors = (m.IsDead || m.Graphic == 0x3DB);


            for (int i = 0; i < tiles.Length; ++i)
            {
                Static tile = tiles[i];

                if ((tile.ItemData.Flags & IMPASSABLE_SURFACE) == 0x00000200) //  || (canSwim && (flags & TileFlag.Wet) != 0) Surface && !Impassable
                {
                    // if (cantWalk && (flags & TileFlag.Wet) == 0)
                    //     continue;

                    int itemZ = tile.Position.Z;
                    int itemTop = itemZ;
                    int ourZ = itemZ + ((tile.ItemData.Flags & 0x00000400) != 0 ? tile.ItemData.Height / 2 : tile.ItemData.Height);
                    int ourTop = ourZ + PERSON_HEIGHT;
                    int testTop = checkTop;

                    if (moveIsOk)
                    {
                        int cmp = Math.Abs(ourZ - m.Position.Z) - Math.Abs(newZ - m.Position.Z);

                        if (cmp > 0 || (cmp == 0 && ourZ > newZ))
                            continue;
                    }

                    if (ourZ + PERSON_HEIGHT > testTop)
                        testTop = ourZ + PERSON_HEIGHT;

                    if ((tile.ItemData.Flags & 0x00000400) == 0)
                        itemTop += tile.ItemData.Height;

                    if (stepTop >= itemTop)
                    {
                        int landCheck = itemZ;

                        if (tile.ItemData.Height >= STEP_HEIGHT)
                            landCheck += STEP_HEIGHT;
                        else
                            landCheck += tile.ItemData.Height;

                        if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landLow)
                            continue;

                        if (IsOK(ignoreDoors, ourZ, testTop, tiles, items, statics))
                        {
                            newZ = ourZ;
                            moveIsOk = true;
                        }
                    }
                }
            }

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = items[i];
                var itemData = item.ItemData;
                ulong flags = itemData.Flags;

                if ((flags & IMPASSABLE_SURFACE) == 0x00000200) // Surface && !Impassable && !Movable
                {
                    //  || (m.CanSwim && (flags & TileFlag.Wet) != 0))
                    // !item.Movable && 
                    // if (cantWalk && (flags & TileFlag.Wet) == 0)
                    //     continue;

                    int itemZ = item.Position.Z;
                    int itemTop = itemZ;
                    int ourZ = itemZ + ((item.ItemData.Flags & 0x00000400) != 0 ? item.ItemData.Height / 2 : item.ItemData.Height);
                    int ourTop = ourZ + PERSON_HEIGHT;
                    int testTop = checkTop;

                    if (moveIsOk)
                    {
                        int cmp = Math.Abs(ourZ - m.Position.Z) - Math.Abs(newZ - m.Position.Z);

                        if (cmp > 0 || (cmp == 0 && ourZ > newZ))
                            continue;
                    }

                    if (ourZ + PERSON_HEIGHT > testTop)
                        testTop = ourZ + PERSON_HEIGHT;

                    if ((itemData.Flags & 0x00000400) == 0)
                        itemTop += itemData.Height;

                    if (stepTop >= itemTop)
                    {
                        int landCheck = itemZ;

                        if (itemData.Height >= STEP_HEIGHT)
                            landCheck += STEP_HEIGHT;
                        else
                            landCheck += itemData.Height;

                        if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landLow)
                            continue;

                        if (IsOK(ignoreDoors, ourZ, testTop, tiles, items, statics))
                        {
                            newZ = ourZ;
                            moveIsOk = true;
                        }
                    }
                }
            }


            if (considerLand && !landBlocks && (stepTop) >= landLow)
            {
                int ourZ = landCenter;
                int ourTop = ourZ + PERSON_HEIGHT;
                int testTop = checkTop;

                if (ourZ + PERSON_HEIGHT > testTop)
                    testTop = ourZ + PERSON_HEIGHT;

                bool shouldCheck = true;

                if (moveIsOk)
                {
                    int cmp = Math.Abs(ourZ - m.Position.Z) - Math.Abs(newZ - m.Position.Z);

                    if (cmp > 0 || (cmp == 0 && ourZ > newZ))
                        shouldCheck = false;
                }

                if (shouldCheck && IsOK(ignoreDoors, ourZ, testTop, tiles, items, statics))
                {
                    newZ = ourZ;
                    moveIsOk = true;
                }
            }


            return moveIsOk;
        }
    }
}
