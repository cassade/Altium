using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Sort
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // TODO: check args

            var path = args[0];
            var buffer = new SortedSet<Record>();
            var bufferFileQueue = new Queue<string>();
            var bufferFileSize = long.Parse(args[1]);
            var outputPath = args[2];

            Console.WriteLine("Sorting:");

            // Stage 1 - split file into small sorted parts
            // Read file line by line, when buffer is filled sort it and write to the disk.
            // Then fill, sort and write next portion.

            using (var file = File.OpenText(path))
            {
                while (file.BaseStream.Position < file.BaseStream.Length)
                {
                    var record = await ReadLine(file);
                    buffer.Add(record); // buffer is sorted automatically, no need to sort explicitly

                    if (file.BaseStream.Position > bufferFileSize * (bufferFileQueue.Count + 1))
                    {
                        await FlushBuffer(buffer, outputPath, bufferFileQueue);
                    }

                    var progress = decimal.Divide(file.BaseStream.Position, file.BaseStream.Length) * 100;
                    Console.Write($"\rSplit: {progress:N0}%");
                }
            }

            // flush the rest of data
            if (buffer.Any())
            {
                await FlushBuffer(buffer, outputPath, bufferFileQueue);
            }

            Console.WriteLine();

            // Stage 2 - incrementally (pair by pair) merge parts into single sorted file

            var bufferFileCount = bufferFileQueue.Count;

            while (bufferFileQueue.Count > 1)
            {
                var pathToMerge1 = bufferFileQueue.Dequeue();
                var pathToMerge2 = bufferFileQueue.Dequeue();
                var pathMerged = Path.Combine(outputPath, Path.GetRandomFileName());

                using (var fileToMerge1 = File.OpenText(pathToMerge1))
                using (var fileToMerge2 = File.OpenText(pathToMerge2))
                using (var fileMerged = File.CreateText(pathMerged))
                {
                    var record1 = await ReadLine(fileToMerge1);
                    var record2 = await ReadLine(fileToMerge2);

                    while (record1 != null || record2 != null)
                    {
                        var comparisonResult =
                            record1 != null && record2 == null ? -1 :
                            record1 == null && record2 != null ? 1 :
                            record1.CompareTo(record2);

                        if (comparisonResult < 0)
                        {
                            await WriteLine(record1, fileMerged);
                            record1 = await ReadLine(fileToMerge1);
                        }
                        else if (comparisonResult == 0)
                        {
                            await WriteLine(record1, fileMerged);
                            await WriteLine(record2, fileMerged);
                            record1 = await ReadLine(fileToMerge1);
                            record2 = await ReadLine(fileToMerge2);
                        }
                        else
                        {
                            await WriteLine(record2, fileMerged);
                            record2 = await ReadLine(fileToMerge2);
                        }

                        // Not sure if it's required, default stream buffer should be ok.
                        // await fileMerged.FlushAsync();
                    }
                }

                // delete processed buffer files
                File.Delete(pathToMerge1);
                File.Delete(pathToMerge2);

                // add new buffer file to queue to merge it with other parts
                bufferFileQueue.Enqueue(pathMerged);

                var progress = decimal.Divide(bufferFileCount - bufferFileQueue.Count, bufferFileCount) * 100;
                Console.Write($"\rMerge: {progress:N0}%");
            }

            // move result near to source file
            File.Move(bufferFileQueue.Dequeue(), $"{path}.sorted", true);

            Console.Write($"\rMerge: 100%");
            Console.WriteLine();
            Console.WriteLine($"Sorted: {path}.sorted");
        }

        static async Task<Record> ReadLine(StreamReader reader)
        {
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var line = await reader.ReadLineAsync();
                var recordParts = line.Split('.');
                return new Record(int.Parse(recordParts[0]), recordParts[1].Trim());
            }
            else
            {
                return null;
            }
        }

        static async Task WriteLine(Record record, StreamWriter writer) => await writer.WriteLineAsync($"{record.Num}. {record.Str}");

        static async Task FlushBuffer(ICollection<Record> buffer, string outputPath, Queue<string> bufferFileQueue)
        {
            var path = Path.Combine(outputPath, Path.GetRandomFileName());

            using (var writer = File.CreateText(path))
            {
                foreach (var record in buffer)
                {
                    await WriteLine(record, writer);
                }
            }

            bufferFileQueue.Enqueue(path);
            buffer.Clear();
        }
    }
}
