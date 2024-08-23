namespace JoinFive
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void JoinFiveView_StartInteraction(object sender, TouchEventArgs e)
        {
            if (sender is JoinFiveView view && e.Touches?.Count() > 0)
            {
                var point = e.Touches[0];

                // Canvas buttons clicked                
                if (point.Y < GraphicsDrawable.BOARD_ELLIPSE_INTERVAL)
                {
                    if (GraphicsDrawable.BOARD_ELLIPSE_INTERVAL <= point.X && point.X <= 60)
                    {
                        view.Undo();
                    }
                    else if (80 <= point.X && point.X <= 125)
                    {
                        await view.Clear();
                    }
                    else if (145 <= point.X && point.X <= 200)
                    {
                        view.SuggestLine();
                    }
                }
            }
        }
    }
}

