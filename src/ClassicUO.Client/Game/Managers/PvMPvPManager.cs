#region license
// Copyright (c) 2021, andreakarasho
// All rights reserved.
#endregion

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using System;
using ClassicUO;
using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    internal sealed class PvMPvPManager
    {
        public static PvMPvPManager Instance => _instance ??= new PvMPvPManager();
        private static PvMPvPManager _instance;

        private readonly Dictionary<uint, DamageEntry> _damageBySerial = new Dictionary<uint, DamageEntry>();
        private int _sessionKillCount;
        private bool _subscribed;
        private bool _lowHpAlertFired;
        private uint _lowHpAlertSerial;
        private const int LOW_HP_PERCENT = 20;

        private ulong _greyCriminalStartTicks;
        private const int GREY_CRIMINAL_DURATION_SEC = 120;
        private uint _criminalAlertCooldownTicks;
        private const int CRIMINAL_ALERT_COOLDOWN_MS = 10000;
        public bool CriminalOrAttackableNearby { get; private set; }
        public int GreyCriminalSecondsRemaining { get; private set; }

        private struct DamageEntry
        {
            public int TotalDamage;
            public DateTime FirstHitUtc;
        }

        public int SessionKillCount => _sessionKillCount;

        public void OnSceneLoad()
        {
            if (_subscribed)
                return;
            EventSink.OnEntityDamage += OnEntityDamage;
            EventSink.OnDisconnected += OnDisconnected;
            EventSink.GameUpdate += OnGameUpdateTick;
            _subscribed = true;
            _sessionKillCount = 0;
            _damageBySerial.Clear();
            _greyCriminalStartTicks = 0;
        }

        public void OnSceneUnload()
        {
            if (!_subscribed)
                return;
            EventSink.OnEntityDamage -= OnEntityDamage;
            EventSink.OnDisconnected -= OnDisconnected;
            EventSink.GameUpdate -= OnGameUpdateTick;
            _subscribed = false;
        }

        public void OnPlayerNotorietyChanged(NotorietyFlag flag)
        {
            if (flag == NotorietyFlag.Gray || flag == NotorietyFlag.Criminal)
                _greyCriminalStartTicks = Time.Ticks;
        }

        private void OnGameUpdateTick()
        {
            if (!World.InGame || World.Player == null)
                return;
            var profile = ProfileManager.CurrentProfile;
            if (profile?.PvP_CriminalAttackableAlert == true)
            {
                CriminalOrAttackableNearby = false;
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m == World.Player || m.IsDestroyed) continue;
                    if (m.Distance > 12) continue;
                    if (m.NotorietyFlag == NotorietyFlag.Criminal || m.NotorietyFlag == NotorietyFlag.Gray || m.NotorietyFlag == NotorietyFlag.Enemy || m.NotorietyFlag == NotorietyFlag.Murderer)
                    {
                        CriminalOrAttackableNearby = true;
                        if (Time.Ticks - _criminalAlertCooldownTicks > CRIMINAL_ALERT_COOLDOWN_MS)
                        {
                            _criminalAlertCooldownTicks = Time.Ticks;
                            GameActions.Print("Criminal / attackable nearby!", 0x0026);
                            if (profile.PvX_ConfigurableSoundsPerEvent && profile.PvX_SoundCriminalAlert != 0)
                                Client.Game.Audio.PlaySound(profile.PvX_SoundCriminalAlert);
                        }
                        break;
                    }
                }
            }
            if (profile?.PvX_NameOverheadProfilesByContext == true && World.Player != null)
            {
                var flags = World.Player.InWarMode ? profile.PvP_NameOverheadProfileFlags : profile.PvM_NameOverheadProfileFlags;
                NameOverHeadManager.ActiveOverheadOptions = (NameOverheadOptions)flags;
            }
            var flag = World.Player.NotorietyFlag;
            if (flag == NotorietyFlag.Gray || flag == NotorietyFlag.Criminal)
            {
                if (_greyCriminalStartTicks == 0)
                    _greyCriminalStartTicks = Time.Ticks;
                ulong elapsed = (Time.Ticks - _greyCriminalStartTicks) / 1000;
                GreyCriminalSecondsRemaining = Math.Max(0, GREY_CRIMINAL_DURATION_SEC - (int)elapsed);
            }
            else
            {
                _greyCriminalStartTicks = 0;
                GreyCriminalSecondsRemaining = 0;
            }
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            _damageBySerial.Clear();
            _sessionKillCount = 0;
        }

        private void OnEntityDamage(object sender, int damage)
        {
            if (sender is not Entity entity || damage <= 0)
                return;
            uint serial = entity.Serial;
            if (!_damageBySerial.TryGetValue(serial, out var entry))
            {
                entry.FirstHitUtc = DateTime.UtcNow;
                entry.TotalDamage = 0;
            }
            entry.TotalDamage += damage;
            _damageBySerial[serial] = entry;
        }

        public void NotifyMobileDeath(uint serial)
        {
            if (serial == TargetManager.LastAttack || serial == TargetManager.LastTargetInfo.Serial)
                _sessionKillCount++;
        }

        public void CheckLowHpAlert(uint serial, int hits, int hitsMax)
        {
            if (ProfileManager.CurrentProfile?.PvM_LowHpAlertOnLastTarget != true || hitsMax <= 0)
                return;
            uint lt = TargetManager.LastAttack != 0 ? TargetManager.LastAttack : TargetManager.LastTargetInfo.Serial;
            if (serial != lt || serial == 0)
                return;
            int pct = (int)(100.0 * hits / hitsMax);
            if (pct > LOW_HP_PERCENT)
            {
                _lowHpAlertFired = false;
                return;
            }
            if (_lowHpAlertFired && _lowHpAlertSerial == serial)
                return;
            _lowHpAlertFired = true;
            _lowHpAlertSerial = serial;
            GameActions.Print($"Last target low HP: {pct}%", 0x0026);
        }

        public int GetTotalDamage(uint serial)
        {
            return _damageBySerial.TryGetValue(serial, out var e) ? e.TotalDamage : 0;
        }

        public double GetDPS(uint serial)
        {
            if (!_damageBySerial.TryGetValue(serial, out var e) || e.TotalDamage == 0)
                return 0;
            double elapsed = (DateTime.UtcNow - e.FirstHitUtc).TotalSeconds;
            return elapsed > 0 ? e.TotalDamage / elapsed : 0;
        }

        public void ResetDamageFor(uint serial)
        {
            _damageBySerial.Remove(serial);
        }

        public string GetDamageCounterText(uint serial)
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile?.PvM_DamageCounterOnLastTarget != true && profile?.PvM_AggroIndicatorOnHealthBar != true)
                return string.Empty;
            string aggro = profile.PvM_AggroIndicatorOnHealthBar && serial == TargetManager.LastAttack && serial != 0 ? "Aggro" : string.Empty;
            if (profile?.PvM_DamageCounterOnLastTarget != true)
                return aggro;
            int total = GetTotalDamage(serial);
            double dps = GetDPS(serial);
            string dmg = total > 0 ? $"{total} | {dps:F1} DPS" : string.Empty;
            string kills = profile.PvM_KillCountMarkerPerSession && SessionKillCount > 0
                ? (string.IsNullOrEmpty(dmg) ? $"Kills: {SessionKillCount}" : $" | Kills: {SessionKillCount}")
                : string.Empty;
            string line = dmg + kills;
            if (!string.IsNullOrEmpty(aggro))
                line = string.IsNullOrEmpty(line) ? aggro : aggro + " | " + line;
            return line;
        }

        public string GetDamageCounterTextForOverhead(uint serial)
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile?.PvM_DamageCounterAsOverhead != true || serial == 0 || serial != TargetManager.LastAttack)
                return string.Empty;
            string aggro = profile.PvM_AggroIndicatorOnHealthBar ? "Aggro" : string.Empty;
            int total = GetTotalDamage(serial);
            double dps = GetDPS(serial);
            string dmg = total > 0 ? $"{total} | {dps:F1} DPS" : "0 | 0 DPS";
            string kills = profile.PvM_KillCountMarkerPerSession && SessionKillCount > 0
                ? $" | Kills: {SessionKillCount}"
                : string.Empty;
            string line = dmg + kills;
            if (!string.IsNullOrEmpty(aggro))
                line = aggro + " | " + line;
            return line;
        }
    }
}
