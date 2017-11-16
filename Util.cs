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
            if (toTime > monthLength)
            {
                var interY = fromY + (toY - fromY) * ((float)(monthLength - fromTime) / (toTime - fromTime));
                g.DrawLine(pen, (float)fromTime / horizontalScale, fromY, (float)monthLength / horizontalScale, interY);
                g.DrawLine(pen, 0f, interY, (float)(toTime % monthLength) / horizontalScale, toY);
            }
            else
            {
                g.DrawLine(pen, (float)fromTime / horizontalScale, fromY, (float)toTime / horizontalScale, toY);
            }
        }
    }
}
