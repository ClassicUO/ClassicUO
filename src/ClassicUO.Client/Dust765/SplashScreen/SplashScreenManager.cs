using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ClassicUO.Dust765
{
    /// <summary>
    /// Gerencia a janela de loading (SplashForm) em uma thread STA separada.
    /// O splash abre ANTES da janela SDL/FNA aparecer, durante Client.Load().
    /// </summary>
    internal static class SplashScreenManager
    {
        private static Thread _thread;
        private static SplashForm _form;
        private static volatile bool _ready;

        public static bool IsSupported =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Exibe o splash. Retorna imediatamente (non-blocking).
        /// </summary>
        public static void Show()
        {
            if (!IsSupported)
                return;

            _ready = false;

            _thread = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                _form = new SplashForm();
                _form.Shown += (s, e) => _ready = true;

                Application.Run(_form);
            });

            _thread.Name = "CUO_SPLASH_THREAD";
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.IsBackground = true;
            _thread.Start();

            // Aguarda até o form aparecer (max 2s)
            int waited = 0;
            while (!_ready && waited < 2000)
            {
                Thread.Sleep(20);
                waited += 20;
            }
        }

        /// <summary>
        /// Atualiza o texto de status exibido no splash.
        /// </summary>
        public static void SetStatus(string text)
        {
            try { _form?.SetStatus(text); }
            catch { /* ignora */ }
        }

        /// <summary>
        /// Fecha o splash com fade-out. Chamado quando o game está pronto.
        /// </summary>
        public static void Close()
        {
            if (_form == null || !IsSupported)
                return;

            try
            {
                if (_form.InvokeRequired)
                    _form.Invoke(new Action(FadeOutAndClose));
                else
                    FadeOutAndClose();
            }
            catch { /* ignora — form já pode ter sido fechado */ }
        }

        private static void FadeOutAndClose()
        {
            if (_form == null || _form.IsDisposed)
                return;

            var fadeOut = new System.Windows.Forms.Timer { Interval = 16 };
            fadeOut.Tick += (s, e) =>
            {
                if (_form.Opacity <= 0.05)
                {
                    fadeOut.Stop();
                    fadeOut.Dispose();
                    _form.Close();
                    _form = null;
                }
                else
                {
                    _form.Opacity -= 0.10;
                }
            };
            fadeOut.Start();
        }
    }
}
