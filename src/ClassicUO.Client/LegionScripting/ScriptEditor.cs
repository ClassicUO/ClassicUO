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
        private UOLabel title;
        private bool built;
        private LineNumberEditor editorPanel;
        public ScriptEditor(ScriptFile scriptFile) : base(600, 400, 600, 400, 0, 0)
        {
            ScriptFile = scriptFile;
            AcceptMouseInput = true;
            CanMove = true;

            Add(background = new AlphaBlendControl());

            Add(title = new UOLabel(scriptFile.FileName, 1, UOLabelHue.Text, Assets.TEXT_ALIGN_TYPE.TS_LEFT, Width - 100) { AcceptMouseInput = false });

            Add(save = new NiceButton(Width - 50, 0, 50, 50, ButtonAction.Default, "Save"));
            save.MouseUp += Save_MouseUp;

            scrollArea = new ScrollArea(BorderControl.BorderSize, 50 + BorderControl.BorderSize, Width, Height - 50, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(scrollArea);

            int editorW = Width - scrollArea.ScrollBarWidth() - LineNumberEditor.GutterWidth - 12;

            scrollArea.Add(editorPanel = new LineNumberEditor(editorW, Height - 60, string.Join("\n", scriptFile.FileContents)) { X = BorderControl.BorderSize, Y = BorderControl.BorderSize });
            editorPanel.TextChanged += (s, e) =>
            {
                int h = editorPanel.Editor.TextBox.TotalHeight > scrollArea.Height ? editorPanel.Editor.TextBox.TotalHeight : scrollArea.Height;
                editorPanel.UpdateSize(scrollArea.Width - scrollArea.ScrollBarWidth() - LineNumberEditor.GutterWidth - 12, h);
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
            int h = editorPanel.Editor.TextBox.TotalHeight > scrollArea.Height ? editorPanel.Editor.TextBox.TotalHeight : scrollArea.Height;
            editorPanel.UpdateSize(scrollArea.Width - scrollArea.ScrollBarWidth() - LineNumberEditor.GutterWidth - 12, h);
        }

        private void Save_MouseUp(object sender, MouseEventArgs e)
        {
            string sb = editorPanel.Text;
            try
            {
                File.WriteAllText(ScriptFile.FullPath, sb);
                ScriptFile.ReadFromFile();
                if (ScriptFile.IsUOScript)
                    ScriptFile.GenerateUOScript();
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
