using ClassicUO.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClassicUO.Configuration
{
    internal static class AccountManager
    {
        public static List<Account> Accounts;

        private static void Load(string serverName)
        {
            if (Accounts == null)
            {
                var accounts = LoadAccountsFromFile();
                Accounts = accounts.Where(x => x.Server == serverName).ToList();
            }
        }

        public static string[] GetAccountNames(string serverName)
        {
            Load(serverName);
            return Accounts?.Where(x => x.Server == serverName).Select(x => x.UserName).ToArray() ?? new string[] { };
        }

        public static string GetAccountPassword(string serverName, string userName)
        {
            Load(serverName);
            return Accounts?.FirstOrDefault(x => x.UserName == userName)?.Password;
        }

        public static void SaveAccount(string serverName, string userName, string password)
        {
            Load(serverName);
            var existingRecord = Accounts.FirstOrDefault(x => x.Server == serverName && x.UserName == userName);
            if(existingRecord == null)
            {
                Accounts.Add(new Account() { UserName = userName, Server = serverName, Password = password }); //TODO: Figure if the pw needs to be encrypted here or before.
            }
            else if(existingRecord.Password != password)
            {
                existingRecord.Password = password; //TODO: figure out if this needs to be encrypted here, also double check the equality check.
            }
            ConfigurationResolver.Save<List<Account>>(Accounts, PathToAccountFile());
        }
        
        private static List<Account> LoadAccountsFromFile()
        {
            var accounts = ConfigurationResolver.Load<List<Account>>(PathToAccountFile()) ?? new List<Account>();
            return accounts;
        }

        private static string PathToAccountFile()
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Data", "Profiles");
            string fileToLoad = Path.Combine(path, "accounts.json");
            return fileToLoad;
        }
    }
}
