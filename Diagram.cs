using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suitougreentea.SimuDia
{
    class Diagram
    {
        private long calculateTripTime(LineStationData from, LineStationData to, int lineId)
        {
            var result = _calculateTripTime(from, to, lineId);
            if (result >= 0) return result;
            return _calculateTripTime(to, from, lineId);
        }

        private long _calculateTripTime(LineStationData from, LineStationData to, int lineId)
        {
            var time = 0L;

            var fromStationIndex = stations.IndexOf(from.station);
            var toStationIndex = stations.IndexOf(to.station);
            var matchedTimes = times.Where(it => it.fromStation == from.station && it.toStation == to.station && it.id == lineId);
            if (matchedTimes.Count() > 0)
            {
                time = (long)matchedTimes.Select(it => it.time).Average();
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
                    var matchedExpandedTimes = times.Where(it => it.fromStation == expandedFrom && it.toStation == expandedTo && it.id == lineId);
                    if (matchedExpandedTimes.Count() > 0) time += (long)matchedExpandedTimes.Select(it => it.time).Average();
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
                var forTimes = times.Where(it => it.fromStation == upStation && it.toStation == downStation);
                var revTimes = times.Where(it => it.fromStation == downStation && it.toStation == upStation);
                var bidiTimes = forTimes.Concat(revTimes);
                if (bidiTimes.Count() == 0) accumulatedTicks.Add(0);
                else
                {
                    var meanTicks = bidiTimes.Select(it => it.time).Average();
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
                    var timeId = from.timeId ?? it.defaultTimeId ?? 0;

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
                    if (i == 0) arrival = 0;    // Will be set later

                    long? actualStoppingTime = null;
                    if (shiftTime != null)
                    {
                        accum = shiftTime.Value;
                        while (accum < arrival + essentialStoppingTime) accum += monthLength / it.divisor;
                    }
                    else
                    {
                        accum += essentialStoppingTime;
                    }

                    var departure = accum;

                    long tripTime;
                    tripTime = calculateTripTime(from, to, timeId);
                    if (from.tripTime != null) tripTime = from.tripTime.Value;

                    if (from.tripTimeOffset != null) tripTime += from.tripTimeOffset.Value;
                    accum += tripTime;

                    list.Add(new LineTimeStationData(arrival, departure, shiftNum, essentialStoppingTime, tripTime));
                }

                var oldFirst = list[0];
                var last = list.Last();
                var firstArrival = last.departure + last.tripTime;
                while (oldFirst.departure - oldFirst.essentialStoppingTime < firstArrival) firstArrival -= monthLength / it.divisor;
                list[0] = new LineTimeStationData(firstArrival, oldFirst.departure, oldFirst.shiftNum, oldFirst.essentialStoppingTime, oldFirst.tripTime);

                return new LineTimeData(list);
            }).ToList();
        }
    }
}
