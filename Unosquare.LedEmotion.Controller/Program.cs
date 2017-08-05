namespace Unosquare.LedEmotion.Controller
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Unosquare.LedEmotion.Controller.Workers;
    using Unosquare.Swan;

    static public class Program
    {

        static void Main(string[] args)
        {
            WebServerWorker.Instance.Start();
            LedStripWorker.Instance.Start();

            if (Debugger.IsAttached)
            {
                $"Debug mode started".Info();
                var browser = new Process() { StartInfo = new ProcessStartInfo(WebServerWorker.Instance.Server.UrlPrefixes.First()) { UseShellExecute = true } };
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
    }
}
