using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Sort
{
    class Program
    {
        static RecordComparer _comparer = new RecordComparer();

        static async Task Main(string[] args)
        {
            // TODO: check args

            var path = args[0];
            var outputPath = Directory.CreateDirectory(args[1]).FullName;
            var bufferSize = args.Length > 2 ? int.Parse(args[2]) : 1024 * 1024 * 64;
            var bufferFiles = new List<string>();

            Console.WriteLine("Sorting stage 1 (split) ...");

            // Stage 1 - split file into small sorted parts

            using (var file = File.OpenText(path))
            {
                var buffer = new char[bufferSize];
                var rest = new char[0];

                while (!file.EndOfStream)
                {
                    var tasks = new List<Task<string>>();

                    for (int threadCount = 0; threadCount < 4 && Read(file, buffer, ref rest, out var records); threadCount++)
                    {
                        tasks.Add(Task.Run(() => SortAndSave(records, outputPath)));
                    }

                    bufferFiles.AddRange(await Task.WhenAll(tasks));
                }
            }

            // Stage 2 - incrementally (pair by pair) merge parts into single sorted file

            Console.WriteLine("Sorting stage 2 (merge) ...");

            while (bufferFiles.Count > 1)
            {
                var tasks = new List<Task<string>>();
                var threadCount = 0;

                for (int i = bufferFiles.Count - 1; i > 0 && threadCount < 4; i = i - 2)
                {
                    var fileToMerge1 = bufferFiles[i];
                    var fileToMerge2 = bufferFiles[i - 1];

                    tasks.Add(Task.Run(() => Merge(bufferSize, fileToMerge1, fileToMerge2, outputPath)));

                    bufferFiles.RemoveAt(i);
                    bufferFiles.RemoveAt(i - 1);
                    threadCount++;
                }

                bufferFiles = bufferFiles
                    .Concat(await Task.WhenAll(tasks))
                    .ToList();
            }

            Console.WriteLine($"Sorted: {bufferFiles[0]}");
        }

        static bool Read(StreamReader reader, char[] buffer, ref char[] previousReadRest, out List<Record> records)
        {
            records = new List<Record>();

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
                    (
                        new Span<char>(buffer, numIndex, dotIndex - numIndex),
                        new Span<char>(buffer, dotIndex, i - dotIndex)
                    ));

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
            writer.Write(record.Num);
            writer.WriteLine(record.Str);
        }

        static string SortAndSave(List<Record> records, string outputPath)
        {
            records.Sort(_comparer);

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

        static string Merge(int bufferSize, string path1, string path2, string outputPath)
        {
            var pathMerged = Path.Combine(outputPath, Path.GetRandomFileName());

            using (var fileToMerge1 = File.OpenText(path1))
            using (var fileToMerge2 = File.OpenText(path2))
            using (var fileMerged = new StreamWriter(pathMerged, false, Encoding.UTF8, 65536))
            {
                var buffer1 = new char[bufferSize];
                var buffer2 = new char[bufferSize];
                var rest1 = new char[0];
                var rest2 = new char[0];

                Read(fileToMerge1, buffer1, ref rest1, out var records1);
                Read(fileToMerge2, buffer2, ref rest2, out var records2);

                var enumerator1 = records1.GetEnumerator();
                var enumerator2 = records2.GetEnumerator();

                enumerator1.MoveNext();
                enumerator2.MoveNext();

                while (enumerator1.Current != null || enumerator2.Current != null)
                {
                    var comparisonResult = _comparer.Compare(enumerator1.Current, enumerator2.Current);
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

                    if (enumerator1.Current == null && Read(fileToMerge1, buffer1, ref rest1, out records1))
                    {
                        enumerator1 = records1.GetEnumerator();
                        enumerator1.MoveNext();
                    }

                    if (enumerator2.Current == null && Read(fileToMerge2, buffer2, ref rest2, out records2))
                    {
                        enumerator2 = records2.GetEnumerator();
                        enumerator2.MoveNext();
                    }
                }
            }

            // delete processed buffer files
            File.Delete(path1);
            File.Delete(path2);

            return pathMerged;
        }
    }
}
