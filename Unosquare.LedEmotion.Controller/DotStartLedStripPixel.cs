namespace Unosquare.LedEmotion.Controller
{
    using Unosquare.Swan;

    /// <summary>
    /// Represents an LED in a strip of LEDs
    /// This class is not meant to be instantiated by the user.
    /// </summary>
    public class DotStarLedStripPixel
    {
        private readonly int BaseAddress;
        private readonly DotStarLedStrip Owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotStarLedStripPixel"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="baseAddress">The base address.</param>
        public DotStarLedStripPixel(DotStarLedStrip owner, int baseAddress)
        {
            Owner = owner;
            BaseAddress = baseAddress;
        }

        /// <summary>
        /// Gets or sets the brightness, from 0 to 1.
        /// </summary>
        public float Brightness
        {
            get
            {
                var brightnessByte = (byte)(DotStarLedStrip.BrightnessGetMask & Owner.FrameBuffer[BaseAddress]);
                return brightnessByte / 31f;
            }
            set
            {
                // clamp value
                value = value.Clamp(0f, 1f);
                var brightnessByte = (byte)(value * 31);
                Owner.FrameBuffer[BaseAddress] = (byte)(brightnessByte | DotStarLedStrip.BrightnessSetMask);
            }
        }

        /// <summary>
        /// The Red Buye
        /// </summary>
        public byte R
        {
            get
            {
                return Owner.ReverseRgb ? Owner.FrameBuffer[BaseAddress + 3] : Owner.FrameBuffer[BaseAddress + 1];
            }
            set
            {
                if (Owner.ReverseRgb)
                    Owner.FrameBuffer[BaseAddress + 3] = value;
                else
                    Owner.FrameBuffer[BaseAddress + 1] = value;
            }
        }

        /// <summary>
        /// The green
        /// </summary>
        public byte G
        {
            get
            {
                return Owner.FrameBuffer[BaseAddress + 2];
            }
            set
            {
                Owner.FrameBuffer[BaseAddress + 2] = value;
            }
        }

        /// <summary>
        /// The blue
        /// </summary>
        public byte B
        {
            get
            {
                return Owner.ReverseRgb ? Owner.FrameBuffer[BaseAddress + 1] : Owner.FrameBuffer[BaseAddress + 3];
            }
            set
            {
                if (Owner.ReverseRgb)
                    Owner.FrameBuffer[BaseAddress + 1] = value;
                else
                    Owner.FrameBuffer[BaseAddress + 3] = value;
            }
        }
    }

}
