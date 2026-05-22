// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.GameObjects
{
    // Regression: PlayerMobile ctor used to dereference
    // Client.Game.UO.FileManager.Skills unconditionally, which NREs in unit
    // tests where Client.Game is null. The ctor was changed to use a null-safe
    // path so PlayerMobile can be constructed in tests. This test pins that
    // behaviour.
    public class PlayerMobileConstructionTest
    {
        [Fact]
        public void Ctor_With_Null_ClientGame_Should_Not_Throw_And_Has_Empty_Skills()
        {
            var world = new World();

            var player = new PlayerMobile(world, 0x0000_1234u);

            player.Should().NotBeNull();
            player.Skills.Should().NotBeNull();
            player.Skills.Length.Should().Be(0);
            player.Walker.Should().NotBeNull();
            player.Pathfinder.Should().NotBeNull();
        }
    }
}
