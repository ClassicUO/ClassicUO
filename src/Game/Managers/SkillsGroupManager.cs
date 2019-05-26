using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.IO;

namespace ClassicUO.Game.Managers
{
    static class SkillsGroupManager
    {
        private static readonly Dictionary<string, List<int>> _groups = new Dictionary<string, List<int>>();

        public static Dictionary<string, List<int>> Groups => _groups;


        public static void MakeDefault()
        {
            _groups.Clear();

            int count = FileManager.Skills.SkillsCount;

            _groups.Add("Miscellaneous", new List<int>()
                {
                    4, 6, 10, 12, 19, 3, 36
                }
            );

            _groups.Add("Combat", new List<int>()
                {
                    1, 31, 42, 17, 41, 5, 40, 27
                }
            );

            if (count > 57)
                _groups["Combat"].Add(57);
            _groups["Combat"].Add(43);
            if (count > 50)
                _groups["Combat"].Add(50);
            if (count > 51)
                _groups["Combat"].Add(51);
            if (count > 52)
                _groups["Combat"].Add(52);
            if (count > 53)
                _groups["Combat"].Add(53);


            _groups.Add("Trade Skills", new List<int>()
                {
                    0, 7, 8, 11, 13, 23, 44, 45, 34, 37
                }
            );

            _groups.Add("Magic", new List<int>()
            {
                16
            });

            if (count > 56)
                _groups["Magic"].Add(56);
            _groups["Magic"].Add(25);
            _groups["Magic"].Add(46);
            if (count > 55)
                _groups["Magic"].Add(55);
            _groups["Magic"].Add(26);
            if (count > 54)
                _groups["Magic"].Add(54);
            _groups["Magic"].Add(32);
            if (count > 49)
                _groups["Magic"].Add(49);


            _groups.Add("Wilderness", new List<int>()
                {
                    2, 35, 18, 20, 38, 39
                }
            );

            _groups.Add("Thieving", new List<int>()
                {
                    14, 21, 24, 30, 48, 28, 33, 47
                }
            );

            _groups.Add("Bard", new List<int>()
                {
                    15, 29, 9, 22
                }
            );
        }


        public static bool AddNewGroup(string group)
        {
            if (!_groups.ContainsKey(group))
            {
                _groups.Add(group, new List<int>());

                return true;
            }

            return false;
        }

        public static void RemoveGroup(string group)
        {
            if (_groups.TryGetValue(group, out var list))
            {
                _groups.Remove(group);

                if (_groups.Count == 0)
                {
                    _groups.Add("All", list);
                }
                else
                {
                    _groups.FirstOrDefault().Value.AddRange(list);
                }
            }
        }

        public static List<int> GetSkillsInGroup(string group)
        {
            _groups.TryGetValue(group, out var list);

            return list;
        }

        public static void ReplaceGroup(string oldGroup, string newGroup)
        {
            if (_groups.TryGetValue(oldGroup, out var oldList) && !_groups.TryGetValue(newGroup, out var newList))
            {
                _groups.Remove(oldGroup);
                _groups[newGroup] = oldList;
            }
        }

        public static void Load(BinaryReader reader)
        {
            try
            {
                {
                    int version = reader.ReadInt32();

                    int groupCount = reader.ReadInt32();

                    for (int i = 0; i < groupCount; i++)
                    {
                        int entriesCount = reader.ReadInt32();
                        string groupName = reader.ReadUTF8String(reader.ReadInt32());

                        if (!_groups.TryGetValue(groupName, out var list) || list == null)
                        {
                            list = new List<int>();
                            _groups[groupName] = list;
                        }

                        for (int j = 0; j < entriesCount; j++)
                        {
                            int skillIndex = reader.ReadInt32();
                            list.Add(skillIndex);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Log.Message(LogTypes.Error, "skillgroups.bin loading failed.\r\n" + e.StackTrace);
            }
        }

        public static void Save(BinaryWriter writer)
        {
            {
                // version
                writer.Write(1);

                writer.Write(_groups.Count);

                foreach (KeyValuePair<string, List<int>> k in _groups)
                {
                    writer.Write(k.Value.Count);

                    writer.Write(k.Key.Length);
                    writer.WriteUTF8String(k.Key);
                    foreach (int i in k.Value)
                    {
                        writer.Write(i);
                    }
                }
            }
        }
    }
}
