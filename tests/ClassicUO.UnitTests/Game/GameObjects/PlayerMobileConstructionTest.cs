// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.GameObjects
{
    // Regression: PlayerMobile and Map ctors used to dereference
    // Client.Game.UO.FileManager.{Skills,Maps} unconditionally, which NREs in
    // unit tests where Client.Game is null. Both ctors were changed to use a
    // null-safe path so they can be constructed in tests, and World exposes
    // SetPlayerForTests / SetMapForTests internal seams that drive InGame.
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

        [Fact]
        public void SetPlayerForTests_And_SetMapForTests_Drive_InGame()
        {
            var world = new World();
            world.InGame.Should().BeFalse();

            world.SetPlayerForTests(new PlayerMobile(world, 0x0000_2222u));
            world.InGame.Should().BeFalse(); // Player set, Map still null

            world.SetMapForTests(new ClassicUO.Game.Map.Map(world, 0));
            world.InGame.Should().BeTrue();
        }
    }
}
