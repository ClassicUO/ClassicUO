// SPDX-License-Identifier: BSD-2-Clause

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