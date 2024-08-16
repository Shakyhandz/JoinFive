namespace JoinFive
{
    // https://github.com/SyncfusionExamples/Draw-Signature-using-GraphicsView-in-.NET-MAUI/blob/master/2DGraphicsDrawing/2DGraphicsDrawing/SignatureView.cs

    public class JoinFiveView : GraphicsView
    {
        GraphicsDrawable graphicsDrawable;

        //public int Score
        //{
        //    get => (int)GetValue(ScoreProperty);
        //    set => SetValue(ScoreProperty, value);
        //}

        //public static readonly BindableProperty ScoreProperty = BindableProperty.Create(nameof(Score), typeof(int), typeof(JoinFiveView), propertyChanged: ScorePropertyChanged);
        ////    public static readonly DependencyProperty ScoreProperty = DependencyProperty.Register(
        ////nameof(Score), typeof(double),
        ////typeof(JoinFiveView)
        ////);

        //private static void ScorePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        //{
        //    if (bindable is not JoinFiveView {Drawable: GraphicsDrawable drawable } view)
        //    {
        //        return;
        //    }

        //    drawable.AnguloFin = (int)newValue;
        //    view.Invalidate();
        //}

        //public double Score
        //{
        //    get => (double)GetValue(ScoreProperty);
        //    set => SetValue(ScoreProperty, value);
        //}

        //public static readonly BindableProperty ScoreProperty = BindableProperty.Create(nameof(Score), typeof(double), typeof(JoinFiveView), propertyChanged: ScorePropertyChanged);

        //private static void ScorePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        //{
        //    if (bindable is not JoinFiveView { Drawable: GraphicsDrawable drawable } view)
        //    {
        //        return;
        //    }

        //    drawable.Score = (double)newValue;
        //    view.Invalidate();
        //}

        public JoinFiveView()
        {
            graphicsDrawable = new GraphicsDrawable();
            Drawable = graphicsDrawable;
            StartInteraction += JoinFiveView_StartInteraction;
            DragInteraction += JoinFiveView_DragInteraction;
            EndInteraction += JoinFiveView_EndInteraction;
            //graphicsDrawable.ScoreChanged += GraphicsDrawable_ScoreChanged;
        }

        private void GraphicsDrawable_ScoreChanged(object? sender, EventArgs e)
        {
            //Score = graphicsDrawable.NoOfLines;
            //Invalidate();
        }

        private void JoinFiveView_EndInteraction(object? sender, TouchEventArgs e)
        {
            graphicsDrawable.IsDrawing = false;
            graphicsDrawable.DragPoints.Clear();            
            Invalidate();
        }

        private void JoinFiveView_DragInteraction(object? sender, TouchEventArgs e)
        {
            graphicsDrawable.DragPoints.Add(e.Touches[0]);
            Invalidate();

        }

        private void JoinFiveView_StartInteraction(object? sender, TouchEventArgs e)
        {
            graphicsDrawable.IsDrawing = true;
            graphicsDrawable.StartPoint = e.Touches[0];
            Invalidate();
        }
    }
}
