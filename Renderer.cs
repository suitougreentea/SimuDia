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

        private float GetLineY(float x, float fromX, float fromY, float toX, float toY)
        {
            return (x - fromX) / (toX - fromX) * (toY - fromY) + fromY;
        }

        private List<Tuple<PointF, PointF>> NormalizeLines(float fromX, float fromY, float toX, float toY, float width)
        {
            var result = new List<Tuple<PointF, PointF>>();
            while (toX < 0f)
            {
                fromX += width;
                toX += width;
            }
            while (fromX > width)
            {
                fromX -= width;
                toX -= width;
            }
            if (fromX < toX)
            {
                var croppedFromX = Math.Max(0f, fromX);
                var croppedFromY = GetLineY(croppedFromX, fromX, fromY, toX, toY);
                var croppedToX = Math.Min(toX, width);
                var croppedToY = GetLineY(croppedToX, fromX, fromY, toX, toY);
                if (fromX < 0f) result.AddRange(NormalizeLines(fromX + width, fromY, width, croppedFromY, width));
                result.Add(Tuple.Create(new PointF(croppedFromX, croppedFromY), new PointF(croppedToX, croppedToY)));
                if (toX > width) result.AddRange(NormalizeLines(0f, croppedToY, toX - width, toY, width));
            }
            return result;
        }

        private void drawLine(Pen pen, long fromTime, float fromY, long toTime, float toY)
        {
            if (fromTime == toTime)
            {
                var x = timeToX(fromTime);
                while (x < 0f) x += plotWidth;
                while (x > plotWidth) x -= plotWidth;
                g.DrawLine(pen, x, fromY, x, toY);
            }
            else
            {
                NormalizeLines(timeToX(fromTime), fromY, timeToX(toTime), toY, plotWidth).ForEach(it =>
                {
                    g.DrawLine(pen, it.Item1, it.Item2);
                });
            }
        }

        public void drawProtrudingLine(Pen pen, bool upsideDownDirection, int stationIndex, long arrival, long departure)
        {
            var normalized = NormalizeLines(timeToX(arrival), 0f, timeToX(departure), 0f, plotWidth);
            var y = stationY[stationIndex];
            float intervalY;
            ProtrudingDirection protrudingDirection;
            if (upsideDownDirection)
            {
                protrudingDirection = ProtrudingDirection.DOWN;
                if (stationIndex == diagram.stations.Count - 1) intervalY = bottomMargin;
                else intervalY = stationY[stationIndex + 1] - stationY[stationIndex];
            }
            else
            {
                protrudingDirection = ProtrudingDirection.UP;
                if (stationIndex == 0) intervalY = topMargin;
                else intervalY = stationY[stationIndex] - stationY[stationIndex - 1];
            }

            var protrudingLevel = normalized.Select(it =>
            {
                var start = it.Item1.X;
                var end = it.Item2.X;
                var i = 0;
                while (true)
                {
                    if (!protruding[stationIndex].Where(ce => ce.level == i).Any(ce => (ce.start <= start && start <= ce.end) || (ce.start <= end && end <= ce.end))) return i;
                    i++;
                }
            }).Max();

            var protrudingOffset = Math.Min(5f * (protrudingLevel + 1), intervalY / 3f * 2f) * (protrudingDirection == ProtrudingDirection.DOWN ? 1 : -1);
            drawLine(pen, arrival, y, arrival, y + protrudingOffset);
            drawLine(pen, arrival, y + protrudingOffset, departure, y + protrudingOffset);
            drawLine(pen, departure, y + protrudingOffset, departure, y);
            protruding[stationIndex].AddRange(normalized.Select(it => new ProtrudingArea(it.Item1.X, it.Item2.X, protrudingLevel, protrudingDirection)));
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
                var reverseDiv = lineTimes[i].wholeTripTime + lineTimes[i].list[0].essentialStoppingTime / (monthLength / it.divisor);

                var pen = new Pen(it.color, it.width);
                var penDashed = new Pen(it.color, it.width);
                penDashed.DashPattern = new float[] { 3f, 3f };

                for(var n = 0; n < it.divisor; n++)
                {
                    var divOffset = monthLength / it.divisor * n;
                    var lastStationIndex = stations.IndexOf(it.stations.Last().station);
                    var firstStationIndex = stations.IndexOf(it.stations.First().station);
                    var upsideDownDirection = lastStationIndex > firstStationIndex;
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
                        if (reverseDirection) drawProtrudingLine(currentPen, upsideDownDirection, fromStationIndex, fromArrival, fromDeparture);
                        else drawLine(currentPen, fromTimeData.arrival + divOffset, fromY, fromTimeData.departure + divOffset, fromY);
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
