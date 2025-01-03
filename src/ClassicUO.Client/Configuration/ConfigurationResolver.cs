// SPDX-License-Identifier: BSD-2-Clause

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration
{
    internal static class ConfigurationResolver
    {
        public static T Load<T>(string file, JsonTypeInfo<T> ctx) where T : class
        {
            if (!File.Exists(file))
            {
                Log.Warn(file + " not found.");

                return null;
            }

            var text = File.ReadAllText(file);

            text = Regex.Replace
            (
                text,
                @"(?<!\\)  # lookbehind: Check that previous character isn't a \
                                                \\         # match a \
                                                (?!\\)     # lookahead: Check that the following character isn't a \",
                @"\\",
                RegexOptions.IgnorePatternWhitespace
            );

            return JsonSerializer.Deserialize(text, ctx);
        }

        public static void Save<T>(T obj, string file, JsonTypeInfo<T> ctx) where T : class
        {
            // this try catch is necessary when multiples cuo instances points to this file.
            try
            {
                var fileInfo = new FileInfo(file);

                if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }

                var json = JsonSerializer.Serialize(obj, ctx);
                File.WriteAllText(file, json);
            }
            catch (IOException e)
            {
                Log.Error(e.ToString());
            }
        }
    }
}