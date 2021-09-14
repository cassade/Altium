using System;

namespace Sort
{
    public class Record
    {
        public Record(Span<char> num, Span<char> str)
        {
            Num = num.ToString();
            Str = str.ToString();
        }

        public int? NumValue { get; set; }
        public string Num { get; }
        public string Str { get; }
    }
}
