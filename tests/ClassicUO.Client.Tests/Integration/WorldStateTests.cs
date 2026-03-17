using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Integration
{
    public class WorldStateTests
    {
        private readonly World _world;

        public WorldStateTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void CreateMultipleMobilesAndItems_ContainsWorksForAll()
        {
            var mob1 = _world.GetOrCreateMobile(0x0001);
            var mob2 = _world.GetOrCreateMobile(0x0002);
            var mob3 = _world.GetOrCreateMobile(0x0003);

            var item1 = _world.GetOrCreateItem(0x40000001);
            var item2 = _world.GetOrCreateItem(0x40000002);

            _world.Contains(0x0001).Should().BeTrue();
            _world.Contains(0x0002).Should().BeTrue();
            _world.Contains(0x0003).Should().BeTrue();
            _world.Contains(0x40000001).Should().BeTrue();
            _world.Contains(0x40000002).Should().BeTrue();

            // Non-existent should still be false
            _world.Contains(0x0099).Should().BeFalse();
            _world.Contains(0x40000099).Should().BeFalse();
        }

        [Fact]
        public void GetOrCreateItem_Idempotency_SameSerialReturnsSameObject()
        {
            var item1 = _world.GetOrCreateItem(0x40000010);
            item1.Graphic = 0x0EEA;
            item1.Amount = 50;

            var item2 = _world.GetOrCreateItem(0x40000010);

            item2.Should().BeSameAs(item1);
            item2.Graphic.Should().Be(0x0EEA);
            item2.Amount.Should().Be(50);
        }

        [Fact]
        public void GetOrCreateMobile_Idempotency_SameSerialReturnsSameObject()
        {
            var mob1 = _world.GetOrCreateMobile(0x0010);
            mob1.Name = "Gandalf";
            mob1.Hits = 100;

            var mob2 = _world.GetOrCreateMobile(0x0010);

            mob2.Should().BeSameAs(mob1);
            mob2.Name.Should().Be("Gandalf");
            mob2.Hits.Should().Be(100);
        }

        [Fact]
        public void OPL_AddProperty_VerifyViaWorldOPL()
        {
            // Create an item, then add OPL data for it
            var item = _world.GetOrCreateItem(0x40000020);
            item.Graphic = 0x0F5E;

            _world.OPL.Add(item.Serial, 1u, "Magic Sword", "Damage Increase 10%", 1050045);

            _world.OPL.Contains(item.Serial).Should().BeTrue();
            _world.OPL.TryGetNameAndData(item.Serial, out var name, out var data).Should().BeTrue();
            name.Should().Be("Magic Sword");
            data.Should().Be("Damage Increase 10%");
        }

        [Fact]
        public void OPL_MultipleEntities_EachHasOwnProperties()
        {
            var sword = _world.GetOrCreateItem(0x40000030);
            var shield = _world.GetOrCreateItem(0x40000031);

            _world.OPL.Add(sword.Serial, 1u, "Sword of Might", "Strength +5", 1050045);
            _world.OPL.Add(shield.Serial, 2u, "Shield of Valor", "Defense +10", 1050045);

            _world.OPL.TryGetNameAndData(sword.Serial, out var swordName, out var swordData).Should().BeTrue();
            swordName.Should().Be("Sword of Might");
            swordData.Should().Be("Strength +5");

            _world.OPL.TryGetNameAndData(shield.Serial, out var shieldName, out var shieldData).Should().BeTrue();
            shieldName.Should().Be("Shield of Valor");
            shieldData.Should().Be("Defense +10");
        }

        [Fact]
        public void CorpseManager_AddCorpse_ExistsReturnsTrue()
        {
            var corpseItem = _world.GetOrCreateItem(0x40000040);
            corpseItem.Graphic = 0x2006; // corpse graphic

            _world.CorpseManager.Add(corpseItem.Serial, 0x0001, Direction.North, false);

            _world.CorpseManager.Exists(corpseItem.Serial, 0u).Should().BeTrue();
        }

        [Fact]
        public void CorpseManager_AddAndRemove_ExistsReturnsFalse()
        {
            _world.CorpseManager.Add(0x40000050, 0x0002, Direction.South, true);
            _world.CorpseManager.Exists(0x40000050, 0u).Should().BeTrue();

            _world.CorpseManager.Remove(0x40000050, 0u);
            _world.CorpseManager.Exists(0x40000050, 0u).Should().BeFalse();
        }

        [Fact]
        public void Party_SetLeader_ContainsReturnsTrueForMember()
        {
            _world.Party.Leader = 0x0001;
            _world.Party.Members[0] = new PartyMember(_world, 0x0001);

            _world.Party.Leader.Should().Be(0x0001u);
            _world.Party.Contains(0x0001).Should().BeTrue();
        }

        [Fact]
        public void Party_MultipleMembers_ContainsWorksForAll()
        {
            _world.Party.Members[0] = new PartyMember(_world, 0x0001);
            _world.Party.Members[1] = new PartyMember(_world, 0x0002);
            _world.Party.Members[2] = new PartyMember(_world, 0x0003);

            _world.Party.Contains(0x0001).Should().BeTrue();
            _world.Party.Contains(0x0002).Should().BeTrue();
            _world.Party.Contains(0x0003).Should().BeTrue();
            _world.Party.Contains(0x0004).Should().BeFalse();
        }

        [Fact]
        public void Party_Clear_RemovesAllMembers()
        {
            _world.Party.Leader = 0x0001;
            _world.Party.Members[0] = new PartyMember(_world, 0x0001);
            _world.Party.Members[1] = new PartyMember(_world, 0x0002);

            _world.Party.Clear();

            _world.Party.Leader.Should().Be(0u);
            _world.Party.Contains(0x0001).Should().BeFalse();
            _world.Party.Contains(0x0002).Should().BeFalse();
        }

        [Fact]
        public void WorldGet_DistinguishesBetweenMobilesAndItems()
        {
            var mobile = _world.GetOrCreateMobile(0x0001);
            var item = _world.GetOrCreateItem(0x40000001);

            var gotMobile = _world.Get(0x0001);
            var gotItem = _world.Get(0x40000001);

            gotMobile.Should().BeOfType<Mobile>();
            gotItem.Should().BeOfType<Item>();
            gotMobile.Should().BeSameAs(mobile);
            gotItem.Should().BeSameAs(item);
        }

        [Fact]
        public void WorldManagers_AllAccessibleAndNonNull()
        {
            _world.OPL.Should().NotBeNull();
            _world.CorpseManager.Should().NotBeNull();
            _world.Party.Should().NotBeNull();
            _world.Journal.Should().NotBeNull();
            _world.TargetManager.Should().NotBeNull();
            _world.ChatManager.Should().NotBeNull();
            _world.Weather.Should().NotBeNull();
        }

        [Fact]
        public void ItemAndMobile_SameWorld_ShareOPLManager()
        {
            var mobile = _world.GetOrCreateMobile(0x0005);
            var item = _world.GetOrCreateItem(0x40000005);

            // Both entities' properties can be stored in the same OPL manager
            _world.OPL.Add(mobile.Serial, 1u, "Warrior", "Strength: 100", 0);
            _world.OPL.Add(item.Serial, 2u, "Excalibur", "Damage: 50", 1050045);

            _world.OPL.TryGetNameAndData(mobile.Serial, out var mobName, out _).Should().BeTrue();
            _world.OPL.TryGetNameAndData(item.Serial, out var itemName, out _).Should().BeTrue();
            mobName.Should().Be("Warrior");
            itemName.Should().Be("Excalibur");
        }
    }
}
