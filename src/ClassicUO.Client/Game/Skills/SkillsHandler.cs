// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Resources;

namespace ClassicUO.Game.Skills
{
    /// <summary>
    /// Listens for skill list and skill update events; rebuilds the cached
    /// skill table and updates open skill gumps with delta notifications.
    /// </summary>
    internal sealed class SkillsHandler : IEventListener
    {
        private readonly World _world;

        public SkillsHandler(World world)
        {
            _world = world;
        }

        public void Subscribe()
        {
            EventSink.SkillListReceived += OnSkillListReceived;
            EventSink.SkillsUpdated += OnSkillsUpdated;
        }

        public void Unsubscribe()
        {
            EventSink.SkillListReceived -= OnSkillListReceived;
            EventSink.SkillsUpdated -= OnSkillsUpdated;
        }

        private void OnSkillListReceived(SkillListReceivedArgs e)
        {
            if (!_world.InGame) return;

            var skills = Client.Game.UO.FileManager.Skills.Skills;
            var sorted = Client.Game.UO.FileManager.Skills.SortedSkills;

            skills.Clear();
            sorted.Clear();

            var entries = e.Entries;
            int count = entries?.Count ?? 0;

            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];
                skills.Add(new SkillEntry(entry.Index, entry.Name, entry.HasButton));
            }

            sorted.AddRange(skills);
            sorted.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture));
        }

        private void OnSkillsUpdated(SkillsUpdatedArgs e)
        {
            if (!_world.InGame) return;

            byte type = e.UpdateType;
            bool isSingleUpdate = e.IsSingleUpdate;

            StandardSkillsGump standard = null;
            SkillGumpAdvanced advanced = null;

            if (ProfileManager.CurrentProfile.StandardSkillsGump)
            {
                standard = UIManager.GetGump<StandardSkillsGump>();
            }
            else
            {
                advanced = UIManager.GetGump<SkillGumpAdvanced>();
            }

            if (!isSingleUpdate && (type == 1 || type == 3 || _world.SkillsRequested))
            {
                _world.SkillsRequested = false;

                // TODO: make a base class for this gump
                if (ProfileManager.CurrentProfile.StandardSkillsGump)
                {
                    if (standard == null)
                    {
                        UIManager.Add(standard = new StandardSkillsGump(_world) { X = 100, Y = 100 });
                    }
                }
                else
                {
                    if (advanced == null)
                    {
                        UIManager.Add(advanced = new SkillGumpAdvanced(_world) { X = 100, Y = 100 });
                    }
                }
            }

            var updates = e.Updates;
            int count = updates?.Count ?? 0;

            for (int i = 0; i < count; i++)
            {
                var u = updates[i];
                ushort id = u.Id;

                if (id >= _world.Player.Skills.Length)
                {
                    continue;
                }

                Skill skill = _world.Player.Skills[id];
                if (skill == null)
                {
                    continue;
                }

                if (isSingleUpdate)
                {
                    float change = u.RealValue / 10.0f - skill.Value;

                    if (
                        change != 0.0f
                        && !float.IsNaN(change)
                        && ProfileManager.CurrentProfile != null
                        && ProfileManager.CurrentProfile.ShowSkillsChangedMessage
                        && Math.Abs(change * 10)
                            >= ProfileManager.CurrentProfile.ShowSkillsChangedDeltaValue
                    )
                    {
                        GameActions.Print(
                            _world,
                            string.Format(
                                ResGeneral.YourSkillIn0Has1By2ItIsNow3,
                                skill.Name,
                                change < 0
                                    ? ResGeneral.Decreased
                                    : ResGeneral.Increased,
                                Math.Abs(change),
                                skill.Value + change
                            ),
                            0x58,
                            MessageType.System,
                            3,
                            false
                        );
                    }
                }

                skill.BaseFixed = u.BaseValue;
                skill.ValueFixed = u.RealValue;
                skill.CapFixed = u.Cap;
                skill.Lock = (Lock)u.LockState;

                standard?.Update(id);
                advanced?.ForceUpdate();
            }
        }
    }
}
