using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IKriv.Threading.Tasks;
using Newtonsoft.Json;

namespace WebRequestOnTimer
{
    struct DegMinSec
    {
        public DegMinSec(double value)
        {
            Degrees = (int)value;
            double min = (Math.Abs(value) - Math.Abs((Degrees)))*60;
            Minutes = (int) min;
            Seconds = (min - Minutes) * 60;
        }

        public int Degrees;
        public int Minutes;
        public double Seconds;

        public string ToString(bool isLatitude)
        {
            char suffix = isLatitude
                ? (Degrees >= 0 ? 'N' : 'S')
                : (Degrees >= 0 ? 'E' : 'W');

            return $"{Math.Abs(Degrees):0}\u00B0{Minutes:00}'{Seconds+0.5:00}\"{suffix}";
        }
    }

    struct Location
    {
        public double Latitude;
        public double Longitude;

        public override string ToString()
        {
            return new DegMinSec(Latitude).ToString(true) + " " + new DegMinSec(Longitude).ToString(false);
        }
    }

    class LocationGenerator
    {
        private readonly Random _random = new Random(DateTime.UtcNow.Ticks.GetHashCode());

        public Location GetRandomLocation()
        {
            // To pick a random point on the surface of a unit sphere, it is incorrect to select spherical 
            // coordinates theta and phi from uniform distributions. If you do, the points will be bunched around the poles.
            // See http://mathworld.wolfram.com/SpherePointPicking.html
            
            var u = _random.NextDouble();
            var v = _random.NextDouble();

            return new Location
            {
                Latitude = Math.Acos(2*u-1) / Math.PI * 180 - 90,
                Longitude = -180.0 + v * 360
            };
        }
    }

#pragma warning disable 0649 // Assigned by deserializer
    class DayInfo
    {
        [JsonProperty("results")] public DayInfoResults Results;
        [JsonProperty("status")] public string Status;
    }

    class DayInfoResults
    {
        [JsonProperty("sunrise")] public string Sunrise;
        [JsonProperty("sunset")] public string Sunset;
        [JsonProperty("day_length")] public string DayLength;
    }
#pragma warning restore 0649

    class Program
    {
        private static Stopwatch _stopwatch;

        private static string GetCurrentTime()
        {
            return (_stopwatch.ElapsedMilliseconds / 1000.0).ToString("F3");
        }

        private static async Task PrintDayInfo(Location location)
        {
            Console.Write($"{GetCurrentTime()} {location} ");

            try
            {
                var url = $"https://api.sunrise-sunset.org/json?lat={location.Latitude}&lng={location.Longitude}";
                var http = new HttpClient();
                var sw = new Stopwatch();
                sw.Start();
                var json = await http.GetStringAsync(url);
                sw.Stop();
                var info = JsonConvert.DeserializeObject<DayInfo>(json);

                if (info.Status == "OK")
                {
                    Console.WriteLine($"Sunrise: {info.Results.Sunrise}, Day Length: {info.Results.DayLength} ({sw.ElapsedMilliseconds}ms)");
                }
                else
                {
                    Console.WriteLine("Error: " + info.Status);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e);
            }
        }

        private static async Task PrintDayInfoOnTimer()
        {
            var generator = new LocationGenerator();
            var timer = new TaskTimer(1000).Start();
            foreach (var task in timer.Take(20))
            {
                await task;
                await PrintDayInfo(generator.GetRandomLocation());
            }
        }

        static void Main()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            PrintDayInfoOnTimer().Wait();
        }
    }
}
