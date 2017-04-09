namespace Unosquare.LedEmotion.Controller
{
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Swan.Abstractions;


    public class WebServerWorker : SingletonBase<WebServerWorker>, IWorker
    {
        private const string DefaultUrl = "http://localhost:9696/";
        private const string StaticFilesFolderName = "wwwroot";

        private static readonly object SyncLock = new object();
        private CancellationTokenSource TokenSource = null;
        private Task WebServerTask = null;

        public string StaticFilesRootPath
        {
            get
            {
                var assemblyPath = Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.Location);

                if (Debugger.IsAttached)
                {
                    return Path.Combine(Directory.GetParent(assemblyPath).Parent.FullName, StaticFilesFolderName);
                }

                return Path.Combine(assemblyPath, StaticFilesFolderName);
            }
        }

        public WebServer Server { get; private set; }


        public void Start()
        {
            lock (SyncLock)
            {
                if (Server != null) return;

                Server = new WebServer(DefaultUrl);
                Server.RegisterModule(new LocalSessionModule());
                Server.RegisterModule(new StaticFilesModule(StaticFilesRootPath));
                Server.Module<StaticFilesModule>().UseRamCache = false;
                Server.Module<StaticFilesModule>().DefaultExtension = ".html";
                Server.RegisterModule(new WebApiModule());
                Server.Module<WebApiModule>().RegisterController<Api>();

                TokenSource = new CancellationTokenSource();
                WebServerTask = Server.RunAsync(TokenSource.Token);
            }
        }

        public void Stop()
        {
            lock (SyncLock)
            {
                if (Server == null) return;

                TokenSource.Cancel(false);
                Server.Dispose();
                WebServerTask = null;
                Server = null;
            }
        }
    }
}
