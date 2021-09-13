using System;
using System.Collections.Generic;

namespace Sort
{
    public class RecordComparer : IComparer<Record>
    {
        public int Compare(Record x, Record y)
        {
            if (x == null && y == null)
                return 0;

            if (x != null && y == null)
                return -1;

            if (x == null && y != null)
                return 1;

            var result = StringComparer.Ordinal.Compare(x.Str, y.Str);
            if (result == 0)
            {
                result = int.Parse(x.Num).CompareTo(int.Parse(y.Num));
            }

            return result;
        }
    }
}
