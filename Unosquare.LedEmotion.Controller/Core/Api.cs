namespace Unosquare.LedEmotion.Controller.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.LedEmotion.Controller.Workers;
    using Unosquare.Swan;
    using Unosquare.Swan.Formatters;

    public class Api : WebApiController
    {

        private const string RelativePath = "/api/";

        [WebApiHandler(HttpVerbs.Get, RelativePath + "status")]
        public async Task<bool> GetStatus(WebServer server, HttpListenerContext context)
        {
            // http://localhost:9696/api/status?r=10&g=245&b=96
            LedStripWorker.Instance.SetColor(new byte[]
            {
                byte.Parse(context.Request.QueryString["r"]),
                byte.Parse(context.Request.QueryString["g"]),
                byte.Parse(context.Request.QueryString["b"])
            }, TimeSpan.FromMilliseconds(LedStripWorker.Instance.MillisecondsPerFrame * 10));

            await context.JsonResponseAsync(new { Name = "STATUS GET" });
            return true;
        }

        [WebApiHandler(HttpVerbs.Get, RelativePath + "appstate")]
        public async Task<bool> GetAppState(WebServer server, HttpListenerContext context)
        {
            await context.JsonResponseAsync(Program.State);
            return true;
        }

        [WebApiHandler(HttpVerbs.Put, RelativePath + "color")]
        public async Task<bool> PutColor(WebServer server, HttpListenerContext context)
        {
            try
            {
                var data = Json.Deserialize(context.RequestBody()) as Dictionary<string, object>;
                var rgb = new byte[3];
                var frames = byte.Parse(data["F"].ToString());

                var transitionTime = TimeSpan.FromMilliseconds(LedStripWorker.Instance.MillisecondsPerFrame * frames);

                rgb[0] = byte.Parse(data["R"].ToString());
                rgb[1] = byte.Parse(data["G"].ToString());
                rgb[2] = byte.Parse(data["B"].ToString());

                LedStripWorker.Instance.SetColor(rgb, transitionTime);

                await context.JsonResponseAsync(new
                {
                    Cl = $"rgb({rgb[0]}, {rgb[1]}, {rgb[2]})",
                    Ms = $"{transitionTime.TotalMilliseconds}"
                });
            }
            catch (Exception ex)
            {
                await context.JsonResponseAsync(new
                {
                    ErrorType = ex.GetType().ToString(),
                    Message = ex.Message
                });

                context.Response.StatusCode = 400;
            }


            return true;
        }

        [WebApiHandler(HttpVerbs.Delete, RelativePath + "preset")]
        public async Task<bool> DeletePreset(WebServer server, HttpListenerContext context)
        {
            try
            {
                var data = Json.Deserialize(context.RequestBody()) as Dictionary<string, object>;

                var removeTarget = 
                    Program.State.SolidColorPresets.FirstOrDefault(d => d.Name.Equals(data["Name"] as string));

                if (removeTarget != null)
                {
                    Program.State.SolidColorPresets.Remove(removeTarget);
                    Program.SaveState();
                }

                await context.JsonResponseAsync(Program.State);
            }
            catch (Exception ex)
            {
                await context.JsonResponseAsync(new
                {
                    ErrorType = ex.GetType().ToString(),
                    Message = ex.Message
                });

                context.Response.StatusCode = 400;
            }


            return true;
        }

        [WebApiHandler(HttpVerbs.Post, RelativePath + "preset")]
        public async Task<bool> AddPreset(WebServer server, HttpListenerContext context)
        {
            try
            {
                var data = Json.Deserialize<SolidColorPreset>(context.RequestBody());

                var existingTarget =
                    Program.State.SolidColorPresets.FirstOrDefault(d => d.Name.Equals(data.Name));

                if (existingTarget != null)
                    data.CopyPropertiesTo(existingTarget);
                else
                    Program.State.SolidColorPresets.Add(data);
                
                Program.SaveState();
                await context.JsonResponseAsync(Program.State);
            }
            catch (Exception ex)
            {
                await context.JsonResponseAsync(new
                {
                    ErrorType = ex.GetType().ToString(),
                    Message = ex.Message
                });

                context.Response.StatusCode = 400;
            }


            return true;
        }


        [WebApiHandler(HttpVerbs.Put, RelativePath + "transition")]
        public async Task<bool> PutTranstion(WebServer server, HttpListenerContext context)
        {
            try
            {
                var data = Json.Deserialize(context.RequestBody()) as Dictionary<string, object>;

                var transitionTime = TimeSpan.FromSeconds(int.Parse(data["Delay"].ToString()));
                var transitionColors = new List<byte[]>();
                var colors = data["Colors"] as List<object>;
                foreach (List<object> color in colors)
                {
                    transitionColors.Add(new byte[] {
                        Convert.ToByte((decimal)color[0]),
                        Convert.ToByte((decimal)color[1]),
                        Convert.ToByte((decimal)color[2])});
                }

                LedStripWorker.Instance.SetTransition(transitionColors, transitionTime);

                await context.JsonResponseAsync(new
                {
                    Cl = $"{colors.Count}",
                    Ms = $"{transitionTime.TotalMilliseconds}"
                });
            }
            catch(Exception ex)
            {
                await context.JsonResponseAsync(new
                {
                    ErrorType = ex.GetType().ToString(),
                    Message = ex.Message
                });

                context.Response.StatusCode = 400;
            }

            return true;
        }
    }
}
