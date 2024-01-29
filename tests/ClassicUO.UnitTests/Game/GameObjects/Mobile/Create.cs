using ClassicUO.Game;
using Xunit;

namespace ClassicUO.UnitTests.Game.GameObjects.Mobile
{
    public class Create
    {
        [Fact]
        public void Create_Returns_Mobile_Instance()
        {
            var world = new World();
            Assert.IsType<ClassicUO.Game.GameObjects.Mobile>( ClassicUO.Game.GameObjects.Mobile.Create(world, 0));
            world.Clear();
        }
    }
}
