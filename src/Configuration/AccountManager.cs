using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClassicUO.Configuration
{
    internal static class AccountManager
    {
        public static List<Account> Accounts;

        public static string[] GetAccountNames(string serverName)
        {
            try
            {
                Load(serverName);
                return Accounts?.Where(x => x.Server == serverName).Select(x => x.UserName).OrderBy(x => x).ToArray() ?? new string[] { };
            }
            catch(Exception ex)
            {
                //Log error?
                return new string[] { };
            }
        }

        public static string GetAccountPassword(string serverName, string userName)
        {
            try
            {
                Load(serverName);
                return Accounts?.FirstOrDefault(x => x.UserName == userName)?.Password;
            }
            catch(Exception ex)
            {
                //Log Error?
                return string.Empty;
            }
        }

        public static void SaveAccount(string serverName, string userName, string password, bool saveAccount)
        {
            try
            {
                Load(serverName);
                var existingRecord = Accounts.FirstOrDefault(x => x.Server == serverName && x.UserName == userName);
                //There is an existing record and the user has opted to not save that account, remove from list.
                if(existingRecord != null && !saveAccount)
                {
                    Accounts.Remove(existingRecord);
                }
                //There is an existing record and they have updated their password.  save the udpated password.
                else if (existingRecord != null && existingRecord.Password != password)
                {
                    existingRecord.Password = password;
                }
                //Otherwise if there is no existing record, the user has opted to save the account, and that account has a username save the record.
                else if (existingRecord == null && saveAccount && !string.IsNullOrWhiteSpace(userName))
                {
                    Accounts.Add(new Account() { UserName = userName, Server = serverName, Password = password });
                }
                ConfigurationResolver.Save(Accounts, PathToAccountFile());
            }
            catch(Exception ex)
            {
                //Log error?
            }
        }

        private static void Load(string serverName)
        {
            if (Accounts == null)
            {
                var accounts = LoadAccountsFromFile();
                Accounts = accounts.Where(x => x.Server == serverName).ToList();
            }
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
