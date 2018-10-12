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
using ClassicUO.Utility;
using Newtonsoft.Json;

namespace ClassicUO.Configuration
{
    public class Settings : NotifyPropertyChange
    {
        private int _allyMessageColor;
        private bool _alwaysRun;
        private bool _backgroundSound;
        private int _backpackStyle;


        private byte _chatFont;
        private string _clientVersion;
        private bool _combatMusic;


        private int _containerDefaultX;
        private int _containerDefaultY;

        private bool _criminalActionQuery;
        private int _criminalColor;
        private bool _debug;
        private int _delayAppearTooltips;
        private int _emoteColor;

        private bool _enablePathfind;
        private int _enemyColor;
        private bool _footstepsSound;
        private int _friendColor;
        private int _gameWindowHeight = 600;
        private int _gameWindowWidth = 800;
        private int _gameWindowX = 32;
        private int _gameWindowY = 32;
        private int _guildMessageColor;

        private bool _highlightGameObjects = true;

        private int _innocentColor;
        private string _ip;
        private string _lastCharName;
        private int _maxFPS;
        private int _murdererColor;
        private bool _music;
        private int _musicVolume;
        private int _partyMessageColor;
        private string _password;
        private ushort _port;
        private bool _profiler;

        private bool _reduceFpsInactiveWindow;
        private bool _scaleSpeechDelay;
        private bool _showIncomingNames;
        private bool _skillReport;
        private bool _smoothMovement = true;

        private bool _sound;
        private int _soundVolume;
        private int _speechColor;

        private int _speechDelay;
        private bool _statReport;
        private ushort _tooltipsTextColor;
        private string _uoDir;


        private bool _useOldStatus;
        private string _username;

        private bool _useTooltips;

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

        [JsonProperty(PropertyName = "sound")]
        public bool Sound
        {
            get => _sound;
            set => SetProperty(ref _sound, value);
        }

        [JsonProperty(PropertyName = "sound_volume")]
        public int SoundVolume
        {
            get => _soundVolume;
            set => SetProperty(ref _soundVolume, value);
        }

        [JsonProperty(PropertyName = "music")]
        public bool Music
        {
            get => _music;
            set => SetProperty(ref _music, value);
        }

        [JsonProperty(PropertyName = "music_volume")]
        public int MusicVolume
        {
            get => _musicVolume;
            set => SetProperty(ref _musicVolume, value);
        }

        [JsonProperty(PropertyName = "footsteps_sounds")]
        public bool FootstepsSound
        {
            get => _footstepsSound;
            set => SetProperty(ref _footstepsSound, value);
        }

        [JsonProperty(PropertyName = "combat_music")]
        public bool CombatMusic
        {
            get => _combatMusic;
            set => SetProperty(ref _combatMusic, value);
        }

        [JsonProperty(PropertyName = "background_sound")]
        public bool BackgroundSound
        {
            get => _backgroundSound;
            set => SetProperty(ref _backgroundSound, value);
        }

        [JsonProperty(PropertyName = "chat_font")]
        public byte ChatFont
        {
            get => _chatFont;
            set => SetProperty(ref _chatFont, value);
        }

        [JsonProperty(PropertyName = "enable_pathfind")]
        public bool EnablePathfind
        {
            get => _enablePathfind;
            set => SetProperty(ref _enablePathfind, value);
        }

        [JsonProperty(PropertyName = "always_run")]
        public bool AlwaysRun
        {
            get => _alwaysRun;
            set => SetProperty(ref _alwaysRun, value);
        }

        [JsonProperty(PropertyName = "reduce_fps_inactive_window")]
        public bool ReduceFpsInactiveWindow
        {
            get => _reduceFpsInactiveWindow;
            set => SetProperty(ref _reduceFpsInactiveWindow, value);
        }

        [JsonProperty(PropertyName = "container_default_x")]
        public int ContainerDefaultX
        {
            get => _containerDefaultX;
            set => SetProperty(ref _containerDefaultX, value);
        }

        [JsonProperty(PropertyName = "container_default_y")]
        public int ContainerDefaultY
        {
            get => _containerDefaultY;
            set => SetProperty(ref _containerDefaultY, value);
        }

        [JsonProperty(PropertyName = "backpack_style")]
        public int BackpackStyle
        {
            get => _backpackStyle;
            set => SetProperty(ref _backpackStyle, value);
        }

        [JsonProperty(PropertyName = "game_window_x")]
        public int GameWindowX
        {
            get => _gameWindowX;
            set => SetProperty(ref _gameWindowX, value);
        }

        [JsonProperty(PropertyName = "game_window_y")]
        public int GameWindowY
        {
            get => _gameWindowY;
            set => SetProperty(ref _gameWindowY, value);
        }

        [JsonProperty(PropertyName = "game_window_width")]
        public int GameWindowWidth
        {
            get => _gameWindowWidth;
            set => SetProperty(ref _gameWindowWidth, value);
        }

