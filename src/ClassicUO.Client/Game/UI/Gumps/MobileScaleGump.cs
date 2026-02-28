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
using ClassicUO.Game;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MobileScaleGump : Gump
    {
        private const int ButtonSize = 14;
        private const int Padding = 2;
        private const int Step = 10;
        private const int MinScale = 50;
        private const int MaxScale = 150;
        private const int BelowMobileOffset = 22;

        private GothicStyleButton _btnMinus;
        private GothicStyleButton _btnPlus;
        private Mobile _displayMobile;

        public MobileScaleGump() : base(0, 0)
        {
            CanMove = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            Width = ButtonSize + Padding + ButtonSize + Padding * 2;
            Height = ButtonSize + Padding * 2;
            LayerOrder = UILayer.Over;

            Add(new RoundedColorBox(Width, Height, Color.FromNonPremultiplied(60, 60, 60, 200), 3) { X = 0, Y = 0, AcceptMouseInput = false });

            _btnMinus = new GothicStyleButton(Padding, Padding, ButtonSize, ButtonSize, "−");
            _btnMinus.BaseColor = new Color(90, 90, 90);
            _btnMinus.HighlightColor = new Color(120, 120, 120);
            _btnMinus.ShadowColor = new Color(50, 50, 50);
            _btnMinus.TextColor = new Color(180, 180, 180);
            _btnMinus.OnClick += OnMinus;
            Add(_btnMinus);

            _btnPlus = new GothicStyleButton(Padding + ButtonSize + Padding, Padding, ButtonSize, ButtonSize, "+");
            _btnPlus.BaseColor = new Color(90, 90, 90);
            _btnPlus.HighlightColor = new Color(120, 120, 120);
            _btnPlus.ShadowColor = new Color(50, 50, 50);
            _btnPlus.TextColor = new Color(180, 180, 180);
            _btnPlus.OnClick += OnPlus;
            Add(_btnPlus);
        }

        private bool IsMouseOverGump =>
            UIManager.MouseOverControl != null
            && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this);

        public override void Update()
        {
            base.Update();

            if (!World.InGame || ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.ScaleMonstersEnabled)
            {
                IsVisible = false;
                _displayMobile = null;
                return;
            }

            bool ctrlShift = Keyboard.Ctrl && Keyboard.Shift;
            Mobile currentMobile = null;
            if (ctrlShift)
            {
                if (SelectedObject.Object is Mobile m && !m.IsHuman && !m.IsDestroyed)
                    currentMobile = m;
                if (currentMobile == null && UIManager.MouseOverControl != null)
                {
                    var root = UIManager.MouseOverControl.RootParent ?? UIManager.MouseOverControl;
                    if (root is NameOverheadGump nohg && World.Get(nohg.LocalSerial) is Mobile nm && !nm.IsHuman && !nm.IsDestroyed)
                        currentMobile = nm;
                }
                if (currentMobile == null)
                {
                    var camera = Client.Game.Scene.Camera;
                    int cx = camera.Bounds.X + camera.Bounds.Width / 2;
                    int cy = camera.Bounds.Y + camera.Bounds.Height / 2;
                    int bestDist = int.MaxValue;
                    foreach (Mobile mob in World.Mobiles.Values)
                    {
                        if (mob.IsHuman || mob.IsDestroyed || !mob.AllowedToDraw)
                            continue;
                        int gx = camera.Bounds.X + mob.RealScreenPosition.X + 22 + (int)mob.Offset.X;
                        int gy = camera.Bounds.Y + mob.RealScreenPosition.Y + (int)(mob.Offset.Y - mob.Offset.Z);
                        if (gx < camera.Bounds.X - 50 || gx > camera.Bounds.Right + 50 || gy < camera.Bounds.Y - 50 || gy > camera.Bounds.Bottom + 50)
                            continue;
                        int dx = gx - cx;
                        int dy = gy - cy;
                        int d = dx * dx + dy * dy;
                        if (d < bestDist)
                        {
                            bestDist = d;
                            currentMobile = mob;
                        }
                    }
                }
            }

            if (currentMobile != null)
                _displayMobile = currentMobile;
            else if (!IsMouseOverGump)
                _displayMobile = null;
            if (ctrlShift && _displayMobile != null && !_displayMobile.IsDestroyed)
            {
                var camera = Client.Game.Scene.Camera;
                int gx = camera.Bounds.X + _displayMobile.RealScreenPosition.X + 22 + (int)_displayMobile.Offset.X;
                int gy = camera.Bounds.Y + _displayMobile.RealScreenPosition.Y + (int)(_displayMobile.Offset.Y - _displayMobile.Offset.Z) + BelowMobileOffset;

                X = gx - (Width >> 1);
                Y = gy;
                IsVisible = true;
            }
            else
            {
                IsVisible = false;
            }
        }

        private void ApplyScale(int delta)
        {
            if (_displayMobile == null || _displayMobile.IsDestroyed || ProfileManager.CurrentProfile == null)
                return;

            var dict = ProfileManager.CurrentProfile.MonsterScaleByGraphic;
            if (dict == null)
                ProfileManager.CurrentProfile.MonsterScaleByGraphic = dict = new System.Collections.Generic.Dictionary<int, int>();
            int graphic = _displayMobile.Graphic;
            int current = dict.TryGetValue(graphic, out int s) ? s : 100;
            dict[graphic] = Math.Max(MinScale, Math.Min(MaxScale, current + delta));
        }

        private void OnMinus()
        {
            ApplyScale(-Step);
        }

        private void OnPlus()
        {
            ApplyScale(Step);
        }
    }
}
