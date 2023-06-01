using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    internal class BoatControl : Gump
    {
        public BoatControl() : base(0, 0)
        {
            Width = 200;
            Height = 250;
            CanCloseWithRightClick = true;
            CanMove = true;

            BorderControl bc;
            Add(bc = new BorderControl(0, 0, Width, Height, 10));
            bc.T_Left = 9270;
            bc.H_Border = (ushort)(bc.T_Left + 1);
            bc.T_Right = (ushort)(bc.T_Left + 2);
            bc.V_Border = (ushort)(bc.T_Left + 3);

            bc.V_Right_Border = (ushort)(bc.T_Left + 5);
            bc.B_Left = (ushort)(bc.T_Left + 6);
            bc.H_Bottom_Border = (ushort)(bc.T_Left + 7);
            bc.B_Right = (ushort)(bc.T_Left + 8);

            Add(new GumpPicTiled((ushort)(bc.T_Left + 4)) { X = 10, Y = 10, Width = Width - 20, Height = Height - 20 });




            GumpPic _;

            Add(_ = new GumpPic(10, 10, 4507, 0));
            Add(_ = new GumpPic(75, 10, 4500, 0));
            Add(_ = new GumpPic(140, 10, 4501, 0));

            Add(_ = new GumpPic(10, 70, 4506, 0));
            //Add center B
            Add(_ = new GumpPic(140, 70, 4502, 0));

            Add(_ = new GumpPic(10, 140, 4505, 0));
            Add(_ = new GumpPic(75, 140, 4504, 0));
            Add(_ = new GumpPic(140, 140, 4503, 0));

        }
    }
}