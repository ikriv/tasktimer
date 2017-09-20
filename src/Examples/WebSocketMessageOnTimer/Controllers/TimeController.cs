using System;
using System.Web.Http;
using WebSocketMessageOnTimer.Models;

namespace WebSocketMessageOnTimer.Controllers
{
    public class TimeController : ApiController
    {
        public TimeRecord GetTime()
        {
            return new TimeRecord { TimeUtc = DateTime.UtcNow };
        }
    }
}
