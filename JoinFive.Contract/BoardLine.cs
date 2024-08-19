namespace JoinFive.Contract
{
    public class BoardLine : IEquatable<BoardLine>
    {
        public float X1 { get; set; }
        public float Y1 { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }
        public HashSet<BoardDot> Dots { get; set; } = [];
        public BoardLineType LineType => X1 == X2 ? BoardLineType.Vertical : 
                                         Y1 == Y2 ? BoardLineType.Horizontal : 
                                         (X2 > X1 && Y2 > Y1) || (X2 < X1 && Y2 < Y1) ? BoardLineType.DiagonalDown : 
                                         BoardLineType.DiagonalUp;
        public HashSet<BoardDot> InsideDots(float halfEllipseWidth) => Dots.Where(d => !((d.X == X1 - halfEllipseWidth && d.Y == Y1 - halfEllipseWidth) || (d.X == X2 - halfEllipseWidth && d.Y == Y2 - halfEllipseWidth)))
                                                                           .ToHashSet();

        public bool Equals(BoardLine? other)
        {
            // Check whether the compared object is null.
            if (ReferenceEquals(other, null))
                return false;

            // Check whether the compared object references the same data.
            if (ReferenceEquals(this, other))
                return true;

            //Check whether the products' properties are equal.
            return X1.Equals(other.X1) && Y1.Equals(other.Y1) && X2.Equals(other.X2) && Y2.Equals(other.Y2);
        }

        public override int GetHashCode() => X1.GetHashCode() ^ Y1.GetHashCode() ^ X2.GetHashCode() ^ Y2.GetHashCode();        
    }
}
