﻿#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;

namespace ClassicUO.Utility
{
    public static class GarbageCollectionWatcher
    {
        private static int numberOfPasses;
        private static long start;
        private static long end;
        private static long difference;
        private static long totalMemoryAllocatedInTheBlock;
        private static long totalMemoryLostInTheBlock;

        public static void Start()
        {
            start = GC.GetTotalMemory(false);
        }

        public static void Stop()
        {
            end = GC.GetTotalMemory(false);
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
        ///     This is to tell you how much total memory the code between start and stop creates.
        ///     If the area is constantly generating garbage then its probably bad..
        /// </summary>
        public static long GetTotalMemoryAllocatedInTheBlock() => totalMemoryAllocatedInTheBlock;

        /// <summary>
        ///     When we see memory lost in this block this value increases.
        ///     This is typically a bad sign that the block triggered a collect.
        ///     If this is increasing non stop its a very bad sign.
        /// </summary>
        public static long GetTotalMemoryLostInTheBlock() => totalMemoryLostInTheBlock;

        public static long GetDifferences() => difference;
    }
}