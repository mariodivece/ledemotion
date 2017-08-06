namespace Unosquare.LedEmotion.Controller.Core
{
    public class SolidColorPreset
    {
        public string Name { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public byte[] ToBytes()
        {
            return new byte[] { R, G, B };
        }

        public override string ToString()
        {
            return $"{Name}: {R},{G},{B}";
        }
    }
}
