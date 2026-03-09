using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using SDL3;
using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    public enum ActionBarSlotType
    {
        Spell = 0,
        Skill = 1,
        Ability = 2
    }

    public class ActionBarSlotData
    {
        public int SlotType { get; set; }
        public int SpellID { get; set; }
        public int SkillIndex { get; set; } = -1;
        public int AbilityIndex { get; set; }
        public int TargetType { get; set; }
        public int Key { get; set; }
        public bool Alt { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }

        public ActionBarSlotData Clone()
        {
            return new ActionBarSlotData
            {
                SlotType = SlotType,
                SpellID = SpellID,
                SkillIndex = SkillIndex,
                AbilityIndex = AbilityIndex,
                TargetType = TargetType,
                Key = Key,
                Alt = Alt,
                Ctrl = Ctrl,
                Shift = Shift
            };
        }
    }

    public class ActionBarManager
    {
        public const int SLOT_COUNT = 12;
        public const int MAX_SLOT_COUNT = 48;
        public const int TARGET_SELF = 0;
        public const int TARGET_LAST = 1;

        private readonly long[] _cooldownEnd = new long[MAX_SLOT_COUNT];
        private readonly long[] _cooldownDuration = new long[MAX_SLOT_COUNT];
        private readonly long[] _glowEnd = new long[MAX_SLOT_COUNT];
        private int _pendingTargetSlot = -1;
        private long _pendingTargetTime;

        public bool IsSlotOnCooldown(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MAX_SLOT_COUNT && Time.Ticks < _cooldownEnd[slotIndex];
        }

        public bool IsSlotGlowing(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MAX_SLOT_COUNT && Time.Ticks < _glowEnd[slotIndex];
        }

        public double GetCooldownPercent(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SLOT_COUNT) return 0;
            long now = Time.Ticks;
            if (now >= _cooldownEnd[slotIndex] || _cooldownDuration[slotIndex] <= 0) return 0;
            return (double)(_cooldownEnd[slotIndex] - now) / _cooldownDuration[slotIndex];
        }

        public bool TryExecuteSlot(SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift, out int executedSlot)
        {
            executedSlot = -1;
            var profile = ProfileManager.CurrentProfile;
            if (profile == null || !profile.ActionBarEnabled || profile.ActionBarSlots == null)
                return false;

            for (int i = 0; i < profile.ActionBarSlots.Count && i < MAX_SLOT_COUNT; i++)
            {
                var slot = profile.ActionBarSlots[i];
                if (!HasValidAction(slot)) continue;
                if ((SDL.SDL_Keycode)slot.Key != key || slot.Alt != alt || slot.Ctrl != ctrl || slot.Shift != shift)
                    continue;
                if (IsSlotOnCooldown(i)) return false;

                ExecuteSlot(i, slot);
                executedSlot = i;
                return true;
            }
            return false;
        }

        private static bool HasValidAction(ActionBarSlotData slot)
        {
            return ((slot.SlotType == (int)ActionBarSlotType.Spell || slot.SlotType == 0) && slot.SpellID > 0) ||
                   (slot.SlotType == (int)ActionBarSlotType.Skill && slot.SkillIndex >= 0) ||
                   (slot.SlotType == (int)ActionBarSlotType.Ability && slot.AbilityIndex > 0);
        }

        private void ExecuteSlot(int slotIndex, ActionBarSlotData slot)
        {
            if (!World.InGame || World.Player == null) return;

            if (slot.SlotType == (int)ActionBarSlotType.Spell)
            {
                var spell = SpellDefinition.FullIndexGetSpell(slot.SpellID);
                if (spell == null || spell.ID <= 0) return;
                GameActions.CastSpell(slot.SpellID);
                _pendingTargetSlot = slotIndex;
                _pendingTargetTime = Time.Ticks + 150;
                double castTime = 1.25;
                long duration = (long)((castTime + 0.5) * 1000);
                _cooldownDuration[slotIndex] = duration;
            }
            else if (slot.SlotType == (int)ActionBarSlotType.Skill)
            {
                if (slot.SkillIndex < 0 || slot.SkillIndex >= World.Player.Skills.Length) return;
                GameActions.UseSkill(slot.SkillIndex);
                long duration = 1500;
                _cooldownDuration[slotIndex] = duration;
            }
            else if (slot.SlotType == (int)ActionBarSlotType.Ability)
            {
                if (slot.AbilityIndex <= 0 || slot.AbilityIndex > AbilityData.Abilities.Length) return;
                GameActions.UseCombatAbility((byte)slot.AbilityIndex);
                long duration = 1500;
                _cooldownDuration[slotIndex] = duration;
            }

            _glowEnd[slotIndex] = Time.Ticks + 400;
            _cooldownEnd[slotIndex] = Time.Ticks + _cooldownDuration[slotIndex];
        }

        public void NotifySpellCast(int spellId)
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile?.ActionBarSlots == null || spellId <= 0) return;
            for (int i = 0; i < profile.ActionBarSlots.Count && i < MAX_SLOT_COUNT; i++)
            {
                var slot = profile.ActionBarSlots[i];
                if (slot.SlotType == (int)ActionBarSlotType.Spell && slot.SpellID == spellId)
                {
                    _glowEnd[i] = Time.Ticks + 400;
                    double castTime = 1.25;
                    long duration = (long)((castTime + 0.5) * 1000);
                    _cooldownDuration[i] = duration;
                    _cooldownEnd[i] = Time.Ticks + duration;
                    break;
                }
            }
        }

        public void NotifySkillUsed(int skillIndex)
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile?.ActionBarSlots == null || skillIndex < 0) return;
            for (int i = 0; i < profile.ActionBarSlots.Count && i < MAX_SLOT_COUNT; i++)
            {
                var slot = profile.ActionBarSlots[i];
                if (slot.SlotType == (int)ActionBarSlotType.Skill && slot.SkillIndex == skillIndex)
                {
                    _glowEnd[i] = Time.Ticks + 400;
                    long duration = 1500;
                    _cooldownDuration[i] = duration;
                    _cooldownEnd[i] = Time.Ticks + duration;
                    break;
                }
            }
        }

        public void NotifyAbilityUsed(int abilityIndex)
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile?.ActionBarSlots == null || abilityIndex <= 0) return;
            for (int i = 0; i < profile.ActionBarSlots.Count && i < MAX_SLOT_COUNT; i++)
            {
                var slot = profile.ActionBarSlots[i];
                if (slot.SlotType == (int)ActionBarSlotType.Ability && slot.AbilityIndex == abilityIndex)
                {
                    _glowEnd[i] = Time.Ticks + 400;
                    long duration = 1500;
                    _cooldownDuration[i] = duration;
                    _cooldownEnd[i] = Time.Ticks + duration;
                    break;
                }
            }
        }

        public void Update()
        {
            if (_pendingTargetSlot < 0) return;
            if (Time.Ticks < _pendingTargetTime) return;

            var profile = ProfileManager.CurrentProfile;
            if (profile == null || _pendingTargetSlot >= profile.ActionBarSlots.Count)
            {
                _pendingTargetSlot = -1;
                return;
            }

            var slot = profile.ActionBarSlots[_pendingTargetSlot];
            if (TargetManager.IsTargeting)
            {
                if (slot.TargetType == TARGET_SELF && World.Player != null)
                    TargetManager.Target(World.Player);
                else if (slot.TargetType == TARGET_LAST && TargetManager.LastTargetInfo.IsEntity)
                    TargetManager.Target(TargetManager.LastTargetInfo.Serial);
            }
            _pendingTargetSlot = -1;
        }
    }
}
