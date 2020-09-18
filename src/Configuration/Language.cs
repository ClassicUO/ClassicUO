#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

namespace ClassicUO.Configuration
{
    using ClassicUO.Resources;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public static class Language
    {
        public static IReadOnlyDictionary<string, string> SupportedLanguages = CreateSupportedLanguages();

        public static string[] GetSupportedLanguages()
        {
            return SupportedLanguages.Values.ToArray();
        }

        private static IReadOnlyDictionary<string, string> CreateSupportedLanguages()
        {
            return new Dictionary<string, string>
            {
                { "en", ResGeneral.English },
                { "cs", ResGeneral.Czech },
            };
        }

        public static void ChangeLanguage(int languageIndex)
        {
            Settings.GlobalSettings.Language = languageIndex;
            CultureInfo cultureInfo = new CultureInfo(SupportedLanguages.ElementAt(languageIndex).Key);
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = cultureInfo;
        }
    }
}