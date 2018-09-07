using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Utility
{
    public static class GarbageCollectionWatcher
    {
        private static int numberOfPasses = 0;
        private static long start = 0;
        private static long end = 0;
        private static long difference = 0;
        private static long totalMemoryAllocatedInTheBlock = 0;
        private static long totalMemoryLostInTheBlock = 0;

        public static void Start()
        {
            start = System.GC.GetTotalMemory(false);
        }
        public static void Stop()
        {
            end = System.GC.GetTotalMemory(false);
            numberOfPasses++;
            difference = end - start;
            if (difference < 0)
            {
                // a collect between start end which resulted in lost memory occured.
                totalMemoryLostInTheBlock += difference;
            }
            if (difference > 0)
            {
                // something has generated garbage here. 
                // that however isn't neccessarily a bad thing.
                // unless it is continuous beyond start up.
                totalMemoryAllocatedInTheBlock += difference;
            }
        }
        /// <summary>
        /// This is to tell you how much total memory the code between start and stop creates.
        /// If the area is constantly generating garbage then its probably bad..
        /// </summary>
        public static long GetTotalMemoryAllocatedInTheBlock()
        {
            return totalMemoryAllocatedInTheBlock;
        }
        /// <summary>
        /// When we see memory lost in this block this value increases.
        /// This is typically a bad sign that the block triggered a collect.
        /// If this is increasing non stop its a very bad sign.
        /// </summary>
        public static long GetTotalMemoryLostInTheBlock()
        {
            return totalMemoryLostInTheBlock;
        }

        public static long GetDifferences() => difference;
    }
}
