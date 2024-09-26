#region license

// Copyright (c) 2021, jaedan
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using FontStashSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class TrueTypeLoader
    {
        public const string EMBEDDED_FONT = "Roboto-Regular";

        private Dictionary<string, FontSystem> _fonts = new();

        private TrueTypeLoader()
        {
        }

        private static TrueTypeLoader _instance;
        public static TrueTypeLoader Instance => _instance ??= new TrueTypeLoader();

        public Task Load()
        {
            var settings = new FontSystemSettings
            {
                FontResolutionFactor = 1,
                KernelWidth = 1,
                KernelHeight = 1
            };

            string _fontPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Fonts");

            if (!Directory.Exists(_fontPath))
                Directory.CreateDirectory(_fontPath);

            foreach (var ttf in Directory.GetFiles(_fontPath, "*.ttf"))
            {
                var fontSystem = new FontSystem(settings);
                fontSystem.AddFont(File.ReadAllBytes(ttf));

                _fonts[Path.GetFileNameWithoutExtension(ttf)] = fontSystem;
            }

            if (!_fonts.ContainsKey("Roboto-Regular"))
            {
                var assembly = this.GetType().Assembly;
                var resourceName = assembly.GetName().Name + ".Roboto-Regular.ttf";
                System.Console.WriteLine(resourceName);
                Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    var memoryStream = new MemoryStream();

                    stream.CopyTo(memoryStream);
                    var fontSystem = new FontSystem(settings);
                    fontSystem.AddFont(memoryStream.ToArray());
                    _fonts["Roboto-Regular"] = fontSystem;
                }
            }

            return Task.CompletedTask;
        }

        public SpriteFontBase GetFont(string name, float size)
        {
            if (_fonts.TryGetValue(name, out var font))
            {
                return font.GetFont(size);
            }

            if (_fonts.Count > 0)
                return _fonts.First().Value.GetFont(size);

            return null;
        }

        public SpriteFontBase GetFont(string name)
        {
            return GetFont(name, 12);
        }

        public string[] Fonts => _fonts.Keys.ToArray();
    }
}