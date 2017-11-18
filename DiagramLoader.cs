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
    class DiagramLoadingError : Exception {
        public int line;
        public string message;

        public DiagramLoadingError(int line, string message)
        {
            this.line = line;
            this.message = message;
        }
    }

    class AssertionFailedException : DiagramLoadingError
    {
        public AssertionFailedException(int line) : base(line, "Assertion failed; it's a bug. Please report to the developer.")
        {
        }
    }

    class DiagramLoader
    {
        private string path;
        // current line number
        private int i = 0;

        private string mode = "Top";

        private long? _monthLength = null;
        private int? _shiftDivisor = null;
        private long defaultLoadingTime = new TimeSpan(0, 0, 30).Ticks;
        private long defaultReversingTime = new TimeSpan(0, 1, 0).Ticks;

        private string currentLineName;
        private int? currentLineDivisor;
        private float currentLineWidth;
        private Color currentLineColor;
        private List<LineStationData> currentLineStations;
        private long? currentLineDefaultLoadingTime;
        private long? currentLineDefaultReversingTime;

        private List<Station> stations = new List<Station>();
        private List<TimeData> times = new List<TimeData>();
        private List<LineData> lines = new List<LineData>();

        public DiagramLoader(string path)
        {
            this.path = path;
            resetCurrentLineData();
        }

        private TimeSpan parseOffsetTime(string input)
        {
            var match = Regex.Match(input, @"([+-]?)(.*)");
            var time = parseTime(match.Groups[2].Value.Trim());
            if (match.Groups[1].Value == "-") return time.Negate();
            return time;
        }

        // Syntax: hmmss
        // Output: h:mm:ss
        // where h is optional
        private TimeSpan parseTime(string input)
        {
            var hms = Regex.Match(input, @"([0-9]*)([0-5][0-9])([0-5][0-9])");
            var hour = hms.Groups[1].Value.Length == 0 ? 0 : int.Parse(hms.Groups[1].Value);
            var minute = int.Parse(hms.Groups[2].Value);
            var second = int.Parse(hms.Groups[3].Value);
            return new TimeSpan(hour, minute, second);
        }
    
        private Station parseStation(string input)
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

        // Syntax: "{key} = {value}"
        // Output: key -> value
        // "= {value}" is optional
        private KeyValuePair<string, string> parseOption(string input)
        {
            var split = Regex.Match(input, @"(.*)=(.*)");
            return new KeyValuePair<string, string>(split.Groups[1].Value.Trim(), split.Groups[2].Value.Trim());
        }

        // Syntax: "{option}, ..."
        // Output: [option, ...]
        // where option is parsed by parseOption()
        private List<KeyValuePair<string, string>> parseInlineOptions(string input)
        {
            return input.Split(',').Select(it => parseOption(it.Trim())).ToList();
        }

        // Syntax: "<{option}, ...> {station}"
        // Output: ([option, ...], station)
        // "<{option}, ...>" is optional
        private Tuple<List<KeyValuePair<string, string>>, string> parseEntryWithInlineOptions(string input)
        {
            if (input.StartsWith("<"))
            {
                var split = Regex.Match(input, @"<(.*)>(.*)");
                return Tuple.Create(parseInlineOptions(split.Groups[1].Value.Trim()), split.Groups[2].Value.Trim());
            }
            return Tuple.Create(new List<KeyValuePair<string, string>>(), input);
        }

        private void resetCurrentLineData()
        {
            currentLineName = null;
            currentLineDivisor = null;
            currentLineWidth = 1f;
            currentLineColor = Color.Black;
            currentLineStations = new List<LineStationData>();
            currentLineDefaultLoadingTime = null;
            currentLineDefaultReversingTime = null;
        }

        private void putLine()
        {
            if (currentLineDivisor == null) throw new DiagramLoadingError(i, "Line cannot end without specifying divisor");
            lines.Add(new LineData(currentLineName,
                currentLineDivisor.Value,
                currentLineWidth,
                currentLineColor,
                currentLineDefaultLoadingTime,
                currentLineDefaultReversingTime,
                currentLineStations));
            resetCurrentLineData();
        }

        public Diagram load()
        {
            var textLines = System.IO.File.ReadAllLines(path);
            foreach (var _l in textLines)
            {
                i++;
                var l = _l.Trim();
                if (l.Length == 0) continue;
                if (l.StartsWith("#")) continue;
                if (l.StartsWith("["))
                {
                    if (mode == "General")
                    {
                        if (_monthLength == null) throw new DiagramLoadingError(i, "month_length must be specified");
                        if (_shiftDivisor == null) throw new DiagramLoadingError(i, "shift_divisor must be specified");
                    }
                    var type = Regex.Match(l, @"\[(.*)\]").Groups[1].Value.Trim();
                    if (mode == "Top" && type != "General") throw new DiagramLoadingError(i, "[General] must come first");
                    if (mode == "General" && type != "Stations") throw new DiagramLoadingError(i, "[Stations] must come after [General]");
                    if (mode == "Stations" && type != "RawTimes") throw new DiagramLoadingError(i, "[RawTimes] must come after [Stations]");
                    if (mode == "RawTimes" && type != "Lines") throw new DiagramLoadingError(i, "[Lines] must come after [RawTimes]");
                    if (mode == "Lines") throw new DiagramLoadingError(i, "[Lines] section must be last");
                    mode = type;
                }
                else
                {
                    if (mode == "General") {
                        var option = parseOption(l);
                        var key = option.Key;
                        var value = option.Value;

                        if (key == "month_length") _monthLength = parseTime(value).Ticks;
                        if (key == "shift_divisor") _shiftDivisor = int.Parse(value);
                        if (key == "default_loading_time") defaultLoadingTime = parseTime(value).Ticks;
                        if (key == "default_reversing_time") defaultReversingTime = parseTime(value).Ticks;
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
                        if (fromStationIndex == -1) throw new DiagramLoadingError(i, "Undefined station");
                        if (toStationIndex == -1) throw new AssertionFailedException(i);
                        var rawTime = split.Groups[3].Value.Trim();
                        var timesList = rawTime.Split(',').Select(s => {
                            var trimmed = s.Trim();
                            if (trimmed.IndexOf("-") >= 0)
                            {
                                var multipleTimeSplit = Regex.Match(l, @"(.*)-(.*)");
                                var fromTime = parseTime(multipleTimeSplit.Groups[1].Value.Trim()).Ticks;
                                var toTime = parseTime(multipleTimeSplit.Groups[2].Value.Trim()).Ticks;
                                var time = toTime - fromTime;
                                if (_monthLength == null) throw new AssertionFailedException(i);
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
                            putLine();
                        }
                        else if (!l.StartsWith("<") && l.IndexOf("=") >= 0)
                        {
                            var option = parseOption(l);
                            var key = option.Key;
                            var value = option.Value;

                            if (key == "name") currentLineName = value;
                            if (key == "divisor") currentLineDivisor = int.Parse(value);
                            if (key == "divisor_by_every" && currentLineDivisor == null) currentLineDivisor = (int)(_monthLength / parseTime(value).Ticks);
                            if (key == "width") currentLineWidth = float.Parse(value);
                            if (key == "color") currentLineColor = ColorTranslator.FromHtml(value);
                            if (key == "default_loading_time") currentLineDefaultLoadingTime = parseTime(value).Ticks;
                            if (key == "default_reversing_time") currentLineDefaultReversingTime = parseTime(value).Ticks;
                        }
                        else
                        {
                            long? shiftTime = null;
                            int? shiftNum = null;
                            long? waitingTime = null;
                            long? loadingTime = null;
                            bool reverse = false;
                            long? reversingTime = null;
                            long? tripTime = null;
                            long? tripTimeOffset = null;

                            var entry = parseEntryWithInlineOptions(l);
                            entry.Item1.ForEach(it =>
                            {
                                var key = it.Key;
                                var value = it.Value;
                                if (key == "shift") shiftTime = parseTime(value).Ticks;
                                if (key == "shift_num") shiftNum = int.Parse(value);
                                if (key == "wait") waitingTime = parseTime(value).Ticks;
                                if (key == "load") loadingTime = parseTime(value).Ticks;
                                if (key == "trip") tripTime = parseTime(value).Ticks;
                                if (key == "trip_offset") tripTimeOffset = parseOffsetTime(value).Ticks;
                                if (key == "reverse")
                                {
                                    reverse = true;
                                    if(value != null) reversingTime = parseTime(value).Ticks;
                                }
                            });

                            Station station = parseStation(entry.Item2);
                            currentLineStations.Add(new LineStationData(station, shiftTime, shiftNum, waitingTime, loadingTime, reverse, reversingTime, tripTime, tripTimeOffset));
                        }
                    }
                }
            }
            putLine();

            if (_monthLength == null) throw new AssertionFailedException(i);
            if (_shiftDivisor == null) throw new AssertionFailedException(i);
            var monthLength = _monthLength.Value;
            var shiftDivisor = _shiftDivisor.Value;

            return new Diagram(monthLength, shiftDivisor, defaultLoadingTime, defaultReversingTime, stations, times, lines);
        }
    }
}
