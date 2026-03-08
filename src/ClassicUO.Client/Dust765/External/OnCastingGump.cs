using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Dust765.Managers;
using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Dust765.External
{
    public class OnCastingGump : Gump
    {
        const byte _iconSize = 16, _spaceSize = 3, _borderSize = 3;
        public uint Timer { get; set; }
        private uint _startTime;
        private uint _endTime;
        //private float _updateTime;
        private static AlphaBlendControl _background;
        private static Label _text;
        //private TextureControl _icon;
        private StaticPic _icon;
        private static Dictionary<string, SpellRangeInfo> spellRangePowerWordCache = new Dictionary<string, SpellRangeInfo>();

        private static string RemoveContentInBrackets(string input)
        {
            return Regex.Replace(input, @"\[.*?\]", "").Trim();
        }

        private class SpellRangeInfo
        {
            public int ID { get; set; } = -1;
            public string Name { get; set; } = "";
            public string PowerWords { get; set; } = "";
        }
        private AlphaBlendControl _loadingBar;
        private Timer _loadingTimer;
        private int _loadingProgress;



        public OnCastingGump() : base(0, 0)
        {
            CanMove = false;
            AcceptMouseInput = false;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            GameActions.iscasting = false;
            IsVisible = false;

            BuildGump();
        }

        private static int[] _stopAtClilocs = new int[]
        {
            // procurar clilocs de disturb de magia
            500641,     // Your concentration is disturbed, thus ruining thy spell.
            502625,     // Insufficient mana. You must have at least ~1_MANA_REQUIREMENT~ Mana to use this spell.
            502630,     // More reagents are needed for this spell.
            500946,     // You cannot cast this in town!
            500015,     // You do not have that spell
            502643,     // You can not cast a spell while frozen.
            1061091,    // You cannot cast that spell in this form.
            502644,     // You have not yet recovered from casting a spell.
            1072060,    // You cannot cast a spell while calmed.
    };


        public void Start(uint _spell_id, uint _re = 0)
        {
            _startTime = Time.Ticks;
            uint circle;
            System.TimeSpan spellTime;

            if (!ProfileManager.CurrentProfile.OnCastingGump_hidden)
            {
                IsVisible = true;
            }

            try
            {
                SpellAction spell = (SpellAction)_spell_id;
                circle = (uint)SpellManager.GetCircle(spell);
                uint protection_delay = 0;
                if (World.Player.IsBuffIconExists(BuffIconType.Protection))
                {
                    protection_delay = 1;
                    if (circle != 9)
                    {
                        protection_delay = protection_delay + 2;
                    } else
                    {
                        protection_delay = protection_delay + 5;
                        circle = circle + 2;
                    }
                }
                _endTime = _startTime + 400 + (circle + protection_delay) * 250 + _re; // (0.5+ 0.25 * circle) * 1000
                GameActions.iscasting = true;
            }
            catch
            {
                // cant discover the spell
                Stop();

            }
        }

        private static void OnRawMessageReceived(object sender, MessageEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                if (e.Parent != null && ReferenceEquals(e.Parent, World.Player))
                {
                    if (spellRangePowerWordCache.TryGetValue(RemoveContentInBrackets(e.Text.Trim()), out SpellRangeInfo spell2))
                        if (!GameActions.iscasting)
                            World.Player.OnCasting.Start((uint)spell2.ID);
                    
                }
            });
        }

        public void OnClilocReceived(int cliloc)
        {
            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < _stopAtClilocs.Length; i++)
                {
                    if (_stopAtClilocs[i] == cliloc)
                    {
                        Stop();
                        return;
                    }
                }
            });
        }

        public void Stop()
        {
            GameActions.iscasting = false;
            IsVisible = false;
        }

        public static void OnSceneLoad()
        {
            EventSink.RawMessageReceived += OnRawMessageReceived;
        }

        public static void OnSceneUnload()
        {
            EventSink.RawMessageReceived -= OnRawMessageReceived;
        }

        /*
        public void OnMessage(string text, uint hue, string name, bool isunicode = true)
        {
            // attempt to pick up on things that OnCliloc missed
            if (name != "System" && text.Length <= 0)
                return;

            // stop the timer
            for (int i = 0; i < _stopAtClilocs.Length; i++)
            {
                if (ClilocLoader.Instance.GetString(_stopAtClilocs[i]) == text)
                {
                    Stop();
                    return;
                }
            }
        }
        */
        public void OnCliloc(uint cliloc)
        {
            // stop the timer
            if (!GameActions.iscasting)
            {
                return;
            }
                
            for (int i = 0; i < _stopAtClilocs.Length; i++)
            {
                if (_stopAtClilocs[i] == cliloc)
                {
                    Stop();
                    return;
                }
            }
        }

        public class CastTimerProgressBar : Gump
        {
            public Rectangle barBounds, barBoundsF;
            private Texture2D background;
            public Texture2D foreground;
            public Vector3 hue = ShaderHueTranslator.GetHueVector(0);

            public CastTimerProgressBar() : base(0, 0)
            {
                CanMove = false;
                AcceptMouseInput = false;
                CanCloseWithEsc = false;
                CanCloseWithRightClick = false;

                ref readonly var gi = ref Client.Game.Gumps.GetGump(0x0805);
                background = gi.Texture;
                barBounds = gi.UV;

                gi = ref Client.Game.Gumps.GetGump(0x0806);
                foreground = gi.Texture;
                barBoundsF = gi.UV;
            }


            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (!IsVisible ||
                    ProfileManager.CurrentProfile == null ||
                    !ProfileManager.CurrentProfile.OnCastingGump ||
                    World.Player == null ||
                    World.Player.IsDestroyed)
                    return false;


                Width = _borderSize * 2 + _iconSize + _spaceSize + _text.Width;
                Height = _borderSize * 2 + _iconSize;

                _background.Width = Width;
                _background.Height = Height;


                /*
                int gx = ProfileManager.CurrentProfile.GameWindowPosition.X;
                int gy = ProfileManager.CurrentProfile.GameWindowPosition.Y;

                x = gx + World.Player.RealScreenPosition.X;
                y = gy + World.Player.RealScreenPosition.Y;

                x += (int) World.Player.Offset.X;// + 22; -OFFSET FIX
                y += (int) (World.Player.Offset.Y - World.Player.Offset.Z);// + 22; -OFFSET FIX

                x -= Width >> 1;
                x += 5;
                y += 10;

                //x += ProfileManager.CurrentProfile.BandageGumpOffset.X;
                //y += ProfileManager.CurrentProfile.BandageGumpOffset.Y;

                Y = y;
                X = x;
                */

                return base.Draw(batcher, Mouse.Position.X, Mouse.Position.Y);
            }
        }

        //public override void Update(double totalMS, double frameMS)
        public override void Update()
        {
            //base.Update(totalMS, frameMS);
            base.Update();

            if (IsDisposed)
                return;

            if (World.Player == null || World.Player.IsDestroyed)
            {
                Dispose();
                return;
            }


            //if (GameActions.iscasting && _updateTime < totalMS)
            if (GameActions.iscasting)
            {
                //_updateTime = (float) totalMS + 125;
                if (Time.Ticks >= _endTime)
                {
                    Stop();
                }
            }
            else
            {
                if (!GameActions.iscasting && IsVisible)
                {
                    Stop();
                }
            }
        }

        private void BuildGump()
        {


            _background = new AlphaBlendControl()
            {
                Alpha = 0.6f
            };

            _text = new Label("Casting", true, 0x35, 0, 1, FontStyle.BlackBorder)
            {
                X = _borderSize + _iconSize + _spaceSize + 3,
                Y = _borderSize - 2
            };

            // Adiciona uma barra de carregamento
            _loadingBar = new AlphaBlendControl()
            {
                X = _borderSize,
                Y = _borderSize + 20,
                Width = 0,  // Começa com largura 0
                Height = 10,
                Hue = 0x35,
                Alpha = 1.0f
            };

            Add(_background);
            Add(_text);
            Add(_loadingBar);

            // Inicializa o timer para atualizar a barra de carregamento
            _loadingProgress = 0;
            _loadingTimer = new Timer(UpdateLoadingBar, null, 0, 100); // Atualiza a cada 100ms
        }
        private void UpdateLoadingBar(object state)
        {
            if (_loadingProgress < 100)
            {
                _loadingProgress++;
                _loadingBar.Width = _loadingProgress;  // Aumenta a largura da barra
            }
            else
            {
                // Opcional: Resetar ou parar o timer quando o carregamento estiver completo
                _loadingProgress = 0;
                _loadingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }
}