using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace Sort
{
    class Program
    {
        static RecordComparer recordComparer = new();
        static List<Record> buffer = new List<Record>(1_000_000);
        static Queue<string> bufferFileQueue = new();
        static long bufferSize;
        static string path;
        static string outputPath;

        static void Main(string[] args)
        {
            // TODO: check args

            path = args[0];
            bufferSize = long.Parse(args[1]);
            outputPath = args[2];

            Console.WriteLine("Sorting stage 1 (split) ...");

            // Stage 1 - split file into small sorted parts
            // Read file line by line, when buffer is filled sort it and write to the disk.
            // Then fill, sort and write next portion.

            using (var file = File.OpenText(path))
            {
                while (file.BaseStream.Position < file.BaseStream.Length)
                {
                    buffer.Add(ReadLine(file));

                    if (file.BaseStream.Position > bufferSize * (bufferFileQueue.Count + 1))
                    {
                        FlushBuffer();
                    }
                }
            }

            // flush the rest of data
            if (buffer.Any())
            {
                FlushBuffer();
            }

            // Stage 2 - incrementally (pair by pair) merge parts into single sorted file

            Console.WriteLine("Sorting stage 2 (merge) ...");

            var bufferFileCount = bufferFileQueue.Count;

            while (bufferFileQueue.Count > 1)
            {
                var pathToMerge1 = bufferFileQueue.Dequeue();
                var pathToMerge2 = bufferFileQueue.Dequeue();
                var pathMerged = Path.Combine(outputPath, Path.GetRandomFileName());

                using (var fileToMerge1 = File.OpenText(pathToMerge1))
                using (var fileToMerge2 = File.OpenText(pathToMerge2))
                using (var fileMerged = new StreamWriter(pathMerged, false, Encoding.UTF8, 65536))
                {
                    var record1 = ReadLine(fileToMerge1);
                    var record2 = ReadLine(fileToMerge2);

                    while (record1 != null || record2 != null)
                    {
                        var comparisonResult = recordComparer.Compare(record1, record2);
                        if (comparisonResult < 0)
                        {
                            WriteLine(record1, fileMerged);
                            record1 = ReadLine(fileToMerge1);
                        }
                        else if (comparisonResult == 0)
                        {
                            WriteLine(record1, fileMerged);
                            WriteLine(record2, fileMerged);
                            record1 = ReadLine(fileToMerge1);
                            record2 = ReadLine(fileToMerge2);
                        }
                        else
                        {
                            WriteLine(record2, fileMerged);
                            record2 = ReadLine(fileToMerge2);
                        }
                    }
                }

                // delete processed buffer files
                File.Delete(pathToMerge1);
                File.Delete(pathToMerge2);

                // add new buffer file to queue to merge it with other parts
                bufferFileQueue.Enqueue(pathMerged);
            }

            Console.WriteLine($"Sorted: {bufferFileQueue.Dequeue()}");
        }

        static Record ReadLine(StreamReader reader) =>
            reader.BaseStream.Position < reader.BaseStream.Length
                ? new Record(reader.ReadLine())
                : null;

        static void WriteLine(Record record, StreamWriter writer) => writer.WriteLine(record.ToString());

        static void FlushBuffer()
        {
            buffer.Sort(recordComparer);

            var tempPath = Path.Combine(outputPath, Path.GetRandomFileName());

            using (var writer = new StreamWriter(tempPath, false, Encoding.UTF8, 65536))
            {
                foreach (var record in buffer)
                {
                    WriteLine(record, writer);
                }
            }

            bufferFileQueue.Enqueue(tempPath);
            buffer.Clear();
        }
    }
}
