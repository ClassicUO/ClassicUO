﻿#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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


    //    public override void Update()
    //    {
    //        base.Update();
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