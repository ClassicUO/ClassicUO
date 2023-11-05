using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    internal static class UpdateManager
    {
        public static bool SkipUpdateCheck { get; set; } = false;
        public static event EventHandler<EventArgs> UpdateStatusChanged;
        public static bool HasUpdate { get; private set; } = false;

        public static void CheckForUpdates()
        {
            if (!SkipUpdateCheck)
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            string webVer = httpClient.GetStringAsync("https://raw.githubusercontent.com/bittiez/TazUO/main/tazuoversioninfo.txt").Result;

                            Version remoteV = new Version(webVer);

                            if (remoteV > CUOEnviroment.Version)
                            {
                                HasUpdate = true;
                                UpdateStatusChanged?.Invoke(null, EventArgs.Empty);
                            }
                        }
                    }
                    catch { }
                });
            }
        }

        public static void SendDelayedUpdateMessage()
        {
            Task.Factory.StartNew(() => {
                Task.Delay(30000).Wait();
                GameActions.Print("TazUO has an update available, please visit https://github.com/bittiez/TazUO to get the most recent version.", 32);
            });
        }
    }
}
