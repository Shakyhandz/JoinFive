namespace JoinFive
{
    // https://github.com/SyncfusionExamples/Draw-Signature-using-GraphicsView-in-.NET-MAUI/blob/master/2DGraphicsDrawing/2DGraphicsDrawing/SignatureView.cs

    public class JoinFiveView : GraphicsView
    {
        GraphicsDrawable drawable;
        public int HiScore { get; set; }

        public JoinFiveView()
        {
            drawable = new GraphicsDrawable();
            Drawable = drawable;
            StartInteraction += JoinFiveView_StartInteraction;
            DragInteraction += JoinFiveView_DragInteraction;
            EndInteraction += JoinFiveView_EndInteraction;
        }

        public void Undo()
        {
            if (drawable.LastCommittedDot != null &&
                !drawable.LastCommittedDot.IsInitialDot &&
                drawable.LastCommittedLine != null)
            {
                drawable.BoardDots.Remove(drawable.LastCommittedDot);
                drawable.BoardLines.Remove(drawable.LastCommittedLine);

                drawable.LastCommittedDot = null;
                drawable.LastCommittedLine = null;

                Invalidate();
            }
            else
            {
                drawable.ErrorMessage = "Nothing to undo";
            }
        }

        public void Clear()
        {
            if (drawable.Score > HiScore)
                HiScore = drawable.Score;

            drawable = new GraphicsDrawable();
            Drawable = drawable;
            Invalidate();
        }

        private void JoinFiveView_EndInteraction(object? sender, TouchEventArgs e)
        {
            if (drawable.StartPoint.Y >= GraphicsDrawable.BOARD_ELLIPSE_INTERVAL && e.Touches[0].Y >= GraphicsDrawable.BOARD_ELLIPSE_INTERVAL)
            {
                drawable.IsDrawing = false;
                drawable.DragPoints.Clear();
                Invalidate();
            }
        }

        private void JoinFiveView_DragInteraction(object? sender, TouchEventArgs e)
        {
            if (drawable.StartPoint.Y >= GraphicsDrawable.BOARD_ELLIPSE_INTERVAL && e.Touches[0].Y >= GraphicsDrawable.BOARD_ELLIPSE_INTERVAL)
            {
                drawable.DragPoints.Add(e.Touches[0]);
                Invalidate();
            }
        }

        private void JoinFiveView_StartInteraction(object? sender, TouchEventArgs e)
        {
            drawable.StartPoint = e.Touches[0];

            if (e.Touches[0].Y >= GraphicsDrawable.BOARD_ELLIPSE_INTERVAL)
            {
                drawable.IsDrawing = true;                
                Invalidate();
            }
        }
    }
}
