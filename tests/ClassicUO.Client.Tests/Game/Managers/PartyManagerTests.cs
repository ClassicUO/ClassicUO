using ClassicUO.Client.Tests;
using ClassicUO.Game;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class PartyManagerTests
    {
        private readonly World _world;

        public PartyManagerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void Leader_DefaultIsZero()
        {
            _world.Party.Leader.Should().Be(0u);
        }

        [Fact]
        public void Inviter_DefaultIsZero()
        {
            _world.Party.Inviter.Should().Be(0u);
        }

        [Fact]
        public void Contains_ReturnsFalse_WhenEmpty()
        {
            _world.Party.Contains(12345u).Should().BeFalse();
        }

        [Fact]
        public void Members_ArrayExists_AndHasTenSlots()
        {
            _world.Party.Members.Should().NotBeNull();
            _world.Party.Members.Length.Should().Be(10);
        }

        [Fact]
        public void Members_AllNull_Initially()
        {
            foreach (var member in _world.Party.Members)
            {
                member.Should().BeNull();
            }
        }

        [Fact]
        public void CanLoot_GetSet()
        {
            _world.Party.CanLoot.Should().BeFalse();

            _world.Party.CanLoot = true;
            _world.Party.CanLoot.Should().BeTrue();

            _world.Party.CanLoot = false;
            _world.Party.CanLoot.Should().BeFalse();
        }

        [Fact]
        public void Clear_ResetsLeaderAndInviter()
        {
            _world.Party.Leader = 100u;
            _world.Party.Inviter = 200u;

            _world.Party.Clear();

            _world.Party.Leader.Should().Be(0u);
            _world.Party.Inviter.Should().Be(0u);
        }

        [Fact]
        public void Clear_NullsAllMembers()
        {
            _world.Party.Clear();

            foreach (var member in _world.Party.Members)
            {
                member.Should().BeNull();
            }
        }

        [Fact]
        public void PartyHealTimer_GetSet()
        {
            _world.Party.PartyHealTimer = 12345L;
            _world.Party.PartyHealTimer.Should().Be(12345L);
        }

        [Fact]
        public void PartyHealTarget_GetSet()
        {
            _world.Party.PartyHealTarget = 999u;
            _world.Party.PartyHealTarget.Should().Be(999u);
        }
    }
}
