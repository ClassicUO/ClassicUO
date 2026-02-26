using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;

namespace ClassicUO.LegionScripting
{
    public enum PersistentVar
    {
        Char,
        Account,
        Server,
        Global
    }

    public static class PersistentVars
    {
        private const string DATA_FILE = "legionvars.dat";
        private const string GlobalScopeKey = "GLOBAL";
        private const char SEPARATOR = '\t';

        private static string _charScopeKey = "";
        private static string _accountScopeKey = "";
        private static string _serverScopeKey = "";

        private static readonly object _fileLock = new object();
        private static readonly ConcurrentQueue<(PersistentVar scope, string scopeKey, string key, string value)> _saveQueue = new ConcurrentQueue<(PersistentVar scope, string scopeKey, string key, string value)>();
        private static int _saveTaskRunning = 0;

        private static string DataPath => Path.Combine(CUOEnviroment.ExecutablePath, "Data", DATA_FILE);
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _data = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

        public static void Load()
        {
            _charScopeKey = ProfileManager.CurrentProfile.ServerName + ProfileManager.CurrentProfile.Username + ProfileManager.CurrentProfile.CharacterName;
            _accountScopeKey = ProfileManager.CurrentProfile.ServerName + ProfileManager.CurrentProfile.Username;
            _serverScopeKey = ProfileManager.CurrentProfile.ServerName;
            LoadFromFile();
        }

        private static void LoadFromFile()
        {
            lock (_fileLock)
            {
                try
                {
                    var dataDir = Path.GetDirectoryName(DataPath);
                    if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
                        Directory.CreateDirectory(dataDir);

                    _data = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

                    if (File.Exists(DataPath))
                    {
                        var lines = File.ReadAllLines(DataPath, Encoding.UTF8);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrEmpty(line)) continue;
                            var parts = line.Split(SEPARATOR);
                            if (parts.Length >= 4)
                            {
                                var scope = parts[0];
                                var scopeKey = parts[1];
                                var key = parts[2];
                                var value = parts.Length > 4 ? string.Join(SEPARATOR.ToString(), parts, 3, parts.Length - 3) : parts[3];
                                value = UnescapeValue(value);
                                if (!_data.ContainsKey(scope))
                                    _data[scope] = new Dictionary<string, Dictionary<string, string>>();
                                if (!_data[scope].ContainsKey(scopeKey))
                                    _data[scope][scopeKey] = new Dictionary<string, string>();
                                _data[scope][scopeKey][key] = value;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    _data = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                }
            }
        }

        private static void SaveToFile()
        {
            lock (_fileLock)
            {
                try
                {
                    var lines = new List<string>();
                    foreach (var scope in _data)
                    {
                        foreach (var scopeKey in scope.Value)
                        {
                            foreach (var keyValue in scopeKey.Value)
                            {
                                var escapedValue = EscapeValue(keyValue.Value);
                                lines.Add($"{scope.Key}{SEPARATOR}{scopeKey.Key}{SEPARATOR}{keyValue.Key}{SEPARATOR}{escapedValue}");
                            }
                        }
                    }
                    var tempPath = DataPath + ".tmp";
                    File.WriteAllLines(tempPath, lines, Encoding.UTF8);
                    if (File.Exists(DataPath))
                        File.Delete(DataPath);
                    File.Move(tempPath, DataPath);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to save persistent vars: {ex.Message}", ex);
                }
            }
        }

        private static string EscapeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string UnescapeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\\", "\\");
        }

        private static (PersistentVar scope, string scopeKey) GetScopeKeyPair(PersistentVar scope)
        {
            switch (scope)
            {
                case PersistentVar.Char: return (scope, _charScopeKey);
                case PersistentVar.Account: return (scope, _accountScopeKey);
                case PersistentVar.Server: return (scope, _serverScopeKey);
                case PersistentVar.Global: return (scope, GlobalScopeKey);
                default: throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
        }

        public static string GetVar(PersistentVar scope, string key, string defaultValue = "")
        {
            var (s, scopeKey) = GetScopeKeyPair(scope);
            var scopeStr = s.ToString();
            lock (_fileLock)
            {
                if (_data.TryGetValue(scopeStr, out var scopeData) &&
                    scopeData.TryGetValue(scopeKey, out var keyData) &&
                    keyData.TryGetValue(key, out var value))
                    return value;
                return defaultValue;
            }
        }

        public static void SaveVar(PersistentVar scope, string key, string value)
        {
            var (s, scopeKey) = GetScopeKeyPair(scope);
            var scopeStr = s.ToString();
            lock (_fileLock)
            {
                if (!_data.ContainsKey(scopeStr))
                    _data[scopeStr] = new Dictionary<string, Dictionary<string, string>>();
                if (!_data[scopeStr].ContainsKey(scopeKey))
                    _data[scopeStr][scopeKey] = new Dictionary<string, string>();
                _data[scopeStr][scopeKey][key] = value;
            }
            _saveQueue.Enqueue((s, scopeKey, key, value));
            if (Interlocked.CompareExchange(ref _saveTaskRunning, 1, 0) == 0)
                Task.Run(ProcessSaveQueue);
        }

        public static void DeleteVar(PersistentVar scope, string key)
        {
            var (s, scopeKey) = GetScopeKeyPair(scope);
            var scopeStr = s.ToString();
            lock (_fileLock)
            {
                if (_data.TryGetValue(scopeStr, out var scopeData) &&
                    scopeData.TryGetValue(scopeKey, out var keyData) &&
                    keyData.ContainsKey(key))
                    keyData.Remove(key);
            }
            _saveQueue.Enqueue((s, scopeKey, key, null));
            if (Interlocked.CompareExchange(ref _saveTaskRunning, 1, 0) == 0)
                Task.Run(ProcessSaveQueue);
        }

        private static async Task ProcessSaveQueue()
        {
            try
            {
                var hasChanges = false;
                while (_saveQueue.TryDequeue(out var item))
                    hasChanges = true;
                if (hasChanges)
                    SaveToFile();
            }
            finally
            {
                Interlocked.Exchange(ref _saveTaskRunning, 0);
            }
        }

        public static void Unload()
        {
            if (_saveQueue.Count > 0 && Interlocked.CompareExchange(ref _saveTaskRunning, 1, 0) == 0)
                Task.Run(ProcessSaveQueue).Wait();
        }
    }
}
