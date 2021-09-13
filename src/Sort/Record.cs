using System.Collections.Generic;
using System;

namespace Sort
{
    public class Record
    {
        public Record(string line)
        {
            var parts = line.Split('.');
            Num = parts[0];
            Str = parts[1];
        }

        public string Num;
        public string Str;

        public override string ToString() => $"{Num}.{Str}";
    }
}
