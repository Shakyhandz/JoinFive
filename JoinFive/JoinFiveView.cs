using JoinFive.Contract;
using System.Text.Json;

namespace JoinFive
{
    // https://github.com/SyncfusionExamples/Draw-Signature-using-GraphicsView-in-.NET-MAUI/blob/master/2DGraphicsDrawing/2DGraphicsDrawing/SignatureView.cs

    public class JoinFiveView : GraphicsView
    {
        GraphicsDrawable drawable;        
        private static readonly string SETTINGS_PATH = Path.Combine(FileSystem.AppDataDirectory, "settings.json");

        public JoinFiveView()
        {
            drawable = new GraphicsDrawable();
            Drawable = drawable;
            var settings = ReadSettings();

            if (settings != null)
            {
                drawable.HiScore = settings.HiScore;
                drawable.BoardLines = settings.CurrentLines;
                drawable.BoardDots = settings.CurrentDots;
            }

            StartInteraction += JoinFiveView_StartInteraction;
            DragInteraction += JoinFiveView_DragInteraction;
            EndInteraction += JoinFiveView_EndInteraction;
        }

        private Settings? ReadSettings()
        {
            if (File.Exists(SETTINGS_PATH))
            {
                var json = File.ReadAllText(SETTINGS_PATH);
                return JsonSerializer.Deserialize<Settings>(json);
            }

            return null;
        }

        private async Task SaveSettings()
        {
            // Small delay for rendering to complete for accurate data
            await Task.Delay(250);

            var settings = new Settings
            {
                HiScore = drawable?.HiScore ?? 0,
                CurrentLines = drawable?.BoardLines ?? [], 
                CurrentDots = drawable?.BoardDots ?? [],
            };

            File.WriteAllText(SETTINGS_PATH, JsonSerializer.Serialize(settings));
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
            var highScore = Math.Max(drawable.Score, drawable.HiScore);
            
            drawable = new GraphicsDrawable();
            Drawable = drawable;
            drawable.HiScore = highScore;
            SaveSettings();
            Invalidate();
        }

        private HashSet<BoardLine> _alreadySuggested = new();

        public void SuggestLine()
        {
            if (drawable != null && !drawable.IsDrawing)
            {
                var suggestedLines = drawable.SuggestNextLine();
                var suggestedLine = suggestedLines.FirstOrDefault(l => !_alreadySuggested.Contains(l));

                if (suggestedLine != null)
                {
                    _alreadySuggested.Add(suggestedLine);
                    drawable.SuggestedLine = suggestedLine;
                    Invalidate();
                }
                else
                {
                    _alreadySuggested = new();
                    drawable.ErrorMessage = "No more lines to suggest";
                }
            }
        }

        private void JoinFiveView_EndInteraction(object? sender, TouchEventArgs e)
        {
            if (drawable.StartPoint.Y >= GraphicsDrawable.BOARD_ELLIPSE_INTERVAL && e.Touches[0].Y >= GraphicsDrawable.BOARD_ELLIPSE_INTERVAL)
            {
                drawable.IsDrawing = false;
                drawable.DragPoints.Clear();
                Invalidate();
                SaveSettings();
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
