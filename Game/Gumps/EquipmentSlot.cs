namespace ClassicUO.Game.Gumps
{
    internal class EquipmentSlot : GumpControl
    {
        private double _frameMS;

        public EquipmentSlot(int x, int y)
        {
            AcceptMouseInput = true;
            X = x;
            Y = y;
            AddChildren(new GumpPicTiled(0, 0, 19, 20, 0x243A));
            AddChildren(new GumpPic(0, 0, 0x2344, 0));
        }
    }
}