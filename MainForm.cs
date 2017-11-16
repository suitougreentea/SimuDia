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
        bool ignoreCheckEvent = false;
        int horizontalZoom = 0;
        int verticalZoom = 0;

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
            var lineTimes = diagram.lineTimes;

            var stationY = new List<float>(diagram.accumulatedTicks.Count);
            stationY.Add(0L);
            var baseIndex = 0;
            var baseY = 0f;
            for (var i = 1; i < diagram.accumulatedTicks.Count; i++)
            {
                var it = diagram.accumulatedTicks[i];
                if (it == 0L)
                {
                    baseIndex = i;
                    baseY = stationY[i - 1] + 20f;
                    stationY.Add(baseY);
                }
                else stationY.Add(baseY + (float)(it / 100000000.0 * Math.Pow(2.0, verticalZoom / 4.0)));
            }

            var graphicsForMeasureString = Graphics.FromImage(new Bitmap(1, 1));
            var font = SystemFonts.DefaultFont;
            var stationNameWidth = (int)stations.Select(it => graphicsForMeasureString.MeasureString(it.name, font).Width).Max() + 20;

            var leftMargin = 20;
            var rightMargin = 20;
            var plotWidth = (int)(1200 * Math.Pow(2.0, horizontalZoom / 2.0));
            var horizontalScale = monthLength / plotWidth;

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
            StringFormat bottomAlignFormat = new StringFormat();
            bottomAlignFormat.LineAlignment = StringAlignment.Far;

            long mainGridAt = new TimeSpan(1, 0, 0).Ticks;
            long subGridAt = new TimeSpan(0, 15, 0).Ticks;
            long subsubGridAt = new TimeSpan(0, 3, 0).Ticks;

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
            for (var i = 0; i < lines.Count; i++)
            {
                if (!ListLine.GetItemChecked(i + 1)) continue;
                var it = lines[i];
                var list = lineTimes[i].list;
                var pen = new Pen(it.color, it.width);
                var penDashed = new Pen(it.color, it.width);
                penDashed.DashPattern = new float[] { 3f, 3f };
                for(var n = 0; n < it.divisor; n++)
                {
                    var divOffset = monthLength / it.divisor * n;
                    for (var j = 0; j < list.Count; j++)
                    {
                        var fromTimeData = list[j];
                        var toTimeData = list[(j + 1) % list.Count];
                        var fromStationData = it.stations[j];
                        var toStationData = it.stations[(j + 1) % list.Count];
                        var fromStationIndex = stations.IndexOf(fromStationData.station);
                        var toStationIndex = stations.IndexOf(toStationData.station);
                        var fromY = stationY[fromStationIndex];
                        var toY = stationY[toStationIndex];
                        var currentPen = (fromStationData.station.name == toStationData.station.name) ? penDashed : pen;
                        Util.drawLine(g, currentPen, fromTimeData.arrival + divOffset, fromY, fromTimeData.departure + divOffset, fromY, monthLength, horizontalScale);
                        Util.drawLine(g, currentPen, fromTimeData.departure + divOffset, fromY, fromTimeData.departure + fromTimeData.tripTime + divOffset, toY, monthLength, horizontalScale);
                    }
                }
            }

            g.Dispose();
            MainPicture.Image = bitmap;
            MainPicture.Size = new Size(width, height);
        }

        private void setupComponents()
        {
            ignoreCheckEvent = true;
            string previousSelectedLineName = null;
            List<string> previousCheckedLineName = new List<string>();
            List<string> previousUncheckedLineName = new List<string>();
            if (ListLine.SelectedIndex > 0) previousSelectedLineName = ListLine.Items[ListLine.SelectedIndex].ToString();
            for (var i = 1; i < ListLine.Items.Count; i++)
            {
                if (ListLine.GetItemChecked(i)) previousCheckedLineName.Add(ListLine.Items[i].ToString());
                else previousUncheckedLineName.Add(ListLine.Items[i].ToString());
            }
            
            ListLine.Items.Clear();
            ListLine.Items.Add("(General / All Lines)");
            for (var i = 0; i < diagram.lines.Count; i++)
            {
                var l = diagram.lines[i];
                ListLine.Items.Add(l.name);
                var previousChecked = previousCheckedLineName.IndexOf(l.name) >= 0;
                var previousUnchecked = previousUncheckedLineName.IndexOf(l.name) >= 0;
                ListLine.SetItemCheckState(i + 1, (previousChecked || !previousUnchecked) ? CheckState.Checked : CheckState.Unchecked);
            }
            var selectedIndex = diagram.lines.FindIndex(it => it.name == previousSelectedLineName);
            if (selectedIndex >= 0) ListLine.SetSelected(selectedIndex + 1, true);
            else ListLine.SetSelected(0, true);

            ignoreCheckEvent = false;
            updateGlobalCheck();
        }

        private void updateGlobalCheck()
        {
            var allChecked = true;
            var allUnchecked = true;
            for (var i = 1; i < ListLine.Items.Count; i++)
            {
                var state = ListLine.GetItemChecked(i);
                allChecked &= state;
                allUnchecked &= !state;
            }
            ignoreCheckEvent = true;
            if (allChecked) ListLine.SetItemCheckState(0, CheckState.Checked);
            else if (allUnchecked) ListLine.SetItemCheckState(0, CheckState.Unchecked);
            else ListLine.SetItemCheckState(0, CheckState.Indeterminate);
            ignoreCheckEvent = false;
        }

        private void onLoad(object sender, EventArgs e)
        {
            var args = Environment.GetCommandLineArgs();
            var path = Path.GetFullPath(args[1]);

            diagram = DiagramLoader.load(path);
            setupComponents();
            redraw();

            var watcher = new FileSystemWatcher(Path.GetDirectoryName(path));
            watcher.Filter = Path.GetFileName(path);
            watcher.SynchronizingObject = this;
            watcher.IncludeSubdirectories = false;
            watcher.Changed += (_, __) =>
            {
                System.Threading.Thread.Sleep(100);
                diagram = DiagramLoader.load(path);
                setupComponents();
                redraw();
            };
            watcher.EnableRaisingEvents = true;
        }

        private void ListLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ListLine.SelectedIndex == -1) return;
            TextInfo.Clear();
            if (ListLine.SelectedIndex == 0)
            {

            }
            else
            {
                var index = ListLine.SelectedIndex - 1;
                var l = diagram.lines[index];
                var t = diagram.lineTimes[index];
                TextInfo.SelectedText = $"Name: {l.name}\n";
                TextInfo.SelectedText = $"Estimated trip time: {new TimeSpan(t.tripTime)}\n\n";
                for (var i = 0; i < t.list.Count; i++)
                {
                    var le = l.stations[i];
                    var te = t.list[i];
                    if (i != 0) TextInfo.SelectedText = $"{new TimeSpan(te.arrival)}-";
                    TextInfo.SelectedText = $"{new TimeSpan(te.departure)} {le.station.name}";
                    if (te.shiftNum != null) TextInfo.SelectedText = $" [shift: {te.shiftNum}]";
                    TextInfo.SelectedText = $"\n";
                    TextInfo.SelectedText = $"  | {new TimeSpan(te.tripTime)}\n";
                }
                TextInfo.SelectedText = $"{new TimeSpan(t.list.Last().departure + t.list.Last().tripTime)} {l.stations[0].station.name}";
            }
        }

        private void ListLine_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (ignoreCheckEvent) return;

            BeginInvoke(new Action(() =>
            {
                if (e.Index == 0)
                {
                    ignoreCheckEvent = true;
                    for (var i = 1; i < ListLine.Items.Count; i++)
                    {
                        ListLine.SetItemCheckState(i, e.NewValue);
                    }
                    ignoreCheckEvent = false;
                }
                else
                {
                    updateGlobalCheck();
                }
                redraw();
            }));
        }

        private void ButtonZoomInH_Click(object sender, EventArgs e)
        {
            horizontalZoom++;
            redraw();
        }

        private void ButtonZoomOutH_Click(object sender, EventArgs e)
        {
            horizontalZoom--;
            redraw();
        }

        private void ButtonZoomInV_Click(object sender, EventArgs e)
        {
            verticalZoom++;
            redraw();
        }

        private void ButtonZoomOutV_Click(object sender, EventArgs e)
        {
            verticalZoom--;
            redraw();
        }
    }
}
