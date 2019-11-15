#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using System.Linq;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal class PlayerMobile : Mobile
    {
        private readonly Dictionary<Graphic, BuffIcon> _buffIcons = new Dictionary<Graphic, BuffIcon>();
        public BandageGump BandageTimer;
        public PlayerMobile(Serial serial) : base(serial)
        {
            Skills = new Skill[FileManager.Skills.SkillsCount];

            Engine.UI.Add(BandageTimer = new BandageGump());

            for (int i = 0; i < Skills.Length; i++)
            {
                SkillEntry skill = FileManager.Skills.GetSkill(i);
                Skills[i] = new Skill(skill.Name, skill.Index, skill.HasAction);
            }
        }

        public Skill[] Skills { get; }

        public override bool InWarMode { get; set; }

#if MOVEMENT2
        public Deque<Step> RequestedSteps { get; } = new Deque<Step>();
#endif

        public IReadOnlyDictionary<Graphic, BuffIcon> BuffIcons => _buffIcons;

        public ushort Strength;

        public ushort Intelligence;

        public ushort Dexterity;

        public ushort Weight;

        public ushort WeightMax;

        public uint Gold;

        public ushort PhysicalResistence;

        public ushort FireResistance;

        public ushort ColdResistance;

        public ushort PoisonResistance;

        public ushort EnergyResistance;

        public byte Followers;

        public byte FollowersMax;

        public ushort Luck;

        public uint TithingPoints;

        public ushort DamageMin;

        public ushort DamageMax;

        public ushort StatsCap;

        public ushort HitChanceIncrease;

        public ushort SwingSpeedIncrease;

        public ushort DamageIncrease;

        public ushort LowerReagentCost;

        public ushort HitPointsRegeneration;

        public ushort StaminaRegeneration;

        public ushort ManaRegeneration;

        public ushort MaxPhysicResistence;

        public ushort MaxFireResistence;

        public ushort MaxColdResistence;

        public ushort MaxPoisonResistence;

        public ushort MaxEnergyResistence;

        public ushort MaxDefenseChanceIncrease;

        public ushort ReflectPhysicalDamage;

        public ushort EnhancePotions;

        public ushort DefenseChanceIncrease;
        public ushort SpellDamageIncrease;

        public ushort FasterCastRecovery;

        public ushort FasterCasting;

        public ushort LowerManaCost;

        public ushort StrengthIncrease;

        public ushort DexterityIncrease;

        public ushort IntelligenceIncrease;

        public ushort HitPointsIncrease;

        public ushort StaminaIncrease;

        public ushort ManaIncrease;

        public ushort MaxHitPointsIncrease;

        public ushort MaxStaminaIncrease;

        public ushort MaxManaIncrease;

        public Ability PrimaryAbility
        {
            get => Abilities[0];
            set => Abilities[0] = value;
        }

        public Ability SecondaryAbility
        {
            get => Abilities[1];
            set => Abilities[1] = value;
        }

        public Ability[] Abilities = new Ability[2]
        {
            Ability.Invalid, Ability.Invalid
        };

        public Lock StrLock;

        public Lock DexLock;

        public Lock IntLock;

        protected override bool IsWalking => LastStepTime > Time.Ticks - Constants.PLAYER_WALKING_DELAY;

        public Item FindBandage()
        {
            Item backpack = Equipment[(int) Layer.Backpack];
            Item item = null;

            if (backpack != null)
                item = backpack.FindItem(0x0E21);

            return item;
        }

        public Item FindItemByGraphic(ushort graphic)
        {
            Item backpack = Equipment[(int)Layer.Backpack];

            if (backpack != null)
            {
                return FindItemInContainerRecursive(backpack, graphic);
            }

            return null;
        }

        private Item FindItemInContainerRecursive(Item container, ushort graphic)
        {
            Item found = null;
            if (container != null)
            {
                foreach (var item in container.Items)
                {
                    if (item.Graphic == graphic)
                        return item;

                    if (item.Items.Count != 0)
                    {
                        found = FindItemInContainerRecursive(item, graphic);

                        if (found != null && found.Graphic == graphic)
                            return found;
                    }
                }
            }

            return found;
        }

        public void AddBuff(Graphic graphic, uint time, string text)
        {
            _buffIcons[graphic] = new BuffIcon(graphic, time, text);
        }


        public bool IsBuffIconExists(Graphic graphic)
        {
            return _buffIcons.ContainsKey(graphic);
        }

        public void RemoveBuff(Graphic graphic)
        {
            _buffIcons.Remove(graphic);
        }

        public void UpdateSkill(int id, ushort realValue, ushort baseValue, Lock @lock, ushort cap, bool displayMessage = false)
        {
            if (id < Skills.Length)
            {
                Skill skill = Skills[id];

                if (displayMessage && skill.ValueFixed != realValue)
                {
                    var delta = realValue - skill.ValueFixed;
                    var direction = delta < 0 ? "decreased" : "increased";

                    GameActions.Print($"Your skill in {skill.Name} has {direction} by {delta / 10.0:#0.0}%.  It is now {realValue / 10.0:#0.0}%.", 0x58, MessageType.System, 3, false);
                }

                skill.ValueFixed = realValue;
                skill.BaseFixed = baseValue;
                skill.Lock = @lock;
                skill.CapFixed = cap;

                // check needed to avoid crash when you create a char
                if (ProfileManager.Current != null)
                {
                    if (ProfileManager.Current.StandardSkillsGump)
                        UIManager.GetGump<StandardSkillsGump>()?.ForceUpdate(id);
                    else
                        UIManager.GetGump<SkillGumpAdvanced>()?.ForceUpdate();
                }
            }
        }

        public void UpdateAbilities()
        {
            ushort equippedGraphic = 0;

            Item layerObject = Equipment[(int) Layer.OneHanded];

            if (layerObject != null)
                equippedGraphic = layerObject.Graphic;
            else
            {
                layerObject = Equipment[(int) Layer.TwoHanded];

                if (layerObject != null)
                    equippedGraphic = layerObject.Graphic;
            }

            Abilities[0] = Ability.Invalid;
            Abilities[1] = Ability.Invalid;

            if (equippedGraphic != 0)
            {
                ushort[] graphics = {equippedGraphic, 0};

                if (layerObject != null)
                {
                    ushort imageID = layerObject.ItemData.AnimID;

                    int count = 1;

                    ushort testGraphic = (ushort) (equippedGraphic - 1);

                    if (FileManager.TileData.StaticData[testGraphic].AnimID == imageID)
                    {
                        graphics[1] = testGraphic;
                        count = 2;
                    }
                    else
                    {
                        testGraphic = (ushort) (equippedGraphic + 1);

                        if (FileManager.TileData.StaticData[testGraphic].AnimID == imageID)
                        {
                            graphics[1] = testGraphic;
                            count = 2;
                        }
                    }

                    for (int i = 0; i < count; i++)
                    {
                        Graphic g = graphics[i];

                        switch (g)
                        {
                            case 0x0901: // Gargish Cyclone
                                Abilities[0] = Ability.MovingShot;
                                Abilities[1] = Ability.InfusedThrow;

                                goto done;

                            case 0x0902: // Gargish Dagger
                                Abilities[0] = Ability.InfectiousStrike;
                                Abilities[1] = Ability.ShadowStrike;

                                goto done;

                            case 0x0905: // Glass Staff
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x0906: // serpentstone staff
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x090C: // Glass Sword
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x0DF0:
                            case 0x0DF1: // Black Staves
                                Abilities[0] = Ability.WhirlwindAttack;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x0DF2:
                            case 0x0DF3:
                            case 0x0DF4:
                            case 0x0DF5: // Wands BookType A-D
                                Abilities[0] = Ability.Dismount;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x0E81:
                            case 0x0E82: // Shepherd's Crooks
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x0E85:
                            case 0x0E86: // Pickaxes
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x0E87:
                            case 0x0E88: // Pitchforks
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x0E89:
                            case 0x0E8A: // Quarter Staves
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x0EC2:
                            case 0x0EC3: // Cleavers
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.InfectiousStrike;

                                goto done;

                            case 0x0EC4:
                            case 0x0EC5: // Skinning Knives
                                Abilities[0] = Ability.ShadowStrike;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x0F43:
                            case 0x0F44: // Hatchets
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x0F45:
                            case 0x0F46: // Double Axes
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x0F47:
                            case 0x0F48: // Battle Axes
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x0F49:
                            case 0x0F4A: // Axes
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x0F4B:
                            case 0x0F4C:
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.WhirlwindAttack;

                                goto done;

                            case 0x0F4D:
                            case 0x0F4E: // Bardiches
                                Abilities[0] = Ability.ParalyzingBlow;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x0F4F:
                            case 0x0F50: // Crossbows
                                Abilities[0] = Ability.ConcussionBlow;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x0F51:
                            case 0x0F52: // Daggers
                                Abilities[0] = Ability.InfectiousStrike;
                                Abilities[1] = Ability.ShadowStrike;

                                goto done;

                            case 0x0F5C:
                            case 0x0F5D: // Maces
                                Abilities[0] = Ability.ConcussionBlow;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x0F5E:
                            case 0x0F5F: // Broadswords
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.ArmorIgnore;

                                goto done;

                            case 0x0F60:
                            case 0x0F61: // Longswords
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x0F62:
                            case 0x0F63: // Spears
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x0FB5:
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.ShadowStrike;

                                goto done;

                            case 0x13AF:
                            case 0x13B0: // War Axes
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.BleedAttack;

                                goto done;

                            case 0x13B1:
                            case 0x13B2: // Bows
                                Abilities[0] = Ability.ParalyzingBlow;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x13B3:
                            case 0x13B4: // Clubs
                                Abilities[0] = Ability.ShadowStrike;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x13B7:
                            case 0x13B8: // Scimitars
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x13B9:
                            case 0x13BA: // Viking Swords
                                Abilities[0] = Ability.ParalyzingBlow;
                                Abilities[1] = Ability.CrushingBlow;

                                goto done;

                            case 0x13FD: // Heavy Crossbows
                                Abilities[0] = Ability.MovingShot;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x13E3: // Smith's Hammers
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.ShadowStrike;

                                goto done;

                            case 0x13F6: // Butcher Knives
                                Abilities[0] = Ability.InfectiousStrike;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x13F8: // Gnarled Staves
                                Abilities[0] = Ability.ConcussionBlow;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x13FB: // Large Battle Axes
                                Abilities[0] = Ability.WhirlwindAttack;
                                Abilities[1] = Ability.BleedAttack;

                                goto done;

                            case 0x13FF: // Katana
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.ArmorIgnore;

                                goto done;

                            case 0x1401: // Kryss
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.InfectiousStrike;

                                goto done;

                            case 0x1402:
                            case 0x1403: // Short Spears
                                Abilities[0] = Ability.ShadowStrike;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x1404:
                            case 0x1405: // War Forks
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x1406:
                            case 0x1407: // War Maces
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.BleedAttack;

                                goto done;

                            case 0x1438:
                            case 0x1439: // War Hammers
                                Abilities[0] = Ability.WhirlwindAttack;
                                Abilities[1] = Ability.CrushingBlow;

                                goto done;

                            case 0x143A:
                            case 0x143B: // Mauls
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x143C:
                            case 0x143D: // Hammer Picks
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x143E:
                            case 0x143F: // Halberds
                                Abilities[0] = Ability.WhirlwindAttack;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x1440:
                            case 0x1441: // Cutlasses
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.ShadowStrike;

                                goto done;

                            case 0x1442:
                            case 0x1443: // Two Handed Axes
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.ShadowStrike;

                                goto done;

                            case 0x26BA: // Scythes
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x26BB: // Bone Harvesters
                                Abilities[0] = Ability.ParalyzingBlow;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x26BC: // Scepters
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x26BD: // Bladed Staves
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x26BE: // Pikes
                                Abilities[0] = Ability.ParalyzingBlow;
                                Abilities[1] = Ability.InfectiousStrike;

                                goto done;

                            case 0x26BF: // Double Bladed Staff
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.InfectiousStrike;

                                goto done;

                            case 0x26C0: // Lances
                                Abilities[0] = Ability.Dismount;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x26C1: // Crescent Blades
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x26C2: // Composite Bows
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.MovingShot;

                                goto done;

                            case 0x26C3: // Repeating Crossbows
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.MovingShot;

                                goto done;

                            case 0x26C4: // also Scythes
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x26C5: // also Bone Harvesters
                                Abilities[0] = Ability.ParalyzingBlow;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x26C6: // also Scepters
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x26C7: // also Bladed Staves
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x26C8: // also Pikes
                                Abilities[0] = Ability.ParalyzingBlow;
                                Abilities[1] = Ability.InfectiousStrike;

                                goto done;

                            case 0x26C9: // also Double Bladed Staff
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.InfectiousStrike;

                                goto done;

                            case 0x26CA: // also Lances
                                Abilities[0] = Ability.Dismount;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x26CB: // also Crescent Blades
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x26CC: // also Composite Bows
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.MovingShot;

                                goto done;

                            case 0x26CD: // also Repeating Crossbows
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.MovingShot;

                                goto done;

                            case 0x27A2: // No-Dachi
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.RidingSwipe;

                                goto done;

                            case 0x27A3: // Tessen
                                Abilities[0] = Ability.Feint;
                                Abilities[1] = Ability.Block;

                                goto done;

                            case 0x27A4: // Wakizashi
                                Abilities[0] = Ability.FrenziedWhirlwind;
                                Abilities[1] = Ability.DoubleShot;

                                goto done;

                            case 0x27A5: // Yumi
                                Abilities[0] = Ability.ArmorPierce;
                                Abilities[1] = Ability.DoubleShot;

                                goto done;

                            case 0x27A6: // Tetsubo
                                Abilities[0] = Ability.FrenziedWhirlwind;
                                Abilities[1] = Ability.CrushingBlow;

                                goto done;

                            case 0x27A7: // Lajatang
                                Abilities[0] = Ability.DefenseMastery;
                                Abilities[1] = Ability.FrenziedWhirlwind;

                                goto done;

                            case 0x27A8: // Bokuto
                                Abilities[0] = Ability.Feint;
                                Abilities[1] = Ability.NerveStrike;

                                goto done;

                            case 0x27A9: // Daisho
                                Abilities[0] = Ability.Feint;
                                Abilities[1] = Ability.DoubleShot;

                                goto done;

                            case 0x27AA: // Fukya
                                Abilities[0] = Ability.Disarm;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x27AB: // Tekagi
                                Abilities[0] = Ability.DualWield;
                                Abilities[1] = Ability.TalonStrike;

                                goto done;

                            case 0x27AD: // Kama
                                Abilities[0] = Ability.WhirlwindAttack;
                                Abilities[1] = Ability.DefenseMastery;

                                goto done;

                            case 0x27AE: // Nunchaku
                                Abilities[0] = Ability.Block;
                                Abilities[1] = Ability.Feint;

                                goto done;

                            case 0x27AF: // Sai
                                Abilities[0] = Ability.Block;
                                Abilities[1] = Ability.ArmorPierce;

                                goto done;

                            case 0x27ED: // also No-Dachi
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.RidingSwipe;

                                goto done;

                            case 0x27EE: // also Tessen
                                Abilities[0] = Ability.Feint;
                                Abilities[1] = Ability.Block;

                                goto done;

                            case 0x27EF: // also Wakizashi
                                Abilities[0] = Ability.FrenziedWhirlwind;
                                Abilities[1] = Ability.DoubleShot;

                                goto done;

                            case 0x27F0: // also Yumi
                                Abilities[0] = Ability.ArmorPierce;
                                Abilities[1] = Ability.DoubleShot;

                                goto done;

                            case 0x27F1: // also Tetsubo
                                Abilities[0] = Ability.FrenziedWhirlwind;
                                Abilities[1] = Ability.CrushingBlow;

                                goto done;

                            case 0x27F2: // also Lajatang
                                Abilities[0] = Ability.DefenseMastery;
                                Abilities[1] = Ability.FrenziedWhirlwind;

                                goto done;

                            case 0x27F3: // also Bokuto
                                Abilities[0] = Ability.Feint;
                                Abilities[1] = Ability.NerveStrike;

                                goto done;

                            case 0x27F4: // also Daisho
                                Abilities[0] = Ability.Feint;
                                Abilities[1] = Ability.DoubleShot;

                                goto done;

                            case 0x27F5: // also Fukya
                                Abilities[0] = Ability.Disarm;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x27F6: // also Tekagi
                                Abilities[0] = Ability.DualWield;
                                Abilities[1] = Ability.TalonStrike;

                                goto done;

                            case 0x27F8: // Kama
                                Abilities[0] = Ability.WhirlwindAttack;
                                Abilities[1] = Ability.DefenseMastery;

                                goto done;

                            case 0x27F9: // Nunchaku
                                Abilities[0] = Ability.Block;
                                Abilities[1] = Ability.Feint;

                                goto done;

                            case 0x27FA: // Sai
                                Abilities[0] = Ability.Block;
                                Abilities[1] = Ability.ArmorPierce;

                                goto done;

                            case 0x2D1E: // Elven Composite Longbows
                                Abilities[0] = Ability.ForceArrow;
                                Abilities[1] = Ability.SerpentArrow;

                                goto done;

                            case 0x2D1F: // Magical Shortbows
                                Abilities[0] = Ability.LightningArrow;
                                Abilities[1] = Ability.PsychicAttack;

                                goto done;

                            case 0x2D20: // Elven Spellblades
                                Abilities[0] = Ability.PsychicAttack;
                                Abilities[1] = Ability.BleedAttack;

                                goto done;

                            case 0x2D21: // Assassin Spikes
                                Abilities[0] = Ability.InfectiousStrike;
                                Abilities[1] = Ability.ShadowStrike;

                                goto done;

                            case 0x2D22: // Leafblades
                                Abilities[0] = Ability.Feint;
                                Abilities[1] = Ability.ArmorIgnore;

                                goto done;

                            case 0x2D23: // War Cleavers
                                Abilities[0] = Ability.Disarm;
                                Abilities[1] = Ability.Bladeweave;

                                goto done;

                            case 0x2D24: // Diamond Maces
                                Abilities[0] = Ability.ConcussionBlow;
                                Abilities[1] = Ability.CrushingBlow;

                                goto done;

                            case 0x2D25: // Wild Staves
                                Abilities[0] = Ability.Block;
                                Abilities[1] = Ability.ForceOfNature;

                                goto done;

                            case 0x2D26: // Rune Blades
                                Abilities[0] = Ability.Disarm;
                                Abilities[1] = Ability.Bladeweave;

                                goto done;

                            case 0x2D27: // Radiant Scimitars
                                Abilities[0] = Ability.WhirlwindAttack;
                                Abilities[1] = Ability.Bladeweave;

                                goto done;

                            case 0x2D28: // Ornate Axes
                                Abilities[0] = Ability.Disarm;
                                Abilities[1] = Ability.CrushingBlow;

                                goto done;

                            case 0x2D29: // Elven Machetes
                                Abilities[0] = Ability.DefenseMastery;
                                Abilities[1] = Ability.Bladeweave;

                                goto done;

                            case 0x2D2A: // also Elven Composite Longbows
                                Abilities[0] = Ability.ForceArrow;
                                Abilities[1] = Ability.SerpentArrow;

                                goto done;

                            case 0x2D2B: // also Magical Shortbows
                                Abilities[0] = Ability.LightningArrow;
                                Abilities[1] = Ability.PsychicAttack;

                                goto done;

                            case 0x2D2C: // also Elven Spellblades
                                Abilities[0] = Ability.PsychicAttack;
                                Abilities[1] = Ability.BleedAttack;

                                goto done;

                            case 0x2D2D: // also Assassin Spikes
                                Abilities[0] = Ability.InfectiousStrike;
                                Abilities[1] = Ability.ShadowStrike;

                                goto done;

                            case 0x2D2E: // also Leafblades
                                Abilities[0] = Ability.Feint;
                                Abilities[1] = Ability.ArmorIgnore;

                                goto done;

                            case 0x2D2F: // also War Cleavers
                                Abilities[0] = Ability.Disarm;
                                Abilities[1] = Ability.Bladeweave;

                                goto done;

                            case 0x2D30: // also Diamond Maces
                                Abilities[0] = Ability.ConcussionBlow;
                                Abilities[1] = Ability.CrushingBlow;

                                goto done;

                            case 0x2D31: // also Wild Staves
                                Abilities[0] = Ability.Block;
                                Abilities[1] = Ability.ForceOfNature;

                                goto done;

                            case 0x2D32: // also Rune Blades
                                Abilities[0] = Ability.Disarm;
                                Abilities[1] = Ability.Bladeweave;

                                goto done;

                            case 0x2D33: // also Radiant Scimitars
                                Abilities[0] = Ability.WhirlwindAttack;
                                Abilities[1] = Ability.Bladeweave;

                                goto done;

                            case 0x2D34: // also Ornate Axes
                                Abilities[0] = Ability.Disarm;
                                Abilities[1] = Ability.CrushingBlow;

                                goto done;

                            case 0x2D35: // also Elven Machetes
                                Abilities[0] = Ability.DefenseMastery;
                                Abilities[1] = Ability.Bladeweave;

                                goto done;

                            case 0x4067: // Boomerang
                                Abilities[0] = Ability.MysticArc;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x4068: // Dual Short Axes
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.InfectiousStrike;

                                goto done;

                            case 0x406B: // Soul Glaive
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x406C: // Cyclone
                                Abilities[0] = Ability.MovingShot;
                                Abilities[1] = Ability.InfusedThrow;

                                goto done;

                            case 0x406D: // Dual Pointed Spear
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x406E: // Disc Mace
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x4072: // Blood Blade
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x4074: // Dread Sword
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x4075: // Gargish Talwar
                                Abilities[0] = Ability.WhirlwindAttack;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x4076: // Shortblade
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x48AE: // Gargish Cleaver
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.InfectiousStrike;

                                goto done;

                            case 0x48B0: // Gargish Battle Axe
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x48B2: // Gargish Axe
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x48B4: // Gargish Bardiche
                                Abilities[0] = Ability.ParalyzingBlow;
                                Abilities[1] = Ability.Dismount;

                                goto done;

                            case 0x48B6: // Gargish Butcher Knife
                                Abilities[0] = Ability.InfectiousStrike;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x48B8: // Gargish Gnarled Staff
                                Abilities[0] = Ability.ConcussionBlow;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x48BA: // Gargish Katana
                                Abilities[0] = Ability.DoubleShot;
                                Abilities[1] = Ability.ArmorIgnore;

                                goto done;

                            case 0x48BC: // Gargish Kryss
                                Abilities[0] = Ability.ArmorIgnore;
                                Abilities[1] = Ability.InfectiousStrike;

                                goto done;

                            case 0x48BE: // Gargish War Fork
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.Disarm;

                                goto done;

                            case 0x48CA: // Gargish Lance
                                Abilities[0] = Ability.Dismount;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x48C0: // Gargish War Hammer
                                Abilities[0] = Ability.WhirlwindAttack;
                                Abilities[1] = Ability.CrushingBlow;

                                goto done;

                            case 0x48C2: // Gargish Maul
                                Abilities[0] = Ability.CrushingBlow;
                                Abilities[1] = Ability.ConcussionBlow;

                                goto done;

                            case 0x48C4: // Gargish Scyte
                                Abilities[0] = Ability.BleedAttack;
                                Abilities[1] = Ability.ParalyzingBlow;

                                goto done;

                            case 0x48C6: // Gargish Bone Harvester
                                Abilities[0] = Ability.ParalyzingBlow;
                                Abilities[1] = Ability.MortalStrike;

                                goto done;

                            case 0x48C8: // Gargish Pike
                                Abilities[0] = Ability.ParalyzingBlow;
                                Abilities[1] = Ability.InfectiousStrike;

                                goto done;

                            case 0x48CD: // Gargish Tessen
                                Abilities[0] = Ability.Feint;
                                Abilities[1] = Ability.Block;

                                goto done;

                            case 0x48CE: // Gargish Tekagi
                                Abilities[0] = Ability.DualWield;
                                Abilities[1] = Ability.TalonStrike;

                                goto done;

                            case 0x48D0: // Gargish Daisho
                                Abilities[0] = Ability.Feint;
                                Abilities[1] = Ability.DoubleShot;

                                goto done;
                        }
                    }
                }

                done: ;
            }


            if (Abilities[0] == Ability.Invalid)
            {
                Abilities[0] = Ability.Disarm;
                Abilities[1] = Ability.ParalyzingBlow;
            }
        }

        protected override void OnPositionChanged()
        {
            base.OnPositionChanged();

            if (World.Map != null && World.Map.Index >= 0)
                World.Map.Center = new Point(X, Y);

            Plugin.UpdatePlayerPosition(X, Y, Z);

            TryOpenDoors();
            TryOpenCorpses();
        }

        public void TryOpenCorpses()
        {
            if (ProfileManager.Current.AutoOpenCorpses)
            {
                if ((ProfileManager.Current.CorpseOpenOptions == 1 || ProfileManager.Current.CorpseOpenOptions == 3) && TargetManager.IsTargeting)
                    return;

                if ((ProfileManager.Current.CorpseOpenOptions == 2 || ProfileManager.Current.CorpseOpenOptions == 3) && IsHidden)
                    return;

                foreach (var c in World.Items.Where(t => t.Graphic == 0x2006 && !OpenedCorpses.Contains(t.Serial) && t.Distance <= ProfileManager.Current.AutoOpenCorpseRange))
                {
                    OpenedCorpses.Add(c.Serial);
                    GameActions.DoubleClickQueued(c.Serial);
                }
            }
        }


        protected override void OnDirectionChanged()
        {
            base.OnDirectionChanged();
            TryOpenDoors();
        }

        private void TryOpenDoors()
        {
            if (!World.Player.IsDead && ProfileManager.Current.AutoOpenDoors)
            {
                int x = X, y = Y, z = Z;
                Pathfinder.GetNewXY((byte) Direction, ref x, ref y);

                if (World.Items.Any(s =>
                                        IsDoor(s.Graphic) && s.X == x && s.Y == y && s.Z - 15 <= z &&
                                        s.Position.Z + 15 >= z))
                    GameActions.OpenDoor();
            }
        }

        public override void Destroy()
        {
            Log.Warn( "PlayerMobile disposed!");
            base.Destroy();
        }

        public void CloseBank()
        {
            var bank = Equipment[(int) Layer.Bank];

            if (bank != null)
            {
                UIManager.GetGump<ContainerGump>(bank)?.Dispose();
            }
        }

        public void CloseRangedGumps()
        {
            foreach (var gump in UIManager.Gumps)
            {
                switch (gump)
                {
                    case PaperDollGump _:
                    case MapGump _:
                    case SpellbookGump _:

                        if (World.Get(gump.LocalSerial) == null)
                            gump.Dispose();

                        break;
                    case TradingGump _:
                    case ShopGump _:

                        Entity ent = World.Get(gump.LocalSerial);
                        int distance = int.MaxValue;
                        if (ent != null)
                        {
                            if (ent.Serial.IsItem)
                            {
                                var top = World.Get(((Item)ent).RootContainer);

                                if (top != null)
                                    distance = top.Distance;
                            }
                            else
                                distance = ent.Distance;
                        }

                        if (distance > 18)
                            gump.Dispose();

                        break;
                    case ContainerGump _:

                        ent = World.Get(gump.LocalSerial);
                        distance = int.MaxValue;
                        if (ent != null)
                        {
                            if (ent.Serial.IsItem)
                            {
                                var top = World.Get(((Item) ent).RootContainer);

                                if (top != null)
                                    distance = top.Distance;
                            }
                            else
                                distance = ent.Distance;
                        }

                        if (distance > 3)
                            gump.Dispose();
                        break;
                }
            }

        }


#if !JAEDAN_MOVEMENT_PATCH && !MOVEMENT2
        internal WalkerManager Walker { get; } = new WalkerManager();


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            /*const int TIME_TURN_TO_LASTTARGET = 2000;

            if (InWarMode && Walker.LastStepRequestTime + TIME_TURN_TO_LASTTARGET < Time.Ticks)
            {
                Mobile enemy = World.Mobiles.Get(World.LastAttack);

                if (enemy != null)
                {
                    Point center = new Point(ProfileManager.Current.GameWindowPosition.X + (ProfileManager.Current.GameWindowSize.X >> 1), ProfileManager.Current.GameWindowPosition.Y + (ProfileManager.Current.GameWindowSize.Y >> 1));
                    Direction direction = DirectionHelper.DirectionFromPoints(center, new Point(enemy.RealScreenPosition.X, enemy.RealScreenPosition.Y));

                    if (Direction != direction)
                        Walk(direction, false);
                }
            }*/
        }

        protected override bool NoIterateAnimIndex()
        {
            return false;
        }

        public bool Walk(Direction direction, bool run)
        {
            if (Walker.WalkingFailed || Walker.LastStepRequestTime > Time.Ticks || Walker.StepsCount >= Constants.MAX_STEP_COUNT ||
                (FileManager.ClientVersion >= ClientVersions.CV_60142 && IsParalyzed))
                return false;

            if (SpeedMode >= CharacterSpeedType.CantRun || Stamina <= 1 && !IsDead)
                run = false;
            else if (!run)
            {
                if (!IsHidden ||
                    IsHidden && !ProfileManager.Current.AlwaysRunUnlessHidden)
                    run = ProfileManager.Current.AlwaysRun;
            }

            int x = X;
            int y = Y;
            sbyte z = Z;
            Direction oldDirection = Direction;

            bool emptyStack = Steps.Count == 0;

            if (!emptyStack)
            {
                ref Step walkStep = ref Steps.Back();
                x = walkStep.X;
                y = walkStep.Y;
                z = walkStep.Z;
                oldDirection = (Direction) walkStep.Direction;
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
                    walkTime = (ushort) MovementSpeed.TimeToCompleteMovement(this, run);
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
                    walkTime = (ushort) MovementSpeed.TimeToCompleteMovement(this, run);
                }

                direction = newDir;
            }

            CloseBank();

            if (emptyStack)
            {
                if (!IsWalking)
                    SetAnimation(0xFF);
                LastStepTime = Time.Ticks;
            }

            ref var step = ref Walker.StepInfos[Walker.StepsCount];
            step.Sequence = Walker.WalkSequence;
            step.Accepted = false;
            step.Running = run;
            step.OldDirection = (byte) (oldDirection & Direction.Mask);
            step.Direction = (byte) direction;
            step.Timer = Time.Ticks;
            step.X = (ushort) x;
            step.Y = (ushort) y;
            step.Z = z;
            step.NoRotation = step.OldDirection == (byte) direction && oldZ - z >= 11;

            Walker.StepsCount++;

            Steps.AddToBack(new Step
            {
                X = x,
                Y = y,
                Z = z,
                Direction = (byte) direction,
                Run = run
            });


            NetClient.Socket.Send(new PWalkRequest(direction, Walker.WalkSequence, run, Walker.FastWalkStack.GetValue()));


            if (Walker.WalkSequence == 0xFF)
                Walker.WalkSequence = 1;
            else
                Walker.WalkSequence++;

            Walker.UnacceptedPacketsCount++;


            int nowDelta = 0;

            if (_lastDir == (int) direction && _lastMount == IsMounted && _lastRun == run)
            {
                nowDelta = (int) (Time.Ticks - _lastStepTime - walkTime + _lastDelta);

                if (Math.Abs(nowDelta) > 70)
                    nowDelta = 0;
                _lastDelta = nowDelta;
            }
            else
                _lastDelta = 0;

            _lastStepTime = (int) Time.Ticks;
            _lastRun = run;
            _lastMount = IsMounted;
            _lastDir = (int) direction;


            Walker.LastStepRequestTime = Time.Ticks + walkTime - nowDelta;
            GetGroupForAnimation(this, 0, true);

            return true;
        }

        private bool _lastRun, _lastMount;
        private int _lastDir = -1, _lastDelta, _lastStepTime;

