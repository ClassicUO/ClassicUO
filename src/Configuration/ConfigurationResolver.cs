using System.IO;
using System.Text.RegularExpressions;
using ClassicUO.Utility.Logging;
using TinyJson;

namespace ClassicUO.Configuration
{
    internal static class ConfigurationResolver
    {
        public static T Load<T>(string file) where T : class
        {
            if (!File.Exists(file))
            {
                Log.Warn(file + " not found.");

                return null;
            }

            string text = File.ReadAllText(file);

            text = Regex.Replace
            (
                text, @"(?<!\\)  # lookbehind: Check that previous character isn't a \
                                                \\         # match a \
                                                (?!\\)     # lookahead: Check that the following character isn't a \",
                @"\\", RegexOptions.IgnorePatternWhitespace
            );

            T settings = text.Decode<T>();

            return settings;
        }

        public static void Save<T>(T obj, string file) where T : class
        {
            // this try catch is necessary when multiples cuo instances points to this file.
            try
            {
                File.WriteAllText(file, obj.Encode(true));
            }
            catch (IOException e)
            {
                Log.Error(e.ToString());
            }
        }
    }
}