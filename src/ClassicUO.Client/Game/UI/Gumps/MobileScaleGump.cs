#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.

#endregion

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MobileScaleGump : Gump
    {
        private const int ButtonSize = 28;
        private const int LabelWidth = 44;
        private const int Padding = 4;
        private const int Step = 10;
        private const int MinScale = 50;
        private const int MaxScale = 150;

        private GothicStyleButton _btnMinus;
        private GothicStyleButton _btnPlus;
        private TextBox _label;
        private Mobile _lastMobile;
        private int _lastGraphic = -1;

        public MobileScaleGump() : base(0, 0)
        {
            CanMove = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            Width = ButtonSize + LabelWidth + ButtonSize + Padding * 2;
            Height = ButtonSize + Padding * 2;
            LayerOrder = UILayer.Over;

            Add(new RoundedColorBox(Width, Height, Color.FromNonPremultiplied(40, 20, 20, 230), 6) { X = 0, Y = 0, AcceptMouseInput = false });

            _btnMinus = new GothicStyleButton(Padding, Padding, ButtonSize, ButtonSize, "−", null, 18);
            _btnMinus.OnClick += OnMinus;
            Add(_btnMinus);

            _label = new TextBox("100%", TrueTypeLoader.EMBEDDED_FONT, 14, LabelWidth, Color.White, TextHorizontalAlignment.Center, false)
            {
                X = Padding + ButtonSize + Padding,
                Y = (Height - 20) / 2,
                AcceptMouseInput = false
            };
            Add(_label);

            _btnPlus = new GothicStyleButton(Padding + ButtonSize + Padding + LabelWidth, Padding, ButtonSize, ButtonSize, "+", null, 18);
            _btnPlus.OnClick += OnPlus;
            Add(_btnPlus);
        }

        public override void Update()
        {
            base.Update();

            if (!World.InGame || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.ScaleMonstersEnabled)
            {
                IsVisible = false;
                return;
            }

            if (SelectedObject.Object is Mobile m && !m.IsHuman && !m.IsDestroyed)
            {
                var camera = Client.Game.Scene.Camera;
                int gx = camera.Bounds.X + m.RealScreenPosition.X + 22 + (int)m.Offset.X;
                int gy = camera.Bounds.Y + m.RealScreenPosition.Y + (int)(m.Offset.Y - m.Offset.Z) - Height - 8;

                X = gx - (Width >> 1);
                Y = Math.Max(0, gy);

                if (m != _lastMobile || m.Graphic != _lastGraphic)
                {
                    _lastMobile = m;
                    _lastGraphic = m.Graphic;
                    RefreshLabel(m.Graphic);
                }
                IsVisible = true;
            }
            else
            {
                IsVisible = false;
            }
        }

        private void RefreshLabel(int graphic)
        {
            var dict = ProfileManager.CurrentProfile.MonsterScaleByGraphic;
            int scale = dict != null && dict.TryGetValue(graphic, out int s) ? s : 100;
            _label.Text = scale + "%";
        }

        private void OnMinus()
        {
            if (SelectedObject.Object is Mobile m && !m.IsHuman && ProfileManager.CurrentProfile != null)
            {
                var dict = ProfileManager.CurrentProfile.MonsterScaleByGraphic;
                if (dict == null)
                    ProfileManager.CurrentProfile.MonsterScaleByGraphic = dict = new System.Collections.Generic.Dictionary<int, int>();
                int graphic = m.Graphic;
                int current = dict.TryGetValue(graphic, out int s) ? s : 100;
                dict[graphic] = Math.Max(MinScale, current - Step);
                RefreshLabel(graphic);
            }
        }

        private void OnPlus()
        {
            if (SelectedObject.Object is Mobile m && !m.IsHuman && ProfileManager.CurrentProfile != null)
            {
                var dict = ProfileManager.CurrentProfile.MonsterScaleByGraphic;
                if (dict == null)
                    ProfileManager.CurrentProfile.MonsterScaleByGraphic = dict = new System.Collections.Generic.Dictionary<int, int>();
                int graphic = m.Graphic;
                int current = dict.TryGetValue(graphic, out int s) ? s : 100;
                dict[graphic] = Math.Min(MaxScale, current + Step);
                RefreshLabel(graphic);
            }
        }
    }
}
