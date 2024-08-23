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
                drawable.Id = settings.GameId;
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

            var settings = ReadSettings() ?? new Settings();

            if (drawable != null)
            {                
                settings.GameId = drawable.Id;
                settings.HiScore = drawable.HiScore;
                settings.CurrentLines = drawable.BoardLines;
                settings.CurrentDots = drawable.BoardDots;

                // NOTE: High score settings are saved separately
                File.WriteAllText(SETTINGS_PATH, JsonSerializer.Serialize(settings));
            }
        }

        public async Task SaveScreenshot()
        {
            try
            {
                if (drawable != null && Screenshot.Default.IsCaptureSupported)
                {
                    var filePath = Path.Combine(FileSystem.AppDataDirectory, $"high_score_{drawable.Id}.png");

                    if (!File.Exists(filePath))
                    {
                        var screen = await Screenshot.Default.CaptureAsync();
                        var stream = await screen.OpenReadAsync();

                        using FileStream localFile = File.OpenWrite(filePath);
                        await stream.CopyToAsync(localFile);
                        localFile.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
            }   
        }

        

        private async Task SaveHighScoreSettings()
        {
            // Small delay for rendering to complete for accurate data
            await Task.Delay(250);

            var settings = ReadSettings() ?? new Settings();

            if (drawable != null)
            {
                var tmp = settings.HiScoreSettings.Where(x => x.HiScore >= drawable.Score).ToList();

                var newHiScore = new HiScoreSettings
                {
                    GameId = drawable.Id,
                    Timestamp = DateTime.Now,
                    HiScore = drawable.Score,
                    CurrentLines = drawable.BoardLines,
                    CurrentDots = drawable.BoardDots
                };

                tmp.Add(newHiScore);

                settings.HiScoreSettings = tmp;
                settings.HiScore = drawable.Score;

                File.WriteAllText(SETTINGS_PATH, JsonSerializer.Serialize(settings));

                await SaveScreenshot();
            }
        }

        public void Undo()
        {
            if (drawable != null)
            {
                drawable.ErrorMessage = "";
                drawable.LastLine = null;
                drawable.SuggestedLine = null;

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
                    Invalidate();
                }
            }
        }

        public async Task Clear()
        {
            // Save high score settings first            
            if (drawable.Score >= drawable.HiScore)
            {
                await SaveHighScoreSettings();
            }

            // Remember from drawable before resetting
            var highScore = Math.Max(drawable.Score, drawable.HiScore);
            var gameId = drawable.Id + 1;

            // Reset
            drawable = new GraphicsDrawable();
            Drawable = drawable;
            drawable.Id = gameId;
            drawable.HiScore = highScore;

            // Save the new game settings
            await SaveSettings();
            
            // Draw the new board
            Invalidate();
        }

        public void SuggestLine()
        {
            if (drawable != null && !drawable.IsDrawing)
            {
                drawable.ErrorMessage = "";
                drawable.LastLine = null;

                var suggestedLines = drawable.SuggestNextLine();
                var suggestedLine = suggestedLines.FirstOrDefault(l => !drawable.AlreadySuggested.Contains(l));

                if (suggestedLine != null)
                {
                    drawable.AlreadySuggested.Add(suggestedLine);
                    drawable.SuggestedLine = suggestedLine;
                    Invalidate();
                }
                else
                {
                    drawable.ErrorMessage = drawable.AlreadySuggested.Any()
                                            ? "No more lines to suggest"
                                            : "There are no lines to suggest";

                    drawable.AlreadySuggested = new();
                    drawable.SuggestedLine = null;

                    Invalidate();
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
