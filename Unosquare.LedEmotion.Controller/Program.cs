namespace Unosquare.LedEmotion.Controller
{
    using System;
    using System.Diagnostics;
    using System.Linq;
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
                var browser = new Process() { StartInfo = new ProcessStartInfo(WebServerWorker.Instance.Server.UrlPrefixes.First()) { UseShellExecute = true } };
                browser.Start();
            }


            "Press any key to stop the workers.".Info();

            Terminal.ReadKey(true, true);
            WebServerWorker.Instance.Stop();
            $"Stopped Web Server".Warn();
            LedStripWorker.Instance.Stop();
            $"Stopped LED Strip Animation".Warn();
            $"Press any key to continue . . .".ReadKey(true);
            Terminal.Flush();
        }
    }
}
