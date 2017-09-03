using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IKriv.Threading.Tasks;

namespace SimpleTimer
{
    class Program
    {
        private static void PrintCurrentTime()
        {
            Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
        }

        private static async Task UseTimer()
        {
            PrintCurrentTime();
            Console.WriteLine("Starting timer...");
            var now = DateTime.UtcNow;
            var lastSecond = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
            var nextSecond = lastSecond.AddSeconds(1);

            var timer = new TaskTimer(1000).StartAt(nextSecond);

            foreach (var task in timer.Take(10))
            {
                await task;
                PrintCurrentTime();
                Thread.Sleep(400);
            }

            Console.WriteLine("Done");
        }

        public static void Main(string[] args)
        {
            UseTimer().Wait();
        }

}
}
