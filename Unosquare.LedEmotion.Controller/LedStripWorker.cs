namespace Unosquare.LedEmotion.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Unosquare.Swan;
    using Unosquare.Swan.Abstractions;

    /// <summary>
    /// Represents a worker capable of initializing and animating the LED strip
    /// </summary>
    /// <seealso cref="Unosquare.Swan.Abstractions.SingletonBase{Unosquare.LedEmotion.Controller.LedStripWorker}" />
    /// <seealso cref="Unosquare.LedEmotion.Controller.IWorker" />
    public class LedStripWorker : SingletonBase<LedStripWorker>, IWorker
    {

        private enum AnimationType
        {
            SolidColor,
            Transition,
            Image
        }

        private readonly Dictionary<AnimationType, IAnimation> Animations = new Dictionary<AnimationType, IAnimation>()
        {
            { AnimationType.SolidColor, new SolidColorAnimation() },
            { AnimationType.Transition, new TransitionColorAnimation() }
        };

        private AnimationType CurrentAnimationType = AnimationType.SolidColor;

        private static readonly object SyncLock = new object();

        public int LedCount { get; set; } = 240;
        public int FramesPerSecond { get; set; } = 25;
        public int SpiChannel { get; set; } = 1;
        public int SpiFrequency { get; set; } = 1000000; // 1MHz is plenty for 240 LEDs at 24 FPS (4 bytes * 240 LEDs * 25 FPS = ~24kHz minimum)
        public int MillisecondsPerFrame { get; private set; }
        public ulong FrameNumber { get; private set; }

        public bool IsPendingStop { get; private set; }

        private Thread AnimationThread = null;

        public DotStarLedStrip LedStrip { get; private set; }

        public void Start()
        {
            if (FramesPerSecond <= 1)
                FramesPerSecond = 25;

            MillisecondsPerFrame = Convert.ToInt32(Math.Round(1f / FramesPerSecond * 1000f, 0));

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

                AnimationThread = new Thread(AnimateContinuosly)
                {
                    IsBackground = true
                };

                AnimationThread.Start();

            }
        }

        private void AnimateContinuosly()
        {
            IsPendingStop = false;

            var frameStopwatch = new Stopwatch();
            while (IsPendingStop)
            {
                frameStopwatch.Restart();
                FrameNumber = (FrameNumber == UInt64.MaxValue) ? 1 : FrameNumber + 1;

                PaintFrame();

                if (frameStopwatch.ElapsedMilliseconds > MillisecondsPerFrame)
                    $"Frames are lagging. Increase the frequency or simplify the rendering logic.".Warn(typeof(LedStripWorker));

                while (frameStopwatch.ElapsedMilliseconds < MillisecondsPerFrame)
                    Thread.Sleep(1);
            }

            frameStopwatch.Stop();
        }

        private void PaintFrame()
        {
            lock (SyncLock)
            {
                var animation = Animations[CurrentAnimationType];
                animation.PaintNextFrame();
            }
        }


        public void SetColor(byte[] rgbValue, TimeSpan transitionTime)
        {
            lock (SyncLock)
            {
                var animation = Animations[AnimationType.SolidColor] as SolidColorAnimation;
                animation.EnqueueColor(rgbValue, transitionTime);
                CurrentAnimationType = AnimationType.SolidColor;
            }
        }

        public void SetTransition(List<byte[]> rgbValues, TimeSpan transitionTime)
        {
            lock (SyncLock)
            {
                var animation = Animations[AnimationType.Transition] as TransitionColorAnimation;
                animation.SetTransitions(rgbValues, transitionTime);
                CurrentAnimationType = AnimationType.Transition;
            }
        }

        public void Stop()
        {
            lock (SyncLock)
            {
                if (LedStrip == null) return;

                IsPendingStop = true;

                while (AnimationThread.ThreadState == System.Threading.ThreadState.Running)
                {
                    Thread.Sleep(1);
                }

                AnimationThread = null;

                LedStrip.ClearPixels();
                LedStrip.Render();
                LedStrip.Render();
                LedStrip = null;


            }
        }

    }



}
