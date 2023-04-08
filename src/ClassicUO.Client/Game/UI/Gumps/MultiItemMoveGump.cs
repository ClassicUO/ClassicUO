using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MultiItemMoveGump : Gump
    {
        private Label label;

        public static ConcurrentQueue<Item> MoveItems = new ConcurrentQueue<Item>();

        public static int ObjDelay = 1000;

        public MultiItemMoveGump(int x, int y) : base(0, 0)
        {
            Width = 200;
            Height = 100;

            X = x;
            Y = y;
            SetInScreen();

            CanMove = true;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;

            ObjDelay = ProfileManager.CurrentProfile.MoveMultiObjectDelay;

            Add(new AlphaBlendControl(0.75f) { Width = Width, Height = Height });

            Add(label = new Label($"Moving {MoveItems.Count} items.", true, 0xff, Width, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER));

            Add(new Label($"Object delay:", true, 0xff, 150) { Y = label.Height + 5});
            StbTextBox delay;
            Add(delay = new StbTextBox(0xFF, 3000, 50, true, FontStyle.None, 0x048)
            {
                X = 150,
                Y = label.Height + 5,
                Width = 50,
                Height = 20,
                Multiline = false,
                NumbersOnly = true,
            });
            delay.SetText(ObjDelay.ToString());
            delay.Add(new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                Width = delay.Width,
                Height = delay.Height
            });
            delay.TextChanged += (s, e) => {
                if (int.TryParse(delay.Text, out int newDelay))
                {
                    ObjDelay = newDelay;
                    ProfileManager.CurrentProfile.MoveMultiObjectDelay = newDelay;
                }
            };


            NiceButton cancel;
            Add(cancel = new NiceButton(0, Height - 20, 100, 20, ButtonAction.Default, "Cancel", align: Assets.TEXT_ALIGN_TYPE.TS_CENTER));
            cancel.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    MoveItems = new ConcurrentQueue<Item>();
                    cancel.Dispose();
                }
            };

            NiceButton move;
            Add(move = new NiceButton(100, Height - 20, 100, 20, ButtonAction.Default, "Move to", align: Assets.TEXT_ALIGN_TYPE.TS_CENTER));
            move.SetTooltip("Select a container to move these items to.");
            move.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    GameActions.Print("Select the container to move these items to");
                    TargetManager.SetTargeting(CursorTarget.MoveItemContainer, CursorType.Target, TargetType.Neutral);
                    delay.IsEditable = false;
                    move.Dispose();
                }
            };

            Add(new SimpleBorder() { Width = Width, Height = Height });
        }

        public static void OnContainerTarget(uint serial)
        {
            Item moveToContainer = World.Items.Get(serial);
            if (!moveToContainer.ItemData.IsContainer)
            {
                GameActions.Print("That does not appear to be a container...");
                return;
            }
            GameActions.Print("Moving items to the selected container..");
            processItemMoves(moveToContainer);
        }

        public static void AddMultiItemMoveGumpToUI(int x, int y)
        {
            if (MoveItems.Count > 0)
            {
                Gump moveItemGump = UIManager.GetGump<MultiItemMoveGump>();
                if (moveItemGump == null)
                    UIManager.Add(new MultiItemMoveGump(x, y));
            }
        }

        private static async Task processItemMoves(Item container)
        {
            while(MoveItems.TryDequeue(out Item moveItem))
            {
                if(GameActions.PickUp(moveItem.Serial, 0, 0, moveItem.Amount))
                    GameActions.DropItem(moveItem.Serial, 10, 10, 0, container);
                await Task.Delay(ObjDelay);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (MoveItems.Count < 1)
                Dispose();

            label.Text = $"Moving {MoveItems.Count} items.";

            return base.Draw(batcher, x, y);
        }
    }
}
