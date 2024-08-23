namespace JoinFive.Contract
{
    public class Settings
    {
        public int GameId { get; set; }
        public int HiScore { get; set; }
        public HashSet<BoardLine> CurrentLines { get; set; } = [];
        public HashSet<BoardDot> CurrentDots { get; set; } = [];
        public List<HiScoreSettings> HiScoreSettings { get; set; } = [];
    }

    public class HiScoreSettings
    {
        public int GameId { get; set; }
        public DateTime Timestamp { get; set; }
        public int HiScore { get; set; }
        public HashSet<BoardLine> CurrentLines { get; set; } = [];
        public HashSet<BoardDot> CurrentDots { get; set; } = [];
    }
}
