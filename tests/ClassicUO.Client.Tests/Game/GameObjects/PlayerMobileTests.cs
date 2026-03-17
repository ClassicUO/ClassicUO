using System.Linq;
using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.GameObjects
{
    public class PlayerMobileTests
    {
        private readonly World _world;
        private readonly PlayerMobile _player;

        public PlayerMobileTests()
        {
            _world = TestHelpers.CreateTestWorld();
            _player = new PlayerMobile(_world, 0x00000001, skillCount: 58);
        }

        // --- Construction ---

        [Fact]
        public void Constructor_SetsSerial()
        {
            _player.Serial.Should().Be(0x00000001u);
        }

        [Fact]
        public void Constructor_CreatesSkillsArray()
        {
            _player.Skills.Should().NotBeNull();
            _player.Skills.Length.Should().Be(58);
        }

        [Fact]
        public void Constructor_InitializesAllSkills()
        {
            for (int i = 0; i < _player.Skills.Length; i++)
            {
                _player.Skills[i].Should().NotBeNull();
                _player.Skills[i].Name.Should().Be($"Skill{i}");
                _player.Skills[i].Index.Should().Be(i);
                _player.Skills[i].IsClickable.Should().BeTrue();
            }
        }

        [Fact]
        public void Constructor_CreatesWalker()
        {
            _player.Walker.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_CreatesPathfinder()
        {
            _player.Pathfinder.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_InitializesAbilities()
        {
            _player.Abilities.Should().HaveCount(2);
            _player.Abilities[0].Should().Be(Ability.Invalid);
            _player.Abilities[1].Should().Be(Ability.Invalid);
        }

        // --- Stats ---

        [Fact]
        public void Strength_GetSet()
        {
            _player.Strength = 125;
            _player.Strength.Should().Be(125);
        }

        [Fact]
        public void Dexterity_GetSet()
        {
            _player.Dexterity = 80;
            _player.Dexterity.Should().Be(80);
        }

        [Fact]
        public void Intelligence_GetSet()
        {
            _player.Intelligence = 90;
            _player.Intelligence.Should().Be(90);
        }

        [Fact]
        public void Weight_GetSet()
        {
            _player.Weight = 350;
            _player.Weight.Should().Be(350);
        }

        [Fact]
        public void WeightMax_GetSet()
        {
            _player.WeightMax = 500;
            _player.WeightMax.Should().Be(500);
        }

        [Fact]
        public void Gold_GetSet()
        {
            _player.Gold = 1000000;
            _player.Gold.Should().Be(1000000u);
        }

        [Fact]
        public void Luck_GetSet()
        {
            _player.Luck = 200;
            _player.Luck.Should().Be(200);
        }

        [Fact]
        public void Followers_GetSet()
        {
            _player.Followers = 3;
            _player.FollowersMax = 5;
            _player.Followers.Should().Be(3);
            _player.FollowersMax.Should().Be(5);
        }

        [Fact]
        public void TithingPoints_GetSet()
        {
            _player.TithingPoints = 50000;
            _player.TithingPoints.Should().Be(50000u);
        }

        // --- Stat Locks ---

        [Fact]
        public void StrLock_GetSet_Up()
        {
            _player.StrLock = Lock.Up;
            _player.StrLock.Should().Be(Lock.Up);
        }

        [Fact]
        public void StrLock_GetSet_Down()
        {
            _player.StrLock = Lock.Down;
            _player.StrLock.Should().Be(Lock.Down);
        }

        [Fact]
        public void StrLock_GetSet_Locked()
        {
            _player.StrLock = Lock.Locked;
            _player.StrLock.Should().Be(Lock.Locked);
        }

        [Fact]
        public void DexLock_GetSet()
        {
            _player.DexLock = Lock.Down;
            _player.DexLock.Should().Be(Lock.Down);
        }

        [Fact]
        public void IntLock_GetSet()
        {
            _player.IntLock = Lock.Locked;
            _player.IntLock.Should().Be(Lock.Locked);
        }

        // --- Resistances ---

        [Fact]
        public void PhysicalResistance_GetSet()
        {
            _player.PhysicalResistance = 45;
            _player.PhysicalResistance.Should().Be(45);
        }

        [Fact]
        public void FireResistance_GetSet()
        {
            _player.FireResistance = 55;
            _player.FireResistance.Should().Be(55);
        }

        [Fact]
        public void ColdResistance_GetSet()
        {
            _player.ColdResistance = 40;
            _player.ColdResistance.Should().Be(40);
        }

        [Fact]
        public void PoisonResistance_GetSet()
        {
            _player.PoisonResistance = 35;
            _player.PoisonResistance.Should().Be(35);
        }

        [Fact]
        public void EnergyResistance_GetSet()
        {
            _player.EnergyResistance = 50;
            _player.EnergyResistance.Should().Be(50);
        }

        [Fact]
        public void MaxResistances_GetSet()
        {
            _player.MaxPhysicResistence = 70;
            _player.MaxFireResistence = 70;
            _player.MaxColdResistence = 70;
            _player.MaxPoisonResistence = 70;
            _player.MaxEnergyResistence = 70;

            _player.MaxPhysicResistence.Should().Be(70);
            _player.MaxFireResistence.Should().Be(70);
            _player.MaxColdResistence.Should().Be(70);
            _player.MaxPoisonResistence.Should().Be(70);
            _player.MaxEnergyResistence.Should().Be(70);
        }

        // --- Combat Modifiers ---

        [Fact]
        public void DamageRange_GetSet()
        {
            _player.DamageMin = 10;
            _player.DamageMax = 25;
            _player.DamageMin.Should().Be(10);
            _player.DamageMax.Should().Be(25);
        }

        [Fact]
        public void DamageIncrease_GetSet()
        {
            _player.DamageIncrease = 50;
            _player.DamageIncrease.Should().Be(50);
        }

        [Fact]
        public void HitChanceIncrease_GetSet()
        {
            _player.HitChanceIncrease = 45;
            _player.HitChanceIncrease.Should().Be(45);
        }

        [Fact]
        public void DefenseChanceIncrease_GetSet()
        {
            _player.DefenseChanceIncrease = 40;
            _player.DefenseChanceIncrease.Should().Be(40);
        }

        [Fact]
        public void SwingSpeedIncrease_GetSet()
        {
            _player.SwingSpeedIncrease = 30;
            _player.SwingSpeedIncrease.Should().Be(30);
        }

        // --- Casting Modifiers ---

        [Fact]
        public void FasterCasting_GetSet()
        {
            _player.FasterCasting = 4;
            _player.FasterCasting.Should().Be(4);
        }

        [Fact]
        public void FasterCastRecovery_GetSet()
        {
            _player.FasterCastRecovery = 6;
            _player.FasterCastRecovery.Should().Be(6);
        }

        [Fact]
        public void LowerManaCost_GetSet()
        {
            _player.LowerManaCost = 40;
            _player.LowerManaCost.Should().Be(40);
        }

        [Fact]
        public void LowerReagentCost_GetSet()
        {
            _player.LowerReagentCost = 100;
            _player.LowerReagentCost.Should().Be(100);
        }

        [Fact]
        public void SpellDamageIncrease_GetSet()
        {
            _player.SpellDamageIncrease = 30;
            _player.SpellDamageIncrease.Should().Be(30);
        }

        // --- Regeneration ---

        [Fact]
        public void HitPointsRegeneration_GetSet()
        {
            _player.HitPointsRegeneration = 18;
            _player.HitPointsRegeneration.Should().Be(18);
        }

        [Fact]
        public void ManaRegeneration_GetSet()
        {
            _player.ManaRegeneration = 10;
            _player.ManaRegeneration.Should().Be(10);
        }

        [Fact]
        public void StaminaRegeneration_GetSet()
        {
            _player.StaminaRegeneration = 12;
            _player.StaminaRegeneration.Should().Be(12);
        }

        // --- Other Modifiers ---

        [Fact]
        public void EnhancePotions_GetSet()
        {
            _player.EnhancePotions = 50;
            _player.EnhancePotions.Should().Be(50);
        }

        [Fact]
        public void ReflectPhysicalDamage_GetSet()
        {
            _player.ReflectPhysicalDamage = 15;
            _player.ReflectPhysicalDamage.Should().Be(15);
        }

        [Fact]
        public void StatsCap_GetSet()
        {
            _player.StatsCap = 225;
            _player.StatsCap.Should().Be(225);
        }

        [Fact]
        public void MaxHitPointsIncrease_GetSet()
        {
            _player.MaxHitPointsIncrease = 25;
            _player.MaxHitPointsIncrease.Should().Be(25);
        }

        [Fact]
        public void MaxManaIncrease_GetSet()
        {
            _player.MaxManaIncrease = 25;
            _player.MaxManaIncrease.Should().Be(25);
        }

        [Fact]
        public void MaxStaminaIncrease_GetSet()
        {
            _player.MaxStaminaIncrease = 25;
            _player.MaxStaminaIncrease.Should().Be(25);
        }

        [Fact]
        public void MaxDefenseChanceIncrease_GetSet()
        {
            _player.MaxDefenseChanceIncrease = 45;
            _player.MaxDefenseChanceIncrease.Should().Be(45);
        }

        // --- Stat Increase Modifiers ---

        [Fact]
        public void StrengthIncrease_GetSet()
        {
            _player.StrengthIncrease = 8;
            _player.StrengthIncrease.Should().Be(8);
        }

        [Fact]
        public void DexterityIncrease_GetSet()
        {
            _player.DexterityIncrease = 5;
            _player.DexterityIncrease.Should().Be(5);
        }

        [Fact]
        public void IntelligenceIncrease_GetSet()
        {
            _player.IntelligenceIncrease = 8;
            _player.IntelligenceIncrease.Should().Be(8);
        }

        [Fact]
        public void HitPointsIncrease_GetSet()
        {
            _player.HitPointsIncrease = 25;
            _player.HitPointsIncrease.Should().Be(25);
        }

        [Fact]
        public void ManaIncrease_GetSet()
        {
            _player.ManaIncrease = 20;
            _player.ManaIncrease.Should().Be(20);
        }

        [Fact]
        public void StaminaIncrease_GetSet()
        {
            _player.StaminaIncrease = 15;
            _player.StaminaIncrease.Should().Be(15);
        }

        // --- Skills ---

        [Fact]
        public void Skills_SetValue_ReadsCorrectly()
        {
            _player.Skills[0].ValueFixed = 1000; // 100.0
            _player.Skills[0].Value.Should().Be(100.0f);
        }

        [Fact]
        public void Skills_SetBase_ReadsCorrectly()
        {
            _player.Skills[5].BaseFixed = 750; // 75.0
            _player.Skills[5].Base.Should().Be(75.0f);
        }

        [Fact]
        public void Skills_SetCap_ReadsCorrectly()
        {
            _player.Skills[10].CapFixed = 1200; // 120.0
            _player.Skills[10].Cap.Should().Be(120.0f);
        }

        [Fact]
        public void Skills_SetLock()
        {
            _player.Skills[0].Lock = Lock.Down;
            _player.Skills[0].Lock.Should().Be(Lock.Down);
        }

        [Fact]
        public void Skills_Index_MatchesPosition()
        {
            for (int i = 0; i < _player.Skills.Length; i++)
            {
                _player.Skills[i].Index.Should().Be(i);
            }
        }

        // --- Buff Icons ---

        [Fact]
        public void AddBuff_AppearsInBuffIcons()
        {
            _player.AddBuff(BuffIconType.Agility, 0x753A, 60000, "Agility +10");
            _player.BuffIcons.Should().ContainKey(BuffIconType.Agility);
        }

        [Fact]
        public void IsBuffIconExists_ReturnsTrueAfterAdd()
        {
            _player.AddBuff(BuffIconType.Strength, 0x753B, 60000, "Strength +10");
            _player.IsBuffIconExists(BuffIconType.Strength).Should().BeTrue();
        }

        [Fact]
        public void IsBuffIconExists_ReturnsFalseWhenNotAdded()
        {
            _player.IsBuffIconExists(BuffIconType.Cunning).Should().BeFalse();
        }

        [Fact]
        public void RemoveBuff_RemovesFromBuffIcons()
        {
            _player.AddBuff(BuffIconType.Agility, 0x753A, 60000, "Agility +10");
            _player.RemoveBuff(BuffIconType.Agility);
            _player.IsBuffIconExists(BuffIconType.Agility).Should().BeFalse();
        }

        [Fact]
        public void AddBuff_OverwritesExisting()
        {
            _player.AddBuff(BuffIconType.Agility, 0x753A, 60000, "Old text");
            _player.AddBuff(BuffIconType.Agility, 0x753A, 120000, "New text");
            _player.BuffIcons.Should().HaveCount(1);
        }

        // --- InWarMode ---

        [Fact]
        public void InWarMode_DefaultFalse()
        {
            _player.InWarMode.Should().BeFalse();
        }

        [Fact]
        public void InWarMode_GetSet()
        {
            _player.InWarMode = true;
            _player.InWarMode.Should().BeTrue();
        }

        // --- Collections ---

        [Fact]
        public void AutoOpenedCorpses_InitiallyEmpty()
        {
            _player.AutoOpenedCorpses.Should().BeEmpty();
        }

        [Fact]
        public void AutoOpenedCorpses_AddAndContains()
        {
            _player.AutoOpenedCorpses.Add(0x40001000);
            _player.AutoOpenedCorpses.Should().Contain(0x40001000u);
        }

        [Fact]
        public void ManualOpenedCorpses_InitiallyEmpty()
        {
            _player.ManualOpenedCorpses.Should().BeEmpty();
        }

        // --- DeathScreenTimer ---

        [Fact]
        public void DeathScreenTimer_DefaultZero()
        {
            _player.DeathScreenTimer.Should().Be(0);
        }

        [Fact]
        public void DeathScreenTimer_GetSet()
        {
            _player.DeathScreenTimer = 5000;
            _player.DeathScreenTimer.Should().Be(5000);
        }

        // --- FindBandage ---

        [Fact]
        public void FindBandage_NullWhenNoBackpack()
        {
            _player.FindBandage().Should().BeNull();
        }

        [Fact]
        public void FindBandage_NullWhenBackpackEmpty()
        {
            var backpack = Item.Create(_world, 0x40000010);
            backpack.Layer = Layer.Backpack;
            backpack.Container = _player.Serial;
            _player.PushToBack(backpack);

            _player.FindBandage().Should().BeNull();
        }

        [Fact]
        public void FindBandage_FindsBandageInBackpack()
        {
            var backpack = Item.Create(_world, 0x40000010);
            backpack.Layer = Layer.Backpack;
            backpack.Container = _player.Serial;
            _player.PushToBack(backpack);

            var bandage = Item.Create(_world, 0x40000011);
            bandage.Graphic = 0x0E21; // bandage graphic
            bandage.Container = backpack.Serial;
            backpack.PushToBack(bandage);

            _player.FindBandage().Should().BeSameAs(bandage);
        }

        // --- FindItemByGraphic ---

        [Fact]
        public void FindItemByGraphic_NullWhenNoBackpack()
        {
            _player.FindItemByGraphic(0x0F0E).Should().BeNull();
        }

        [Fact]
        public void FindItemByGraphic_FindsItemInBackpack()
        {
            var backpack = Item.Create(_world, 0x40000020);
            backpack.Layer = Layer.Backpack;
            backpack.Container = _player.Serial;
            _player.PushToBack(backpack);

            var potion = Item.Create(_world, 0x40000021);
            potion.Graphic = 0x0F0E; // greater heal potion
            potion.Container = backpack.Serial;
            backpack.PushToBack(potion);

            _player.FindItemByGraphic(0x0F0E).Should().BeSameAs(potion);
        }

        // --- Inherited Mobile behavior ---

        [Fact]
        public void IsHuman_WithHumanGraphic_True()
        {
            _player.Graphic = 400;
            _player.IsHuman.Should().BeTrue();
        }

        [Fact]
        public void Mana_GetSet()
        {
            _player.Mana = 80;
            _player.ManaMax = 100;
            _player.Mana.Should().Be(80);
            _player.ManaMax.Should().Be(100);
        }

        [Fact]
        public void Stamina_GetSet()
        {
            _player.Stamina = 90;
            _player.StaminaMax = 120;
            _player.Stamina.Should().Be(90);
            _player.StaminaMax.Should().Be(120);
        }

        // --- PrimaryAbility / SecondaryAbility ---

        [Fact]
        public void PrimaryAbility_DefaultInvalid()
        {
            _player.PrimaryAbility.Should().Be(Ability.Invalid);
        }

        [Fact]
        public void SecondaryAbility_DefaultInvalid()
        {
            _player.SecondaryAbility.Should().Be(Ability.Invalid);
        }

        // --- Full stat scenario ---

        [Fact]
        public void FullStatProfile_AllValuesCoexist()
        {
            _player.Strength = 125;
            _player.Dexterity = 40;
            _player.Intelligence = 130;
            _player.Hits = 110;
            _player.HitsMax = 125;
            _player.Mana = 100;
            _player.ManaMax = 130;
            _player.Stamina = 40;
            _player.StaminaMax = 40;
            _player.Weight = 300;
            _player.WeightMax = 550;
            _player.Gold = 50000;
            _player.Luck = 150;
            _player.TithingPoints = 10000;
            _player.StatsCap = 225;
            _player.DamageMin = 12;
            _player.DamageMax = 18;
            _player.PhysicalResistance = 70;
            _player.FireResistance = 65;
            _player.ColdResistance = 60;
            _player.PoisonResistance = 55;
            _player.EnergyResistance = 50;

            // Verify all values are independent
            _player.Strength.Should().Be(125);
            _player.Dexterity.Should().Be(40);
            _player.Intelligence.Should().Be(130);
            _player.Hits.Should().Be(110);
            _player.HitsMax.Should().Be(125);
            _player.Mana.Should().Be(100);
            _player.ManaMax.Should().Be(130);
            _player.Stamina.Should().Be(40);
            _player.StaminaMax.Should().Be(40);
            _player.Weight.Should().Be(300);
            _player.WeightMax.Should().Be(550);
            _player.Gold.Should().Be(50000u);
            _player.Luck.Should().Be(150);
            _player.TithingPoints.Should().Be(10000u);
            _player.StatsCap.Should().Be(225);
            _player.DamageMin.Should().Be(12);
            _player.DamageMax.Should().Be(18);
            _player.PhysicalResistance.Should().Be(70);
            _player.FireResistance.Should().Be(65);
            _player.ColdResistance.Should().Be(60);
            _player.PoisonResistance.Should().Be(55);
            _player.EnergyResistance.Should().Be(50);
        }
    }
}
