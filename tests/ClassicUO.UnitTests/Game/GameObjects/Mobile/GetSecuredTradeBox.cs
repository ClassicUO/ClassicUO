using ClassicUO.Game.GameObjects;
using Xunit;

namespace ClassicUO.UnitTests.Game.GameObjects.Mobile
{
    public class GetSecuredTradeBox
    {
        [Fact]
        public void GetSecureTradeBox_Returns_Null_Without_Items()
        {
            var sut = new ClassicUO.Game.GameObjects.Mobile();

            Assert.Null(sut.GetSecureTradeBox());
        }

        [Fact]
        public void GetSecureTradeBox_Returns_Null_With_Irrelevant_Items()
        {
            Item irrelevantItem1 = new Item();
            Item irrelevantItem2 = new Item();

            var sut = new ClassicUO.Game.GameObjects.Mobile();

            sut.Insert(sut.Items, irrelevantItem1);
            sut.Insert(sut.Items, irrelevantItem2);

            Assert.Null(sut.GetSecureTradeBox());
        }

        [Fact]
        public void GetSecureTradeBox_Returns_Only_Relevant_Item()
        {
            Item irrelevantItem1 = new Item();
            Item irrelevantItem2 = new Item();

            Item relevantItem = new Item
            {
                Graphic = 0x1E5E,
                Layer = 0
            };

            var sut = new ClassicUO.Game.GameObjects.Mobile();

            sut.Insert(sut.Items, irrelevantItem1);
            sut.Insert(sut.Items, relevantItem);
            sut.Insert(sut.Items, irrelevantItem2);

            Assert.Same(relevantItem, sut.GetSecureTradeBox());
        }
    }
}
