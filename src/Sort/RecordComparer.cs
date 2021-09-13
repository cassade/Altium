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

            var result = x.Str.Span.SequenceCompareTo(y.Str.Span);
            if (result == 0)
            {
                result = int.Parse(x.Num.Span).CompareTo(int.Parse(y.Num.Span));
            }

            return result;
        }
    }
}
