// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeExtendedMisc()
        {
            EventSink.MapPatchesEnabled += OnMapPatchesEnabled;
            EventSink.AbilityIconsReset += OnAbilityIconsReset;
            EventSink.DamageOverhead += OnDamageOverhead;
            EventSink.SpellIconToggle += OnSpellIconToggle;
            EventSink.CharacterSpeedMode += OnCharacterSpeedMode;
            EventSink.MobileAnimationFrame += OnMobileAnimationFrame;
            EventSink.CuoCommand += OnCuoCommand;
        }

        private void UnsubscribeExtendedMisc()
        {
            EventSink.MapPatchesEnabled -= OnMapPatchesEnabled;
            EventSink.AbilityIconsReset -= OnAbilityIconsReset;
            EventSink.DamageOverhead -= OnDamageOverhead;
            EventSink.SpellIconToggle -= OnSpellIconToggle;
            EventSink.CharacterSpeedMode -= OnCharacterSpeedMode;
            EventSink.MobileAnimationFrame -= OnMobileAnimationFrame;
            EventSink.CuoCommand -= OnCuoCommand;
        }

        private void OnMapPatchesEnabled(MapPatchesEnabledArgs e)
        {
            if (Client.Game.UO.FileManager.Maps.ApplyPatches(e.PatchesCount, e.Entries))
            {
                int map = MapIndex;
                MapIndex = -1;
                MapIndex = map;

                Log.Trace("Map Patches applied.");
            }
        }

        private void OnAbilityIconsReset(AbilityIconsResetArgs e)
        {
            if (Player == null) return;

            for (int i = 0; i < 2; i++)
            {
                Player.Abilities[i] &= (Ability)0x7F;
            }
        }

        private void OnDamageOverhead(DamageOverheadArgs e)
        {
            Entity en = Get(e.Serial);
            if (en == null) return;
            if (e.Damage == 0) return;

            WorldTextManager.AddDamage(en, e.Damage);
        }

        private void OnSpellIconToggle(SpellIconToggleArgs e)
        {
            ushort spell = e.SpellId;

            foreach (Gump g in UIManager.Gumps)
            {
                if (!g.IsDisposed && g.IsVisible)
                {
                    if (g is UseSpellButtonGump spellButton && spellButton.SpellID == spell)
                    {
                        if (e.Active)
                        {
                            spellButton.Hue = 38;
                            ActiveSpellIcons.Add(spell);
                        }
                        else
                        {
                            spellButton.Hue = 0;
                            ActiveSpellIcons.Remove(spell);
                        }

                        break;
                    }
                }
            }
        }

        private void OnCharacterSpeedMode(CharacterSpeedModeArgs e)
        {
            if (Player == null) return;

            byte val = e.SpeedMode;
            if (val > (int)CharacterSpeedType.FastUnmountAndCantRun)
            {
                val = 0;
            }

            Player.SpeedMode = (CharacterSpeedType)val;
        }

        private void OnMobileAnimationFrame(MobileAnimationFrameArgs e)
        {
            ushort partial = e.PartialSerial;
            byte animID = e.AnimationId;
            byte frameCount = e.FrameCount;

            foreach (Mobile m in Mobiles.Values)
            {
                if ((m.Serial & 0xFFFF) == partial)
                {
                    m.SetAnimation(animID);
                    m.AnimIndex = frameCount;
                    m.ExecuteAnimation = false;
                    break;
                }
            }
        }

        private void OnCuoCommand(CuoCommandArgs e)
        {
            // Original handler simply read the type byte and dropped it on the floor.
            // Reserved for future server-driven ClassicUO extensions.
            _ = e.Type;
        }
    }
}
