using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simutrans_diagram
{
    class Util
    {
        public static List<Station> expandStation(List<Station> list, Station from, Station to)
        {
            var fromIndex = list.IndexOf(from);
            var toIndex = list.IndexOf(to);
            if (fromIndex < toIndex) return list.GetRange(fromIndex, toIndex - fromIndex + 1);
            else if (fromIndex > toIndex) return list.GetRange(toIndex, fromIndex - toIndex + 1).Reverse<Station>().ToList();
            else throw new InvalidOperationException();
        }

        public static void drawLine(Graphics g, Pen pen, long fromTime, float fromY, long toTime, float toY, long monthLength, long horizontalScale)
        {
            if (fromTime > toTime) return;
            while (fromTime > monthLength)
            {
                fromTime -= monthLength;
                toTime -= monthLength;
            }
            if (toTime > monthLength)
            {
                var interY = fromY + (toY - fromY) * ((float)(monthLength - fromTime) / (toTime - fromTime));
                drawLine(g, pen, fromTime, fromY, monthLength, interY, monthLength, horizontalScale);
                drawLine(g, pen, 0L, interY, toTime - monthLength, toY, monthLength, horizontalScale);
            }
            else
            {
                g.DrawLine(pen, (float)fromTime / horizontalScale, fromY, (float)toTime / horizontalScale, toY);
            }
        }
    }
}
