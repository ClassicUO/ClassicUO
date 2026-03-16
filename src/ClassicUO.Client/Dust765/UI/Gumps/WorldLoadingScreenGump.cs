using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Dust765.UI.Gumps
{
    /// <summary>
    /// Full-screen loading overlay shown when the player selects a character
    /// and enters the world. Auto-dismisses once the world and player are ready.
    /// </summary>
    internal class WorldLoadingScreenGump : Gump
    {
        private const ushort HUE_TITLE   = 0x0035; // gold
        private const ushort HUE_LOADING = 0xFFFF; // white
        private const ushort HUE_PANEL   = 997;    // dark tint

        private const int MIN_DISPLAY_MS = 800;     // mínimo de tempo visível
        private const int DOT_INTERVAL_MS = 280;    // velocidade da animação

        private readonly long _shownAt;
        private long _nextDotUpdate;
        private int  _dotFrame;
        private readonly Label _loadingLabel;
        private readonly Label _hintLabel;

        // Spinner frames: barra giratória de texto
        private static readonly string[] _spinnerFrames = { "|", "/", "-", "\\" };

        public WorldLoadingScreenGump() : base(0, 0)
        {
            _shownAt = Time.Ticks;

            CanCloseWithEsc        = false;
            CanCloseWithRightClick = false;
            CanMove                = false;
            AcceptMouseInput       = true;   // bloqueia cliques enquanto carrega
            AcceptKeyboardInput    = false;
            LayerOrder             = UILayer.Over;

            int sw = Client.Game.Window.ClientBounds.Width;
            int sh = Client.Game.Window.ClientBounds.Height;

            Width  = sw;
            Height = sh;

            // ── Overlay escuro cobrindo a tela toda ──────────────────────────
            Add(new AlphaBlendControl(0.92f)
            {
                Width  = sw,
                Height = sh
            });

            // ── Painel central ───────────────────────────────────────────────
            const int panelW = 340;
            const int panelH = 130;
            int px = (sw - panelW) / 2;
            int py = (sh - panelH) / 2;

            // borda externa (1px mais escura)
            Add(new AlphaBlendControl(1f)
            {
                X = px - 1, Y = py - 1,
                Width  = panelW + 2,
                Height = panelH + 2,
                Hue    = HUE_PANEL
            });

            // fundo do painel
            Add(new AlphaBlendControl(0.96f)
            {
                X = px, Y = py,
                Width  = panelW,
                Height = panelH,
                Hue    = 0
            });

            // ── Título ──────────────────────────────────────────────────────
            var title = new Label("Dust765", true, HUE_TITLE, panelW - 20, 1, FontStyle.BlackBorder)
            {
                X = px + 10,
                Y = py + 14
            };
            Add(title);

            // ── Subtítulo ───────────────────────────────────────────────────
            var sub = new Label("Entrando no Mundo...", true, 0xBBBB, panelW - 20, 1)
            {
                X = px + 10,
                Y = py + 38
            };
            Add(sub);

            // ── Texto animado (spinner + "Carregando") ───────────────────────
            _loadingLabel = new Label("| Carregando", true, HUE_LOADING, panelW - 20, 1)
            {
                X = px + 10,
                Y = py + 72
            };
            Add(_loadingLabel);

            // ── Dica rodapé ──────────────────────────────────────────────────
            _hintLabel = new Label("Aguarde, carregando interface e mapa...", true, 0x7777, panelW - 20, 1)
            {
                X = px + 10,
                Y = py + 102
            };
            Add(_hintLabel);
        }

        public override void Update()
        {
            base.Update();

            // Anima o spinner
            if (Time.Ticks >= _nextDotUpdate)
            {
                _dotFrame       = (_dotFrame + 1) % _spinnerFrames.Length;
                _loadingLabel.Text = $"{_spinnerFrames[_dotFrame]} Carregando";
                _nextDotUpdate  = Time.Ticks + DOT_INTERVAL_MS;
            }

            // Auto-dismiss: espera mínimo + mundo pronto
            if (Time.Ticks - _shownAt >= MIN_DISPLAY_MS
                && World.Player != null
                && World.Map   != null)
            {
                Dispose();
            }
        }

        /// <summary>
        /// Mostra o loading screen. Chame quando o usuário selecionar o personagem.
        /// </summary>
        public static void Show()
        {
            // Remove instância anterior se existir
            UIManager.GetGump<WorldLoadingScreenGump>()?.Dispose();
            UIManager.Add(new WorldLoadingScreenGump());
        }
    }
}
