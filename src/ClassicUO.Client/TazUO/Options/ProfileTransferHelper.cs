using ClassicUO.Configuration;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.TazUO.Options
{
    public static class ProfileTransferHelper
    {
        public sealed class ProfileLocationData
        {
            public readonly DirectoryInfo Server;
            public readonly DirectoryInfo Username;
            public readonly DirectoryInfo Character;

            public ProfileLocationData(string server, string username, string character)
            {
                Server = new DirectoryInfo(server);
                Username = new DirectoryInfo(username);
                Character = new DirectoryInfo(character);
            }

            public override string ToString()
            {
                return Character.ToString();
            }
        }

        public static void OverrideAllProfiles(List<ProfileLocationData> allProfiles)
        {
            foreach (ProfileLocationData profile in allProfiles)
            {
                ProfileManager.CurrentProfile.Save(profile.ToString(), false);
            }
        }
    }
}
