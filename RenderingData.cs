using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simutrans_diagram
{
    class ProtrudingArea
    {
        public readonly float start;
        public readonly float end;
        public readonly int level;
        public readonly ProtrudingDirection direction;

        public ProtrudingArea(float start, float end, int level, ProtrudingDirection direction)
        {
            this.start = start;
            this.end = end;
            this.level = level;
            this.direction = direction;
        }
    }
    enum ProtrudingDirection
    {
        UP,
        DOWN,
    }
}
