using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ClassicUO.Configuration
{
    internal static class AccountManager
    {
        private static List<Account> _accounts;

        public static IReadOnlyCollection<Account> GetAccounts(string serverName)
        {
            LoadAccounts(serverName);
            var accounts = _accounts?
                .Where(account => account.Server == serverName)
                .OrderBy(account => account.UserName)
                .ToList() ?? new List<Account>();

            return new ReadOnlyCollection<Account>(accounts);
        }

        public static string GetAccountPassword(string serverName, string userName)
        {
            LoadAccounts(serverName);
            return _accounts?.FirstOrDefault(account => account.UserName == userName)?.Password ?? string.Empty;
        }

        public static void SaveAccount(string serverName, string userName, string password, bool saveAccount)
        {
            LoadAccounts(serverName);
            var existingRecord = _accounts.FirstOrDefault(x => x.Server == serverName && x.UserName == userName);
            //There is an existing record and the user has opted to not save that account, remove from list.
            if (existingRecord != null && !saveAccount)
            {
                _accounts.Remove(existingRecord);
            }
            //There is an existing record and they have updated their password.  save the udpated password.
            else if (existingRecord != null && existingRecord.Password != password)
            {
                existingRecord.UpdatePassword(password);
            }
            //Otherwise if there is no existing record, the user has opted to save the account, and that account has a username save the record.
            else if (existingRecord == null && saveAccount && !string.IsNullOrWhiteSpace(userName))
            {
                _accounts.Add(new Account(serverName, userName, password));
            }
            SaveAccountsToFile();
        }

        private static void LoadAccounts(string serverName)
        {
            if(_accounts != null)
            {
                return;
            }

            var accounts = LoadAccountsFromFile();
            _accounts = accounts.Where(account => account.Server == serverName).ToList();
        }

        private static bool SaveAccountsToFile()
        {
            try
            {
                ConfigurationResolver.Save(_accounts, PathToAccountFile());
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }

        private static List<Account> LoadAccountsFromFile()
        {
            try
            {
                var accounts = ConfigurationResolver.Load<List<Account>>(PathToAccountFile()) ?? new List<Account>();
                return accounts;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new List<Account>();
            }
        }

        private static string PathToAccountFile()
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Data", "Profiles");
            string fileToLoad = Path.Combine(path, "accounts.json");
            return fileToLoad;
        }
    }
}
