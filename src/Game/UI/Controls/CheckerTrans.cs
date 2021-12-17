#region license

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

using System;
using System.Collections.Generic;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class CheckerTrans : Control
    {
        //TODO(deccer): should be moved into Renderer namespace
        private static readonly Lazy<DepthStencilState> _checkerStencil = new Lazy<DepthStencilState>
        (
            () =>
            {
                DepthStencilState depthStencilState = new DepthStencilState
                {
                    DepthBufferEnable = false,
                    StencilEnable = true,
                    StencilFunction = CompareFunction.Always,
                    ReferenceStencil = 1,
                    StencilMask = 1,
                    StencilFail = StencilOperation.Keep,
                    StencilDepthBufferFail = StencilOperation.Keep,
                    StencilPass = StencilOperation.Replace
                };


                return depthStencilState;
            }
        );


        //TODO(deccer): should be moved into Renderer namespace
        private static readonly Lazy<BlendState> _checkerBlend = new Lazy<BlendState>
        (
            () =>
            {
                BlendState blendState = new BlendState
                {
                    ColorWriteChannels = ColorWriteChannels.None
                };

                return blendState;
            }
        );

        //public CheckerTrans(float alpha = 0.5f)
        //{
        //    _alpha = alpha;
        //    AcceptMouseInput = false;
        //}

        public CheckerTrans(List<string> parts)
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            AcceptMouseInput = false;
            IsFromServer = true;
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            //batcher.SetBlendState(_checkerBlend.Value);
            //batcher.SetStencil(_checkerStencil.Value);

            //batcher.Draw2D(TransparentTexture, new Rectangle(position.X, position.Y, Width, Height), Vector3.Zero /*ShaderHueTranslator.GetHueVector(0, false, 0.5f, false)*/);

            //batcher.SetBlendState(null);
            //batcher.SetStencil(null);

            //return true;

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 0.5f);

            //batcher.SetStencil(_checkerStencil.Value);
            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(Color.Black),
                new Rectangle
                (
                    x,
                    y,
                    Width,
                    Height
                ),
                hueVector
            );

            //batcher.SetStencil(null);
            return true;
        }
    }
}