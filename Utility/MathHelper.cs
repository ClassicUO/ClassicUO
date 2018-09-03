namespace ClassicUO.Utility
{
    public static class MathHelper
    {
        public static bool InRange(in int input, in int low, in int high)
        {
            return input >= low && input <= high;
        }
    }
}