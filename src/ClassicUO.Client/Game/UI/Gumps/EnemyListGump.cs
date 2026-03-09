#region license
// Copyright (c) 2021, andreakarasho
// All rights reserved.
#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.Gumps
{
    internal sealed class EnemyListGump : Gump
    {
        private const int ENEMY_RANGE = 15;
        private const int ROW_HEIGHT = 22;
        private const int WIDTH = 220;
        private const int HEIGHT = 280;

        private ScrollArea _scrollArea;
        private readonly List<EnemyRowControl> _rows = new List<EnemyRowControl>();
        private uint _rebuildTicks;

        public EnemyListGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            Add(new AlphaBlendControl(0.92f)
            {
                X = 0,
                Y = 0,
                Width = WIDTH,
                Height = HEIGHT,
                AcceptMouseInput = true,
                CanMove = true
            });

            Add(new Label("Enemies", true, 0xFFFF, 200) { X = 8, Y = 6 });
            _scrollArea = new ScrollArea(8, 28, WIDTH - 16, HEIGHT - 36, true);
            Add(_scrollArea);
            WantUpdateSize = false;
            BuildList();
        }

        private void BuildList()
        {
            _scrollArea.Clear();
            _rows.Clear();
            if (!World.InGame || World.Player == null || ProfileManager.CurrentProfile?.PvP_QuickTargetEnemyList != true)
                return;
            int y = 0;
            foreach (Mobile m in World.Mobiles.Values.OrderBy(m => m.Distance))
            {
                if (m.IsDestroyed || m == World.Player || m.Distance > ENEMY_RANGE) continue;
                if (m.NotorietyFlag != NotorietyFlag.Criminal && m.NotorietyFlag != NotorietyFlag.Gray
                    && m.NotorietyFlag != NotorietyFlag.Enemy && m.NotorietyFlag != NotorietyFlag.Murderer)
                    continue;
                var row = new EnemyRowControl(m) { Y = y };
                row.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        TargetManager.Target(m.Serial);
                        if (World.Player.InWarMode)
                            GameActions.Attack(m.Serial);
                    }
                };
                _scrollArea.Add(row);
                _rows.Add(row);
                y += ROW_HEIGHT;
            }
        }

        public override void Update()
        {
            base.Update();
            if (IsDisposed) return;
            if (ProfileManager.CurrentProfile?.PvP_QuickTargetEnemyList != true)
            {
                Dispose();
                return;
            }
            if ((int)(Time.Ticks - _rebuildTicks) > 250)
            {
                _rebuildTicks = Time.Ticks;
                BuildList();
            }
        }

        private sealed class EnemyRowControl : Control
        {
            private readonly Mobile _mobile;
            private readonly Label _label;

            public EnemyRowControl(Mobile mobile)
            {
                _mobile = mobile;
                Height = ROW_HEIGHT;
                Width = 200;
                AcceptMouseInput = true;
                CanMove = false;
                string name = string.IsNullOrEmpty(mobile.Name) ? $"0x{mobile.Serial:X}" : mobile.Name;
                ushort hue = Notoriety.GetHue(mobile.NotorietyFlag);
                _label = new Label($"{name} ({mobile.Distance})", true, hue, 180) { X = 4, Y = 2 };
                Add(_label);
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (_mobile.IsDestroyed) return false;
                if (MouseIsOver)
                    batcher.Draw(SolidColorTextureCache.GetTexture(Color.White), new Rectangle(x, y, Width, Height), ShaderHueTranslator.GetHueVector(0, false, 0.3f));
                return base.Draw(batcher, x, y);
            }
        }
    }
}
