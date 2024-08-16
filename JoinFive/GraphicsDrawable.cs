using JoinFive.Contract;

namespace JoinFive
{
    // https://learn.microsoft.com/en-us/dotnet/maui/user-interface/graphics/?view=net-maui-8.0
    public class GraphicsDrawable : IDrawable
    {
        #region Consts

        public const float BOARD_MARGIN_TOP = 192;
        public const float BOARD_MARGIN = 120;
        public const float BOARD_ELLIPSE_INTERVAL = 24;

        public const float ELLIPSE_WIDTH = 12;
        public const float UNCOMMITED_LINE_STROKE_THICKNESS = 8;
        public const float COMMITED_LINE_STROKE_THICKNESS = 4;

        public const string ERROR_NOT_ENOUGH_DOTS = "Not enough dots for a new line.";
        public const string ERROR_NO_NEW_DOT = "One new dot must be part of a new line";
        public const string ERROR_LINE_TOO_SHORT = "The line is too short";
        public const string ERROR_LINE_OVERLAPPING = "Overlapping lines";

        #endregion

        public HashSet<BoardLine> BoardLines { get; set; } = [];
        public HashSet<BoardDot> BoardDots { get; set; } = [];

        // For undo
        public BoardLine? LastCommittedLine { get; set; }
        public BoardDot? LastCommittedDot { get; set; }

        public PointF StartPoint { get; set; }
        public List<PointF> DragPoints { get; set; } = new List<PointF>();
        public bool IsDrawing { get; set; }
        private BoardLine? _lastLine = null;

        public string ErrorMessage { get; set; } = "";
        public int Score { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            ErrorMessage = "";

            // Draw background 
            canvas.FillColor = Colors.CornflowerBlue;
            canvas.FillRectangle(dirtyRect);

            // Check if last line is valid
            if (!IsDrawing && _lastLine != null)
            {
                // If ok, add to lines
                if (SetAndValidateLine(canvas, _lastLine))
                {
                    BoardLines.Add(_lastLine);
                    LastCommittedLine = _lastLine;
                    _lastLine = null;
                }
            }

            // Draw board
            // TODO: Done twice? Just add to dots at init
            DrawButtons(canvas);
            DrawEmptyDots(canvas, dirtyRect);
            DrawBoard(canvas);

            // Need to draw all valid lines and dots every time
            foreach (var item in BoardDots)
            {
                DrawDot(canvas, item, item.IsInitialDot ? Colors.Red : Colors.Green);
            }

            foreach (var item in BoardLines)
            {
                DrawLine(canvas, item);
            }

            Score = BoardLines.Count;

            // Draw temp line while dragging
            if (DragPoints?.Count > 0)
            {
                _lastLine = new BoardLine
                {
                    X1 = StartPoint.X,
                    Y1 = StartPoint.Y,
                    X2 = DragPoints.Last().X,
                    Y2 = DragPoints.Last().Y,
                };

                DrawLine(canvas, _lastLine);
            }
        }

        private void DrawButtons(ICanvas canvas)
        {
            canvas.Font = new Microsoft.Maui.Graphics.Font("Arial");
            canvas.FontSize = 12;
            canvas.FontColor = Colors.Black;
            canvas.DrawString("UNDO", BOARD_ELLIPSE_INTERVAL, 0, 80, BOARD_ELLIPSE_INTERVAL, HorizontalAlignment.Left, VerticalAlignment.Center);
            canvas.DrawString("CLEAR", BOARD_ELLIPSE_INTERVAL + 60, 0, 80, BOARD_ELLIPSE_INTERVAL, HorizontalAlignment.Left, VerticalAlignment.Center);
        }

        public static void DrawLine(ICanvas canvas, BoardLine? line)
        {
            if (line != null)
            {
                canvas.StrokeSize = 4;
                canvas.StrokeColor = Colors.Black;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawLine(line.X1, line.Y1, line.X2, line.Y2);
            }
        }

        #region Validate line
        
