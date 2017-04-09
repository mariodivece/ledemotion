namespace Unosquare.LedEmotion.Controller
{
    using System;
    using Unosquare.Swan.Abstractions;

    public class LedStripWorker : SingletonBase<LedStripWorker>, IWorker
    {
        private const int LedCount = 240;
        private const int FramesPerSecond = 25;
        private const int SpiChannel = 1;
        private const int SpiFrequency = 1000000; // 1MHz is plenty for 240 LEDs at 24 FPS (4 bytes * 240 LEDs * 25 FPS = ~24kHz minimum)

        private static readonly object SyncLock = new object();


        public DotStarLedStrip LedStrip { get; private set; }

        public void Start()
        {
            lock (SyncLock)
            {
                if (LedStrip != null) return;

                LedStrip = new DotStarLedStrip(
                    ledCount: LedCount, 
                    spiChannel: SpiChannel, 
                    spiFrequency: SpiFrequency, 
                    reverseRgb: true);

                LedStrip.ClearPixels();
                LedStrip.Render();
            }
        }

        public void Stop()
        {
            lock (SyncLock)
            {
                if (LedStrip == null) return;

                LedStrip.ClearPixels();
                LedStrip.Render();
                LedStrip.Render();
                LedStrip = null;
            }
        }
    }
}
