namespace Unosquare.LedEmotion.Controller.Animation
{
    using System;
    using System.Collections.Generic;
    using Unosquare.Swan;

    public class TransitionColorAnimation : IAnimation
    {
        private readonly object SyncLock = new object();
        private readonly List<byte[]> ColorSteps = new List<byte[]>();
        private int CurrentColorStep = 0;
        private readonly SolidColorAnimation CurrentAnimation = new SolidColorAnimation();
        private int ColorStepDirection = 1;

        public TimeSpan TransitionTimePerColor { get; private set; }

        public TransitionColorAnimation()
        {
            // placeholder
        }

        public void SetTransitions(List<byte[]> rgbValues, TimeSpan totalTransitionTime)
        {
            lock (SyncLock)
            {
                ColorSteps.Clear();
                foreach (var color in rgbValues)
                    ColorSteps.Add(color);

                TransitionTimePerColor = TimeSpan.FromMilliseconds(totalTransitionTime.TotalMilliseconds / ColorSteps.Count);
                CurrentColorStep = 0;
                ColorStepDirection = 1;
            }

        }

        public void PaintNextFrame()
        {
            lock (SyncLock)
            {
                CurrentAnimation.PaintNextFrame();
                $"Queue Length: {CurrentAnimation.QueueLength}".Trace();
                if (CurrentAnimation.IsTransitionComplete)
                {
                    CurrentColorStep += ColorStepDirection;

                    if (CurrentColorStep >= ColorSteps.Count)
                    {
                        ColorStepDirection = -1;
                        CurrentColorStep = ColorSteps.Count - 1;
                    }

                    if (CurrentColorStep < 0)
                    {
                        CurrentColorStep = 0;
                        ColorStepDirection = 1;
                    }

                    var targetColor = ColorSteps[CurrentColorStep];
                    CurrentAnimation.EnqueueColor(targetColor, TransitionTimePerColor);
                    $"New Target Color is: {CurrentAnimation.TargetColor[0]}, {CurrentAnimation.TargetColor[1]}, {CurrentAnimation.TargetColor[2]}".Trace();
                }
            }

        }
    }
}
