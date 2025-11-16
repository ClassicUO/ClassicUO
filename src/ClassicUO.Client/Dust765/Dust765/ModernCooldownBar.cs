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
using System.IO;
using System.Linq;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765.Dust765
{
    internal class ModernCooldownBar : Gump
    {
        private GumpPic _background;
        private Button _button;
        private ushort _graphic;
        private DataBox _box;

        public ModernCooldownBar() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            //
            if (ProfileManager.CurrentProfile.ModernCooldwonBar_locked)
            {
                CanMove = false;
                AcceptMouseInput = false;
                CanCloseWithRightClick = false;
            }
            //
        }

        public ModernCooldownBar(int x, int y) : this()
        {
            X = x;
            Y = y;

            //
            _graphic = 0x767;
            //


            SetInScreen();

            BuildGump();
        }

        public override GumpType GumpType => GumpType.ModernCooldownBar;

        private void BuildGump()
        {
            WantUpdateSize = true;

            _box?.Clear();
            _box?.Children.Clear();

            Clear();


            Add
            (
                _background = new GumpPic(0, 0, _graphic, 0)
                {
                    LocalSerial = 1
                }
            );
            
            //
            Add
            (
                _button = new Button(0, 0x837, 0x838, 0x838)
                {
                    ButtonAction = ButtonAction.Activate
                }
            );
            //

            Add
            (
                _box = new DataBox(0, 0, 0, 0)
                {
                    WantUpdateSize = true
                }
            );


            if (World.Player != null)
            {
                foreach (KeyValuePair<BuffIconType, BuffIcon> k in World.Player.BuffIcons)
                {
                    //
                    //check if we want to show it at all
                    if (BuffFilters.IsModernBuff(World.Player.BuffIcons[k.Key].Graphic))
                    {
                        //determine color
                        if (BuffFilters.IsBuff(World.Player.BuffIcons[k.Key].Graphic))
                        {
                            _box.Add(new BuffControlEntry(World.Player.BuffIcons[k.Key], 1, Time.Ticks));
                        }
                        else if (BuffFilters.IsDebuff(World.Player.BuffIcons[k.Key].Graphic))
                        {
                            _box.Add(new BuffControlEntry(World.Player.BuffIcons[k.Key], 2, Time.Ticks));
                        }
                        else if (BuffFilters.IsState(World.Player.BuffIcons[k.Key].Graphic))
                        {
                            _box.Add(new BuffControlEntry(World.Player.BuffIcons[k.Key], 3, Time.Ticks));
                        }
                        else
                        {
                            _box.Add(new BuffControlEntry(World.Player.BuffIcons[k.Key], 0, Time.Ticks));
                        }
                    }
                    //
                }
            }

            _background.Graphic = _graphic;
            _background.X = 0;
            _background.Y = 0;
            //
            if (ProfileManager.CurrentProfile.ModernCooldwonBar_locked)
            {
                _background.Hue = 0x26;
            };
            _button.X = 4;
            _button.Y = 4;
            //


            UpdateElements();
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("graphic", _graphic.ToString());
            writer.WriteAttributeString("name", "ToggleModernCooldownBar");
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            _graphic = ushort.Parse(xml.GetAttribute("graphic"));
            BuildGump();
        }

        protected override void UpdateContents()
        {
            BuildGump();
        }

        private void UpdateElements()
        {
            for (int i = 0, offset = 0; i < _box.Children.Count; i++, offset += 31)
            {
                Control e = _box.Children[i];

                //
                e.X = 17;
                e.Y = 18 + offset;
                //
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                //
                if (ProfileManager.CurrentProfile.ModernCooldwonBar_locked)
                {
                    ProfileManager.CurrentProfile.ModernCooldwonBar_locked = false;

                    CanMove = true;
                    AcceptMouseInput = true;
                    CanCloseWithRightClick = true;

                    _background.Hue = 0;
                }
                else
                {
                    ProfileManager.CurrentProfile.ModernCooldwonBar_locked = true;

                    CanMove = false;
                    AcceptMouseInput = false;
                    CanCloseWithRightClick = false;

                    _background.Hue = 0x26;
                }
                //
            }
        }

        private class LineCHB : Line
        {
            public LineCHB(int x, int y, int w, int h, uint color) : base
            (
                x,
                y,
                w,
                h,
                color
            )
            {
                LineWidth = w;

                LineColor = SolidColorTextureCache.GetTexture(new Color { PackedValue = color });

                CanMove = true;
                AcceptMouseInput = true;
            }
            public int LineWidth { get; set; }
            public Texture2D LineColor { get; set; }
        }

        private class BuffControlEntry : GumpPic
        {
            private byte _alpha;
            private bool _decreaseAlpha;
            private readonly RenderedText _gText;
            private float _updateTooltipTime;
            //
            private LineCHB _borderline, _bgline, _tickline, _sepline, _timeborderline, _timebgline;
            private readonly RenderedText _label;
            //

            public BuffControlEntry(BuffIcon icon, int type, uint starttime) : base(0, 0, icon.Graphic, 0)
            {
                if (IsDisposed)
                {
                    return;
                }

                Icon = icon;
                _alpha = 0xFF;
                _decreaseAlpha = true;

                //
                StartTime = starttime;

                _borderline = new LineCHB(0, 0, 122, 28, Color.Gray.PackedValue);
                Add(_borderline);

                //1 buff / 2 debuff / 3 state / 0 unknown
                switch (type)
                {
                    case 3:
                        _bgline = new LineCHB(0, 0, 120, 26, Color.DarkBlue.PackedValue);
                        Add(_bgline);

                        _tickline = new LineCHB(0, 0, 0, 26, Color.Blue.PackedValue);
                        Add(_tickline);

                        break;

                    case 2:
                        _bgline = new LineCHB(0, 0, 120, 26, Color.DarkRed.PackedValue);
                        Add(_bgline);

                        _tickline = new LineCHB(0, 0, 0, 26, Color.Red.PackedValue);
                        Add(_tickline);

                        break;

                    case 1:
                        _bgline = new LineCHB(0, 0, 120, 26, Color.DarkGreen.PackedValue);
                        Add(_bgline);

                        _tickline = new LineCHB(0, 0, 0, 26, Color.Green.PackedValue);
                        Add(_tickline);

                        break;

                    case 0:
                    default:
                        _bgline = new LineCHB(0, 0, 120, 26, Color.DarkGray.PackedValue);
                        Add(_bgline);

                        _tickline = new LineCHB(0, 0, 0, 26, Color.Gray.PackedValue);
                        Add(_tickline);

                        break;
                }

                _sepline = new LineCHB(0, 0, 0, 26, Color.White.PackedValue);
                Add(_sepline);

                _label = RenderedText.Create
                (
                    "",
                    0x35,
                    0xFF,
                    true,
                    FontStyle.BlackBorder,
                    TEXT_ALIGN_TYPE.TS_CENTER,
                    122
                );

                _timeborderline = new LineCHB(0, 0, 28, 28, Color.Gray.PackedValue);
                Add(_timeborderline);

                _timebgline = new LineCHB(0, 0, 26, 26, Color.Black.PackedValue);
                Add(_timebgline);
                //

                _gText = RenderedText.Create
                (
                    "",
                    0xFFFF,
                    2,
                    true,
                    FontStyle.Fixed | FontStyle.BlackBorder,
                    TEXT_ALIGN_TYPE.TS_CENTER,
                    Width
                );


                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
                
                //
                if (ProfileManager.CurrentProfile.ModernCooldwonBar_locked)
                {
                    CanMove = false;
                    AcceptMouseInput = false;
                    CanCloseWithRightClick = false;
                }
                //

                SetTooltip(icon.Text);
            }

            public BuffIcon Icon { get; }

            //
            public uint StartTime { get; }
            //


            public override void Update()
            {
                base.Update();

                if (!IsDisposed && Icon != null)
                {
                    //
                    _label.Text = Icon.Type.ToString();
                    //
                    int delta = (int) (Icon.Timer - Time.Ticks);

                    if (_updateTooltipTime < Time.Ticks && delta > 0)
                    {
                        TimeSpan span = TimeSpan.FromMilliseconds(delta);

                        SetTooltip
                        (
                            string.Format
                            (
                                ResGumps.TimeLeft,
                                Icon.Text,
                                span.Hours,
                                span.Minutes,
                                span.Seconds
                            )
                        );

                        _updateTooltipTime = (float) Time.Ticks + 1000;

                        //
                        int maxtime = (int) (Icon.Timer - StartTime);
                        int currpercentage = CalculatePercents(maxtime / 1000, delta / 1000, 120);

                        _tickline.Width = currpercentage;
                        _sepline.Width = 1;
                        _sepline.X = currpercentage;
                        //

                        if (span.Hours > 0)
                        {
                            _gText.Text = string.Format(ResGumps.Span0Hours, span.Hours);
                        }
                        else
                        {
                            _gText.Text = span.Minutes > 0 ? $"{span.Minutes}:{span.Seconds:00}" : $"{span.Seconds:00}s";
                        }
                    }

                    if (Icon.Timer != 0xFFFF_FFFF && delta < 10000)
                    {
                        if (delta <= 0)
                        {
                            ((ModernCooldownBar) Parent.Parent)?.RequestUpdateContents();
                        }
                        else
                        {
                            int alpha = _alpha;
                            int addVal = (10000 - delta) / 600;

                            if (_decreaseAlpha)
                            {
                                alpha -= addVal;

                                if (alpha <= 60)
                                {
                                    _decreaseAlpha = false;
                                    alpha = 60;
                                }
                            }
                            else
                            {
                                alpha += addVal;

                                if (alpha >= 255)
                                {
                                    _decreaseAlpha = true;
                                    alpha = 255;
                                }
                            }

                            _alpha = (byte) alpha;
                        }
                    }
                }
            }
            //
            protected static int CalculatePercents(int max, int current, int maxValue)
            {
                if (max > 0)
                {
                    max = current * 100 / max;

                    if (max > 100)
                    {
                        max = 100;
                    }

                    if (max > 1)
                    {
                        max = maxValue * max / 100;
                    }
                }

                return max;
            }
            //

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector
                                    (
                                        0,
                                        false,
                                        _alpha / 255f,
                                        true
                                    );

                var texture = GumpsLoader.Instance.GetGumpTexture(Graphic, out var bounds);

                if (texture != null)
                {
                    batcher.Draw
                    (
                        texture,
                        new Vector2(x, y),
                        bounds,
                        hueVector
                    );
                    //should there be the need to resize the pic, use this
                    //batcher.Draw(texture, new Rectangle(x, y, bounds.Width, bounds.Height), bounds, hueVector);
                    //

                    if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BuffBarTime)
                    {
                        //
                        if (_gText.Text != "")
                        {
                            _timeborderline.Draw(batcher, x + 154, y);
                            _timebgline.Draw(batcher, x + 155, y + 1);
                        }
                        _gText.Draw(batcher, x + 154, y + bounds.Height / 2 - 3, hueVector.Z);
                        //
                    }

                    //
                    _borderline.Draw(batcher, x + 30, y);
                    _bgline.Draw(batcher, x + 31, y + 1);
                    _tickline.Draw(batcher, x + 31, y + 1);
                    _sepline.Draw(batcher, x + 30 + this._sepline.X, y + 1);
                    _label.Draw(batcher, x + 31, y + bounds.Height / 2 - 4);
                    //


                }

                return true;
            }

            public override void Dispose()
            {
                _gText?.Destroy();
                //
                _timeborderline?.Dispose();
                _timebgline?.Dispose();
                _borderline?.Dispose();
                _bgline?.Dispose();
                _tickline?.Dispose();
                _sepline?.Dispose();
                _label?.Destroy();
                //
                base.Dispose();
            }
        }
    }
}