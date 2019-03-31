using System;

using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
	internal class QuestArrowGump : Gump
	{
		private int _mx;
		private int _my;
		private Direction _direction;
		private GumpPic _arrow;
	    private float _timer;
	    private bool _needHue;

		public QuestArrowGump(Serial serial, int mx, int my) : base(serial, serial)
		{
			CanMove = false;
			CanCloseWithRightClick = false;
			
			AcceptMouseInput = true;

			SetRelativePosition(mx, my);
		}

	    public void SetRelativePosition(int x, int y)
	    {
	        _mx = x;
	        _my = y;
	    }

		public override void Update(double totalMS, double frameMS)
		{
			base.Update(totalMS, frameMS);

		    if (!World.InGame)
		    {
                Dispose();
		    }

		    var scene = Engine.SceneManager.GetScene<GameScene>();

            if (IsDisposed || Engine.Profile.Current == null || scene == null)
                return;

		    Direction dir = (Direction)GameCursor.GetMouseDirection(World.Player.X, World.Player.Y, _mx, _my, 0);
		    ushort gumpID = (ushort)(0x1194 + (((int)dir + 1) % 8));

		    if (_direction != dir || _arrow == null)
		    {
		        _direction = dir;

		        if (_arrow == null)
		        {
		            Add(_arrow = new GumpPic(0, 0, gumpID, 0));
		        }
		        else
		            _arrow.Graphic = gumpID;
		    }

		    int gox = _mx - World.Player.X;
		    int goy = _my - World.Player.Y;

		    int x = (Engine.Profile.Current.GameWindowPosition.X + Engine.Profile.Current.GameWindowSize.X / 2) + (((gox - goy) * 22) - (_arrow.Width / 1));
		    int y = (Engine.Profile.Current.GameWindowPosition.Y + Engine.Profile.Current.GameWindowSize.Y / 2) + (((gox + goy) * 22) + (_arrow.Height * 2));

		    if (x < Engine.Profile.Current.GameWindowPosition.X)
		        x = Engine.Profile.Current.GameWindowPosition.X;
		    else if (x > Engine.Profile.Current.GameWindowPosition.X + Engine.Profile.Current.GameWindowSize.X - _arrow.Width)
		        x = Engine.Profile.Current.GameWindowPosition.X + Engine.Profile.Current.GameWindowSize.X - _arrow.Width;


		    if (y < Engine.Profile.Current.GameWindowPosition.Y)
		        y = Engine.Profile.Current.GameWindowPosition.Y;
		    else if (y > Engine.Profile.Current.GameWindowPosition.Y + Engine.Profile.Current.GameWindowSize.Y - _arrow.Height)
		        y = Engine.Profile.Current.GameWindowPosition.Y + Engine.Profile.Current.GameWindowSize.Y - _arrow.Height;
		    var scale = scene.Scale;
            X = (int) (x / scale);
		    Y = (int) (y / scale);

		    if (_timer < Engine.Ticks)
		    {
		        _timer = Engine.Ticks + 1000;
		        _needHue = !_needHue;
		    }

		    _arrow.Hue = (Hue) (_needHue ? 0 : 0x21);
		}

	    protected override void OnMouseClick(int x, int y, MouseButton button)
		{
			var leftClick = button == MouseButton.Left;
			var rightClick = button == MouseButton.Right;

			if (leftClick || rightClick)
				GameActions.QuestArrow(rightClick);
		}

	    protected override bool Contains(int x, int y)
	    {
	        if (_arrow == null)
	            return true;
	        return _arrow.Texture.Contains(x, y);
	    }
	}
}
