using ClassicUO.Game.Data;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.Managers
{
    public class EventSink
    {
        /// <summary>
        /// Invoked when the player is connected to a server
        /// </summary>
        public static event EventHandler<EventArgs> OnConnected;
        public static void InvokeOnConnected(object sender) => OnConnected?.Invoke(sender, EventArgs.Empty);

        /// <summary>
        /// Invoked when the player is connected to a server
        /// </summary>
        public static event EventHandler<EventArgs> OnDisconnected;
        public static void InvokeOnDisconnected(object sender) => OnDisconnected?.Invoke(sender, EventArgs.Empty);

        /// <summary>
        /// Invoked when any message is received from the server after client processing
        /// </summary>
        public static event EventHandler<MessageEventArgs> MessageReceived;
        public static void InvokeMessageReceived(object sender, MessageEventArgs e) => MessageReceived?.Invoke(sender, e);

        /// <summary>
        /// Invoked when any message is received from the server *before* client processing
        /// </summary>
        public static event EventHandler<MessageEventArgs> RawMessageReceived;
        public static void InvokeRawMessageReceived(object sender, MessageEventArgs e) => RawMessageReceived?.Invoke(sender, e);

        /// <summary>
        /// Not currently used. May be removed later or put into use, not sure right now
        /// </summary>
        public static event EventHandler<MessageEventArgs> ClilocMessageReceived;
        public static void InvokeClilocMessageReceived(object sender, MessageEventArgs e) => ClilocMessageReceived?.Invoke(sender, e);

        /// <summary>
        /// Invoked anytime a message is added to the journal
        /// </summary>
        public static event EventHandler<JournalEntry> JournalEntryAdded;
        public static void InvokeJournalEntryAdded(object sender, JournalEntry e) => JournalEntryAdded?.Invoke(sender, e);

        /// <summary>
        /// Invoked anytime we receive object property list data (Tooltip text for items)
        /// </summary>
        public static event EventHandler<OPLEventArgs> OPLOnReceive;
        public static void InvokeOPLOnReceive(object sender, OPLEventArgs e) => OPLOnReceive?.Invoke(sender, e);

        /// <summary>
        /// Invoked when a buff is "added" to a player
        /// </summary>
        public static event EventHandler<BuffEventArgs> OnBuffAdded;
        public static void InvokeOnBuffAdded(object sender, BuffEventArgs e) => OnBuffAdded?.Invoke(sender, e);

        /// <summary>
        /// Invoked when a buff is "removed" to a player (Called before removal)
        /// </summary>
        public static event EventHandler<BuffEventArgs> OnBuffRemoved;
        public static void InvokeOnBuffRemoved(object sender, BuffEventArgs e) => OnBuffRemoved?.Invoke(sender, e);

        /// <summary>
        /// Invoked when the players position is changed
        /// </summary>
        public static event EventHandler<PositionChangedArgs> OnPositionChanged;
        public static void InvokeOnPositionChanged(object sender, PositionChangedArgs e) => OnPositionChanged?.Invoke(sender, e);

        /// <summary>
        /// Invoked when any entity in game receives damage, not neccesarily the player.
        /// </summary>
        public static event EventHandler<int> OnEntityDamage;
        public static void InvokeOnEntityDamage(object sender, int e) => OnEntityDamage?.Invoke(sender, e);

        /// <summary>
        /// Invoked when a container is opened. Sender is the Item, serial is the item serial.
        /// </summary>
        public static event EventHandler<uint> OnOpenContainer;
        public static void InvokeOnOpenContainer(object sender, uint serial) => OnOpenContainer?.Invoke(sender, serial);

        /// <summary>
        /// Invoked when the player receives a death packet from the server
        /// </summary>
        public static event EventHandler<uint> OnPlayerDeath;
        public static void InvokeOnPlayerDeath(object sender, uint serial) => OnPlayerDeath?.Invoke(sender, serial);

        /// <summary>
        /// Invoked when the player or server tells the client to path find
        /// Vector is X, Y, Z and Distance
        /// </summary>
        public static event EventHandler<Vector4> OnPathFinding;
        public static void InvokeOnPathFinding(object sender, Vector4 e) => OnPathFinding?.Invoke(sender, e);

        /// <summary>
        /// Invoked when the server asks the client to generate some weather
        /// </summary>
        public static event EventHandler<WeatherEventArgs> OnSetWeather;
        public static void InvokeOnSetWeather(object sender, WeatherEventArgs e) => OnSetWeather?.Invoke(sender, e);

        /// <summary>
        /// Invoked when a stat of the player is changed(min or max). Currently only Hits is set up.
        /// </summary>
        public static event EventHandler<PlayerStatChangedArgs> OnPlayerStatChange;
        public static void InvokeOnPlayerStatChange(object sender, PlayerStatChangedArgs e) => OnPlayerStatChange?.Invoke(sender, e);

        /// <summary>
        /// This  occurs *before* any TazUO tooltip processing occurs allowing you to modify it before processing happens
        /// </summary>
        public static PreProcessTooltipDelegate PreProcessTooltip;
        public delegate void PreProcessTooltipDelegate(ref ItemPropertiesData e);

        /// <summary>
        /// This event occurs *after* TazUO tooltip processing, this is the final string before being rendered into a tooltip window
        /// </summary>
        public static PostProcessTooltipDelegate PostProcessTooltip;
        public delegate void PostProcessTooltipDelegate(ref string e);

        /// <summary>
        /// This event occurs every game update, essentially the game tick. Be careful with this, it happens many many times per second.
        /// </summary>
        public static GameUpdateDelegate GameUpdate;
        public delegate void GameUpdateDelegate();
    }

    public class OPLEventArgs : EventArgs
    {
        public readonly uint Serial;
        public readonly string Name;
        public readonly string Data;

        public OPLEventArgs(uint serial, string name, string data)
        {
            Serial = serial;
            Name = name;
            Data = data;
        }
    }

    public class BuffEventArgs : EventArgs
    {
        public BuffEventArgs(BuffIcon buff)
        {
            Buff = buff;
        }

        public BuffIcon Buff { get; }
    }

    public class PositionChangedArgs : EventArgs
    {
        public PositionChangedArgs(Vector3 newlocation)
        {
            Newlocation = newlocation;
        }

        public Vector3 Newlocation { get; }
    }

    public class WeatherEventArgs : EventArgs
    {
        public WeatherEventArgs(WeatherType type, byte count, byte temp)
        {
            Type = type;
            Count = count;
            Temp = temp;
        }

        public WeatherType Type { get; }
        public byte Count { get; }
        public byte Temp { get; }
    }

    public class PlayerStatChangedArgs : EventArgs
    {
        public PlayerStatChangedArgs(PlayerStat stat, int oldValue, int newValue)
        {
            Stat = stat;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public PlayerStat Stat { get; }
        public int OldValue { get; }
        public int NewValue { get; }

        public enum PlayerStat
        {
            Hits,
            HitsMax,
            Mana,
            ManaMax,
            Stamina,
            StaminaMax
        }
    }
}
