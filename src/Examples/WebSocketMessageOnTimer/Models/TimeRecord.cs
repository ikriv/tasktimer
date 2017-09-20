using System;
using Newtonsoft.Json;

namespace WebSocketMessageOnTimer.Models
{
    public class TimeRecord
    {
        [JsonProperty("timeUtc")]
        public DateTime TimeUtc { get; set; }
    }
}