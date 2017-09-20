using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;
using IKriv.Threading.Tasks;
using Newtonsoft.Json;
using WebSocketMessageOnTimer.Models;

namespace WebSocketMessageOnTimer.Controllers
{
    public class WsTimeController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage GetMessage()
        {
            var status = HttpStatusCode.BadRequest;
            var context = HttpContext.Current;
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest(ProcessRequest);
                status = HttpStatusCode.SwitchingProtocols;

            }

            return new HttpResponseMessage(status);
        }

        private async Task ProcessRequest(AspNetWebSocketContext context)
        {
            var ws = context.WebSocket;
            await Task.WhenAll(WriteTask(ws), ReadTask(ws));
        }

        // MUST read if we want the socket state to be updated
        private async Task ReadTask(WebSocket ws)
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            while (true)
            {
                await ws.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                if (ws.State != WebSocketState.Open) break;
            }
        }

        private async Task WriteTask(WebSocket ws)
        {
            using (var timer = new TaskTimer(1000).Start())
            {
                foreach (var tick in timer)
                {
                    await tick.ConfigureAwait(false);

                    EnsureWebSocketIsOpen(ws);
                    var record = new TimeRecord {TimeUtc = DateTime.UtcNow};
                    var buffer = Serialize(record);
                    var sendTask = ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

                    await sendTask.ConfigureAwait(false);
                    EnsureWebSocketIsOpen(ws);
                }
            }
        }

        private static void EnsureWebSocketIsOpen(WebSocket ws)
        {
            var state = ws.State;
            if (state == WebSocketState.Open) return;

            var message = "Web socket is no longer open: current state is " + state;
            System.Diagnostics.Trace.WriteLine(message);
            throw new ApplicationException(message);
        }

        private static byte[] Serialize(object what)
        {
            string json = JsonConvert.SerializeObject(what);
            var buffer = Encoding.UTF8.GetBytes(json);
            return buffer;
        }
    }
}