        private bool SetAndValidateLine(ICanvas canvas, BoardLine line)
        {
            // Just a click or a short line - do nothing
            if (Math.Abs(line.X1 - line.X2) < 2 * BOARD_ELLIPSE_INTERVAL &&
                Math.Abs(line.Y1 - line.Y2) < 2 * BOARD_ELLIPSE_INTERVAL)
                return false;

            var newDots = new HashSet<BoardDot>();

            if (Math.Abs(line.X1 - line.X2) < 2 * BOARD_ELLIPSE_INTERVAL) // LineType.Vertical
            {
                // Fix line
                var lineX = SnapValue(line.X1);
                line.X1 = lineX;
                line.X2 = lineX;
                line.Y1 = SnapValue(line.Y1);

                if (Math.Abs(line.Y1 - SnapValue(line.Y2)) < 3 * BOARD_ELLIPSE_INTERVAL)
                {
                    ErrorMessage = ERROR_LINE_TOO_SHORT;
                    return false;
                }

                line.Y2 = line.Y1 + (4 * BOARD_ELLIPSE_INTERVAL * (line.Y2 - line.Y1 < 0 ? -1 : 1));

                // Fix dots
                var x = (int)line.X1 - (int)line.X1 % BOARD_ELLIPSE_INTERVAL;
                var y1 = (int)line.Y1 - (int)line.Y1 % BOARD_ELLIPSE_INTERVAL;

                newDots = Enumerable.Range(0, 5)
                                    .Select(i => new BoardDot
                                    {
                                        X = x,
                                        Y = y1 + BOARD_ELLIPSE_INTERVAL * (line.Y1 < line.Y2 ? i : -i),
                                        IsInitialDot = false,
                                    })
                                    .ToHashSet();
            }
            else if (Math.Abs(line.Y1 - line.Y2) < 2 * BOARD_ELLIPSE_INTERVAL) // LineType.Horizontal
            {
                // Fix line
                var lineY = SnapValue(line.Y1);
                line.Y1 = lineY;
                line.Y2 = lineY;
                line.X1 = SnapValue(line.X1);

                if (Math.Abs(line.X1 - SnapValue(line.X2)) < 3 * BOARD_ELLIPSE_INTERVAL)
                {
                    ErrorMessage = ERROR_LINE_TOO_SHORT;
                    return false;
                }

                line.X2 = line.X1 + (4 * BOARD_ELLIPSE_INTERVAL * (line.X2 - line.X1 < 0 ? -1 : 1));

                // Fix dots
                var y = (int)line.Y1 - (int)line.Y1 % BOARD_ELLIPSE_INTERVAL;
                var x1 = (int)line.X1 - (int)line.X1 % BOARD_ELLIPSE_INTERVAL;

                newDots = Enumerable.Range(0, 5)
                                    .Select(i => new BoardDot
                                    {
                                        X = x1 + BOARD_ELLIPSE_INTERVAL * (line.X1 < line.X2 ? i : -i),
                                        Y = y,
                                        IsInitialDot = false,
                                    })
                                    .ToHashSet();
            }
            else // LineType.Diagonal
            {
                // Fix line
                line.X1 = SnapValue(line.X1);
                line.Y1 = SnapValue(line.Y1);

                if (Math.Abs(line.Y1 - SnapValue(line.Y2)) < 3 * BOARD_ELLIPSE_INTERVAL ||
                    Math.Abs(line.X1 - SnapValue(line.X2)) < 3 * BOARD_ELLIPSE_INTERVAL)
                {
                    ErrorMessage = ERROR_LINE_TOO_SHORT;
                    return false;
                }

                line.X2 = line.X1 + (4 * BOARD_ELLIPSE_INTERVAL * (line.X2 - line.X1 < 0 ? -1 : 1));
                line.Y2 = line.Y1 + (4 * BOARD_ELLIPSE_INTERVAL * (line.Y2 - line.Y1 < 0 ? -1 : 1));

                // Fix dots
                var y1 = (int)line.Y1 - (int)line.Y1 % BOARD_ELLIPSE_INTERVAL;
                var x1 = (int)line.X1 - (int)line.X1 % BOARD_ELLIPSE_INTERVAL;

                newDots = Enumerable.Range(0, 5)
                                    .Select(i => new BoardDot 
                                    { 
                                        X = x1 + BOARD_ELLIPSE_INTERVAL * (line.X1 < line.X2 ? i : -i), 
                                        Y = y1 + BOARD_ELLIPSE_INTERVAL * (line.Y1 < line.Y2 ? i : -i),
                                        IsInitialDot = false,
                                    })
                                    .ToHashSet();
            }

            var j = (from np in newDots
                     join bp in BoardDots
                     on new { np.X, np.Y } equals new { bp.X, bp.Y } into joined
                     select new
                     {
                         np,
                         Hit = joined.Any(),
                     })
                     .ToList();

            if (j.Where(a => a.Hit).Count() < 4)
            {
                ErrorMessage = ERROR_NOT_ENOUGH_DOTS;
                return false;
            }

            if (j.Where(a => !a.Hit).Count() != 1)
            {
                ErrorMessage = ERROR_NO_NEW_DOT;
                return false;
            }

            line.Dots = newDots;

            // Check overlapping lines          
            if (BoardLines != null && BoardLines.Any(x => x.Dots.Intersect(newDots).Count() >= 2))
            {
                ErrorMessage = ERROR_LINE_OVERLAPPING;
                return false;
            }

            j.Where(a => !a.Hit)
             .ToList()
             .ForEach(a => SetEllipse(canvas, a.np.X, a.np.Y, Colors.Green, false));

            return true;
        }