#elif !MOVEMENT2
        private int _movementX, _movementY;
        private sbyte _movementZ;
        private int _stepsOutstanding;
        private Direction _movementDirection = Direction.North;
        private byte _sequenceNumber;
        private int _resynchronizing;
        private long _nextAllowedStepTime;

        public bool IsWaitingNextMovement => _nextAllowedStepTime > Time.Ticks;

        public bool Walk(Direction direction, bool run)
        {
            if (_nextAllowedStepTime > Time.Ticks || IsParalyzed)
            {
                return false;
            }

            if (_stepsOutstanding > Constants.MAX_STEP_COUNT)
            {
                if (_nextAllowedStepTime + 1000 > Time.Ticks)
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

            Log.Panic("SEND");
            NetClient.Socket.Send(new PWalkRequest(direction, _sequenceNumber, run));
            //Log.Trace( $"Walk request - SEQUENCE: {_sequenceNumber}");

            if (_sequenceNumber == 0xFF)
                _sequenceNumber = 1;
            else
                _sequenceNumber++;
            _nextAllowedStepTime = Time.Ticks + walkTime;
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
            if (LastStepRequestedTime > Time.Ticks)
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

            LastStepRequestedTime = Time.Ticks + walkTime;

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
                Log.Warn( $"Resync needed after confirmwalk packet - SEQUENCE: {_sequenceNumber}");
                Resynchronize();
            }
            else
            {
                //Log.Trace( $"Step accepted - SEQUENCE: {_sequenceNumber}");
                _stepsOutstanding--;
            }
#else
            Walker.ConfirmWalk(seq);
#endif
        }

        private bool IsDoor(ushort type)
        {
            return type >= 0x0675 && type <= 0x06F6
                   || type >= 0x0824 && type <= 0x0833
                   || type >= 0x0839 && type <= 0x0848
                   || type >= 0x084C && type <= 0x085B
                   || type >= 0x0866 && type <= 0x0875
                   || type >= 0x1FED && type <= 0x1FFC
                   || type >= 0x241F && type <= 0x2424
                   || type >= 0x2A05 && type <= 0x2A1C
                   || type >= 0x2D46 && type <= 0x2D49
                   || type >= 0x2D63 && type <= 0x2D6E
                   || type >= 0x319C && type <= 0x31AF
                   || type >= 0x367B && type <= 0x369A
                   || type >= 0x410C && type <= 0x4113
                   || type >= 0x41C2 && type <= 0x41C9
                   || type >= 0x41CF && type <= 0x41D6
                   || type >= 0x46DD && type <= 0x46E4
                   || type >= 0x4D1A && type <= 0x4D29
                   || type >= 0x50C8 && type <= 0x50D7
                   || type >= 0x9AD7 && type <= 0x9AE6
                   || type >= 0x9B3C && type <= 0x9B4B;
        }

        private readonly HashSet<Serial> OpenedCorpses = new HashSet<Serial>();
#if JAEDAN_MOVEMENT_PATCH
        public override void ForcePosition(ushort x, ushort y, sbyte z, Direction dir)
        {

            //Log.Warn( $"Forced position. - SEQUENCE: {_sequenceNumber}");

            _nextAllowedStepTime = Time.Ticks;
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
                if (_nextAllowedStepTime + (_resynchronizing * 1000) > Time.Ticks)
                    return;
            }

            _resynchronizing++;
            NetClient.Socket.Send(new PResend());
            Log.Trace( $"Resync request num: {_resynchronizing}");
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