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

            string currentLineName = null;
            int? currentLineDivisor = null;
            var currentLineWidth = 1f;
            var currentLineColor = Color.Black;
            var currentLineStations = new List<LineStationData>();

            long? _monthLength = null;
            int? _shiftDivisor = null;

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
                                while (time < 0) time += _monthLength ?? throw new InvalidOperationException();
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
                            lines.Add(new LineData(currentLineName,
                                currentLineDivisor ?? throw new InvalidOperationException(),
                                currentLineWidth,
                                currentLineColor,
                                currentLineStations));
                            currentLineName = null;
                            currentLineDivisor = null;
                            currentLineWidth = 1f;
                            currentLineColor = Color.Black;
                            currentLineStations = new List<LineStationData>();
                        }
                        else if (!l.StartsWith("<"))
                        {
                            if (l.IndexOf("=") >= 0)
                            {
                                var split = Regex.Match(l, @"(.*)=(.*)");
                                var name = split.Groups[1].Value.Trim();
                                var value = split.Groups[2].Value.Trim();
                                if (name == "name") currentLineName = value;
                                if (name == "divisor") currentLineDivisor = int.Parse(value);
                                if (name == "divisor_by_every" && currentLineDivisor == null) currentLineDivisor = (int)(_monthLength / parseTime(value).Ticks);
                                if (name == "width") currentLineWidth = float.Parse(value);
                                if (name == "color") currentLineColor = ColorTranslator.FromHtml(value);
                            }
                            else
                            {
                                var station = parseStation(l);
                                currentLineStations.Add(new LineStationData(null, null, null, false, false, station));
                            }
                        }
                        else
                        {
                            int? shift = null;
                            int? wait = null;
                            long? shorten = null;
                            var fill = false;
                            var reverse = false;
                            var split = Regex.Match(l, @"<(.*)>(.*)");
                            var options = split.Groups[1].Value.Trim().Split(',');
                            foreach (var str in options) {
                                var s = str.Trim();
                                if (s.IndexOf('=') >= 0)
                                {
                                    var optSplit = Regex.Match(s, @"(.*)=(.*)");
                                    var name = optSplit.Groups[1].Value.Trim();
                                    var value = optSplit.Groups[2].Value.Trim();
                                    if (name == "shift") shift = int.Parse(value);
                                    if (name == "shift_by_time" && shift == null) shift = (int)(((double)parseTime(value).Ticks / _monthLength) * _shiftDivisor);
                                    if (name == "wait" && shift == null) wait = int.Parse(value);
                                    if (name == "wait_by_time" && wait == null && shift == null) wait = (int)(((double)parseTime(value).Ticks / _monthLength) * _shiftDivisor);
                                    if (name == "shorten_by_time") shorten = parseTime(value).Ticks;
                                }
                                else
                                {
                                    if (s == "fill") fill = true;
                                    if (s == "reverse") reverse = true;
                                }
                            }
                            var station = parseStation(split.Groups[2].Value.Trim());
                            currentLineStations.Add(new LineStationData(shift, wait, shorten, fill, reverse, station));
                        }
                    }
                }
            }
            lines.Add(new LineData(currentLineName,
                currentLineDivisor ?? throw new InvalidOperationException(),
                currentLineWidth,
                currentLineColor,
                currentLineStations));

            var monthLength = _monthLength ?? throw new InvalidOperationException();
            var shiftDivisor = _shiftDivisor ?? throw new InvalidOperationException();

            var expandedLines = lines.Select(it =>
            {
                var stationData = new List<LineStationData>();
                stationData.Add(it.stations[0]);
                for (var i = 1; i < it.stations.Count; i++)
                {
                    var from = it.stations[i - 1];
                    var to = it.stations[i];
                    if (to.fill)
                    {
                        var expanded = Util.expandStation(stations, from.station, to.station);
                        for (var j = 1; j < expanded.Count - 1; j++)
                        {
                            stationData.Add(new LineStationData(null, null, null, false, false, expanded[j]));
                        }
                        stationData.Add(it.stations[i]);
                    }
                    else stationData.Add(it.stations[i]);
                }
                return new LineData(it.name, it.divisor, it.width, it.color, stationData);
            }).ToList();

            return new Diagram(monthLength, shiftDivisor, stations, times, lines, expandedLines);
        }
    }
}
