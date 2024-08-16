namespace JoinFive.Contract
{
    public class Settings
    {
        public int HiScore { get; set; }
        public HashSet<BoardLine> CurrentLines { get; set; } = [];
        public HashSet<BoardDot> CurrentDots { get; set; } = [];
    }
}
