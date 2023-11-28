using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ModernColorPicker : Gump
    {
        private const int WIDTH = 200, HEIGHT = 400;

        private BorderControl borderControl;
        private GumpPicTiled backgroundTexture;
        private Area area;

        private const int ROWS = 20;
        private const int COLUMNS = 10;
        private readonly Action<ushort> hueChanged;
        private readonly uint serial;
        private int _cPage = 0;
        private int cPage
        {
            get { return _cPage; }
            set
            {
                if (value < 0)
                {
                    _cPage = 14;
                    return;
                }
                if (value > 14)
                {
                    _cPage = 0;
                    return;
                }

                _cPage = value;
            }
        }

        public ModernColorPicker(Action<ushort> hueChanged, uint serial = 0) : base(0, 0)
        {
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;
            this.hueChanged = hueChanged;
            this.serial = serial;
            borderControl = new BorderControl(0, 0, WIDTH, HEIGHT, 10);
            int graphic = 9270, borderSize = 10;
            backgroundTexture = new GumpPicTiled(borderSize, borderSize, WIDTH - borderSize * 2, HEIGHT - borderSize * 2, (ushort)(graphic + 4));

            borderControl.T_Left = (ushort)graphic;
            borderControl.H_Border = (ushort)(graphic + 1);
            borderControl.T_Right = (ushort)(graphic + 2);
            borderControl.V_Border = (ushort)(graphic + 3);
            borderControl.V_Right_Border = (ushort)(graphic + 5);
            borderControl.B_Left = (ushort)(graphic + 6);
            borderControl.H_Bottom_Border = (ushort)(graphic + 7);
            borderControl.B_Right = (ushort)(graphic + 8);
            Add(borderControl);
            Add(backgroundTexture);
            Add(area = new Area(false) { X = borderSize, Y = borderSize });

            FillHueDisplays();
            NiceButton prev, next;

            Label page = new Label(cPage.ToString(), true, 0xffff, 30, align: TEXT_ALIGN_TYPE.TS_CENTER);
            page.X = (WIDTH / 2) - 10;
            page.Y = HEIGHT - borderSize - 20;
            Add(page);

            Add(prev = new NiceButton(borderSize, HEIGHT - borderSize - 20, 20, 20, ButtonAction.Activate, "<") { IsSelectable = false });
            prev.MouseUp += (sender, e) => { if (e.Button == Input.MouseButtonType.Left) { cPage--; FillHueDisplays(cPage); page.Text = (cPage + 1).ToString(); } };

            Add(next = new NiceButton(WIDTH - borderSize - 20, HEIGHT - borderSize - 20, 20, 20, ButtonAction.Activate, ">") { IsSelectable = false });
            next.MouseUp += (sender, e) => { if (e.Button == Input.MouseButtonType.Left) { cPage++; FillHueDisplays(cPage); page.Text = (cPage + 1).ToString(); } };

        }

        private void FillHueDisplays(int page = 0)
        {
            if (page < 0)
                page = 0;
            foreach (Control c in area.Children)
                c.Dispose();

            for (int col = 1; col < COLUMNS + 1; col++)
            {
                for (int row = 1; row < ROWS + 1; row++)
                {
                    int _ = row + ((col - 1) * ROWS);
                    area.Add(new HueDisplay((ushort)(_ + (page * (ROWS * COLUMNS)) - 1), hueChanged, sendSysMessage: serial == 8787 ? true : false) { X = (col - 1) * 18, Y = (row - 1) * 18 });
                }
            }
        }

        public class HueDisplay : Control
        {
            private ushort hue;
            private readonly Action<ushort> hueChanged;
            private readonly bool isClickable;
            private readonly bool sendSysMessage;
            private Rectangle rect;
            private Rectangle bounds;
            private Texture2D texture;
            private Vector3 hueVector;
            private bool flash = false;
            private float flashAlpha = 1f;
            private bool rev = false;

            public ushort Hue
            {
                get { return hue; }
                set
                {
                    hue = value;
                    HueChanged?.Invoke(this, null);
                    hueChanged?.Invoke(value);
                    if (!isClickable)
                        SetTooltip(hue.ToString());
                    else
                        SetTooltip($"Click to select a hue ({hue})");
                }
            }

            public event EventHandler HueChanged;

            public HueDisplay(ushort hue, Action<ushort> hueChanged, bool isClickable = false, bool sendSysMessage = false)
            {
                hueVector = ShaderHueTranslator.GetHueVector(hue, true, 1);
                ref readonly var staticArt = ref Client.Game.Arts.GetArt(0x0FAB);
                texture = staticArt.Texture;
                rect = Client.Game.Arts.GetRealArtBounds(0x0FAB);
                Width = 18;
                Height = 18;
                this.bounds = staticArt.UV;
                CanMove = true;
                CanCloseWithRightClick = true;
                AcceptMouseInput = true;
                if (!isClickable)
                    SetTooltip(hue.ToString());
                else
                    SetTooltip($"Click to select a hue ({hue})");
                this.hue = hue;
                this.hueChanged = hueChanged;
                this.isClickable = isClickable;
                this.sendSysMessage = sendSysMessage;
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);

                if (button == MouseButtonType.Left)
                {
                    if (isClickable)
                    {
                        UIManager.GetGump<ModernColorPicker>()?.Dispose();
                        UIManager.Add(new ModernColorPicker(s => Hue = s) { X = 100, Y = 100 });
                    }
                    else
                    {
                        hueChanged?.Invoke(hue);
                        flash = true;
                    }
                    if (sendSysMessage)
                        GameActions.Print($"Selected hue: {hue}");
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                if (texture != null)
                {
                    if (isClickable)
                        hueVector = ShaderHueTranslator.GetHueVector(hue, true, 1);
                    if (flash)
                    {
                        hueVector.Z = flashAlpha;
                        if (!rev)
                            flashAlpha -= 0.1f;
                        else
                            flashAlpha += 0.1f;
                        if (flashAlpha <= 0)
                            rev = true;
                        else if (flashAlpha >= 1)
                        {
                            rev = false;
                            flash = false;
                        }

                    }
                    batcher.Draw
                    (
                        texture,
                        new Rectangle
                        (
                            x,
                            y,
                            Width,
                            Height
                        ),
                        new Rectangle
                        (
                            bounds.X + rect.X,
                            bounds.Y + rect.Y,
                            rect.Width,
                            rect.Height
                        ),
                        hueVector
                    );
                }
                return true;
            }
        }
    }
}
