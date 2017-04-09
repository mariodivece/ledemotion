namespace Unosquare.LedEmotion.Controller
{
    using System.Net;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO;
    using Unosquare.Labs.EmbedIO.Modules;

    public class Api : WebApiController
    {

        private const string RelativePath = "/api/";

        [WebApiHandler(HttpVerbs.Get, RelativePath + "status")]
        public async Task<bool> GetStatus(WebServer server, HttpListenerContext context)
        {
            await context.JsonResponseAsync(new { Name = "HELLO" });
            return true;
        }

        public async Task<bool> PutColor(WebServer server, HttpListenerContext context)
        {
            await context.JsonResponseAsync(new { Name = "HELLO" });
            return true;
        }

    }
}
