using System;
using System.IO;
using System.Threading.Tasks;

namespace Generator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // TODO: check args

            var path = args[0];
            var requiredLength = long.Parse(args[1]);
            var dictionary = await File.ReadAllLinesAsync(args[2]);
            var rnd = new Random();

            Console.WriteLine("Generating:");

            using (var file = File.CreateText(path))
            {
                while (file.BaseStream.Length < requiredLength)
                {
                    var num = rnd.Next(1000);
                    var str = dictionary[rnd.Next(dictionary.Length)];

                    await file.WriteLineAsync($"{num:D}. {str}");

                    // Not sure if it's required, default stream buffer should be ok.
                    // Сomprehensive tests (memory/swap/performance) with really large files are required.
                    // await file.FlushAsync();

                    var progress = decimal.Divide(file.BaseStream.Length, requiredLength) * 100;
                    Console.Write($"\rProgress: {progress:N0}%");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Generated: {path}");
        }
    }
}