        [JsonProperty(PropertyName = "game_window_height")]
        public int GameWindowHeight
        {
            get => _gameWindowHeight;
            set => SetProperty(ref _gameWindowHeight, value);
        }

        [JsonProperty(PropertyName = "speech_delay")]
        public int SpeechDelay
        {
            get => _speechDelay;
            set => SetProperty(ref _speechDelay, value);
        }

        [JsonProperty(PropertyName = "scale_speech_delay")]
        public bool ScaleSpeechDelay
        {
            get => _scaleSpeechDelay;
            set => SetProperty(ref _scaleSpeechDelay, value);
        }

        [JsonProperty(PropertyName = "speech_color")]
        public int SpeechColor
        {
            get => _speechColor;
            set => SetProperty(ref _speechColor, value);
        }

        [JsonProperty(PropertyName = "emote_color")]
        public int EmoteColor
        {
            get => _emoteColor;
            set => SetProperty(ref _emoteColor, value);
        }

        [JsonProperty(PropertyName = "party_message_color")]
        public int PartyMessageColor
        {
            get => _partyMessageColor;
            set => SetProperty(ref _partyMessageColor, value);
        }

        [JsonProperty(PropertyName = "guild_message_color")]
        public int GuildMessageColor
        {
            get => _guildMessageColor;
            set => SetProperty(ref _guildMessageColor, value);
        }

        [JsonProperty(PropertyName = "ally_message_color")]
        public int AllyMessageColor
        {
            get => _allyMessageColor;
            set => SetProperty(ref _allyMessageColor, value);
        }

        [JsonProperty(PropertyName = "innocent_color")]
        public int InnocentColor
        {
            get => _innocentColor;
            set => SetProperty(ref _innocentColor, value);
        }

        [JsonProperty(PropertyName = "friend_color")]
        public int FriendColor
        {
            get => _friendColor;
            set => SetProperty(ref _friendColor, value);
        }

        [JsonProperty(PropertyName = "criminal_color")]
        public int CriminalColor
        {
            get => _criminalColor;
            set => SetProperty(ref _criminalColor, value);
        }

        [JsonProperty(PropertyName = "enemy_color")]
        public int EnemyColor
        {
            get => _enemyColor;
            set => SetProperty(ref _enemyColor, value);
        }

        [JsonProperty(PropertyName = "murderer_color")]
        public int MurdererColor
        {
            get => _murdererColor;
            set => SetProperty(ref _murdererColor, value);
        }

        [JsonProperty(PropertyName = "criminal_action_query")]
        public bool CriminalActionQuery
        {
            get => _criminalActionQuery;
            set => SetProperty(ref _criminalActionQuery, value);
        }


        [JsonProperty(PropertyName = "show_incoming_names")]
        public bool ShowIncomingNames
        {
            get => _showIncomingNames;
            set => SetProperty(ref _showIncomingNames, value);
        }


        [JsonProperty(PropertyName = "stat_report")]
        public bool StatReport
        {
            get => _statReport;
            set => SetProperty(ref _statReport, value);
        }


        [JsonProperty(PropertyName = "skill_report")]
        public bool SkillReport
        {
            get => _skillReport;
            set => SetProperty(ref _skillReport, value);
        }


        [JsonProperty(PropertyName = "use_old_status")]
        public bool UseOldStatus
        {
            get => _useOldStatus;
            set => SetProperty(ref _useOldStatus, value);
        }


        [JsonProperty(PropertyName = "use_tooltips")]
        public bool UseTooltips
        {
            get => _useTooltips;
            set => SetProperty(ref _useTooltips, value);
        }

        [JsonProperty(PropertyName = "delay_appear_tooltips")]
        public int DelayAppearTooltips
        {
            get => _delayAppearTooltips;
            set => SetProperty(ref _delayAppearTooltips, value);
        }

        [JsonProperty(PropertyName = "tooltips_text_color")]
        public ushort TooltipsTextColor
        {
            get => _tooltipsTextColor;
            set => SetProperty(ref _tooltipsTextColor, value);
        }


        [JsonProperty(PropertyName = "highlight_gameobjects")]
        public bool HighlightGameObjects
        {
            get => _highlightGameObjects;
            set => SetProperty(ref _highlightGameObjects, value);
        }

        [JsonProperty(PropertyName = "smooth_movement")]
        public bool SmoothMovement
        {
            get => _smoothMovement;
            set => SetProperty(ref _smoothMovement, value);
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

        protected virtual void OnPropertyChanged() => PropertyChanged.Raise();
    }

    public static class EqualityHelper
    {
        public static bool IsEqual<T>(T oldValue, T newValue)
        {
            if (oldValue == null && newValue == null)
            {
                return true;
            }

            if (oldValue == null || newValue == null)
            {
                return false;
            }

            Type type = typeof(T);

            if (type.IsValueType)
            {
                return oldValue.Equals(newValue);
            }

            return Equals(oldValue, newValue);
        }
    }
}