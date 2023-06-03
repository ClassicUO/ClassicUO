using ClassicUO.Game.UI.Controls;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;

namespace ClassicUO.Game.UI.Gumps
{
    internal class BoatControl : Gump
    {
        Checkbox reg, slow, one;

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

            Add(_ = new GumpPic(10, 10, 4507, 0)); //West
            Add(_ = new GumpPic(75, 10, 4500, 0)); //NW
            Add(_ = new GumpPic(140, 10, 4501, 0)); //North

            Add(_ = new GumpPic(10, 70, 4506, 0)); //SW

            Add(_ = new GumpPic((Width / 2) - (29 / 2), 90, 5830, 0)); //Center stop button

            Add(_ = new GumpPic(140, 70, 4502, 0)); //NE

            Add(_ = new GumpPic(10, 140, 4505, 0)); //South
            Add(_ = new GumpPic(75, 140, 4504, 0)); //SE
            Add(_ = new GumpPic(140, 140, 4503, 0)); // East


            Add(_ = new GumpPic(10, 190, 22406, 0)); //Rotate Clockwise
            Add(_ = new GumpPic(Width - 10 - 19, 190, 22400, 0)); //Rotate counter-clockwise

            Add(reg = new Checkbox
            (
                0x00D2,
                0x00D3,
                "Reg",
                0xff,
                0xffff
            )
            {
                IsChecked = true,
                X = 10,
                Y = Height - 30
            });
            reg.MouseUp += Reg_MouseUp;

            Add(slow = new Checkbox
            (
                0x00D2,
                0x00D3,
                "Slow",
                0xff,
                0xffff
            )
            {
                IsChecked = false,
                Y = Height - 30
            });
            slow.X = (Width / 2) - (slow.Width / 2);
            slow.MouseUp += Reg_MouseUp;

            Add(one = new Checkbox
            (
                0x00D2,
                0x00D3,
                "One",
                0xff,
                0xffff
            )
            {
                IsChecked = false,
                Y = Height - 30
            });
            one.X = Width - one.Width - 10;
            one.MouseUp += Reg_MouseUp;
        }

        private void Reg_MouseUp(object sender, Input.MouseEventArgs e)
        {
            if (sender is Checkbox)
            {
                if ((Checkbox)sender == reg)
                {
                    slow.IsChecked = false;
                    one.IsChecked = false;
                }
                if ((Checkbox)sender == slow)
                {
                    reg.IsChecked = false;
                    one.IsChecked = false;
                }
                if ((Checkbox)sender == one)
                {
                    reg.IsChecked = false;
                    slow.IsChecked = false;
                }
            }
        }
    }
}