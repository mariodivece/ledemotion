namespace Unosquare.LedEmotion.Controller
{
    using Core;
    using Swan.Formatters;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Unosquare.LedEmotion.Controller.Workers;
    using Unosquare.Swan;

    static public class Program
    {

        static AppState m_State;
        static readonly object SyncLock = new object();
        static string m_StateFilename;

        public static AppState State
        {
            get { lock (SyncLock) return m_State; }
        }

        public static string StateFilename
        {
            get
            {
                lock (SyncLock)
                {
                    if (string.IsNullOrWhiteSpace(m_StateFilename))
                    {
                        var location = Path.GetDirectoryName(Path.GetFullPath(typeof(Program).Assembly.Location));
                        m_StateFilename = Path.Combine(location, $"{nameof(AppState)}.json");
                    }

                    return m_StateFilename;
                }
            }
        }

        public static void SaveState()
        {
            lock (SyncLock)
            {
                var jsonText = Json.Serialize(State, format: true);
                File.WriteAllText(StateFilename, jsonText, Encoding.UTF8);
            }
        }

        static void Main(string[] args)
        {
            LoadState();
            WebServerWorker.Instance.Start();
            LedStripWorker.Instance.Start();

            if (Debugger.IsAttached)
            {
                $"Debug mode started".Info();
                var browser = new Process()
                {
                    StartInfo = new ProcessStartInfo(
                        WebServerWorker.Instance.Server.UrlPrefixes.First())
                        { UseShellExecute = true }
                };
                browser.Start();
            }
            else
            {
                $"Release mode started".Info();
            }

            if (Debugger.IsAttached)
            {
                "Press any key to stop the workers.".Info();
                Terminal.ReadKey(true, true);
            }
            else
            {
                using (var tickLock = new ManualResetEvent(false))
                {
                    while (tickLock.WaitOne(100) == false)
                    {
                        // placeholder; keep waiting
                    }
                }
            }

            WebServerWorker.Instance.Stop();
            $"Stopped Web Server".Warn();
            LedStripWorker.Instance.Stop();
            $"Stopped LED Strip Animation".Warn();

            if (Debugger.IsAttached)
            {
                $"Press any key to continue . . .".ReadKey(true);
                Terminal.Flush();
            }

        }

        static void LoadState()
        {
            lock (SyncLock)
            {
                if (m_State == null) m_State = new AppState();

                if (File.Exists(StateFilename) == false)
                {
                    State.LoadDefaults();
                    SaveState();
                }

                var jsonText = File.ReadAllText(StateFilename, Encoding.UTF8);
                var stateData = Json.Deserialize<AppState>(jsonText);
                State.Reset();
                stateData.CopyPropertiesTo(State);
                State.SolidColorPresets.AddRange(stateData.SolidColorPresets);
            }
        }
    }
}
