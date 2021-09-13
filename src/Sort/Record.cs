using System;

namespace Sort
{
    public class Record : IComparable<Record>
    {
        public Record()
        {
        }

        public Record(int num, string str)
        {
            Num = num;
            Str = str;
        }

        public int Num { get; }
        public string Str { get; }

        public int CompareTo(Record other)
        {
            var strComparison = Str.CompareTo(other.Str);

            return strComparison == 0
                ? Num.CompareTo(other.Num)
                : strComparison;
        }
    }
}
