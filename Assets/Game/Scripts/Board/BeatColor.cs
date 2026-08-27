namespace GameJam.Gameplay
{
    public enum BeatColor
    {
        Magenta,
        Blue,
        Yellow
    }

    public static class BeatColorExtensions
    {
        public static BeatColor Next(this BeatColor color)
        {
            return color switch
            {
                BeatColor.Magenta => BeatColor.Blue,
                BeatColor.Blue => BeatColor.Yellow,
                _ => BeatColor.Magenta
            };
        }
    }
}
