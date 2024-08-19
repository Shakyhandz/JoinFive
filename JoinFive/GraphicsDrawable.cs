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

        // Must be even number
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
        public BoardLine? LastLine { get; set; } = null;
        public BoardLine? SuggestedLine { get; set; }
        public HashSet<BoardLine> AlreadySuggested { get; set; } = new ();

        public string ErrorMessage { get; set; } = "";
        public int Score { get; set; }
        public int HiScore { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            //ErrorMessage = "";

            // Draw background 
            canvas.FillColor = Colors.CornflowerBlue;
            canvas.FillRectangle(dirtyRect);

            // Check if last line is valid
            if (!IsDrawing && LastLine != null)
            {
                // If ok, add to lines
                if (SetAndValidateLine(canvas, LastLine))
                {
                    ErrorMessage = "";
                    BoardLines.Add(LastLine);
                    LastCommittedLine = LastLine;
                    LastLine = null;
                    AlreadySuggested = new();
                }
            }

            // Draw board
            // TODO: Done twice? Just add to dots at init            
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
            DrawButtons(canvas, dirtyRect);

            // Draw temp line while dragging
            if (DragPoints?.Count > 0)
            {
                LastLine = new BoardLine
                {
                    X1 = StartPoint.X,
                    Y1 = StartPoint.Y,
                    X2 = DragPoints.Last().X,
                    Y2 = DragPoints.Last().Y,
                };

                DrawLine(canvas, LastLine);
                ErrorMessage = "";
            }
            else if (SuggestedLine != null)
            {
                DrawLine(canvas, SuggestedLine, Colors.White);
                SuggestedLine = null;
                ErrorMessage = "";
            }
        }

        private void DrawButtons(ICanvas canvas, RectF dirtyRect)
        {
            canvas.Font = new Microsoft.Maui.Graphics.Font("Arial");
            canvas.FontSize = 12;
            
            canvas.FontColor = Colors.Black;
            canvas.DrawString("UNDO", BOARD_ELLIPSE_INTERVAL, 0, 80, BOARD_ELLIPSE_INTERVAL, HorizontalAlignment.Left, VerticalAlignment.Center);
            canvas.DrawString("CLEAR", BOARD_ELLIPSE_INTERVAL + 60 * 1, 0, 80, BOARD_ELLIPSE_INTERVAL, HorizontalAlignment.Left, VerticalAlignment.Center);
            canvas.DrawString("SUGGEST", BOARD_ELLIPSE_INTERVAL + 60 * 2, 0, 80, BOARD_ELLIPSE_INTERVAL, HorizontalAlignment.Left, VerticalAlignment.Center);
            canvas.DrawString($"YOUR SCORE: {Score}", BOARD_ELLIPSE_INTERVAL + 60 * 3.5F, 0, 120, BOARD_ELLIPSE_INTERVAL, HorizontalAlignment.Left, VerticalAlignment.Center);
            canvas.DrawString($"HIGH SCORE: {HiScore}", BOARD_ELLIPSE_INTERVAL + 60 * 5.5F, 0, 120, BOARD_ELLIPSE_INTERVAL, HorizontalAlignment.Left, VerticalAlignment.Center);

            canvas.FontColor = Colors.Red;
            var errorMsg = string.IsNullOrEmpty(ErrorMessage?.Trim()) 
                           ? ""
                           : $"ERROR: {ErrorMessage}";

            canvas.DrawString(errorMsg, BOARD_ELLIPSE_INTERVAL, dirtyRect.Bottom - ELLIPSE_WIDTH - BOARD_ELLIPSE_INTERVAL, dirtyRect.Right - BOARD_ELLIPSE_INTERVAL, BOARD_ELLIPSE_INTERVAL, HorizontalAlignment.Left, VerticalAlignment.Center);            
        }

        public static void DrawLine(ICanvas canvas, BoardLine? line, Color? color = null)
        {
            if (line != null)
            {
                canvas.StrokeSize = 4;
                canvas.StrokeColor = color ?? Colors.Black;
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

        public List<BoardLine> SuggestNextLine()
        {
            // Find dots with 4 neighbors, where 0 or 1 has a gap of two 
            if (/*BoardLines?.Count > 0 &&*/ BoardDots?.Count > 0)
            {
                //LineType.Vertical
                var verticalLines = BoardLines?.Where(x => x.LineType == BoardLineType.Vertical).ToList() ?? [];
                var insideDots = verticalLines.SelectMany(x => x.InsideDots(ELLIPSE_WIDTH / 2)).ToHashSet();

                var candidates = BoardDots.Where(d => !insideDots.Contains(d))
                                          .GroupBy(x => x.X)
                                          .SelectMany(x =>
                                          {
                                              var res = new List<BoardLine>();

                                              var ys = x.Select(y => y.Y).Distinct().OrderBy(y => y).ToList();

                                              for (var i = 0; i < ys.Count; i++)
                                              {
                                                  var y1 = ys[i];

                                                  if (ys.Where(y2 => y1 - BOARD_ELLIPSE_INTERVAL <= y2 && y2 <= y1 - BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * 4).Count() == 4)
                                                  {
                                                      var dots = Enumerable.Range(0, 5)
                                                                           .Select(ix => new BoardDot { X = x.Key, Y = y1 - BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * ix })
                                                                           .ToHashSet();

                                                      if (dots.Intersect(insideDots).Count() == 0)
                                                      {
                                                          res.Add(new BoardLine
                                                          {
                                                              X1 = x.Key + ELLIPSE_WIDTH / 2,
                                                              X2 = x.Key + ELLIPSE_WIDTH / 2,
                                                              Y1 = y1 - BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2,
                                                              Y2 = y1 - BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * 4,
                                                              Dots = dots,
                                                          });
                                                      }
                                                  }

                                                  if (ys.Where(y2 => y1 <= y2 && y2 <= y1 + BOARD_ELLIPSE_INTERVAL * 4).Count() == 4)
                                                  {
                                                      res.Add(new BoardLine
                                                      {
                                                          X1 = x.Key + ELLIPSE_WIDTH / 2,
                                                          X2 = x.Key + ELLIPSE_WIDTH / 2,
                                                          Y1 = y1 + ELLIPSE_WIDTH / 2,
                                                          Y2 = y1 + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * 4,
                                                          Dots = Enumerable.Range(0, 5)
                                                                           .Select(ix => new BoardDot { X = x.Key, Y = y1 + BOARD_ELLIPSE_INTERVAL * ix })
                                                                           .ToHashSet(),
                                                      });
                                                  }
                                              }

                                              return res;
                                          })
                                          .ToList();

                //LineType.Horizontal
                var horizontalLines = BoardLines?.Where(x => x.LineType == BoardLineType.Horizontal).ToList() ?? [];
                insideDots = horizontalLines.SelectMany(x => x.InsideDots(ELLIPSE_WIDTH / 2)).ToHashSet();

                candidates.AddRange(BoardDots.Where(d => !insideDots.Contains(d))
                                             .GroupBy(x => x.Y)
                                             .SelectMany(x =>
                                             {
                                                 var res = new List<BoardLine>();
                                             
                                                 var xs = x.Select(y => y.X).Distinct().OrderBy(y => y).ToList();
                                             
                                                 for (var i = 0; i < xs.Count; i++)
                                                 {
                                                     var x1 = xs[i];
                                             
                                                     if (xs.Where(x2 => x1 - BOARD_ELLIPSE_INTERVAL <= x2 && x2 <= x1 - BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * 4).Count() == 4)
                                                     {
                                                         var dots = Enumerable.Range(0, 5)
                                                                              .Select(ix => new BoardDot { X = x1 - BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * ix, Y = x.Key })
                                                                              .ToHashSet();

                                                         if (dots.Intersect(insideDots).Count() == 0)
                                                         {
                                                             res.Add(new BoardLine
                                                             {
                                                                 X1 = x1 - BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2,
                                                                 X2 = x1 - BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * 4,
                                                                 Y1 = x.Key + ELLIPSE_WIDTH / 2,
                                                                 Y2 = x.Key + ELLIPSE_WIDTH / 2,
                                                                 Dots = dots,
                                                             });
                                                         }
                                                     }
                                             
                                                     if (xs.Where(x2 => x1 <= x2 && x2 <= x1 + BOARD_ELLIPSE_INTERVAL * 4).Count() == 4)
                                                     {
                                                         res.Add(new BoardLine
                                                         {
                                                             X1 = x1 + ELLIPSE_WIDTH / 2,
                                                             X2 = x1 + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * 4,
                                                             Y1 = x.Key + ELLIPSE_WIDTH / 2,
                                                             Y2 = x.Key + ELLIPSE_WIDTH / 2,
                                                             Dots = Enumerable.Range(0, 5)
                                                                              .Select(ix => new BoardDot { X = x1 + BOARD_ELLIPSE_INTERVAL * ix, Y = x.Key })
                                                                              .ToHashSet(),
                                                         });
                                                     }
                                                 }
                                             
                                                 return res;
                                             })
                                             .Distinct());

                //LineType.DiagonalDown
                var diagonalDownLines = BoardLines?.Where(x => x.LineType == BoardLineType.DiagonalDown).ToList() ?? [];
                insideDots = diagonalDownLines.SelectMany(x => x.InsideDots(ELLIPSE_WIDTH / 2)).ToHashSet();

                candidates.AddRange(BoardDots.Where(d => !insideDots.Contains(d))
                                             .GroupBy(x => x.GroupParam(BoardLineType.DiagonalDown))
                                             .SelectMany(x =>
                                             {
                                                 var res = new List<BoardLine>();
                                             
                                                 var xys = x.Select(y => (y.X, y.Y)).Distinct().OrderBy(y => y.X).ToList();
                                             
                                                 for (var i = 0; i < xys.Count; i++)
                                                 {
                                                     var xy1 = xys[i];
                                             
                                                     if (xys.Where(xy2 => xy1.X - BOARD_ELLIPSE_INTERVAL <= xy2.X && xy2.X <= xy1.X - BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * 4 &&
                                                                                    xy1.Y - BOARD_ELLIPSE_INTERVAL <= xy2.Y && xy2.Y <= xy1.Y - BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * 4)
                                                                      .Count() == 4)
                                                     {
                                                         var dots = Enumerable.Range(0, 5)
                                                                              .Select(ix => new BoardDot
                                                                              {
                                                                                  X = xy1.X - BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * ix,
                                                                                  Y = xy1.Y - BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * ix,
                                                                              })
                                                                              .ToHashSet();

                                                         if (dots.Intersect(insideDots).Count() == 0)
                                                         {
                                                             res.Add(new BoardLine
                                                             {
                                                                 X1 = xy1.X - BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2,
                                                                 X2 = xy1.X - BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * 4,
                                                                 Y1 = xy1.Y - BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2,
                                                                 Y2 = xy1.Y - BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * 4,
                                                                 Dots = dots,
                                                             });
                                                         }
                                                     }

                                                     if (xys.Where(xy2 => xy1.X <= xy2.X && xy2.X <= xy1.X + BOARD_ELLIPSE_INTERVAL * 4 &&
                                                                          xy1.Y <= xy2.Y && xy2.Y <= xy1.Y + BOARD_ELLIPSE_INTERVAL * 4)
                                                            .Count() == 4)
                                                     {
                                                         res.Add(new BoardLine
                                                         {
                                                             X1 = xy1.X + ELLIPSE_WIDTH / 2,
                                                             X2 = xy1.X + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * 4,
                                                             Y1 = xy1.Y + ELLIPSE_WIDTH / 2,
                                                             Y2 = xy1.Y + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * 4,
                                                             Dots = Enumerable.Range(0, 5)
                                                                              .Select(ix => new BoardDot
                                                                              {
                                                                                  X = xy1.X + BOARD_ELLIPSE_INTERVAL * ix,
                                                                                  Y = xy1.Y + BOARD_ELLIPSE_INTERVAL * ix,
                                                                              })
                                                                              .ToHashSet(),
                                                         });
                                                     }
                                                 }
                                             
                                                 return res;
                                             })
                                             .Distinct());

                //LineType.DiagonalUp
                var diagonalUpLines = BoardLines?.Where(x => x.LineType == BoardLineType.DiagonalUp).ToList() ?? [];
                insideDots = diagonalUpLines.SelectMany(x => x.InsideDots(ELLIPSE_WIDTH / 2)).ToHashSet();

                candidates.AddRange(BoardDots.Where(d => !insideDots.Contains(d))
                                             .GroupBy(x => x.GroupParam(BoardLineType.DiagonalUp))
                                             .SelectMany(x =>
                                             {
                                                 var res = new List<BoardLine>();

                                                 var xys = x.Select(y => (y.X, y.Y)).Distinct().OrderBy(y => y.X).ToList();

                                                 for (var i = 0; i < xys.Count; i++)
                                                 {
                                                     var xy1 = xys[i];

                                                     if (xys.Where(xy2 => xy1.X - BOARD_ELLIPSE_INTERVAL <= xy2.X && xy2.X <= xy1.X - BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * 4 &&
                                                                          xy1.Y + BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * -4 <= xy2.Y && xy2.Y <= xy1.Y + BOARD_ELLIPSE_INTERVAL)
                                                                      .Count() == 4)
                                                     {
                                                         var dots = Enumerable.Range(0, 5)
                                                                              .Select(ix => new BoardDot
                                                                              {
                                                                                  X = xy1.X - BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * ix,
                                                                                  Y = xy1.Y + BOARD_ELLIPSE_INTERVAL + BOARD_ELLIPSE_INTERVAL * -ix,
                                                                              })
                                                                              .ToHashSet();

                                                         if (dots.Intersect(insideDots).Count() == 0)
                                                         {
                                                             res.Add(new BoardLine
                                                             {
                                                                 X1 = xy1.X - BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2,
                                                                 X2 = xy1.X - BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * 4,
                                                                 Y1 = xy1.Y + BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2,
                                                                 Y2 = xy1.Y + BOARD_ELLIPSE_INTERVAL + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * -4,
                                                                 Dots = dots,
                                                             });
                                                         }
                                                     }

                                                     if (xys.Where(xy2 => xy1.X <= xy2.X && xy2.X <= xy1.X + BOARD_ELLIPSE_INTERVAL * 4 &&
                                                                          xy1.Y + BOARD_ELLIPSE_INTERVAL * -4 <= xy2.Y && xy2.Y <= xy1.Y)
                                                            .Count() == 4)
                                                     {
                                                         res.Add(new BoardLine
                                                         {
                                                             X1 = xy1.X + ELLIPSE_WIDTH / 2,
                                                             X2 = xy1.X + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * 4,
                                                             Y1 = xy1.Y + ELLIPSE_WIDTH / 2,
                                                             Y2 = xy1.Y + ELLIPSE_WIDTH / 2 + BOARD_ELLIPSE_INTERVAL * -4,
                                                             Dots = Enumerable.Range(0, 5)
                                                                              .Select(ix => new BoardDot
                                                                              {
                                                                                  X = xy1.X + BOARD_ELLIPSE_INTERVAL * ix,
                                                                                  Y = xy1.Y + BOARD_ELLIPSE_INTERVAL * -ix,
                                                                              })
                                                                              .ToHashSet(),
                                                         });
                                                     }
                                                 }

                                                 return res;
                                             })
                                             .Distinct());

                // TODO: Rank lines, order by y and x? good if it has adjacent dot to other line?
                return candidates;
            }
              
            return [];
        }

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
            for (var x = dirtyRect.Left + BOARD_ELLIPSE_INTERVAL; x < dirtyRect.Right - ELLIPSE_WIDTH - BOARD_ELLIPSE_INTERVAL; x += BOARD_ELLIPSE_INTERVAL)
            {
                for (var y = dirtyRect.Top + BOARD_ELLIPSE_INTERVAL; y < dirtyRect.Bottom - ELLIPSE_WIDTH - BOARD_ELLIPSE_INTERVAL; y += BOARD_ELLIPSE_INTERVAL)
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
