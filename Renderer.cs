using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suitougreentea.SimuDia
{
    class Renderer
    {
        public List<bool> lineVisibility = new List<bool>();

        private Diagram diagram;
        private Graphics g;

        private Font font = SystemFonts.DefaultFont;
        private const float basePlotWidth = 1200f;
        private int leftMargin = 20;
        private int rightMargin = 20;
        private int topMargin = 20;
        private int bottomMargin = 20;
        private int stationsHeight;
        private int horizontalZoom = 0;
        private int verticalZoom = 0;

        private int width;
        private int height;
        private int plotWidth;
        private int stationNameWidth;
        private float horizontalScale;
        private long mainGridAt;
        private long subGridAt;
        private long subsubGridAt;
        private List<float> stationY;
        List<List<ProtrudingArea>> protruding;

        public Renderer(Diagram diagram)
        {
            this.diagram = diagram;
            stationY = new List<float>(diagram.stations.Count);
            protruding = new List<List<ProtrudingArea>>(diagram.stations.Count);
            for (var i = 0; i < diagram.stations.Count; i++) protruding.Add(new List<ProtrudingArea>());

            updateHorizontalScale();
            updateVerticalScale();

            var graphicsForMeasureString = Graphics.FromImage(new Bitmap(1, 1));
            stationNameWidth = (int)diagram.stations.Select(it => graphicsForMeasureString.MeasureString(it.name, font).Width).Max() + 20;
        }

        private float timeToX(long time)
        {
            return time / horizontalScale;
        }

        private void drawGrid(Pen pen, long every)
        {
            for (var t = 0L; t < diagram.monthLength; t += every)
            {
                var dx = timeToX(t);
                g.DrawLine(pen, dx, 0, dx, stationsHeight);
            }
        }

        private void drawAxis(Font font, Brush brush, long every, StringFormat format)
        {
            for (var t = 0L; t < diagram.monthLength; t += every)
            {
                var dx = timeToX(t);
                g.DrawString(new TimeSpan(t).ToString(), font, brush, dx, 0, format);
            }
        }

        private void drawLine(Pen pen, long fromTime, float fromY, long toTime, float toY)
        {
            var monthLength = diagram.monthLength;
            if (fromTime > toTime) return;
            while (fromTime > monthLength)
            {
                fromTime -= monthLength;
                toTime -= monthLength;
            }
            if (toTime > monthLength)
            {
                var interY = fromY + (toY - fromY) * ((float)(monthLength - fromTime) / (toTime - fromTime));
                drawLine(pen, fromTime, fromY, monthLength, interY);
                drawLine(pen, 0L, interY, toTime - monthLength, toY);
            }
            else
            {
                g.DrawLine(pen, timeToX(fromTime), fromY, timeToX(toTime), toY);
            }
        }

        private void addProtrudingArea(int stationIndex, long fromTime, long toTime, int level, ProtrudingDirection direction)
        {
            var monthLength = diagram.monthLength;
            if (toTime - fromTime >= monthLength) toTime = fromTime + monthLength;
            while (fromTime > monthLength)
            {
                fromTime -= monthLength;
                toTime -= monthLength;
            }
            if (toTime > monthLength)
            {
                protruding[stationIndex].Add(new ProtrudingArea(timeToX(fromTime), timeToX(monthLength), level, direction));
                protruding[stationIndex].Add(new ProtrudingArea(timeToX(0L), timeToX(toTime - monthLength), level, direction));
            }
            else
            {
                protruding[stationIndex].Add(new ProtrudingArea(timeToX(fromTime), timeToX(toTime), level, direction));
            }
        }

        private int computeProtrudingLevel(int stationIndex, long fromTime, long toTime, ProtrudingDirection direction)
        {
            var monthLength = diagram.monthLength;
            if (toTime - fromTime >= monthLength) toTime = fromTime + monthLength;
            while (fromTime > monthLength)
            {
                fromTime -= monthLength;
                toTime -= monthLength;
            }
            if (toTime > monthLength)
            {
                return Math.Max(computeProtrudingLevel(stationIndex, fromTime, monthLength, direction),
                    computeProtrudingLevel(stationIndex, 0, toTime - monthLength, direction));
            }
            else
            {
                var start = timeToX(fromTime);
                var end = timeToX(toTime);
                var i = 0;
                while (true) {
                    if (!protruding[stationIndex].Where(it => it.level == i).Any(it => (it.start <= start && start <= it.end) || (it.start <= end && end <= it.end))) return i;
                    i++;
                }
            }
        }

        public Bitmap render()
        {
            foreach (var it in protruding) it.Clear();

            var monthLength = diagram.monthLength;
            var shiftDivisor = diagram.shiftDivisor;
            var stations = diagram.stations;
            var times = diagram.times;
            var lines = diagram.lines;
            var lineTimes = diagram.lineTimes;

            Bitmap bitmap = new Bitmap(width, height);
            g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            var brush = new SolidBrush(Color.Black);
            StringFormat bottomAlignFormat = new StringFormat();
            bottomAlignFormat.LineAlignment = StringAlignment.Far;

            g.TranslateTransform(leftMargin + stationNameWidth, topMargin);

            drawGrid(Pens.LightGray, subsubGridAt);
            drawGrid(Pens.DarkGray, subGridAt);
            drawGrid(Pens.Black, mainGridAt);
            drawAxis(font, brush, mainGridAt, bottomAlignFormat);

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
                if (!lineVisibility[i]) continue;
                var it = lines[i];
                var list = lineTimes[i].list;
                var pen = new Pen(it.color, it.width);
                var penDashed = new Pen(it.color, it.width);
                penDashed.DashPattern = new float[] { 3f, 3f };
                for(var n = 0; n < it.divisor; n++)
                {
                    var divOffset = monthLength / it.divisor * n;
                    var upsideDownDirection = false;
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
                        var fromArrival = fromTimeData.arrival + divOffset;
                        var fromDeparture = fromTimeData.departure + divOffset;
                        var toArrival = toTimeData.arrival + divOffset;
                        var currentPen = (fromStationData.station.name == toStationData.station.name) ? penDashed : pen;
                        var newUpsideDownDirection = fromStationIndex > toStationIndex;
                        var reverseDirection = upsideDownDirection != newUpsideDownDirection;
                        upsideDownDirection = newUpsideDownDirection;
                        if (j != 0)
                        {
                            if (reverseDirection)
                            {
                                float intervalY;
                                ProtrudingDirection protrudingDirection;
                                if (upsideDownDirection)
                                {
                                    protrudingDirection = ProtrudingDirection.DOWN;
                                    if (fromStationIndex == stations.Count - 1) intervalY = bottomMargin;
                                    else intervalY = stationY[fromStationIndex + 1] - stationY[fromStationIndex];
                                }
                                else
                                {
                                    protrudingDirection = ProtrudingDirection.UP;
                                    if (fromStationIndex == stations.Count - 1) intervalY = topMargin;
                                    else intervalY = stationY[fromStationIndex] - stationY[fromStationIndex - 1];
                                }
                                var protrudingLevel = computeProtrudingLevel(fromStationIndex, fromArrival, fromDeparture, protrudingDirection);
                                var protrudingOffset = Math.Min(5f * (protrudingLevel + 1), intervalY / 3f * 2f) * (protrudingDirection == ProtrudingDirection.DOWN ? 1 : -1);
                                drawLine(currentPen, fromArrival, fromY, fromArrival, fromY + protrudingOffset);
                                drawLine(currentPen, fromArrival, fromY + protrudingOffset, fromDeparture, fromY + protrudingOffset);
                                drawLine(currentPen, fromDeparture, fromY + protrudingOffset, fromDeparture, fromY);
                                addProtrudingArea(fromStationIndex, fromArrival, fromDeparture, protrudingLevel, protrudingDirection);
                            }
                            else drawLine(currentPen, fromTimeData.arrival + divOffset, fromY, fromTimeData.departure + divOffset, fromY);
                        }
                        drawLine(currentPen, fromTimeData.departure + divOffset, fromY, fromTimeData.departure + fromTimeData.tripTime + divOffset, toY);
                    }
                }
            }

            g.Dispose();
            return bitmap;
        }

        public void updateHorizontalScale()
        {
            plotWidth = (int)(basePlotWidth * Math.Pow(2.0, horizontalZoom / 2.0));
            horizontalScale = diagram.monthLength / plotWidth;
            width = leftMargin + stationNameWidth + plotWidth + rightMargin;
            mainGridAt = new TimeSpan(1, 0, 0).Ticks;
            subGridAt = new TimeSpan(0, 15, 0).Ticks;
            subsubGridAt = new TimeSpan(0, 3, 0).Ticks;
        }

        public void updateVerticalScale() {
            stationY.Clear();
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
            stationsHeight = (int)stationY[diagram.stations.Count - 1];
            height = topMargin + stationsHeight + bottomMargin;
        }

        public void zoomInHorizontal()
        {
            horizontalZoom++;
            updateHorizontalScale();
        }

        public void zoomOutHorizontal()
        {
            horizontalZoom--;
            updateHorizontalScale();
        }

        public void zoomInVertical()
        {
            verticalZoom++;
            updateVerticalScale();
        }

        public void zoomOutVertical()
        {
            verticalZoom--;
            updateVerticalScale();
        }
    }
}
