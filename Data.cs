using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simutrans_diagram
{
    public struct Station : IEquatable<Station>
    {
        public readonly string name;
        public readonly int id;

        public Station(string name, int id)
        {
            this.name = name;
            this.id = id;
        }

        public bool Equals(Station other)
        {
            return this == other;
        }

        public static bool operator ==(Station a, Station b)
        {
            return a.name == b.name && a.id == b.id;
        }

        public static bool operator !=(Station a, Station b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return $"{name}@{id}";
        }
    }
    public struct TimeData
    {
        public readonly Station fromStation;
        public readonly Station toStation;
        public readonly List<long> times;

        public TimeData(Station fromStation, Station toStation, List<long> times)
        {
            this.fromStation = fromStation;
            this.toStation = toStation;
            this.times = times;
        }

        public override string ToString()
        {
            return $"{fromStation} -> {toStation}: {String.Join(",", times)}";
        }
    }
    public struct LineStationData
    {
        public readonly int? shift;
        public readonly int? wait;
        public readonly long? shorten;
        public readonly bool fill;
        public readonly bool reverse;
        public readonly Station station;

        public LineStationData(int? shift, int? wait, long? shorten, bool fill, bool reverse, Station station)
        {
            this.shift = shift;
            this.wait = wait;
            this.shorten = shorten;
            this.fill = fill;
            this.reverse = reverse;
            this.station = station;
        }

        public override string ToString()
        {
            return $"<shift: {shift}, wait: {wait}, fill: {fill}, reverse: {reverse}> {station}";
        }
    }
    public struct LineData
    {
        public readonly string name;
        public readonly int divisor;
        public readonly float width;
        public readonly Color color;
        public readonly List<LineStationData> stations;

        public LineData(string name, int divisor, float width, Color color, List<LineStationData> stations)
        {
            this.name = name;
            this.divisor = divisor;
            this.width = width;
            this.color = color;
            this.stations = stations;
        }

        public override string ToString()
        {
            return $"[{name}] ({stations.Count} stations)";
        }
    }
}
