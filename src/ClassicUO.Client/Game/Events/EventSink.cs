// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Events
{
    internal static class EventSink
    {
        // ---- Chat ----
        public static event Action<ChatMessageArgs> ChatMessage;
        public static event Action<UnicodeChatMessageArgs> UnicodeChatMessage;
        public static event Action<ClilocMessageArgs> ClilocMessage;

        public static void RaiseChatMessage(in ChatMessageArgs e) => Invoke(ChatMessage, e);
        public static void RaiseUnicodeChatMessage(in UnicodeChatMessageArgs e) => Invoke(UnicodeChatMessage, e);
        public static void RaiseClilocMessage(in ClilocMessageArgs e) => Invoke(ClilocMessage, e);

        // ---- Mobiles ----
        public static event Action<MobileSpawnedArgs> MobileSpawned;
        public static event Action<MobileMovedArgs> MobileMoved;
        public static event Action<MobileRemovedArgs> MobileRemoved;
        public static event Action<MobileAttributesUpdatedArgs> MobileAttributesUpdated;
        public static event Action<HitpointsUpdatedArgs> HitpointsUpdated;
        public static event Action<ManaUpdatedArgs> ManaUpdated;
        public static event Action<StaminaUpdatedArgs> StaminaUpdated;

        public static void RaiseMobileSpawned(in MobileSpawnedArgs e) => Invoke(MobileSpawned, e);
        public static void RaiseMobileMoved(in MobileMovedArgs e) => Invoke(MobileMoved, e);
        public static void RaiseMobileRemoved(in MobileRemovedArgs e) => Invoke(MobileRemoved, e);
        public static void RaiseMobileAttributesUpdated(in MobileAttributesUpdatedArgs e) => Invoke(MobileAttributesUpdated, e);
        public static void RaiseHitpointsUpdated(in HitpointsUpdatedArgs e) => Invoke(HitpointsUpdated, e);
        public static void RaiseManaUpdated(in ManaUpdatedArgs e) => Invoke(ManaUpdated, e);
        public static void RaiseStaminaUpdated(in StaminaUpdatedArgs e) => Invoke(StaminaUpdated, e);

        // ---- Items ----
        public static event Action<ItemSpawnedArgs> ItemSpawned;
        public static event Action<ItemRemovedArgs> ItemRemoved;
        public static event Action<ContainerOpenedArgs> ContainerOpened;
        public static event Action<ItemEquippedArgs> ItemEquipped;

        public static void RaiseItemSpawned(in ItemSpawnedArgs e) => Invoke(ItemSpawned, e);
        public static void RaiseItemRemoved(in ItemRemovedArgs e) => Invoke(ItemRemoved, e);
        public static void RaiseContainerOpened(in ContainerOpenedArgs e) => Invoke(ContainerOpened, e);
        public static void RaiseItemEquipped(in ItemEquippedArgs e) => Invoke(ItemEquipped, e);

        // ---- Combat ----
        public static event Action<DamageReceivedArgs> DamageReceived;
        public static event Action<WarModeChangedArgs> WarModeChanged;
        public static event Action<PlayerDeathArgs> PlayerDeath;

        public static void RaiseDamageReceived(in DamageReceivedArgs e) => Invoke(DamageReceived, e);
        public static void RaiseWarModeChanged(in WarModeChangedArgs e) => Invoke(WarModeChanged, e);
        public static void RaisePlayerDeath(in PlayerDeathArgs e) => Invoke(PlayerDeath, e);

        // ---- World ----
        public static event Action<WeatherChangedArgs> WeatherChanged;
        public static event Action<SeasonChangedArgs> SeasonChanged;
        public static event Action<LightLevelChangedArgs> LightLevelChanged;
        public static event Action<ObjectDeletedArgs> ObjectDeleted;

        public static void RaiseWeatherChanged(in WeatherChangedArgs e) => Invoke(WeatherChanged, e);
        public static void RaiseSeasonChanged(in SeasonChangedArgs e) => Invoke(SeasonChanged, e);
        public static void RaiseLightLevelChanged(in LightLevelChangedArgs e) => Invoke(LightLevelChanged, e);
        public static void RaiseObjectDeleted(in ObjectDeletedArgs e) => Invoke(ObjectDeleted, e);

        // ---- Audio ----
        public static event Action<SoundPlayArgs> SoundPlay;
        public static event Action<MusicPlayArgs> MusicPlay;

        public static void RaiseSoundPlay(in SoundPlayArgs e) => Invoke(SoundPlay, e);
        public static void RaiseMusicPlay(in MusicPlayArgs e) => Invoke(MusicPlay, e);

        // ---- Network / session ----
        public static event Action<ConnectedArgs> Connected;
        public static event Action<DisconnectedArgs> Disconnected;

        public static void RaiseConnected(in ConnectedArgs e) => Invoke(Connected, e);
        public static void RaiseDisconnected(in DisconnectedArgs e) => Invoke(Disconnected, e);

        // ---- Login ----
        public static event Action<LoginCompletedArgs> LoginCompleted;
        public static event Action<LoginRejectedArgs> LoginRejected;

        public static void RaiseLoginCompleted(in LoginCompletedArgs e) => Invoke(LoginCompleted, e);
        public static void RaiseLoginRejected(in LoginRejectedArgs e) => Invoke(LoginRejected, e);

        // ---- UI ----
        public static event Action<GumpOpenedArgs> GumpOpened;
        public static event Action<GumpClosedArgs> GumpClosed;

        public static void RaiseGumpOpened(in GumpOpenedArgs e) => Invoke(GumpOpened, e);
        public static void RaiseGumpClosed(in GumpClosedArgs e) => Invoke(GumpClosed, e);

        private static void Invoke<T>(Action<T> handler, in T args)
        {
            if (handler is null) return;

            foreach (var d in handler.GetInvocationList())
            {
                try
                {
                    ((Action<T>)d)(args);
                }
                catch (Exception ex)
                {
                    Log.Error($"EventSink handler failed for {typeof(T).Name}: {ex}");
                }
            }
        }

        /// <summary>Clears every subscription. Intended for tests only.</summary>
        public static void ClearAll()
        {
            ChatMessage = null;
            UnicodeChatMessage = null;
            ClilocMessage = null;

            MobileSpawned = null;
            MobileMoved = null;
            MobileRemoved = null;
            MobileAttributesUpdated = null;
            HitpointsUpdated = null;
            ManaUpdated = null;
            StaminaUpdated = null;

            ItemSpawned = null;
            ItemRemoved = null;
            ContainerOpened = null;
            ItemEquipped = null;

            DamageReceived = null;
            WarModeChanged = null;
            PlayerDeath = null;

            WeatherChanged = null;
            SeasonChanged = null;
            LightLevelChanged = null;
            ObjectDeleted = null;

            SoundPlay = null;
            MusicPlay = null;

            Connected = null;
            Disconnected = null;

            LoginCompleted = null;
            LoginRejected = null;

            GumpOpened = null;
            GumpClosed = null;
        }
    }
}
