#region license

// Copyright (c) 2024, andreakarasho
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

using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class QuestionGump : Gump
    {
        private readonly Action<bool> _result;

        public QuestionGump(World world, string message, Action<bool> result) : base(world, 0, 0)
        {
            CanCloseWithRightClick = true;
            Add(new GumpPic(0, 0, 0x0816, 0));

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x0816);

            Width = gumpInfo.UV.Width;
            Height = gumpInfo.UV.Height;

            Add(new Label(message, false, 0x0386, 165, font: 1) { X = 33, Y = 30 });

            Add(
                new Button((int)Buttons.Cancel, 0x817, 0x818, 0x0819)
                {
                    X = 37,
                    Y = 75,
                    ButtonAction = ButtonAction.Activate
                }
            );

            Add(
                new Button((int)Buttons.Ok, 0x81A, 0x81B, 0x081C)
                {
                    X = 100,
                    Y = 75,
                    ButtonAction = ButtonAction.Activate
                }
            );

            CanMove = false;
            IsModal = true;

            X = (Client.Game.Window.ClientBounds.Width - Width) >> 1;
            Y = (Client.Game.Window.ClientBounds.Height - Height) >> 1;

            WantUpdateSize = false;
            _result = result;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _result(false);
                    Dispose();

                    break;

                case 1:
                    _result(true);
                    Dispose();

                    break;
            }
        }

        private enum Buttons
        {
            Cancel,
            Ok
        }
    }
}
