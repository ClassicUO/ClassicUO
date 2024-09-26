using System.Collections.Generic;
using System.IO;

namespace ClassicUO.Game.Managers
{
    internal static class SimpleAccountManager
    {
        private static string accountPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");

        public static string[] GetAccounts()
        {
            List<string> accounts = new List<string>();

            if (Directory.Exists(accountPath))
            {
                var dirs = Directory.GetDirectories(accountPath);

                foreach (var dir in dirs)
                {
                    accounts.Add(Path.GetFileName(dir));
                }
            }
            return accounts.ToArray();
        }
    }
}
