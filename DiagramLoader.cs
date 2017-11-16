using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace simutrans_diagram
{
    class DiagramLoader
    {
        private static TimeSpan parseOffsetTime(string input)
        {
            var match = Regex.Match(input, @"([+-]?)(.*)");
            var time = parseTime(match.Groups[2].Value.Trim());
            if (match.Groups[1].Value == "-") return time.Negate();
            return time;
        }

        private static TimeSpan parseTime(string input)
        {
            var hms = Regex.Match(input, @"([0-9]*)([0-5][0-9])([0-5][0-9])");
            var hour = hms.Groups[1].Value.Length == 0 ? 0 : int.Parse(hms.Groups[1].Value);
            var minute = int.Parse(hms.Groups[2].Value);
            var second = int.Parse(hms.Groups[3].Value);
            return new TimeSpan(hour, minute, second);
        }
    
        private static Station parseStation(string input)
        {
            if (input.IndexOf('@') >= 0)
            {
                var split = Regex.Match(input, @"(.*)@\s*([0-9]*)");
                var name = split.Groups[1].Value.Trim();
                var id = int.Parse(split.Groups[2].Value);
                return new Station(name, id);
            }
            else return new Station(input, 0);
        }

        public static Diagram load(string path)
        {
            var mode = "Top";

            long? _monthLength = null;
            int? _shiftDivisor = null;
            long defaultLoadingTime = new TimeSpan(0, 0, 30).Ticks;
            long defaultReversingTime = new TimeSpan(0, 1, 0).Ticks;

            string currentLineName = null;
            int? currentLineDivisor = null;
            var currentLineWidth = 1f;
            var currentLineColor = Color.Black;
            var currentLineStations = new List<LineStationData>();
            long? currentLineDefaultLoadingTime = null;
            long? currentLineDefaultReversingTime = null;

            var stations = new List<Station>();
            var times = new List<TimeData>();
            var lines = new List<LineData>();

            var textLines = System.IO.File.ReadAllLines(path);
            foreach (var _l in textLines)
            {
                var l = _l.Trim();
                if (l.Length == 0) continue;
                if (l.StartsWith("#")) continue;
                if (l.StartsWith("["))
                {
                    var type = Regex.Match(l, @"\[(.*)\]").Groups[1].Value.Trim();
                    if (mode == "Top" && type != "General") throw new SyntaxErrorException();
                    if (mode == "General" && type != "Stations") throw new SyntaxErrorException();
                    if (mode == "Stations" && type != "RawTimes") throw new SyntaxErrorException();
                    if (mode == "RawTimes" && type != "Lines") throw new SyntaxErrorException();
                    if (mode == "Lines") throw new SyntaxErrorException();
                    mode = type;
                }
                else
                {
                    if (mode == "General") {
                        var split = Regex.Match(l, @"(.*)=(.*)");
                        var name = split.Groups[1].Value.Trim();
                        var value = split.Groups[2].Value.Trim();

                        if (name == "month_length") _monthLength = parseTime(value).Ticks;
                        if (name == "shift_divisor") _shiftDivisor = int.Parse(value);
                        if (name == "default_loading_time") defaultLoadingTime = parseTime(value).Ticks;
                        if (name == "default_reversing_time") defaultReversingTime = parseTime(value).Ticks;
                    }
                    if (mode == "Stations")
                    {
                        stations.Add(parseStation(l));
                    }
                    if (mode == "RawTimes")
                    {
                        var split = Regex.Match(l, @"(.*)->(.*):(.*)");
                        var fromStation = parseStation(split.Groups[1].Value.Trim());
                        var toStation = parseStation(split.Groups[2].Value.Trim());
                        var fromStationIndex = stations.IndexOf(fromStation);
                        var toStationIndex = stations.IndexOf(toStation);
                        if (fromStationIndex == -1 || toStationIndex == -1) throw new InvalidOperationException();
                        var rawTime = split.Groups[3].Value.Trim();
                        var timesList = rawTime.Split(',').Select(s => {
                            var trimmed = s.Trim();
                            if (trimmed.IndexOf("-") >= 0)
                            {
                                var multipleTimeSplit = Regex.Match(l, @"(.*)-(.*)");
                                var fromTime = parseTime(multipleTimeSplit.Groups[1].Value.Trim()).Ticks;
                                var toTime = parseTime(multipleTimeSplit.Groups[2].Value.Trim()).Ticks;
                                var time = toTime - fromTime;
                                if (_monthLength == null) throw new InvalidOperationException();
                                while (time < 0) time += _monthLength.Value;
                                return time;
                            }
                            else return parseTime(trimmed).Ticks;
                        }).ToList();
                        var sameTimeDataIndex = times.FindIndex(it => it.fromStation == fromStation && it.toStation == toStation);
                        if (sameTimeDataIndex >= 0) times[sameTimeDataIndex].times.AddRange(timesList);
                        else times.Add(new TimeData(fromStation, toStation, timesList));
                    }
                    if (mode == "Lines")
                    {
                        if (l.StartsWith("-"))
                        {
                            if (currentLineDivisor == null) throw new InvalidOperationException();
                            lines.Add(new LineData(currentLineName,
                                currentLineDivisor.Value,
                                currentLineWidth,
                                currentLineColor,
                                currentLineDefaultLoadingTime,
                                currentLineDefaultReversingTime,
                                currentLineStations));
                            currentLineName = null;
                            currentLineDivisor = null;
                            currentLineWidth = 1f;
                            currentLineColor = Color.Black;
                            currentLineDefaultLoadingTime = null;
                            currentLineDefaultReversingTime = null;
                            currentLineStations = new List<LineStationData>();
                        }
                        else if (!l.StartsWith("<") && l.IndexOf("=") >= 0)
                        {
                            var split = Regex.Match(l, @"(.*)=(.*)");
                            var name = split.Groups[1].Value.Trim();
                            var value = split.Groups[2].Value.Trim();
                            if (name == "name") currentLineName = value;
                            if (name == "divisor") currentLineDivisor = int.Parse(value);
                            if (name == "divisor_by_every" && currentLineDivisor == null) currentLineDivisor = (int)(_monthLength / parseTime(value).Ticks);
                            if (name == "width") currentLineWidth = float.Parse(value);
                            if (name == "color") currentLineColor = ColorTranslator.FromHtml(value);
                            if (name == "default_loading_time") currentLineDefaultLoadingTime = parseTime(value).Ticks;
                            if (name == "default_reversing_time") currentLineDefaultReversingTime = parseTime(value).Ticks;
                        }
                        else
                        {
                            Station station;
                            long? shiftTime = null;
                            int? shiftNum = null;
                            long? waitingTime = null;
                            long? loadingTime = null;
                            bool reverse = false;
                            long? reversingTime = null;
                            long? tripTime = null;
                            long? tripTimeOffset = null;

                            if (l.StartsWith("<"))
                            {
                                var split = Regex.Match(l, @"<(.*)>(.*)");
                                var options = split.Groups[1].Value.Trim().Split(',');

                                foreach (var str in options) {
                                    var s = str.Trim();
                                    if (s.IndexOf('=') >= 0)
                                    {
                                        var optSplit = Regex.Match(s, @"(.*)=(.*)");
                                        var name = optSplit.Groups[1].Value.Trim();
                                        var value = optSplit.Groups[2].Value.Trim();
                                        if (name == "shift") shiftTime = parseTime(value).Ticks;
                                        if (name == "shift_num") shiftNum = int.Parse(value);
                                        if (name == "wait") waitingTime = parseTime(value).Ticks;
                                        if (name == "load") loadingTime = parseTime(value).Ticks;
                                        if (name == "trip") tripTime = parseTime(value).Ticks;
                                        if (name == "trip_offset") tripTimeOffset = parseOffsetTime(value).Ticks;
                                        if (name == "reverse")
                                        {
                                            reverse = true;
                                            reversingTime = parseTime(value).Ticks;
                                        }
                                    }
                                    else
                                    {
                                        if (s == "reverse") reverse = true;
                                    }
                                }
                                station = parseStation(split.Groups[2].Value.Trim());
                            }
                            else
                            {
                                station = parseStation(l);
                            }
                            currentLineStations.Add(new LineStationData(station, shiftTime, shiftNum, waitingTime, loadingTime, reverse, reversingTime, tripTime, tripTimeOffset));
                        }
                    }
                }
            }
            if (currentLineDivisor == null) throw new InvalidOperationException();
            lines.Add(new LineData(currentLineName,
                currentLineDivisor.Value,
                currentLineWidth,
                currentLineColor,
                currentLineDefaultLoadingTime,
                currentLineDefaultReversingTime,
                currentLineStations));

            if (_monthLength == null) throw new InvalidOperationException();
            if (_shiftDivisor == null) throw new InvalidOperationException();
            var monthLength = _monthLength.Value;
            var shiftDivisor = _shiftDivisor.Value;

            return new Diagram(monthLength, shiftDivisor, defaultLoadingTime, defaultReversingTime, stations, times, lines);
        }
    }
}
