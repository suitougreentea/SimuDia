using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Suitougreentea.SimuDia
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
    }
}
