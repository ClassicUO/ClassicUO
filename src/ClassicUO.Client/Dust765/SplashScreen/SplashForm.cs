using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace ClassicUO.Dust765
{
    internal sealed class SplashForm : Form
    {
        private System.Windows.Forms.Timer _timer;
        private int _dotFrame;
        private int _shimmerX;
        private float _progress;
        private string _status = "Loading client files...";
        private Image _logo;

        private static readonly string[] _dots = { "", ".", "..", "..." };

        // ── Paleta dark red / dark ─────────────────────────────────────────
        private static readonly Color _bg1     = Color.FromArgb( 14,  10,  10 );  // fundo topo
        private static readonly Color _bg2     = Color.FromArgb( 24,  14,  14 );  // fundo base
        private static readonly Color _gold    = Color.FromArgb(180,  30,  30 );  // vermelho principal
        private static readonly Color _goldHi  = Color.FromArgb(230,  70,  70 );  // vermelho claro (shimmer)
        private static readonly Color _goldDim = Color.FromArgb( 80,  12,  12 );  // vermelho escuro (barra)
        private static readonly Color _border  = Color.FromArgb(120,  30,  30 );  // borda
        private static readonly Color _panel   = Color.FromArgb( 34,  18,  18 );  // painel info
        private static readonly Color _txt     = Color.FromArgb(235, 225, 225 );  // texto principal
        private static readonly Color _txtDim  = Color.FromArgb(130, 100, 100 );  // texto secundário
        private static readonly Color _barBg   = Color.FromArgb( 44,  22,  22 );  // fundo barra

        private const int W = 420;
        private const int H = 320;

        public SplashForm()
        {
            Size            = new Size(W, H);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = _bg1;
            TopMost         = true;
            DoubleBuffered  = true;
            ShowInTaskbar   = false;
            Opacity         = 0;

            // Carrega logo
            try
            {
                string logoPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "logodust.png");
                if (File.Exists(logoPath))
                    _logo = Image.FromFile(logoPath);
            }
            catch { /* sem logo — continua funcionando */ }

            // Fade-in suave
            var fadeIn = new System.Windows.Forms.Timer { Interval = 16 };
            fadeIn.Tick += (s, e) =>
            {
                Opacity += 0.09;
                if (Opacity >= 1.0) { Opacity = 1.0; fadeIn.Stop(); fadeIn.Dispose(); }
            };
            fadeIn.Start();

            // Animação geral
            _timer = new System.Windows.Forms.Timer { Interval = 80 };
            _timer.Tick += (s, e) =>
            {
                _dotFrame  = (int)(Environment.TickCount / 400) % 4;
                _progress  = Math.Min(1f, _progress + 0.012f);
                _shimmerX  = (int)(Environment.TickCount / 18) % (W - 80);
                Invalidate();
            };
            _timer.Start();
        }

        public void SetStatus(string text)
        {
            if (InvokeRequired)
                Invoke(new Action(() => { _status = text; Invalidate(); }));
            else { _status = text; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode     = SmoothingMode.AntiAlias;

            int w = Width, h = Height;
            const int pad = 24;

            // ── Fundo gradiente ──────────────────────────────────────────────
            using (var bg = new LinearGradientBrush(new Rectangle(0, 0, w, h), _bg1, _bg2, 90f))
                g.FillRectangle(bg, 0, 0, w, h);

            // ── Faixa vermelha superior ──────────────────────────────────────
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, w, 5), _goldDim, _gold, 0f))
                g.FillRectangle(br, 0, 0, w, 5);

            // ── Borda ────────────────────────────────────────────────────────
            using (var pen = new Pen(_border, 1f))
                g.DrawRectangle(pen, 0, 0, w - 1, h - 1);

            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            // ── Logo centralizado no topo ────────────────────────────────────
            const int logoH = 90;
            int logoY = 14;
            if (_logo != null)
            {
                double scale = Math.Min((double)(w - pad * 2) / _logo.Width, (double)logoH / _logo.Height);
                int lw = (int)(_logo.Width  * scale);
                int lh = (int)(_logo.Height * scale);
                int lx = (w - lw) / 2;
                int ly = logoY + (logoH - lh) / 2;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_logo, lx, ly, lw, lh);
            }
            else
            {
                using (var f = new Font("Segoe UI", 30, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
                using (var b = new SolidBrush(_gold))
                    g.DrawString("Dust765", f, b, new RectangleF(0, logoY, w, logoH), sf);
            }

            int curY = logoY + logoH + 6;

            // ── Divisor ───────────────────────────────────────────────────────
            using (var pen = new Pen(Color.FromArgb(55, _border), 1f))
                g.DrawLine(pen, pad, curY, w - pad, curY);
            curY += 10;

            // ── Sub-título centrado ───────────────────────────────────────────
            using (var f = new Font("Segoe UI", 10, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(_txtDim))
                g.DrawString("Ultima Online · Custom Client", f, b, new RectangleF(0, curY, w, 16), sf);
            curY += 18;

            // ── Versão centrada ───────────────────────────────────────────────
            string ver = $"version {CUOEnviroment.Version}";
            using (var f = new Font("Segoe UI", 9, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(_txtDim))
                g.DrawString(ver, f, b, new RectangleF(0, curY, w, 14), sf);
            curY += 20;

            // ── Painel de status ─────────────────────────────────────────────
            var panelRect = new Rectangle(pad, curY, w - pad * 2, 46);
            using (var b = new SolidBrush(_panel))
                g.FillRectangle(b, panelRect);
            using (var pen = new Pen(Color.FromArgb(50, _border), 1f))
                g.DrawRectangle(pen, panelRect);

            // Spinner + status text centrado
            string[] spinners = { "▸", "▹", "▸", "▹" };
            string statusText = spinners[_dotFrame] + "  " + _status + _dots[_dotFrame];
            using (var f = new Font("Segoe UI", 11, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(_txt))
                g.DrawString(statusText, f, b, new RectangleF(pad, curY, w - pad * 2, 28), sf);

            // Hint centrado
            using (var f = new Font("Segoe UI", 9, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(_txtDim))
                g.DrawString("Please wait while resources are being loaded...", f, b,
                    new RectangleF(pad, curY + 28, w - pad * 2, 16), sf);

            curY += panelRect.Height + 14;

            // ── Barra de progresso (altura 20px, % dentro) ───────────────────
            int barH   = 20;
            int barW   = w - pad * 2;
            var barBgR = new Rectangle(pad, curY, barW, barH);
            int fillW  = (int)(barW * _progress);
            string pct = $"{(int)(_progress * 100)}%";

            // Fundo
            using (var b = new SolidBrush(_barBg))
                g.FillRectangle(b, barBgR);

            // Preenchimento gradiente
            if (fillW > 2)
            {
                var fillRect = new Rectangle(pad, curY, fillW, barH);
                using (var br = new LinearGradientBrush(fillRect, _goldDim, _gold, 0f))
                    g.FillRectangle(br, fillRect);

                // Shimmer
                int sx = pad + (_shimmerX % Math.Max(1, fillW));
                using (var pen = new Pen(Color.FromArgb(140, _goldHi), 2f))
                    g.DrawLine(pen, sx, curY + 1, sx + 12, curY + barH - 1);
            }

            // Borda da barra
            using (var pen = new Pen(Color.FromArgb(60, _border), 1f))
                g.DrawRectangle(pen, barBgR);

            // % centrado dentro da barra
            using (var f = new Font("Segoe UI", 9, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(_progress > 0.45f ? _bg1 : _txt))
                g.DrawString(pct, f, b, new RectangleF(pad, curY, barW, barH), sf);

            curY += barH + 10;

            // ── Rodapé ────────────────────────────────────────────────────────
            using (var pen = new Pen(Color.FromArgb(30, _border), 1f))
                g.DrawLine(pen, pad, curY, w - pad, curY);
            curY += 4;

            using (var f = new Font("Segoe UI", 9, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(_txtDim))
                g.DrawString("github.com/dust765/ClassicUO  ·  © Dust765 Team", f, b,
                    new RectangleF(0, curY, w, h - curY - 2), sf);

            sf.Dispose();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timer?.Stop();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
                _logo?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
