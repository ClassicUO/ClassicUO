using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
	internal class QuestArrowGump : Gump
	{
		public QuestArrowGump(Serial serial, int mx, int my) : base(serial, serial)
		{
			CanMove = false;
			CanCloseWithRightClick = false;
			
			AcceptMouseInput = true;

			var screenLeft = Engine.Profile.Current.GameWindowPosition.X;
			var screenTop = Engine.Profile.Current.GameWindowPosition.Y;

			int screenRight = screenLeft + Engine.Profile.Current.GameWindowSize.X;
			int screenBottom = screenTop + Engine.Profile.Current.GameWindowSize.Y;

			int screenCenterX = (screenRight - screenLeft) / 2;
			int screenCenterY = (screenBottom - screenTop) / 2;

			int playerX = World.Player.X;
			int playerY = World.Player.Y;

			int offsetX = mx - playerX;
			int offsetY = my - playerY;

			int drawX = screenLeft + (int)(screenCenterX + (offsetX - offsetY) * 22) + 3;
			int drawY = screenTop + (int)(screenCenterY + (offsetX + offsetY) * 22) + 3;

			var direction = DirectionHelper.DirectionFromPoints(
				new Point(screenCenterX, screenCenterY), 
				new Point(drawX, drawY));

			var graphic = (uint)0x1194;

			if (direction >= Direction.North && direction <= Direction.West)
				graphic = 0x1195 + (uint)direction;

			AddChildren(new GumpPic(0, 0, (Graphic)graphic, 0));

			var size = FileManager.Gumps.GetTexture(graphic).Bounds;

			drawX -= (size.Width / 2);
			drawY -= (size.Height / 2);

			var xOffsetTable = new[] { -0.5, -0.75, -0.5,  0.0,  0.5, 0.75, 0.5, 0.0 };
			var yOffsetTable = new[] {  0.5,  0.0, -0.5, -0.75, -0.5, 0.0, 0.5, 0.75 };

			var directionint = (int)direction;

			drawX += (int)(xOffsetTable[(int)direction] * (size.Width + 22));
			drawY += (int)(yOffsetTable[(int)direction] * (size.Height + 22));

			if (drawX < screenLeft) drawX = screenLeft;
			if (drawY < screenTop) drawY = screenTop;

			if (drawX + size.Width > screenRight)
				drawX = (screenRight - size.Width);

			if (drawY + size.Height > screenBottom)
				drawY = (screenBottom - size.Height);

			X = drawX;
			Y = drawY;
		}

		protected override void OnMouseClick(int x, int y, MouseButton button)
		{
			var leftClick = button == MouseButton.Left;
			var rightClick = button == MouseButton.Right;

			if (leftClick || rightClick)
				GameActions.QuestArrow(rightClick);
		}
	}
}
