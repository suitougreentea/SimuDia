using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simutrans_diagram
{
    class Diagram
    {
        private long calculateTripTime(LineStationData from, LineStationData to)
        {
            var result = _calculateTripTime(from, to);
            if (result >= 0) return result;
            return _calculateTripTime(to, from);
        }

        private long _calculateTripTime(LineStationData from, LineStationData to)
        {
            var time = 0L;

            var fromStationIndex = stations.IndexOf(from.station);
            var toStationIndex = stations.IndexOf(to.station);
            var timesIndex = times.FindIndex(ce => ce.fromStation == from.station && ce.toStation == to.station);
            if (timesIndex >= 0)
            {
                var entry = times[timesIndex];
                time = (long)entry.times.Average();
            }
            else if (from.station.name == to.station.name)
            {
                time = 0L;
            }
            else
            {
                var expanded = Util.expandStation(stations, from.station, to.station);
                time = 0L;
                var success = true;

                for (var j = 1; j < expanded.Count; j++)
                {
                    var expandedFrom = expanded[j - 1];
                    var expandedTo = expanded[j];
                    var expandedTimesIndex = times.FindIndex(ce => ce.fromStation == expandedFrom && ce.toStation == expandedTo);
                    if (expandedTimesIndex >= 0) time += (long)times[expandedTimesIndex].times.Average();
                    else
                    {
                        success = false;
                        break;
                    }
                }
                if (!success) time = -1L;
            }
            return time;
        }

        public readonly long monthLength;
        public readonly int shiftDivisor;
        public readonly long defaultLoadingTime;
        public readonly long defaultReversingTime;
        public readonly List<Station> stations;
        public readonly List<TimeData> times;
        public readonly List<LineData> lines;
        public readonly List<long> accumulatedTicks;
        public readonly List<LineTimeData> lineTimes;

        public Diagram(long monthLength, int shiftDivisor, long defaultLoadingTime, long defaultReversingTime, List<Station> stations, List<TimeData> times, List<LineData> lines)
        {
            this.monthLength = monthLength;
            this.shiftDivisor = shiftDivisor;
            this.defaultLoadingTime = defaultLoadingTime;
            this.defaultReversingTime = defaultReversingTime;
            this.stations = stations;
            this.times = times;
            this.lines = lines;

            accumulatedTicks = new List<long>(stations.Count);
            accumulatedTicks.Add(0L);
            for (int i = 1; i < stations.Count; i++)
            {
                var upStation = stations[i - 1];
                var downStation = stations[i];
                var forIndex = times.FindIndex(it => it.fromStation == upStation && it.toStation == downStation);
                var revIndex = times.FindIndex(it => it.fromStation == downStation && it.toStation == upStation);
                var ticksList = new List<Double>(2);
                if (forIndex >= 0) ticksList.Add(times[forIndex].times.Average());
                if (revIndex >= 0) ticksList.Add(times[revIndex].times.Average());
                if (ticksList.Count == 0) accumulatedTicks.Add(0);
                else
                {
                    var meanTicks = ticksList.Average();
                    accumulatedTicks.Add(accumulatedTicks[i - 1] + (long)meanTicks);
                }
            }

            lineTimes = lines.Select(it =>
            {
                var accum = 0L;

                var list = new List<LineTimeStationData>();

                for (var i = 0; i < it.stations.Count; i++)
                {
                    var from = it.stations[i];
                    var to = it.stations[(i + 1) % it.stations.Count];

                    var arrival = accum;

                    int? shiftNum = null;
                    if (i == 0) shiftNum = 0;
                    long? shiftTime = null;
                    if (from.waitingTime != null) shiftNum = (int)(((double)(arrival + from.waitingTime) / monthLength) * shiftDivisor);
                    if (from.shiftNum != null) shiftNum = from.shiftNum.Value;
                    else if (from.shiftTime != null) shiftNum = (int)(((double)from.shiftTime.Value / monthLength) * shiftDivisor);

                    if (shiftNum != null) shiftTime = ((monthLength / shiftDivisor) * shiftNum.Value);

                    var loadingTime = from.loadingTime ?? it.defaultLoadingTime ?? defaultLoadingTime;
                    if (from.station.name == to.station.name) loadingTime = 0L;
                    var reversingTime = from.reversingTime ?? it.defaultReversingTime ?? defaultReversingTime;
                    var essentialStoppingTime = Math.Max(loadingTime, from.reverse ? reversingTime : 0);
                    if (i == 0)
                    {
                        // TODO
                        if (shiftTime != null) arrival = shiftTime.Value;
                        essentialStoppingTime = 0;
                    }

                    long? plannedStoppingTime = null;
                    if (shiftTime != null)
                    {
                        plannedStoppingTime = shiftTime.Value - arrival;
                        accum = shiftTime.Value;
                    }
                    else
                    {
                        accum += essentialStoppingTime;
                    }

                    var departure = accum;

                    long tripTime;
                    tripTime = calculateTripTime(from, to);
                    if (from.tripTime != null) tripTime = from.tripTime.Value;

                    if (from.tripTimeOffset != null) tripTime += from.tripTimeOffset.Value;
                    accum += tripTime;

                    list.Add(new LineTimeStationData(arrival, departure, shiftNum, plannedStoppingTime, essentialStoppingTime, tripTime));
                }

                return new LineTimeData(list);
            }).ToList();
        }
    }
}
