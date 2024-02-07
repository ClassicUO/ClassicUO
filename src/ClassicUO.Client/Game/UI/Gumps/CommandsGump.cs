using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    public class CommandsGump : Gump
    {
        public CommandsGump() : base(0, 0)
        {
            X = 300;
            Y = 200;
            Width = 400;
            Height = 500;
            CanCloseWithRightClick = true;
            CanMove = true;

            BorderControl bc = new BorderControl(0, 0, Width, Height, 36);
            bc.T_Left = 39925;
            bc.H_Border = 39926;
            bc.T_Right = 39927;
            bc.V_Border = 39928;
            bc.V_Right_Border = 39930;
            bc.B_Left = 39931;
            bc.B_Right = 39933;
            bc.H_Bottom_Border = 39932;

            Add(new GumpPicTiled(39929) { X = bc.BorderSize, Y = bc.BorderSize, Width = Width - (bc.BorderSize * 2), Height = Height - (bc.BorderSize * 2) });

            Add(bc);

            TextBox t;
            Add(t = new TextBox(Language.Instance.CommandGump, TrueTypeLoader.EMBEDDED_FONT, 28, Width, Color.Gold, FontStashSharp.RichText.TextHorizontalAlignment.Center) { Y = 5 });

            ScrollArea scroll = new ScrollArea(10, 10 + t.Height, Width - 20, Height - t.Height - 40, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(new AlphaBlendControl(0.45f) { Width = scroll.Width, Height = scroll.Height, X = scroll.X, Y = scroll.Y });

            GenerateEntries(scroll);

            Add(scroll);
        }

        private void GenerateEntries(ScrollArea scroll)
        {
            int y = 0;
            foreach (var command in CommandManager.Commands)
            {
                TextBox t = new TextBox(command.Key, TrueTypeLoader.EMBEDDED_FONT, 18, scroll.Width, Color.White) { Y = y, AcceptMouseInput = false };
                scroll.Add(t);
                y += t.Height + 10;
            }
        }
    }
}
