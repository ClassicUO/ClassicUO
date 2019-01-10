using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps
{
	internal class ProfileGump : Gump
	{
		private ExpandableScroll _background;
		private ScrollFlag _scrollBar;
		private TextBox _textBox;

		public ProfileGump(Serial serial, string header, string footer, string body, bool canEdit) : base(serial, serial)
		{
			CanMove = true;

			AcceptKeyboardInput = true;
			AcceptMouseInput = true;

			AddChildren(_background = new ExpandableScroll(0, 0, 300)
			{
			});
			_scrollBar = new ScrollFlag(this, 0, 0, Height);

			AddHorizontalBar(92, 40, 40, 220);
			AddChildren(new Label(header, true, 0, font: 1, maxwidth: 140)
			{
				X = 90, Y = 32,
			});

			AddChildren(_textBox = new TextBox(new TextEntry(1, width: 220, maxWidth: 220, hue: 0), canEdit)
			{
				X = 40, Y = 82,
				MultiLineInputAllowed = true,
				Text = body,
			});

			/*
			AddHorizontalBar(95, 40, _textBox.Y + _textBox.Height, 220);
			AddChildren(new Label(footer, true, 0, font: 1, maxwidth: 220)
			{
				X = 40, Y = _textBox.Y + _textBox.Height + 20,
			});
			*/
		}

		protected override void OnMouseWheel(MouseEvent delta)
		{
			switch (delta)
			{
				case MouseEvent.WheelScrollUp:
					_scrollBar.Value -= 5;

					break;
				case MouseEvent.WheelScrollDown:
					_scrollBar.Value += 5;

					break;
			}
		}

		public override void Update(double totalMS, double frameMS)
		{
			WantUpdateSize = true;

			_textBox.Height = Height - 98;

			base.Update(totalMS, frameMS);
		}

		private void AddHorizontalBar(Graphic start, int x, int y, int width)
		{
			var startBounds = FileManager.Gumps.GetTexture(start);
			var middleBounds = FileManager.Gumps.GetTexture((Graphic)(start + 1));
			var endBounds = FileManager.Gumps.GetTexture((Graphic)(start + 2));

			AddChildren(new GumpPic(x, y, start, 0));
			AddChildren(new GumpPicWithWidth(x + startBounds.Width, y, (Graphic)(start + 1), 0, 
				width - startBounds.Width - endBounds.Width));
			AddChildren(new GumpPic(x + width - endBounds.Width, y, (Graphic)(start + 2), 0));
		}
	}
}
