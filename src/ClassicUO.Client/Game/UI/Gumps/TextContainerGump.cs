#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal abstract class TextContainerGump : Gump
    {
        protected TextContainerGump(World world, uint local, uint server) : base(world, local, server)
        {
            TextRenderer = new TextRenderer(World);
        }

        public TextRenderer TextRenderer { get; }

        public void AddText(TextObject msg)
        {
            if (msg == null)
            {
                return;
            }

            msg.Time = Time.Ticks + 4000;

            TextRenderer.AddMessage(msg);
        }

        public override void Update()
        {
            base.Update();
            TextRenderer.Update();
        }

        public override void Dispose()
        {
            TextRenderer.UnlinkD();

            //TextRenderer.Clear();
            base.Dispose();
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            //TextRenderer.MoveToTopIfSelected();
            TextRenderer.ProcessWorldText(true);

            TextRenderer.Draw
            (
                batcher,
                x,
                y,
                true
            );

            return true;
        }
    }
}