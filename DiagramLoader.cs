using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Suitougreentea.SimuDia
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
        private int? currentLineDefaultTimeId;

        private List<Station> stations = new List<Station>();
        private List<TimeData> times = new List<TimeData>();
        private List<LineData> lines = new List<LineData>();

        public DiagramLoader(string path)
        {
            this.path = path;
            ResetCurrentLineData();
        }

        private int ParseInt(string input)
        {
            try { return int.Parse(input); }
            catch (FormatException) { throw new DiagramLoadingError(i, "Invalid number format: expected integer, found " + input); };
        }

        private float ParseFloat(string input)
        {
            try { return float.Parse(input); }
            catch (FormatException) { throw new DiagramLoadingError(i, "Invalid number format: expected float, found " + input); }
        }

        private Color ParseColor(string input)
        {
            try { return ColorTranslator.FromHtml(input); }
            catch (Exception) { throw new DiagramLoadingError(i, "Invalid color format: " + input); }
        }

        private TimeSpan ParseOffsetTime(string input)
        {
            var match = Regex.Match(input, @"([+-]?)(.*)");
            var time = ParseTime(match.Groups[2].Value.Trim());
            if (match.Groups[1].Value == "-") return time.Negate();
            return time;
        }

        // Syntax: hmmss
        // Output: h:mm:ss
        // where h is optional
        private TimeSpan ParseTime(string input)
        {
            var hms = Regex.Match(input, @"([0-9]*)([0-5][0-9])([0-5][0-9])");
            var hour = hms.Groups[1].Value.Length == 0 ? 0 : ParseInt(hms.Groups[1].Value);
            var minute = ParseInt(hms.Groups[2].Value);
            var second = ParseInt(hms.Groups[3].Value);
            return new TimeSpan(hour, minute, second);
        }
    
        private Station ParseStation(string input)
        {
            if (input.IndexOf('@') >= 0)
            {
                var split = Regex.Match(input, @"(.*)@\s*([0-9]*)");
                var name = split.Groups[1].Value.Trim();
                var id = ParseInt(split.Groups[2].Value);
                return new Station(name, id);
            }
            else return new Station(input, 0);
        }

        // Syntax: "{key} = {value}"
        // Output: key -> value
        // "= {value}" is optional
        private KeyValuePair<string, string> ParseOption(string input)
        {
            var split = Regex.Match(input, @"(.*)=(.*)");
            return new KeyValuePair<string, string>(split.Groups[1].Value.Trim(), split.Groups[2].Value.Trim());
        }

        // Syntax: "{option}, ..."
        // Output: [option, ...]
        // where option is parsed by parseOption()
        private List<KeyValuePair<string, string>> ParseInlineOptions(string input)
        {
            return input.Split(',').Select(it => ParseOption(it.Trim())).ToList();
        }

        // Syntax: "<{option}, ...> {station}"
        // Output: ([option, ...], station)
        // "<{option}, ...>" is optional
        private Tuple<List<KeyValuePair<string, string>>, string> ParseEntryWithInlineOptions(string input)
        {
            if (input.StartsWith("<"))
            {
                var split = Regex.Match(input, @"<(.*?)>(.*)");
                return Tuple.Create(ParseInlineOptions(split.Groups[1].Value.Trim()), split.Groups[2].Value.Trim());
            }
            return Tuple.Create(new List<KeyValuePair<string, string>>(), input);
        }

        private List<TimeData> ParseRawTimes(string input)
        {
            var entry = ParseEntryWithInlineOptions(input);
            var timeId = 0;
            entry.Item1.ForEach(it =>
            {
                if (it.Key == "time_id") timeId = ParseInt(it.Value);
            });

            var body = entry.Item2;
            var split = Regex.Match(body, @"(.*)->(.*):(.*)");
            var fromStation = ParseStation(split.Groups[1].Value.Trim());
            var toStation = ParseStation(split.Groups[2].Value.Trim());

            var fromStationIndex = stations.IndexOf(fromStation);
            var toStationIndex = stations.IndexOf(toStation);
            if (fromStationIndex == -1) throw new DiagramLoadingError(i, "Undefined station: " + fromStation);
            if (toStationIndex == -1) throw new DiagramLoadingError(i, "Undefined station: " + toStation);

            var rawTimes = split.Groups[3].Value.Trim();
            return rawTimes.Split(',').Select(it => {
                var trimmed = it.Trim();
                if (trimmed.IndexOf("-") >= 0)
                {
                    var multipleTimeSplit = Regex.Match(trimmed, @"(.*)-(.*)");
                    var fromTime = ParseTime(multipleTimeSplit.Groups[1].Value.Trim()).Ticks;
                    var toTime = ParseTime(multipleTimeSplit.Groups[2].Value.Trim()).Ticks;
                    var time = toTime - fromTime;
                    if (_monthLength == null) throw new AssertionFailedException(i);
                    while (time < 0) time += _monthLength.Value;
                    return time;
                }
                else return ParseTime(trimmed).Ticks;
            }).Select(it => new TimeData(fromStation, toStation, it, timeId)).ToList();
        }

        private void ResetCurrentLineData()
        {
            currentLineName = null;
            currentLineDivisor = null;
            currentLineWidth = 1f;
            currentLineColor = Color.Black;
            currentLineStations = new List<LineStationData>();
            currentLineDefaultLoadingTime = null;
            currentLineDefaultReversingTime = null;
            currentLineDefaultTimeId = null;
        }

        private void PutLine()
        {
            if (currentLineDivisor == null) throw new DiagramLoadingError(i, "Line cannot end without specifying divisor");
            lines.Add(new LineData(currentLineName,
                currentLineDivisor.Value,
                currentLineWidth,
                currentLineColor,
                currentLineDefaultLoadingTime,
                currentLineDefaultReversingTime,
                currentLineDefaultTimeId,
                currentLineStations));
            ResetCurrentLineData();
        }

        public Diagram Load()
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
                        var option = ParseOption(l);
                        var key = option.Key;
                        var value = option.Value;

                        if (key == "month_length") _monthLength = ParseTime(value).Ticks;
                        if (key == "shift_divisor") _shiftDivisor = ParseInt(value);
                        if (key == "default_loading_time") defaultLoadingTime = ParseTime(value).Ticks;
                        if (key == "default_reversing_time") defaultReversingTime = ParseTime(value).Ticks;
                    }
                    if (mode == "Stations")
                    {
                        stations.Add(ParseStation(l));
                    }
                    if (mode == "RawTimes")
                    {
                        times.AddRange(ParseRawTimes(l));
                    }
                    if (mode == "Lines")
                    {
                        if (l.StartsWith("-"))
                        {
                            PutLine();
                        }
                        else if (!l.StartsWith("<") && l.IndexOf("=") >= 0)
                        {
                            var option = ParseOption(l);
                            var key = option.Key;
                            var value = option.Value;

                            if (key == "name") currentLineName = value;
                            if (key == "divisor") currentLineDivisor = ParseInt(value);
                            if (key == "divisor_by_every" && currentLineDivisor == null) currentLineDivisor = (int)(_monthLength / ParseTime(value).Ticks);
                            if (key == "width") currentLineWidth = ParseFloat(value);
                            if (key == "color") currentLineColor = ParseColor(value);
                            if (key == "default_loading_time") currentLineDefaultLoadingTime = ParseTime(value).Ticks;
                            if (key == "default_reversing_time") currentLineDefaultReversingTime = ParseTime(value).Ticks;
                            if (key == "default_time_id") currentLineDefaultTimeId = ParseInt(value);
                        }
                        else
                        {
                            long? shiftTime = null;
                            int? shiftNum = null;
                            long? waitingTime = null;
                            long? loadingTime = null;
                            bool reverse = false;
                            long? reversingTime = null;
                            int? timeId = null;
                            long? tripTime = null;
                            long? tripTimeOffset = null;

                            var entry = ParseEntryWithInlineOptions(l);
                            entry.Item1.ForEach(it =>
                            {
                                var key = it.Key;
                                var value = it.Value;
                                if (key == "shift") shiftTime = ParseTime(value).Ticks;
                                if (key == "shift_num") shiftNum = ParseInt(value);
                                if (key == "wait") waitingTime = ParseTime(value).Ticks;
                                if (key == "load") loadingTime = ParseTime(value).Ticks;
                                if (key == "time_id") timeId = ParseInt(value);
                                if (key == "trip") tripTime = ParseTime(value).Ticks;
                                if (key == "trip_offset") tripTimeOffset = ParseOffsetTime(value).Ticks;
                                if (key == "reverse")
                                {
                                    reverse = true;
                                    if(value != null) reversingTime = ParseTime(value).Ticks;
                                }
                            });

                            Station station = ParseStation(entry.Item2);
                            var stationIndex = stations.IndexOf(station);
                            if (stationIndex == -1) throw new DiagramLoadingError(i, "Undefined station: " + station);
                            currentLineStations.Add(new LineStationData(station, shiftTime, shiftNum, waitingTime, loadingTime, reverse, reversingTime, timeId, tripTime, tripTimeOffset));
                        }
                    }
                }
            }
            PutLine();

            if (_monthLength == null) throw new AssertionFailedException(i);
            if (_shiftDivisor == null) throw new AssertionFailedException(i);
            var monthLength = _monthLength.Value;
            var shiftDivisor = _shiftDivisor.Value;

            return new Diagram(monthLength, shiftDivisor, defaultLoadingTime, defaultReversingTime, stations, times, lines);
        }
    }
}