        private float SnapValue(float axisValue)
        {
            return axisValue // initial value
                   - axisValue % BOARD_ELLIPSE_INTERVAL // Remove offset from point
                   + ELLIPSE_WIDTH / 2 // Put in the middle of the point    
                   // Decides which point is closest:
                   + (axisValue % BOARD_ELLIPSE_INTERVAL < ((BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2) / 2) ? 0 : BOARD_ELLIPSE_INTERVAL);
        } 
        #endregion

        #region Init board
        private static readonly List<(int a, int b)> _boardInitDots = new[]
{
            (0, 3),
            (0, 4),
            (0, 5),
            (0, 6),
            (1, 3),
            (1, 6),
            (2, 3),
            (2, 6),
            (3, 3),
            (3, 6),
            (3, 7),
            (3, 8),
            (3, 9),
            (4, 9),
            (5, 9),
            (6, 6),
            (6, 7),
            (6, 8),
            (6, 9),
        }
        .ToList();

        private void DrawEmptyDots(ICanvas canvas, RectF dirtyRect)
        {
            for (var x = dirtyRect.Left + BOARD_ELLIPSE_INTERVAL; x < dirtyRect.Right - ELLIPSE_WIDTH; x += BOARD_ELLIPSE_INTERVAL)
            {
                for (var y = dirtyRect.Top + BOARD_ELLIPSE_INTERVAL; y < dirtyRect.Bottom - ELLIPSE_WIDTH; y += BOARD_ELLIPSE_INTERVAL)
                {
                    DrawDot(canvas, new BoardDot { X = x, Y = y, IsInitialDot = true }, Colors.LightGray);
                }
            }
        }

        private void DrawBoard(ICanvas canvas)
        {
            foreach (var (a, b) in _boardInitDots)
            {
                SetBoardEllipse(canvas, a * BOARD_ELLIPSE_INTERVAL + BOARD_MARGIN, b * BOARD_ELLIPSE_INTERVAL + BOARD_MARGIN_TOP);

                if (a != b)
                    SetBoardEllipse(canvas, b * BOARD_ELLIPSE_INTERVAL + BOARD_MARGIN, a * BOARD_ELLIPSE_INTERVAL + BOARD_MARGIN_TOP);
            }
        }

        private void SetBoardEllipse(ICanvas canvas, float x, float y) => SetEllipse(canvas, x, y, Colors.Red, true);
        
        #endregion

        #region Board dots

        private void SetEllipse(ICanvas canvas, float x, float y, Color color, bool isInit)
        {
            var dot = new BoardDot 
            { 
                X = x, 
                Y = y,
                IsInitialDot = isInit,
            };

            BoardDots.Add(dot);

            if (!isInit)
                LastCommittedDot = dot;

            DrawDot(canvas, dot, color);
        }

        private void DrawDot(ICanvas canvas, BoardDot dot, Color color, float? alpha = null)
        {
            canvas.FillColor = color;

            if (alpha != null)
                canvas.Alpha = 0.2F;
            
            canvas.FillEllipse(dot.X, dot.Y, ELLIPSE_WIDTH, ELLIPSE_WIDTH);
        }

        #endregion
    }
}
