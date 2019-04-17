using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.IO.Resources
{
    internal class SkillsLoader : ResourceLoader
    {
        private UOFileMul _file;
        private readonly Dictionary<int, SkillEntry> _skills = new Dictionary<int, SkillEntry>();

        public int SkillsCount => _skills.Count;

        public string[] SkillNames { get; private set; }

        public override void Load()
        {
            if (SkillsCount > 0)
                return;
            string path = Path.Combine(FileManager.UoFolderPath, "skills.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "Skills.idx");

            if (!File.Exists(path) || !File.Exists(pathidx))
                throw new FileNotFoundException();
            _file = new UOFileMul(path, pathidx, 56, 16);
            int i = 0;
            while (_file.Position < _file.Length) GetSkill(i++);

            SkillNames = _skills.Select(o => o.Value.Name).ToArray();
        }

        protected override void CleanResources()
        {
            throw new NotImplementedException();
        }

        public SkillEntry GetSkill(int index)
        {
            if (!_skills.TryGetValue(index, out SkillEntry value))
            {
                (int length, int extra, bool patched) = _file.SeekByEntryIndex(index);

                if (length == 0)
                    return default;

	            var hasAction = _file.ReadBool();
	            var name = Encoding.UTF8.GetString(_file.ReadArray<byte>(length - 1)).TrimEnd('\0');

				_skills[index] = new SkillEntry(index, name, hasAction); 
            }

            return value;
        }
    }

    internal readonly struct SkillEntry
    {
        public SkillEntry(int index, string name, bool hasAction)
        {
            Index = index;
            Name = name;
            HasAction = hasAction;
        }

        public readonly int Index;
        public readonly string Name;
        public readonly bool HasAction;
    }
}
