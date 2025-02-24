// SPDX-License-Identifier: BSD-2-Clause

using System.Collections;
using System.Collections.Generic;

namespace ClassicUO.Game.GameObjects
{
    static class DictExt
    {
        public static T Get<T>(this Dictionary<uint, T> dict, uint serial) where T : Entity
        {
            dict.TryGetValue(serial, out var v);

            return v;
        }

        public static bool Contains<T>(this Dictionary<uint, T> dict, uint serial) where T : Entity
        {
            return dict.ContainsKey(serial);
        }

        public static bool Add<T>(this Dictionary<uint, T> dict, T entity) where T : Entity
        {
            if (dict.ContainsKey(entity.Serial))
            {
                return false;
            }

            dict[entity.Serial] = entity;

            return true;
        }
    }
}