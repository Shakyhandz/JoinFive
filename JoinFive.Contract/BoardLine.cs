using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinFive.Contract
{
    public class BoardLine
    {
        public float X1 { get; set; }
        public float Y1 { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }
        public HashSet<BoardDot> Dots { get; set; } = [];
    }
}
