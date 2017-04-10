namespace Unosquare.LedEmotion.Controller.Animation
{
    using System;
    using System.Collections.Generic;
    using Unosquare.LedEmotion.Controller.Workers;
    using Unosquare.Swan;

    public class SolidColorAnimation : IAnimation
    {
        private readonly Queue<byte[]> ColorQueue = new Queue<byte[]>();
        private readonly object SyncLock = new object();

        public SolidColorAnimation()
        {
            // placeholder
        }

        public bool IsTransitionComplete { get { return ColorQueue.Count <= 1; } }

        public void EnqueueColor(byte[] rgb, TimeSpan transitionTime)
        {
            lock (SyncLock)
            {
                if (ColorQueue.Count == 0)
                    ColorQueue.Enqueue(new byte[3]);

                var currentColor = ColorQueue.Peek();

                // if the colors are the same just stay where we are.
                if (currentColor.Contains(rgb))
                    return;

                ColorQueue.Clear();

                var framesInTransition = Convert.ToInt32(
                    Math.Round(
                        (transitionTime.TotalMilliseconds / LedStripWorker.Instance.MillisecondsPerFrame), 0));

                if (framesInTransition < 1) framesInTransition = 1;

                var currentR = Convert.ToSingle(currentColor[0]);
                var currentG = Convert.ToSingle(currentColor[1]);
                var currentB = Convert.ToSingle(currentColor[2]);

                var deltaR = (rgb[0] - currentR) / framesInTransition;
                var deltaG = (rgb[1] - currentG) / framesInTransition;
                var deltaB = (rgb[2] - currentB) / framesInTransition;

                for (var i = 0; i < framesInTransition - 1; i++)
                {
                    var colorToAdd = new byte[] {
                        (byte)currentR.Clamp(0, 255),
                        (byte)currentG.Clamp(0, 255),
                        (byte)currentB.Clamp(0, 255)
                    };

                    ColorQueue.Enqueue(colorToAdd);

                    currentR += deltaR;
                    currentG += deltaG;
                    currentB += deltaB;
                }

                ColorQueue.Enqueue(rgb);
            }

        }

        public void PaintNextFrame()
        {
            lock (SyncLock)
            {
                // Ensure we have at least 1 frame
                if (ColorQueue.Count == 0)
                    ColorQueue.Enqueue(new byte[3]);

                // If we ar in the last frame, just peek it, otherwise, dequeue it.
                var c = ColorQueue.Count == 1 ? ColorQueue.Peek() : ColorQueue.Dequeue();
                LedStripWorker.Instance.LedStrip.SetPixels(c[0], c[1], c[2]);
                if (ColorQueue.Count >= 1)
                {
                    $"Solid Color Render: {c[0]}, {c[1]}, {c[2]}".Trace();
                }
            }
        }
    }
}
