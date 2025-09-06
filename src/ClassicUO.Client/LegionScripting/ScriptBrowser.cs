using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;

namespace ClassicUO.LegionScripting
{
    internal class ScriptBrowser : Gump
    {
        private const int WIDTH = 400;
        private const int HEIGHT = 600;
        private const string REPO = "bittiez/PublicLegionScripts";

        public static readonly HttpClient client = new HttpClient();
        private ScrollArea scrollArea;
        private string lastPath = "";
        public ScriptBrowser() : base(0, 0)
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));

            CanMove = true;
            CanCloseWithRightClick = true;

            Width = WIDTH;
            Height = HEIGHT;

            Add(new AlphaBlendControl() { Width = Width, Height = Height });
            Add(scrollArea = new ScrollArea(0, 0, Width, Height, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            GetFilesAsync().ContinueWith((r) =>
            {
                SetFiles(r.Result);
            });

            CenterXInViewPort();
            CenterYInViewPort();
        }

        public void SetFiles(List<GHFileObject> files)
        {
            while (scrollArea.Children.Count > 1)
                scrollArea.Children[1].Dispose();

            string lastP = lastPath;
            if (lastPath.Length > 0)
            {
                lastP = Path.GetDirectoryName(lastPath);
                scrollArea.Add(new ItemControl(new GHFileObject() { type = "dir", name = $"<- Back{(string.IsNullOrEmpty(lastP)? "" : $" ({lastP})")}", path = lastP }, this));

            }

            foreach (GHFileObject file in files)
            {
                if (file.type == "file" && !file.name.EndsWith(".lscript"))
                    continue;

                scrollArea.Add(new ItemControl(file, this));
            }

            int y = 0;
            foreach (Control c in scrollArea.Children)
            {
                if (c is not ItemControl)
                    continue;

                c.Y = y;
                y += c.Height + 3;
            }
        }

        private async Task<List<GHFileObject>> GetFilesAsync(string path = "")
        {
            try
            {
                lastPath = path;

                var files = new List<GHFileObject>();
                var url = $"https://api.github.com/repos/{REPO}/contents{path}";
                var response = await client.GetStringAsync(url);

                return JsonSerializer.Deserialize<List<GHFileObject>>(response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                GameActions.Print("There was an error trying to load public scripts. You can browse them manually at: https://github.com/bittiez/PublicLegionScripts");
            }

            return new List<GHFileObject>();
        }


        internal class GHFileObject
        {
            public string name { get; set; }
            public string path { get; set; }
            public string sha { get; set; }
            public int size { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string git_url { get; set; }
            public string download_url { get; set; }
            public string type { get; set; }
            public _Links _links { get; set; }
        }

        internal class _Links
        {
            public string self { get; set; }
            public string git { get; set; }
            public string html { get; set; }
        }


        internal class ItemControl : Control
        {
            public ItemControl(GHFileObject gHFileObject, ScriptBrowser scriptBrowser)
            {
                Width = WIDTH - 18;
                Height = 50;

                Add(new AlphaBlendControl() { Width = Width, Height = Height });

                GHFileObject = gHFileObject;
                ScriptBrowser = scriptBrowser;
                if (gHFileObject.type == "dir")
                {
                    Add(GenTextBox("Directory", 14, 5, 5));
                    MouseDown += DirectoryMouseDown;
                }
                else if (gHFileObject.type == "file" && gHFileObject.name.EndsWith(".lscript"))
                {
                    Add(GenTextBox("Script", 14, 5, 5));
                    MouseDown += FileMouseDown;
                }

                var tb = GenTextBox(gHFileObject.name, 20);
                tb.X = Width - tb.MeasuredSize.X - 5;
                tb.Y = (Height - tb.MeasuredSize.Y) / 2;
                Add(tb);
            }

            private void FileMouseDown(object sender, MouseEventArgs e)
            {
                var t = ScriptBrowser.client.GetStringAsync(GHFileObject.download_url);
                t.Wait();

                ScriptFile f = new ScriptFile(LegionScripting.ScriptPath, t.Result, GHFileObject.name);
                UIManager.Add(new ScriptEditor(f));
            }

            private void DirectoryMouseDown(object sender, MouseEventArgs e)
            {
                ScriptBrowser.GetFilesAsync(GHFileObject.path).ContinueWith((r) =>
                {
                    ScriptBrowser.SetFiles(r.Result);
                });
            }

            private TextBox GenTextBox(string text, int fontsize, int x = 0, int y = 0)
            {

                TextBox tb = new TextBox(text, TrueTypeLoader.EMBEDDED_FONT, fontsize, null, Microsoft.Xna.Framework.Color.White, strokeEffect: false);
                tb.X = x;
                tb.Y = y;
                tb.AcceptMouseInput = false;
                return tb;
            }

            public GHFileObject GHFileObject { get; }
            public ScriptBrowser ScriptBrowser { get; }
        }
    }
}
