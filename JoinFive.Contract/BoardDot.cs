namespace JoinFive.Contract
{
    public class BoardDot : IEquatable<BoardDot>
    {
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsInitialDot { get; set; }

        public bool Equals(BoardDot? other)
        {
            // Check whether the compared object is null.
            if (ReferenceEquals(other, null)) 
                return false;

            // Check whether the compared object references the same data.
            if (ReferenceEquals(this, other))
                return true;

            //Check whether the products' properties are equal.
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override string ToString() => $"X: {X}, Y: {Y}, initial: {IsInitialDot}";
        
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();        
    }
}
