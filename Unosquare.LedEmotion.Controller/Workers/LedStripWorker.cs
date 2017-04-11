namespace Unosquare.LedEmotion.Controller.Workers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Threading;
    using Unosquare.LedEmotion.Controller.Animation;
    using Unosquare.LedEmotion.Controller.Core;
    using Unosquare.Swan;
    using Unosquare.Swan.Abstractions;
    using Unosquare.Swan.Formatters;

    /// <summary>
    /// Represents a worker capable of initializing and animating the LED strip
    /// </summary>
    /// <seealso cref="Unosquare.Swan.Abstractions.SingletonBase{Unosquare.LedEmotion.Controller.LedStripWorker}" />
    /// <seealso cref="Unosquare.LedEmotion.Controller.IWorker" />
    public class LedStripWorker : SingletonBase<LedStripWorker>, IWorker
    {
        #region Private Declarations

        private static readonly object SyncLock = new object();
        private Thread AnimationThread = null;

        private readonly Dictionary<AnimationType, IAnimation> Animations = new Dictionary<AnimationType, IAnimation>()
        {
            { AnimationType.SolidColor, new SolidColorAnimation() },
            { AnimationType.Transition, new TransitionColorAnimation() },
            { AnimationType.Image, new ImageAnimation() },
        };

        private AnimationType CurrentAnimationType = AnimationType.SolidColor;
        private IAnimation CurrentAnimation { get { return Animations[CurrentAnimationType]; } }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the led count.
        /// </summary>
        public int LedCount { get; private set; } = 240;

        /// <summary>
        /// Gets the frames per second.
        /// </summary>
        public int FramesPerSecond { get; private set; } = 25;

        /// <summary>
        /// Gets the spi channel.
        /// </summary>
        public int SpiChannel { get; private set; } = 1;

        /// <summary>
        /// Gets the spi frequency.
        /// </summary>
        public int SpiFrequency { get; private set; } = 1000000; // 1MHz is plenty for 240 LEDs at 24 FPS (4 bytes * 240 LEDs * 25 FPS = ~24kHz minimum)

        /// <summary>
        /// Gets the milliseconds per frame.
        /// </summary>
        public int MillisecondsPerFrame { get; private set; }

        /// <summary>
        /// Gets the frame number.
        /// </summary>
        public ulong FrameNumber { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is pending stop.
        /// </summary>
        public bool IsPendingStop { get; private set; }

        /// <summary>
        /// Gets the led strip.
        /// </summary>
        public DotStarLedStrip LedStrip { get; private set; }

        #endregion

        #region Init, Start and Stop

        /// <summary>
        /// Prevents a default instance of the <see cref="LedStripWorker"/> class from being created.
        /// </summary>
        private LedStripWorker()
            : base()
        {
            // placeholder
        }

        /// <summary>
        /// Sets the start parameters parameters.
        /// </summary>
        /// <param name="ledCount">The led count.</param>
        /// <param name="spiChannel">The spi channel.</param>
        /// <param name="spiFrequency">The spi frequency.</param>
        /// <param name="framesPerSecond">The frames per second.</param>
        public void SetParameters(int ledCount, int spiChannel, int spiFrequency, int framesPerSecond)
        {
            LedCount = ledCount;
            SpiChannel = spiChannel;
            SpiFrequency = spiFrequency;
            FramesPerSecond = framesPerSecond;
            MillisecondsPerFrame = Convert.ToInt32(Math.Round(1f / FramesPerSecond * 1000f, 0));

            if (FramesPerSecond <= 1)
                FramesPerSecond = 25;
        }

        /// <summary>
        /// Should start the task immediately and asynchronously
        /// </summary>
        public void Start()
        {
            SetParameters(LedCount, SpiChannel, SpiFrequency, FramesPerSecond);

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

                for (var i = 0; i < LedCount; i++)
                {
                    LedStrip.SetPixel(i, 1f, 255, 0, 0);
                    LedStrip.Render();
                    Thread.Sleep(10);
                }

                for (var i = 0; i < 3; i++)
                {
                    LedStrip.SetPixels(0, 255, 0);
                    LedStrip.Render();
                    Thread.Sleep(200);
                    LedStrip.ClearPixels();
                    LedStrip.Render();
                    Thread.Sleep(200);
                }

                LedStrip.Render();

                AnimationThread = new Thread(AnimateContinuosly)
                {
                    IsBackground = true
                };

                AnimationThread.Start();

            }
        }

        /// <summary>
        /// Should stop the task immediately and synchronously
        /// </summary>
        public void Stop()
        {
            lock (SyncLock)
            {
                if (LedStrip == null) return;

                IsPendingStop = true;

                while (AnimationThread.ThreadState == System.Threading.ThreadState.Running)
                    Thread.Sleep(1);

                AnimationThread = null;

                LedStrip.ClearPixels();
                LedStrip.Render();
                LedStrip.Render();
                LedStrip = null;


            }
        }

        #endregion

        #region Animation

        /// <summary>
        /// This is a thread worker. It performs the continuous animation until Stop is called.
        /// </summary>
        private void AnimateContinuosly()
        {
            IsPendingStop = false;

            var startFrameTime = DateTime.UtcNow;

            while (IsPendingStop == false)
            {
                startFrameTime = DateTime.UtcNow;
                FrameNumber = (FrameNumber == UInt64.MaxValue) ? 1 : FrameNumber + 1;

                lock (SyncLock)
                {
                    CurrentAnimation.PaintNextFrame();
                    LedStrip.Render();
                }

                var elapsedToFrame = MillisecondsPerFrame - Convert.ToInt32(DateTime.UtcNow.Subtract(startFrameTime).TotalMilliseconds);

                if (elapsedToFrame <= 0)
                {
                    $"Frames are lagging. Increase the frequency or simplify the rendering logic.".Warn(); // typeof(LedStripWorker));
                    continue;
                }
                else
                {
                    Thread.Sleep(elapsedToFrame);
                }
            }

            IsPendingStop = false;
        }


        /// <summary>
        /// Changes the animation mode to Color
        /// </summary>
        /// <param name="rgbValue">The RGB value.</param>
        /// <param name="transitionTime">The transition time.</param>
        public void SetColor(byte[] rgbValue, TimeSpan transitionTime)
        {
            lock (SyncLock)
            {
                if (transitionTime.TotalMilliseconds < MillisecondsPerFrame)
                    transitionTime = TimeSpan.FromMilliseconds(MillisecondsPerFrame);

                if (transitionTime.TotalMilliseconds > MillisecondsPerFrame * 1024)
                    transitionTime = TimeSpan.FromMilliseconds(MillisecondsPerFrame * 1024);

                var animation = Animations[AnimationType.SolidColor] as SolidColorAnimation;
                animation.EnqueueColor(rgbValue, transitionTime);
                CurrentAnimationType = AnimationType.SolidColor;
            }
        }

        public void SetTransition(List<byte[]> rgbValues, TimeSpan totalTransitionTime)
        {
            lock (SyncLock)
            {
                var animation = Animations[AnimationType.Transition] as TransitionColorAnimation;
                animation.SetTransitions(rgbValues, totalTransitionTime);
                CurrentAnimationType = AnimationType.Transition;
            }
        }

        public void SetImage(Image image, TimeSpan totalTransitionTime)
        {
            lock (SyncLock)
            {
                var animation = Animations[AnimationType.Image] as ImageAnimation;
                //animation.EnqueueColor(rgbValue, transitionTime);
                CurrentAnimationType = AnimationType.Image;
            }
        }

        #endregion

    }



}
