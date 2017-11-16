using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;

namespace simutrans_diagram
{
    public partial class MainForm : Form
    {
        Diagram diagram;

        public MainForm()
        {
            InitializeComponent();
        }

        private void redraw()
        {
            var monthLength = diagram.monthLength;
            var shiftDivisor = diagram.shiftDivisor;
            var stations = diagram.stations;
            var times = diagram.times;
            var lines = diagram.lines;
            var expandedLines = diagram.expandedLines;

            var stationY = new float[stations.Count];
            stationY[0] = 0.0f;
            for (int i = 1; i < stations.Count; i++)
            {
                var upStation = stations[i - 1];
                var downStation = stations[i];
                var forIndex = times.FindIndex(it => it.fromStation == upStation && it.toStation == downStation);
                var revIndex = times.FindIndex(it => it.fromStation == downStation && it.toStation == upStation);
                var ticksList = new List<Double>(2);
                if (forIndex >= 0) ticksList.Add(times[forIndex].times.Average());
                if (revIndex >= 0) ticksList.Add(times[revIndex].times.Average());
                var meanTicks = ticksList.Average();
                stationY[i] = stationY[i - 1] + (float)meanTicks / 100000000.0f;
            }

            var leftMargin = 20;
            var rightMargin = 20;
            var stationNameWidth = 100;
            var plotWidth = 1600;
            var horizontalScale = monthLength / plotWidth;
            var mainGridAt = new TimeSpan(1, 0, 0).Ticks;
            var subGridAt = new TimeSpan(0, 15, 0).Ticks;
            var subsubGridAt = new TimeSpan(0, 3, 0).Ticks;

            var topMargin = 20;
            var bottomMargin = 20;
            var stationsHeight = (int)stationY[stations.Count - 1];

            var width = leftMargin + stationNameWidth + plotWidth + rightMargin;
            var height = topMargin + stationsHeight + bottomMargin;

            Bitmap bitmap = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            var brush = new SolidBrush(Color.Black);
            var font = SystemFonts.DefaultFont;
            StringFormat bottomAlignFormat = new StringFormat();
            bottomAlignFormat.LineAlignment = StringAlignment.Far;

            g.TranslateTransform(leftMargin + stationNameWidth, topMargin);
            for (var t = 0L; t < monthLength; t += subsubGridAt)
            {
                var dx = (float)t / horizontalScale;
                g.DrawLine(Pens.LightGray, dx, 0, dx, stationsHeight);
            }
            for (var t = 0L; t < monthLength; t += subGridAt)
            {
                var dx = (float)t / horizontalScale;
                g.DrawLine(Pens.DarkGray, dx, 0, dx, stationsHeight);
            }
            for (var t = 0L; t < monthLength; t += mainGridAt)
            {
                var dx = (float)t / horizontalScale;
                g.DrawLine(Pens.Black, dx, 0, dx, stationsHeight);
                g.DrawString(new TimeSpan(t).ToString(), font, brush, dx, 0, bottomAlignFormat);
            }

            g.TranslateTransform(-stationNameWidth, 0);
            for (var i = 0; i < stations.Count; i++)
            {
                var name = stations[i].name;
                var sy = stationY[i];
                g.DrawLine(Pens.Black, 0, sy, stationNameWidth + plotWidth, sy);
                g.DrawString(name, font, brush, 0, sy, bottomAlignFormat);
            }


            g.TranslateTransform(stationNameWidth, 0);
            foreach (var it in expandedLines)
            {
                var pen = new Pen(it.color, it.width);
                for(var n = 0; n < it.divisor; n++)
                {
                    var baseTime = monthLength / it.divisor * n;
                    var time = baseTime;
                    if (it.stations[0].shift != null) time = (baseTime + (monthLength / shiftDivisor) * it.stations[0].shift.Value) % monthLength;
                    for(var i = 1; i < it.stations.Count; i++)
                    {
                        var from = it.stations[i - 1];
                        var to = it.stations[i];
                        var fromStationIndex = stations.IndexOf(from.station);
                        var toStationIndex = stations.IndexOf(to.station);
                        var fromY = stationY[fromStationIndex];
                        var toY = stationY[toStationIndex];
                        var timesIndex = times.FindIndex(ce => ce.fromStation == from.station && ce.toStation == to.station);
                        if (from.shift != null) time = (baseTime + (monthLength / shiftDivisor) * from.shift.Value) % monthLength;
                        else if (from.wait != null) time = (time + (monthLength / shiftDivisor) * from.wait.Value) % monthLength;
                        else if (from.reverse) time = (time + new TimeSpan(0, 0, 60).Ticks) % monthLength;
                        else time = (time + new TimeSpan(0, 0, 30).Ticks) % monthLength;

                        if (timesIndex >= 0)
                        {
                            var entry = times[timesIndex];
                            var newTime = time + (long)entry.times.Average();
                            if (to.shorten != null) newTime -= to.shorten.Value;
                            Util.drawLine(g, pen, time, fromY, newTime, toY, monthLength, horizontalScale);
                            time = newTime % monthLength;
                        }
                        else
                        {
                            var expanded = Util.expandStation(stations, from.station, to.station);
                            var accum = 0L;
                            var success = true;

                            for (var j = 1; j < expanded.Count; j++)
                            {
                                var expandedFrom = expanded[j - 1];
                                var expandedTo = expanded[j];
                                var expandedTimesIndex = times.FindIndex(ce => ce.fromStation == expandedFrom && ce.toStation == expandedTo);
                                if (expandedTimesIndex >= 0)
                                {
                                    accum += (long)times[expandedTimesIndex].times.Average();
                                }
                                else
                                {
                                    success = false;
                                    break;
                                }
                            }
                            if (success)
                            {
                                if (to.shorten != null) accum -= to.shorten.Value;
                                var newTime = time + accum;
                                Util.drawLine(g, pen, time, fromY, newTime, toY, monthLength, horizontalScale);
                                time = newTime % monthLength;
                            }
                        }
                    }
                }
            }

            g.Dispose();
            MainPicture.Image = bitmap;
            MainPicture.Size = new Size(width, height);
        }

        private void onLoad(object sender, EventArgs e)
        {
            var args = Environment.GetCommandLineArgs();
            var path = Path.GetFullPath(args[1]);

            diagram = DiagramLoader.load(path);
            redraw();

            var watcher = new FileSystemWatcher(Path.GetDirectoryName(path));
            watcher.Filter = Path.GetFileName(path);
            watcher.SynchronizingObject = this;
            watcher.IncludeSubdirectories = false;
            watcher.Changed += (_, __) =>
            {
                System.Threading.Thread.Sleep(100);
                diagram = DiagramLoader.load(path);
                redraw();
            };
            watcher.EnableRaisingEvents = true;
        }
    }
}
