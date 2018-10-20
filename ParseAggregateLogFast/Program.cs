using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ParseAggregateLogFast
{
    //the project sums all run times for each process id

    //**********************************
    //INFO - ASSUMPTIONS
    //file structure remains the same for different file (i.e. each row structure is the same)
    //row length is fixed
    //element length in each row is fixed and have the same format
    //Was not taken into account a file size. It also can be a stopping condition indicator
    //     for row iteration, for example by using (FileInfo.Length / 50) as rows amount
    //     and incrementing the counter to this number,
    //     but this can lead to ignoring newly added rows, unless it is required / expected
    //
    //**********************************

    internal class Program
    {
        private static void Main(string[] args)
        {
            var start = GC.GetAllocatedBytesForCurrentThread();
            int gen0CollectionsAtStart = GC.CollectionCount(0);

            var sp = Stopwatch.StartNew();
            string dir_solution = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string dir_path = dir_solution + @"\data\";
            string data_path = dir_path + @"data_min.txt"; //@"data.txt";
            string summary_path = dir_path + @"summary.txt";
            string stats_path = dir_path + @"stats.txt"; //statistics file

            Dictionary<long, TimeSpan> dic = new Dictionary<long, TimeSpan>();

            using (StreamReader sr = new StreamReader(data_path))
            {
                int char_buffer_length = 50;
                char[] buffer = new char[char_buffer_length];

                int chars_read_length;
                bool end_of_file_achieved = false;

                while (!end_of_file_achieved)
                {
                    //allocating new - cancelled (assumptions above)
                    //buffer = new char[char_buffer_length];
                    //cleaning - cancelled (assumptions above)
                    //buffer.CleanArray();

                    chars_read_length = sr.Read(buffer, 0, char_buffer_length);

                    if (chars_read_length == char_buffer_length) // (assumptions above)
                    {
                        DateTime dt_from = buffer.DateTimeArrayToDate(0); //19 length
                        DateTime dt_to = buffer.DateTimeArrayToDate(20); //19 length

                        long Id = buffer.IntArrayToLong(40, 8);
                        TimeSpan Duration = (dt_to - dt_from);

                        if (dic.ContainsKey(Id))
                        {
                            dic[Id] += Duration;
                        }
                        else
                        {
                            dic.Add(Id, Duration);
                        }
                    }
                    else
                    {
                        end_of_file_achieved = true;
                    }
                }
            }

            File.WriteAllLines(summary_path, dic.Select(el => $"{el.Key:D10} {el.Value:c}"));

            var totalAllocated = GC.GetAllocatedBytesForCurrentThread() - start;

            string stats =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - "
                + $"Took: {sp.ElapsedMilliseconds:#,#} ms "
                + $"and allocated {totalAllocated / 1024:#,#} kb "
                + $"with peak working set of {Process.GetCurrentProcess().PeakWorkingSet64 / 1024:#,#} kb "
                + $"and garbage collections {GC.CollectionCount(0) - gen0CollectionsAtStart:#,#}";

            //saving statistics to statistics file
            using (var output = File.AppendText(stats_path))
            {
                output.WriteLine(stats);
            }

            Console.WriteLine(stats);
        }
    }

    public static class NumericHelper
    {
        public static int Power(int number, int pow)
        {
            int result = 1;
            for (int i = 0; i < pow; i++)
            {
                result *= number;
            }
            return result;
        }
    }

    public static class ArrayExtensions
    {
        private static char default_char = default(char);
        private static char zero_char = '0';

        //not in use
        public static void CleanArray(this char[] arr)
        {
            int arr_length = arr.Length;
            for (int i = 0; i < arr_length; i++)
            {
                arr[i] = default_char;// default(char);
            }
        }

        public static DateTime DateTimeArrayToDate(this char[] data, int start)
        {
            //2015-09-13T14:46:35
            int year = data.IntArrayToInt(start + 0, 4);
            int month = data.IntArrayToInt(start + 5, 2);
            int day = data.IntArrayToInt(start + 8, 2);

            int hour = data.IntArrayToInt(start + 11, 2);
            int minute = data.IntArrayToInt(start + 14, 2);
            int second = data.IntArrayToInt(start + 17, 2);

            return new DateTime(year, month, day, hour, minute, second);
        }

        public static int IntArrayToInt(this char[] data, int start, int length)
        {
            int result = 0;
            for (int i = start + length - 1, j = 0; i >= start; i--, j++)
            {
                result += (data[i] - zero_char) * NumericHelper.Power(10, j);
            }
            return result;
        }

        public static long IntArrayToLong(this char[] data, int start, int length)
        {
            long result = 0;
            for (int i = start + length - 1, j = 0; i >= start; i--, j++)
            {
                result += (data[i] - zero_char) * NumericHelper.Power(10, j);
            }
            return result;
        }
    }

}
