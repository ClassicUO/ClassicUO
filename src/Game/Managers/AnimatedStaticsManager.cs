using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    static class AnimatedStaticsManager
    {
        private static RawList<static_animation_info> _static_infos;

        public static uint ProcessTime;


     

        public static unsafe void Initialize()
        {
            if (_static_infos != null)
                return;

            _static_infos = new RawList<static_animation_info>();
            var file = AnimDataLoader.Instance.AnimDataFile;

            if (file == null)
                return;

            var startAddr = file.StartAddress.ToInt64();
            uint lastaddr = (uint) (startAddr + file.Length - sizeof(AnimDataFrame2));

            for (int i = 0; i < TileDataLoader.Instance.StaticData.Length; i++)
            {
                if (TileDataLoader.Instance.StaticData[i].IsAnimated)
                {
                    var addr = (uint) ((i * 68) + 4 * ((i / 8) + 1));
                    uint offset = (uint) (startAddr + addr);

                    if (offset <= lastaddr)
                    {
                        _static_infos.Add(new static_animation_info()
                        {
                            index = (ushort) i,
                            is_field = StaticFilters.IsField((ushort) i)
                        });
                    }
                }
            }
        }

        public static unsafe void Process()
        {
            if (_static_infos == null || _static_infos.Count == 0 || ProcessTime >= Time.Ticks)
            {
                return;
            }

            var file = AnimDataLoader.Instance.AnimDataFile;

            if (file == null)
                return;

            // fix static animations time to reflect the standard client
            uint delay = Constants.ITEM_EFFECT_ANIMATION_DELAY * 2;
            uint next_time = Time.Ticks + 250;
            bool no_animated_field = ProfileManager.Current != null && ProfileManager.Current.FieldsType != 0;
            var startAddr = file.StartAddress.ToInt64();
            var static_data = ArtLoader.Instance.Entries;

            for (int i = 0; i < _static_infos.Count; i++)
            {
                ref var o = ref _static_infos[i];

                if (no_animated_field && o.is_field)
                {
                    o.anim_index = 0;
                    continue;
                }

                if (o.time < Time.Ticks)
                {
                    var addr = (uint) ((o.index * 68) + 4 * ((o.index / 8) + 1));
                    AnimDataFrame2* info = (AnimDataFrame2*) (startAddr + addr);
                    
                    byte offset = o.anim_index;

                    if (info->FrameInterval > 0)
                    {
                        o.time = Time.Ticks + (info->FrameInterval * delay) + 1;
                    }
                    else
                    {
                        o.time = Time.Ticks + delay;
                    }

                    if (offset < info->FrameCount)
                    {
                        static_data[o.index + 0x4000].AnimOffset = info->FrameData[offset++];
                    }

                    if (offset >= info->FrameCount)
                    {
                        offset = 0;
                    }

                    o.anim_index = offset;
                }

                if (o.time < next_time)
                {
                    next_time = o.time;
                }
            }

            ProcessTime = next_time;
        }

 

        private struct static_animation_info
        {
            public uint time;
            public ushort index;
            public byte anim_index;
            public bool is_field;
        }
    }
}
