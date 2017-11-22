using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suitougreentea.SimuDia
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

        override public bool Equals(object o)
        {
            if (o == null || this.GetType() != o.GetType()) return false;
            return (this == (Station)o);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
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
        public readonly long time;
        public readonly int id;

        public TimeData(Station fromStation, Station toStation, long time, int id)
        {
            this.fromStation = fromStation;
            this.toStation = toStation;
            this.time = time;
            this.id = id;
        }

        public override string ToString()
        {
            return $"{fromStation} -> {toStation}: {time}";
        }
    }
    public struct LineStationData
    {
        public readonly Station station;
        // Waiting time parameters are that AFTER ARRIVAL
        // shift and wait indicate absolute shift;
        // if calculated stopping time is shorter than essential stopping time, the program will emit a warning.
        // shift: specify shift by time
        public readonly long? shiftTime;
        // shift_num: specify shift by in-game parameter
        public readonly int? shiftNum;
        // wait: specify waiting time from arrival to departure
        public readonly long? waitingTime;
        
        // essential stopping time
        // load (time: optional)
        public readonly long? loadingTime;
        // reverse (time: optional)
        public readonly bool reverse;
        public readonly long? reversingTime;

        // Trip time is that between NEXT station
        // trip: you can override trip time
        public readonly int? timeId;
        public readonly long? tripTime;
        // trip_offset
        public readonly long? tripTimeOffset;

        public LineStationData(Station station, long? shiftTime, int? shiftNum, long? waitingTime, long? loadingTime, bool reverse, long? reversingTime, int? timeId, long? tripTime, long? tripTimeOffset)
        {
            this.station = station;
            this.shiftTime = shiftTime;
            this.shiftNum = shiftNum;
            this.waitingTime = waitingTime;
            this.loadingTime = loadingTime;
            this.reverse = reverse;
            this.reversingTime = reversingTime;
            this.timeId = timeId;
            this.tripTime = tripTime;
            this.tripTimeOffset = tripTimeOffset;
        }

        public override string ToString()
        {
            return $"Stops at {station.ToString()}";
        }
    }
    public struct LineData
    {
        public readonly string name;
        public readonly int divisor;
        public readonly float width;
        public readonly Color color;
        public readonly long? defaultLoadingTime;
        public readonly long? defaultReversingTime;
        public readonly int? defaultTimeId;
        public readonly List<LineStationData> stations;

        public LineData(string name, int divisor, float width, Color color, long? defaultLoadingTime, long? defaultReversingTime, int? defaultTimeId, List<LineStationData> stations)
        {
            this.name = name;
            this.divisor = divisor;
            this.width = width;
            this.color = color;
            this.defaultLoadingTime = defaultLoadingTime;
            this.defaultReversingTime = defaultReversingTime;
            this.defaultTimeId = defaultTimeId;
            this.stations = stations;
        }

        public override string ToString()
        {
            return $"[{name}] ({stations.Count} stations)";
        }
    }

    public struct LineTimeData
    {
        public readonly List<LineTimeStationData> list;
        public readonly long tripTime;

        public LineTimeData(List<LineTimeStationData> list)
        {
            this.list = list;
            tripTime = list.Last().departure + list.Last().tripTime - list.First().departure;
        }
    }

    public struct LineTimeStationData
    {
        public readonly long arrival;
        public readonly long departure;
        public readonly int? shiftNum;
        public readonly long? plannedStoppingTime;
        public readonly long essentialStoppingTime;
        public readonly long tripTime;

        public LineTimeStationData(long arrival, long departure, int? shiftNum, long? plannedStoppingTime, long essentialStoppingTime, long tripTime)
        {
            this.arrival = arrival;
            this.departure = departure;
            this.shiftNum = shiftNum;
            this.plannedStoppingTime = plannedStoppingTime;
            this.essentialStoppingTime = essentialStoppingTime;
            this.tripTime = tripTime;
        }
    }
}
