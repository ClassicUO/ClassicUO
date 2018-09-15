using LiteDB;

namespace ClassicUO.Configuration
{
    class SettingsController
    {
        public static SettingsModel Load(string profileName)
        {
            using (var db = new LiteDatabase(@"./settings.db"))
            {
                var settings = db.GetCollection<SettingsModel>("settings");
                settings.EnsureIndex(x => x.ProfileName);

                if (settings.FindOne(x => x.ProfileName.StartsWith(profileName)) == null)
                {
                    SettingsModel currentProfile = new SettingsModel
                    {
                        ProfileName = "Default",
                        Username = "",
                        Password = "",
                        LastCharacterName = "",
                        IP = "",
                        Port = 2599,
                        UltimaOnlineDirectory = "",
                        ClientVersion = "7.0.59.8"
                    };
                    settings.Insert(currentProfile);
                    
                }
                var result = settings.FindOne(x => x.ProfileName.StartsWith(profileName));
                return result;

            }
           
            
        }

        
    }
}
