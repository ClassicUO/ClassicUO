using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Linq;

namespace ClassicUO.Game.UI.Controls
{
    internal class GumpLockButton : Control
    {
        private bool isLocked;

        private Rectangle lockIconBounds;
        private Texture2D lockIcon;
        Vector3 hueLocked = Renderer.ShaderHueTranslator.GetHueVector(32);
        Vector3 hueUnlocked = Renderer.ShaderHueTranslator.GetHueVector(0);

        public GumpLockButton(ref bool isLocked)
        {
            this.isLocked = isLocked;

            ref readonly var ginfo = ref Client.Game.Gumps.GetGump(0x82C);
            lockIcon = ginfo.Texture;
            lockIconBounds = ginfo.UV;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button == MouseButtonType.Left && Keyboard.Alt && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                if (lockIcon != null)
                {
                    if (x >= 0 && x < lockIconBounds.Width && y >= 0 && y <= lockIconBounds.Height)
                    {
                        isLocked ^= true;
                        if (isLocked)
                        {
                            CanMove = false;
                            CanCloseWithRightClick = false;
                        }
                        else
                        {
                            CanMove = true;
                            CanCloseWithRightClick = true;
                        }
                    }
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (lockIcon != null && Keyboard.Alt && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                batcher.Draw(lockIcon, new Vector2(x, y), lockIconBounds, isLocked ? hueLocked : hueUnlocked);
            }

            return true;
        }
    }
}
