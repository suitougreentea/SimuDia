using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simutrans_diagram
{
    class Diagram
    {
        public readonly long monthLength;
        public readonly int shiftDivisor;
        public readonly List<Station> stations;
        public readonly List<TimeData> times;
        public readonly List<LineData> lines;
        public readonly List<LineData> expandedLines;

        public Diagram(long monthLength, int shiftDivisor, List<Station> stations, List<TimeData> times, List<LineData> lines, List<LineData> expandedLines)
        {
            this.monthLength = monthLength;
            this.shiftDivisor = shiftDivisor;
            this.stations = stations;
            this.times = times;
            this.lines = lines;
            this.expandedLines = expandedLines;
        }
    }
}
