using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using Xunit;

namespace ClassicUO.UnitTests.Game.GameObjects.Mobile
{
    public class GetSecuredTradeBox
    {
        [Fact]
        public void GetSecureTradeBox_Returns_Null_Without_Items()
        {
            var world = new World();
            var sut = new ClassicUO.Game.GameObjects.Mobile(world);

            Assert.Null(sut.GetSecureTradeBox());

            world.Clear();
        }

        [Fact]
        public void GetSecureTradeBox_Returns_Null_With_Irrelevant_Items()
        {
            var world = new World();

            var irrelevantItem1 = new Item(world);
            var irrelevantItem2 = new Item(world);

            var sut = new ClassicUO.Game.GameObjects.Mobile(world);

            sut.Insert(sut.Items, irrelevantItem1);
            sut.Insert(sut.Items, irrelevantItem2);

            Assert.Null(sut.GetSecureTradeBox());

            world.Clear();
        }

        [Fact]
        public void GetSecureTradeBox_Returns_Only_Relevant_Item()
        {
            var world = new World();

            var irrelevantItem1 = new Item(world);
            var irrelevantItem2 = new Item(world);

            var relevantItem = new Item(world)
            {
                Graphic = 0x1E5E,
                Layer = 0
            };

            var sut = new ClassicUO.Game.GameObjects.Mobile(world);

            sut.Insert(sut.Items, irrelevantItem1);
            sut.Insert(sut.Items, relevantItem);
            sut.Insert(sut.Items, irrelevantItem2);

            Assert.Same(relevantItem, sut.GetSecureTradeBox());

            world.Clear();
        }
    }
}
