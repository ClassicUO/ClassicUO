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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class ScissorControl : Control
    {
        public ScissorControl(bool enabled, int x, int y, int width, int height) : this(enabled)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public ScissorControl(bool enabled)
        {
            CanMove = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
            Alpha = 1.0f;
            WantUpdateSize = false;
            DoScissor = enabled;
        }

        public bool DoScissor;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (DoScissor)
            {
                ScissorStack.PushScissors(new Rectangle(x, y, Width, Height));
                batcher.EnableScissorTest(true);
            }
            else
            {
                batcher.EnableScissorTest(false);
                ScissorStack.PopScissors();
            }

            return true;
        }
    }
}
