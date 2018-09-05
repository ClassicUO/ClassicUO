namespace ClassicUO.Utility
{
    public static class MathHelper
    {
        public static bool InRange(int input,  int low,  int high)
        {
            return input >= low && input <= high;
        }
    }
}