using System;

using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
	internal class QuestArrowGump : Gump
	{
		private static double[] _offsetTableX = new[] { -0.5, -0.75, -0.5, 0.0, 0.5, 0.75, 0.5, 0.0 };
		private static double[] _offsetTableY = new[] { 0.5, 0.0, -0.5, -0.75, -0.5, 0.0, 0.5, 0.75 };

		private int _mx;
		private int _my;

		private Direction _direction;

		private GumpPic _arrow;
		private Rectangle _arrowBounds;

		public QuestArrowGump(Serial serial, int mx, int my) : base(serial, serial)
		{
			CanMove = false;
			CanCloseWithRightClick = false;
			
			AcceptMouseInput = true;

			_mx = mx;
			_my = my;
		}

		private void UpdateArrow(Direction direction)
		{
			_direction = direction;

			if (_arrow != null)
				Remove(_arrow);

			var graphic = (uint)0x1194;

			if (_direction >= Direction.North && _direction <= Direction.West)
				graphic = 0x1195 + (uint)_direction;

			_arrowBounds = FileManager.Gumps.GetTexture(graphic).Bounds;

			Add(_arrow = new GumpPic(0, 0, (Graphic)graphic, 0));
		}

		public override void Update(double totalMS, double frameMS)
		{
			base.Update(totalMS, frameMS);

			var scale = Engine.SceneManager.GetScene<GameScene>().Scale;
			var viewport = var scale = Engine.SceneManager.GetScene<GameScene>().Scale;

			var screenLeft = Engine.Profile.Current.GameWindowPosition.X;
			var screenTop = Engine.Profile.Current.GameWindowPosition.Y;
			int screenRight = screenLeft + Engine.Profile.Current.GameWindowSize.X;
			int screenBottom = screenTop + Engine.Profile.Current.GameWindowSize.Y;

			

			var screenCenterX = ((screenRight - screenLeft) / 2);
			var screenCenterY = ((screenBottom - screenTop) / 2);

			var offsetX = ((_mx - World.Player.X) / scale);
			var offsetY = ((_my - World.Player.Y) / scale);
			var offsetZ = World.Map.GetTileZ(_mx, _my);

			int drawX = screenLeft + (int)((screenCenterX + (offsetX - offsetY) * 22));
			int drawY = screenTop + (int)((screenCenterY + (offsetX + offsetY) * 22));

			var direction = DirectionHelper.DirectionFromPoints(
				new Point(screenCenterX, screenCenterY),
				new Point(drawX, drawY));

			if (_direction != direction || _arrow == null)
				UpdateArrow(direction);

			var arrowWidth = _arrowBounds.Width;
			var arrowHeight = _arrowBounds.Height;

			drawX += (int)((_offsetTableX[(int)direction] * (arrowWidth + 22)) - (arrowWidth / 2));
			drawY += (int)((_offsetTableY[(int)direction] * (arrowHeight + 22)) - (arrowHeight / 2));
			drawY -= (int)(offsetZ * 3);

			if (drawX < screenLeft) drawX = screenLeft;
			if (drawY < screenTop) drawY = screenTop;

			if (drawX + arrowWidth > screenRight) drawX = (screenRight - arrowWidth);
			if (drawY + arrowHeight > screenBottom) drawY = (screenBottom - arrowHeight);

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
