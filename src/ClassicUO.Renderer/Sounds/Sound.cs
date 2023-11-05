using ClassicUO.Assets;
using ClassicUO.IO.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Renderer.Sounds
{
    public sealed class Sound
    {
        const int MAX_SOUND_DATA_INDEX_COUNT = 0xFFFF;

        private readonly IO.Audio.Sound[] _musics = new IO.Audio.Sound[MAX_SOUND_DATA_INDEX_COUNT];
        private readonly IO.Audio.Sound[] _sounds = new IO.Audio.Sound[MAX_SOUND_DATA_INDEX_COUNT];
        private readonly bool _useDigitalMusicFolder;


        public Sound()
        {
            _useDigitalMusicFolder = Directory.Exists(Path.Combine(UOFileManager.BasePath, "Music", "Digital"));
        }

        public IO.Audio.Sound GetSound(int index)
        {
            if (index >= 0 && index < MAX_SOUND_DATA_INDEX_COUNT)
            {
                ref IO.Audio.Sound sound = ref _sounds[index];

                if (sound == null && SoundsLoader.Instance.TryGetSound(index, out byte[] data, out string name))
                {
                    sound = new UOSound(name, index, data);
                }

                return sound;
            }

            return null;
        }

        public IO.Audio.Sound GetMusic(int index)
        {
            if (index >= 0 && index < MAX_SOUND_DATA_INDEX_COUNT)
            {
                ref IO.Audio.Sound music = ref _musics[index];

                if (music == null && SoundsLoader.Instance.TryGetMusicData(index, out string name, out bool loop))
                {
                    var path = _useDigitalMusicFolder ? $"Music/Digital/{name}" : $"Music/{name}";
                    if (!path.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase))
                    {
                        path += ".mp3";
                    }

                    music = new UOMusic(index, name, loop, UOFileManager.GetUOFilePath(path));
                }

                return music;
            }

            return null;
        }
    }
}
