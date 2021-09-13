using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace Sort
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: check args

            var comparer = new RecordComparer();
            var path = args[0];
            var outputPath = args[1];
            var bufferFileQueue = new Queue<string>();
            var bufferSize = args.Length > 2 ? int.Parse(args[2]) : 1024 * 1024 * 64;

            Console.WriteLine("Sorting stage 1 (split) ...");

            // Stage 1 - split file into small sorted parts

            using (var file = File.OpenText(path))
            {
                var buffer = new char[bufferSize];
                var records = new List<Record>(1000000);
                var rest = new char[0];

                while (Read(records, file, buffer, ref rest))
                {
                    bufferFileQueue.Enqueue(SortAndSave(records, comparer, outputPath));
                }
            }

            // Stage 2 - incrementally (pair by pair) merge parts into single sorted file

            Console.WriteLine("Sorting stage 2 (merge) ...");

            while (bufferFileQueue.Count > 1)
            {
                var pathToMerge1 = bufferFileQueue.Dequeue();
                var pathToMerge2 = bufferFileQueue.Dequeue();
                var pathMerged = Path.Combine(outputPath, Path.GetRandomFileName());

                using (var fileToMerge1 = File.OpenText(pathToMerge1))
                using (var fileToMerge2 = File.OpenText(pathToMerge2))
                using (var fileMerged = new StreamWriter(pathMerged, false, Encoding.UTF8, 65536))
                {
                    var buffer1 = new char[bufferSize];
                    var buffer2 = new char[bufferSize];
                    var records1 = new List<Record>(1000000);
                    var records2 = new List<Record>(1000000);
                    var rest1 = new char[0];
                    var rest2 = new char[0];

                    Read(records1, fileToMerge1, buffer1, ref rest1);
                    Read(records2, fileToMerge2, buffer2, ref rest2);

                    var enumerator1 = records1.GetEnumerator();
                    var enumerator2 = records2.GetEnumerator();

                    enumerator1.MoveNext();
                    enumerator2.MoveNext();

                    while (enumerator1.Current != null || enumerator2.Current != null)
                    {
                        var comparisonResult = comparer.Compare(enumerator1.Current, enumerator2.Current);
                        if (comparisonResult < 0)
                        {
                            Write(enumerator1.Current, fileMerged);
                            enumerator1.MoveNext();
                        }
                        else if (comparisonResult == 0)
                        {
                            Write(enumerator1.Current, fileMerged);
                            Write(enumerator2.Current, fileMerged);
                            enumerator1.MoveNext();
                            enumerator2.MoveNext();
                        }
                        else
                        {
                            Write(enumerator2.Current, fileMerged);
                            enumerator2.MoveNext();
                        }

                        if (enumerator1.Current == null && Read(records1, fileToMerge1, buffer1, ref rest1))
                        {
                            enumerator1 = records1.GetEnumerator();
                            enumerator1.MoveNext();
                        }

                        if (enumerator2.Current == null && Read(records2, fileToMerge2, buffer2, ref rest2))
                        {
                            enumerator2 = records2.GetEnumerator();
                            enumerator2.MoveNext();
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

        static bool Read(List<Record> records, StreamReader reader, char[] buffer, ref char[] previousReadRest)
        {
            records.Clear();

            if (reader.EndOfStream)
            {
                return false;
            }

            if (previousReadRest != null)
            {
                Array.Copy(previousReadRest, 0, buffer, 0, previousReadRest.Length);
            }

            var readCount = reader.ReadBlock(buffer, previousReadRest.Length, buffer.Length - previousReadRest.Length) + previousReadRest.Length;

            int numIndex = 0;
            int dotIndex = 0;

            for (int i = 0; i < readCount; i++)
            {
                if (buffer[i] == '.')
                {
                    dotIndex = i;
                }

                if (buffer[i] == '\r' || buffer[i] == '\n')
                {
                    records.Add(new Record
                    {
                        Num = new Memory<char>(buffer, numIndex, dotIndex - numIndex),
                        Str = new Memory<char>(buffer, dotIndex, i - dotIndex)
                    });

                    numIndex = ++i;

                    if (numIndex < readCount && (buffer[numIndex] == '\r' || buffer[numIndex] == '\n'))
                    {
                        numIndex = ++i;
                    }
                }
            }

            if (numIndex < readCount)
            {
                previousReadRest = new char[readCount - numIndex];
                Array.Copy(buffer, numIndex, previousReadRest, 0, previousReadRest.Length);
            }
            else
            {
                previousReadRest = new char[0];
            }

            return true;
        }

        static void Write(Record record, StreamWriter writer)
        {
            writer.Write(record.Num.Span);
            writer.WriteLine(record.Str.Span);
        }

        static string SortAndSave(List<Record> records, IComparer<Record> comparer, string outputPath)
        {
            records.Sort(comparer);

            var tempPath = Path.Combine(outputPath, Path.GetRandomFileName());

            using (var writer = new StreamWriter(tempPath, false, Encoding.UTF8, 65536))
            {
                foreach (var record in records)
                {
                    Write(record, writer);
                }
            }

            return tempPath;
        }
    }
}
