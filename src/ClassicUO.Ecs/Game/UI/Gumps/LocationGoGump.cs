using System;
using System.Text.RegularExpressions;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps;

internal partial class LocationGoGump : Gump
{
    /**
     * Valid inputs:
     * 3123, 124
     * 123 4141
     * 1331:745 
     */
    [GeneratedRegex(@"^(?<X>\d+)\s*[,:\s]\s*(?<Y>\d+)$")]
    private static partial Regex PointCoordsRegex();
    
    private readonly World _world;
    private readonly Action<int, int> _goTo;

    private readonly StbTextBox _textBox;
    private readonly string _message = ResGumps.EnterLocation;
    private Point _location = Sextant.InvalidPoint;

    public LocationGoGump(World world, Action<int, int> goTo) : base(world, 0, 0)
    {
        _world = world;
        _goTo = goTo;
        CanMove = true;
        CanCloseWithRightClick = true;
        CanCloseWithEsc = true;
        AcceptMouseInput = false;

        IsModal = true;
        LayerOrder = UILayer.Over;
        WantUpdateSize = false;

        Width = 250;
        Height = 150;

        Add
        (
            new AlphaBlendControl(0.7f)
            {
                Width = Width,
                Height = Height,
                AcceptMouseInput = true,
                CanMove = true,
                Hue = 999
            }
        );
        
        Label l = Add
        (
            new Label(_message, true, 0xFFFF, Width - 90, 0xFF)
            {
                X = 12,
                Y = 12
            }
        );

        int ww = Width - 94;


        Add
        (
            new ResizePic(0x0BB8)
            {
                X = 20,
                Y = 20 + l.Height + 5,
                Width = ww + 10,
                Height = 25
            }
        );

        _textBox = Add
        (
            new StbTextBox(0xFF, -1, ww, true, FontStyle.BlackBorder, align: TEXT_ALIGN_TYPE.TS_LEFT)
            {
                X = 26,
                Y = 20 + l.Height + 7,
                Width = ww,
                Height = 25,
            }
        );

        _textBox.TextChanged += OnTextChange;


        // OK
        Button b = Add
        (
            new Button(0, 0x0481, 0x0482, 0x0483)
            {
                X = _textBox.X + _textBox.Width + 12,
                Y = _textBox.Y,
                ButtonAction = ButtonAction.Activate
            }
        );
        
        Add
        (
            new Label("Examples:\n 1639, 1532\n 100o25'S,40o04'E\n 9 14'N 91 37'W", true, 0xFFFF, Width - 90, 0xFF)
            {
                X = _textBox.X - 6,
                Y = _textBox.Y + 28,
            }
        );

        X = (Client.Game.Window.ClientBounds.Width - Width) >> 1;
        Y = (Client.Game.Window.ClientBounds.Height - Height) >> 1;
    }

    private bool ParsePoint(string text, out Point point)
    {
        point = new Point(-1, -1);
        try
        {
            var match = PointCoordsRegex().Match(text);

            if (!match.Success)
            {
                point = Sextant.InvalidPoint;
                return false;
            }

            point.X = int.Parse(match.Groups["X"].Value);
            point.Y = int.Parse(match.Groups["Y"].Value);
            return true;
        }
        catch(Exception e)
        {
            // do nothing
        }
        
        return false;
    }

    private bool Go()
    {
        if (_location == Sextant.InvalidPoint)
            return false;

        _goTo(_location.X, _location.Y);

        return true;
    }

    private void OnTextChange(object obj, EventArgs eventArgs)
    {
        var text = _textBox.Text;

        if (string.IsNullOrWhiteSpace(text))
            return;

        _textBox.Hue = (ushort)(Sextant.Parse(_world.Map, text, out _location) || ParsePoint(text, out _location) ? 0x40 : 0x33);
    }

    public override void OnButtonClick(int id)
    {
        switch (id)
        {
            case 0:
            {
                if (Go())
                    Dispose();

                break;
            }
        }
    }

    public override void OnKeyboardReturn(int id, string text)
    {
        switch (id)
        {
            case 0:
            {
                if (Go())
                    Dispose();

                break;
            }
        }
    }
}