// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Configuration
{
    internal interface IProfileProvider
    {
        Profile CurrentProfile { get; }
        GlobalProfile GlobalProfile { get; }
        string ProfilePath { get; }

        void Load(string servername, string username, string charactername);
        void UnLoadProfile();
        void Save(Profile profile, string path, string filename = "profile.json");
        void SetProfileAsDefault(Profile profile);
        Profile NewFromDefault();
    }
}
