using System;
using System.IO;
using System.Text;

namespace Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: check args

            var path = args[0];
            var requiredLength = long.Parse(args[1]);
            var dictionary = File.ReadAllLines(args[2]);
            var rnd = new Random();

            Console.WriteLine("Generating ...");

            using (var file = new StreamWriter(path, false, Encoding.UTF8, 65536))
            {
                while (file.BaseStream.Length < requiredLength)
                {
                    var num = rnd.Next(1000);
                    var str = dictionary[rnd.Next(dictionary.Length)];

                    file.WriteLine($"{num}. {str}");
                }
            }

            Console.WriteLine($"Generated: {path}");
        }
    }
}
