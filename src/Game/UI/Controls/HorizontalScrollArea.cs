namespace ClassicUO.Game.UI.Controls
{
    //class HorizontalScrollArea : Control
    //{
    //    private Rectangle _rect;

    //    public HorizontalScrollArea(int x, int y, int w, int h)
    //    {
    //        X = x;
    //        Y = y;
    //        Width = w;
    //        Height = h;

    //        AcceptMouseInput = true;
    //        WantUpdateSize = false;
    //        CanMove = true;


    //        HSliderBar bar = new HSliderBar(0, 0, w, 0, 100, 0, HSliderBarStyle.BlueWidgetNoBar)
    //        {
    //            Parent = this
    //        };
    //    }


    //    public override void Update(double totalMS, double frameMS)
    //    {
    //        base.Update(totalMS, frameMS);
    //    }

    //    public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
    //    {
    //        Children[0].Draw(batcher, new Point(position.X + Children[0].X, position.Y + Children[0].Y));
    //        _rect.X = position.X;
    //        _rect.Y = position.Y;
    //        _rect.Width = Width;
    //        _rect.Height = Height;

    //        Rectangle scissor = ScissorStack.CalculateScissors(batcher.TransformMatrix, _rect);

    //        if (ScissorStack.PushScissors(scissor))
    //        {
    //            batcher.EnableScissorTest(true);

    //            int width = 0;
    //            int maxWidth = 


    //            batcher.EnableScissorTest(false);
    //            ScissorStack.PopScissors();
    //        }

    //        return true;
    //    }
    //}
}