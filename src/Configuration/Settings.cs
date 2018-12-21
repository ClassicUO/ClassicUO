#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;

using ClassicUO.Utility;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;

namespace ClassicUO.Configuration
{
    public sealed class Settings : NotifyPropertyChange
    {
        private string _clientVersion = "7.0.59.8";
        private bool _debug = true;
        private string _ip = "YOUR.SERVER.IP.ADDRESS";
        private string _lastCharName = "";
        private int _maxFPS = 144;
        private string _password = ""; //important default otherwise TextBox.SetText crashes from null input on Main menu
        private ushort _port = 2593;
        private bool _preloadMaps;
        private bool _profiler = true;
        private string _uoDir = "YOUR\\PATH\\TO\\ULTIMAONLINE";
        private string _username = ""; //important default otherwise TextBox.SetText crashes from null input on Main menu


        [JsonConstructor]
        public Settings()
        {
        }

        [JsonProperty(PropertyName = "username")]
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        [JsonProperty(PropertyName = "password")]
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        [JsonProperty(PropertyName = "ip")]
        public string IP
        {
            get => _ip;
            set => SetProperty(ref _ip, value);
        }

        [JsonProperty(PropertyName = "port")]
        public ushort Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        [JsonProperty(PropertyName = "lastcharactername")]
        public string LastCharacterName
        {
            get => _lastCharName;
            set => SetProperty(ref _lastCharName, value);
        }

        [JsonProperty(PropertyName = "ultimaonlinedirectory")]
        public string UltimaOnlineDirectory
        {
            get => _uoDir;
            set => SetProperty(ref _uoDir, value);
        }

        [JsonProperty(PropertyName = "clientversion")]
        public string ClientVersion
        {
            get => _clientVersion;
            set => SetProperty(ref _clientVersion, value);
        }

        [JsonProperty(PropertyName = "maxfps")]
        public int MaxFPS
        {
            get => _maxFPS;
            set => SetProperty(ref _maxFPS, value);
        }

        [JsonProperty(PropertyName = "debug")]
        public bool Debug
        {
            get => _debug;
            set => SetProperty(ref _debug, value);
        }

        [JsonProperty(PropertyName = "profiler")]
        public bool Profiler
        {
            get => _profiler;
            set => SetProperty(ref _profiler, value);
        }

        [JsonProperty(PropertyName = "preload_maps")]
        public bool PreloadMaps
        {
            get => _preloadMaps;
            set => SetProperty(ref _preloadMaps, value);
        }



        public void Save()
        {
            ConfigurationResolver.Save(this, "settings.json");
        }
    }

    public abstract class NotifyPropertyChange
    {
        [JsonIgnore] public EventHandler PropertyChanged;

        public virtual bool SetProperty<T>(ref T storage, T value)
        {
            if (EqualityHelper.IsEqual(storage, value))
                return false;
            storage = value;
            OnPropertyChanged();

            return true;
        }

        protected virtual void OnPropertyChanged()
        {
            PropertyChanged.Raise();
        }
    }

    public static class EqualityHelper
    {
        public static bool IsEqual<T>(T oldValue, T newValue)
        {
            if (oldValue == null && newValue == null) return true;
            if (oldValue == null || newValue == null) return false;
            Type type = typeof(T);

            if (type.IsValueType) return oldValue.Equals(newValue);

            return Equals(oldValue, newValue);
        }
    }
}