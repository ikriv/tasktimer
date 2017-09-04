using IKriv.Threading.Tasks;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace TickUntilKeyPress
{
    class Program
    {
        private static void PrintCurrentTime()
        {
            Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
        }

        private static async Task Tick(CancellationToken token)
        {
            using (var timer = new TaskTimer(2000).CancelWith(token).Start())
            {
                try
                {
                    foreach (var task in timer)
                    {
                        await task;
                        PrintCurrentTime();
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Timer Canceled");
                }
            }
        }

        public static void Main()
        {
            Console.WriteLine("Press ENTER to stop timer");
            var src = new CancellationTokenSource();
            var task = Tick(src.Token);
            Console.ReadLine();
            src.Cancel();
            // ReSharper disable once MethodSupportsCancellation
            task.Wait();
            Console.WriteLine("Done");
        }
    }
}
