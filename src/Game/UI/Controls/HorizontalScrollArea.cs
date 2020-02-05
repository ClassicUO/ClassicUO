#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

namespace ClassicUO.Game.UI.Controls
{
    //class HorizontalScrollArea : Control
    //{
    //    private Rectangle _rect;

    //    public HorizontalScrollArea(int x, int y, int w, int h)
    //    {
    //        X = x;
    //        Y = y;
    //        Width = w;
    //        Height = h;

    //        AcceptMouseInput = true;
    //        WantUpdateSize = false;
    //        CanMove = true;


    //        HSliderBar bar = new HSliderBar(0, 0, w, 0, 100, 0, HSliderBarStyle.BlueWidgetNoBar)
    //        {
    //            Parent = this
    //        };
    //    }


    //    public override void Update(double totalMS, double frameMS)
    //    {
    //        base.Update(totalMS, frameMS);
    //    }

    //    public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
    //    {
    //        Children[0].Draw(batcher, new Point(position.X + Children[0].X, position.Y + Children[0].Y));
    //        _rect.X = position.X;
    //        _rect.Y = position.Y;
    //        _rect.Width = Width;
    //        _rect.Height = Height;

    //        Rectangle scissor = ScissorStack.CalculateScissors(batcher.TransformMatrix, _rect);

    //        if (ScissorStack.PushScissors(scissor))
    //        {
    //            batcher.EnableScissorTest(true);

    //            int width = 0;
    //            int maxWidth = 


    //            batcher.EnableScissorTest(false);
    //            ScissorStack.PopScissors();
    //        }

    //        return true;
    //    }
    //}
}