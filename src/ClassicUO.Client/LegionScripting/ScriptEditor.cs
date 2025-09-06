using System;
using System.IO;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    internal class ScriptEditor : ResizableGump
    {
        private NiceButton save;
        private AlphaBlendControl background;
        private ScrollArea scrollArea;
        private TextBox title;
        private bool built;
        private TTFTextInputField textArea;
        public ScriptEditor(ScriptFile scriptFile) : base(600, 400, 600, 400, 0, 0)
        {
            ScriptFile = scriptFile;
            AcceptMouseInput = true;
            CanMove = true;

            Add(background = new AlphaBlendControl());

            Add(title = new TextBox(scriptFile.FileName, TrueTypeLoader.EMBEDDED_FONT, 24, Width - 100, color: Color.White, strokeEffect: false));

            Add(save = new NiceButton(Width - 50, 0, 50, 50, ButtonAction.Default, "Save"));
            save.MouseUp += Save_MouseUp;

            scrollArea = new ScrollArea(BorderControl.BorderSize, 50 + BorderControl.BorderSize, Width, Height - 50, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(scrollArea);

            int width = Width - scrollArea.ScrollBarWidth() - 4;

            scrollArea.Add(textArea = new TTFTextInputField(width, Height - 50, text: string.Join("\n", scriptFile.FileContents), multiline: true, convertHtmlColors: false) { X = BorderControl.BorderSize, Y = BorderControl.BorderSize });
            textArea.TextChanged += (s, e) =>
            {
                int h = textArea.TextBox.TotalHeight > scrollArea.Height ? textArea.TextBox.TotalHeight : scrollArea.Height;
                textArea.UpdateSize(scrollArea.Width - scrollArea.ScrollBarWidth() - 5, h);
            };
            built = true;
            OnResize();
        }

        public override void OnResize()
        {
            base.OnResize();
            if (!built) return;

            background.Width = Width - (BorderControl.BorderSize * 2);
            background.Height = Height - (BorderControl.BorderSize * 2);
            background.X = BorderControl.BorderSize;
            background.Y = BorderControl.BorderSize;

            title.Width = Width - 100;
            title.X = BorderControl.BorderSize;
            title.Y = BorderControl.BorderSize;

            save.X = Width - 50 - BorderControl.BorderSize;
            save.Y = BorderControl.BorderSize;

            scrollArea.Width = Width - (BorderControl.BorderSize * 2);
            scrollArea.Height = Height - (BorderControl.BorderSize * 2) - 50;
            scrollArea.UpdateScrollbarPosition();
            int h = textArea.TextBox.TotalHeight > scrollArea.Height ? textArea.TextBox.TotalHeight : scrollArea.Height;
            textArea.UpdateSize(scrollArea.Width - scrollArea.ScrollBarWidth() - 5, h);
        }

        private void Save_MouseUp(object sender, MouseEventArgs e)
        {
            string sb = textArea.Text;

            try
            {
                File.WriteAllText(ScriptFile.FullPath, sb);
                GameActions.Print($"Saved {ScriptFile.FileName}.");
            }
            catch (Exception ex)
            {
                GameActions.Print(ex.ToString());
            }
        }

        public ScriptFile ScriptFile { get; }
    }
}
