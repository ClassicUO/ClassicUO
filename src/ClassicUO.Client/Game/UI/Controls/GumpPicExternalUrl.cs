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

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Controls
{
    public class GumpPicExternalUrl : Control
    {
        private Vector3 hueVector;

        public GumpPicExternalUrl(int x, int y, string imgUrl, ushort hue, int width, int height, bool resize = false)
        {
            Width = width;
            Height = height;
            X = x; Y = y;
            ImgUrl = imgUrl;
            Hue = hue;
            Resize = resize;
            getImageTexture();
            AcceptMouseInput = true;
            CanMove = true;
            hueVector = ShaderHueTranslator.GetHueVector(Hue);
        }

        public string ImgUrl { get; }
        public ushort Hue { get; }
        public bool Resize { get; }
        public Texture2D imageTexture { get; private set; }

        private void getImageTexture()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    using (Stream stream = httpClient.GetStreamAsync(ImgUrl).Result)
                    {
                        using (System.Drawing.Image image = System.Drawing.Image.FromStream(stream))
                        {
                            Console.WriteLine($"Image size {image.Width} x {image.Height}");

                            var memStream = new MemoryStream();
                            image.Save(memStream, System.Drawing.Imaging.ImageFormat.Png);

                            using (memStream)
                            {
                                var t = Texture2D.FromStream(Client.Game.GraphicsDevice, memStream);
                                //Color[] buffer = new Color[t.Width * t.Height];
                                //t.GetData(buffer);
                                //for (int i = 0; i < buffer.Length; i++)
                                //    buffer[i] = Color.FromNonPremultiplied(buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A);
                                //t.SetData(buffer);
                                imageTexture = t;
                            }
                        }

                    }
                }
                catch { }
            });
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (imageTexture != null)
            {
                if (!Resize)
                    batcher.DrawTiled(imageTexture, new Rectangle(x, y, Width, Height), imageTexture.Bounds, hueVector);
                else
                    batcher.Draw(imageTexture, new Rectangle(x, y, Width, Height), imageTexture.Bounds, hueVector);
            }

            return base.Draw(batcher, x, y); ;
        }
    }
}