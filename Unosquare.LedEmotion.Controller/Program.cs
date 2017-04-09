namespace Unosquare.LedEmotion.Controller
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    static public class Program
    {

        static void Main(string[] args)
        {

            WebServerWorker.Instance.Start();

            
            if (Debugger.IsAttached)
            {
                var browser = new Process() { StartInfo = new ProcessStartInfo(WebServerWorker.Instance.Server.UrlPrefixes.First()) { UseShellExecute = true } };
                browser.Start();
            }

            Console.ReadKey(true);
            WebServerWorker.Instance.Stop();
        }
    }
}
