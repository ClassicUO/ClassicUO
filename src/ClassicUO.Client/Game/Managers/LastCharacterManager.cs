#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    [JsonSerializable(typeof(LastCharacterInfo))]
    [JsonSerializable(typeof(List<LastCharacterInfo>))]
    sealed partial class LastCharacterJsonContext : JsonSerializerContext { }

    public static class LastCharacterManager
    {
        private static readonly string _lastCharacterFilePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
        private static readonly string _lastCharacterFile = Path.Combine(_lastCharacterFilePath, "lastcharacter.json");

        private static List<LastCharacterInfo> LastCharacters { get; set; }

        private static string LastCharacterNameOverride { get; set; }

        public static void Load()
        {
            LastCharacters = new List<LastCharacterInfo>();

            if (!File.Exists(_lastCharacterFile))
            {
                ConfigurationResolver.Save(LastCharacters, _lastCharacterFile, LastCharacterJsonContext.Default.ListLastCharacterInfo);
            }

            LastCharacters = ConfigurationResolver.Load<List<LastCharacterInfo>>(_lastCharacterFile, LastCharacterJsonContext.Default.ListLastCharacterInfo);

            // safety check
            if (LastCharacters == null)
            {
                LastCharacters = new List<LastCharacterInfo>();
            }
        }

        public static void Save(string account, string server, string name)
        {
            LastCharacterInfo lastChar = LastCharacters.FirstOrDefault(c => c.AccountName.Equals(account) && c.ServerName == server);

            // Check to see if they passed in -lastcharactername but picked another character, clear override then
            if (!string.IsNullOrEmpty(LastCharacterNameOverride) && !LastCharacterNameOverride.Equals(name))
            {
                LastCharacterNameOverride = string.Empty;
            }

            if (lastChar != null)
            {
                lastChar.LastCharacterName = name;
            }
            else
            {
                LastCharacters.Add(new LastCharacterInfo
                {
                    ServerName = server,
                    LastCharacterName = name,
                    AccountName = account
                });
            }

            ConfigurationResolver.Save(LastCharacters, _lastCharacterFile, LastCharacterJsonContext.Default.ListLastCharacterInfo);
        }

        public static string GetLastCharacter(string account, string server)
        {
            if (LastCharacters == null)
            {
                Load();
            }

            // If they passed in a -lastcharactername param, ignore json value, use that value instead
            if (!string.IsNullOrEmpty(LastCharacterNameOverride))
            {
                return LastCharacterNameOverride;
            }

            LastCharacterInfo lastChar = LastCharacters.FirstOrDefault(c => c.AccountName.Equals(account) && c.ServerName == server);

            return lastChar != null ? lastChar.LastCharacterName : string.Empty;
        }
        
        public static void OverrideLastCharacter(string name)
        {
            LastCharacterNameOverride = name;
        }
    }

    public class LastCharacterInfo
    {
        public string AccountName { get; set; }
        public string ServerName { get; set; }
        public string LastCharacterName { get; set; }
    }
}