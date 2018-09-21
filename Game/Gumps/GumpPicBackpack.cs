using ClassicUO.Game.GameObjects;
using ClassicUO.Input;

namespace ClassicUO.Game.Gumps
{
    internal class GumpPicBackpack : GumpPic
    {
        public Item BackpackItem { get; protected set; }

        public GumpPicBackpack(int x, int y, Item backpack)
            : base(x, y, 0xC4F6, 0)
        {
            BackpackItem = backpack;
            AcceptMouseInput = false;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            base.OnMouseClick(x, y, button);
        }
    }
}