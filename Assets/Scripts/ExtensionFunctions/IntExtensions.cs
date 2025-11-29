namespace ExtensionFunctions
{
    public static class IntExtensions
    {
        public static string To3Digits(this ref int value) =>
            value switch
            {
                < 1000 => value.ToString(),
                < 1_000_000 => value / 1000 + "K",
                _ => value / 1_000_000 + "M"
            };
    }
}