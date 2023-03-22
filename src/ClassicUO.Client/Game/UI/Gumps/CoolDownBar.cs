using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class CoolDownBar : Gump
    {
        private const int COOL_DOWN_WIDTH = 180, COOL_DOWN_HEIGHT = 30;
        private static int DEFAULT_X { get { return ProfileManager.CurrentProfile.CoolDownX; } }
        private static int DEFAULT_Y { get { return ProfileManager.CurrentProfile.CoolDownY; } }

        private AlphaBlendControl background, foreground;
        private Label textLabel, cooldownLabel;
        private DateTime expire;
        private TimeSpan duration;

        public CoolDownBar(TimeSpan _duration, string _name, ushort _hue, int x, int y) : base(0, 0)
        {
            #region VARS
            Width = COOL_DOWN_WIDTH;
            Height = COOL_DOWN_HEIGHT;
            X = x;
            Y = y;
            expire = DateTime.Now + _duration;
            duration = _duration;
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;
            #endregion

            #region BACK/FORE GROUND
            background = new AlphaBlendControl(0.4f);
            background.Width = COOL_DOWN_WIDTH;
            background.Height = COOL_DOWN_HEIGHT;
            background.Hue = _hue;

            foreground = new AlphaBlendControl(0.8f);
            foreground.Width = COOL_DOWN_WIDTH;
            foreground.Height = COOL_DOWN_HEIGHT;
            foreground.Hue = _hue;
            #endregion

            #region LABELS
            textLabel = new Label(_name, true, _hue, COOL_DOWN_WIDTH, style: FontStyle.BlackBorder, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 0,
                Y = 0,
            };

            cooldownLabel = new Label(_duration.TotalSeconds.ToString(), true, _hue, COOL_DOWN_WIDTH, style: FontStyle.BlackBorder, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 0
            };
            cooldownLabel.Y = COOL_DOWN_HEIGHT - cooldownLabel.Height;
            #endregion

            #region ADD CONTROLS
            Add(background);
            Add(foreground);
            Add(textLabel);
            Add(cooldownLabel);
            #endregion
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            if (DateTime.Now >= expire)
                Dispose();

            TimeSpan remaing = expire - DateTime.Now;

            foreground.Width = (int)((remaing.TotalSeconds / duration.TotalSeconds) * COOL_DOWN_WIDTH);
            cooldownLabel.Text = ((int)remaing.TotalSeconds).ToString();

            base.Draw(batcher, x, y);

            batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(Color.Black),
                    x, y,
                    COOL_DOWN_WIDTH,
                    COOL_DOWN_HEIGHT,
                    ShaderHueTranslator.GetHueVector(background.Hue, false, 1f)
                );
            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.Black),
                x + 1, y + 1,
                COOL_DOWN_WIDTH - 2,
                COOL_DOWN_HEIGHT - 2,
                ShaderHueTranslator.GetHueVector(background.Hue, false, 1f)
            );

            return true;
        }

        public static class CoolDownBarManager
        {
            private const int MAX_COOLDOWN_BARS = 15;
            private static CoolDownBar[] coolDownBars = new CoolDownBar[MAX_COOLDOWN_BARS];

            public static void AddCoolDownBar(TimeSpan _duration, string _name, ushort _hue)
            {
                for (int i = 0; i < coolDownBars.Length; i++)
                {
                    if (coolDownBars[i] == null || coolDownBars[i].IsDisposed)
                    {
                        coolDownBars[i] = new CoolDownBar(_duration, _name, _hue, DEFAULT_X, DEFAULT_Y + (i * (COOL_DOWN_HEIGHT + 5)));
                        UIManager.Add(coolDownBars[i]);
                        return;
                    }
                }

            }
        }
    }
}
