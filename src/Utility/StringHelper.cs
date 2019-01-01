#region license
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
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ClassicUO.Utility
{
    internal static class StringHelper
    {
        public static string CapitalizeFirstCharacter(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (str.Length == 1)
                return char.ToUpper(str[0]).ToString();

            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public static string CapitalizeAllWords(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (str.Length == 1)
                return char.ToUpper(str[0]).ToString();
            StringBuilder sb = new StringBuilder();
            bool capitalizeNext = true;

            for (int i = 0; i < str.Length; i++)
            {
                if (capitalizeNext)
                    sb.Append(char.ToUpper(str[i]));
                else
                    sb.Append(str[i]);
                capitalizeNext = " .,;!".Contains(str[i].ToString());
            }

            return sb.ToString();
        }

        public static unsafe string ReadUTF8(byte* data)
        {
            byte* ptr = data;

            while (*ptr != 0)
                ptr++;

            return Encoding.UTF8.GetString(data, (int)(ptr - data));
        }

        public static bool IsSafeChar(int c)
        {
            return (c >= 0x20 && c < 0xFFFE);
        }
    }

    // Copyright (c) 2015-2017 Michael Popoloski
    // 
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    // 
    // The above copyright notice and this permission notice shall be included in
    // all copies or substantial portions of the Software.
    // 
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    // THE SOFTWARE.


    //class a
    //{

    //    /// <summary>
    //    /// Specifies an interface for types that act as a set of formatting arguments.
    //    /// </summary>
    //    public interface IArgSet
    //    {
    //        /// <summary>
    //        /// The number of arguments in the set.
    //        /// </summary>
    //        int Count { get; }

    //        /// <summary>
    //        /// Format one of the arguments in the set into the given string buffer.
    //        /// </summary>
    //        /// <param name="buffer">The buffer to which to append the argument.</param>
    //        /// <param name="index">The index of the argument to format.</param>
    //        /// <param name="format">A specifier indicating how the argument should be formatted.</param>
    //        void Format(StringBuffer buffer, int index, StringView format);
    //    }

    //    /// <summary>
    //    /// Defines an interface for types that can be formatted into a string buffer.
    //    /// </summary>
    //    public interface IStringFormattable
    //    {
    //        /// <summary>
    //        /// Format the current instance into the given string buffer.
    //        /// </summary>
    //        /// <param name="buffer">The buffer to which to append.</param>
    //        /// <param name="format">A specifier indicating how the argument should be formatted.</param>
    //        void Format(StringBuffer buffer, StringView format);
    //    }

    //    /// <summary>
    //    /// A low-allocation version of the built-in <see cref="StringBuilder"/> type.
    //    /// </summary>
    //    public unsafe sealed partial class StringBuffer
    //    {
    //        CachedCulture culture;
    //        char[] buffer;
    //        int currentCount;

    //        /// <summary>
    //        /// The number of characters in the buffer.
    //        /// </summary>
    //        public int Count
    //        {
    //            get { return currentCount; }
    //        }

    //        /// <summary>
    //        /// The culture used to format string data.
    //        /// </summary>
    //        public CultureInfo Culture
    //        {
    //            get { return culture.Culture; }
    //            set
    //            {
    //                if (culture.Culture == value)
    //                    return;

    //                if (value == CultureInfo.InvariantCulture)
    //                    culture = CachedInvariantCulture;
    //                else if (value == CachedCurrentCulture.Culture)
    //                    culture = CachedCurrentCulture;
    //                else
    //                    culture = new CachedCulture(value);
    //            }
    //        }

    //        /// <summary>
    //        /// Initializes a new instance of the <see cref="StringBuffer"/> class.
    //        /// </summary>
    //        public StringBuffer()
    //            : this(DefaultCapacity)
    //        {
    //        }

    //        /// <summary>
    //        /// Initializes a new instance of the <see cref="StringBuffer"/> class.
    //        /// </summary>
    //        /// <param name="capacity">The initial size of the string buffer.</param>
    //        public StringBuffer(int capacity)
    //        {
    //            buffer = new char[capacity];
    //            culture = CachedCurrentCulture;
    //        }

    //        /// <summary>
    //        /// Sets a custom formatter to use when converting instances of a given type to a string.
    //        /// </summary>
    //        /// <typeparam name="T">The type for which to set the formatter.</typeparam>
    //        /// <param name="formatter">A delegate that will be called to format instances of the specified type.</param>
    //        public static void SetCustomFormatter<T>(Action<StringBuffer, T, StringView> formatter)
    //        {
    //            ValueHelper<T>.Formatter = formatter;
    //        }

    //        /// <summary>
    //        /// Clears the buffer.
    //        /// </summary>
    //        public void Clear()
    //        {
    //            currentCount = 0;
    //        }

    //        /// <summary>
    //        /// Copies the contents of the buffer to the given array.
    //        /// </summary>
    //        /// <param name="sourceIndex">The index within the buffer to begin copying.</param>
    //        /// <param name="destination">The destination array.</param>
    //        /// <param name="destinationIndex">The index within the destination array to which to begin copying.</param>
    //        /// <param name="count">The number of characters to copy.</param>
    //        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    //        {
    //            if (destination == null)
    //                throw new ArgumentNullException(nameof(destination));
    //            if (destinationIndex + count > destination.Length || destinationIndex < 0)
    //                throw new ArgumentOutOfRangeException(nameof(destinationIndex));

    //            fixed (char* destPtr = &destination[destinationIndex])
    //                CopyTo(destPtr, sourceIndex, count);
    //        }

    //        /// <summary>
    //        /// Copies the contents of the buffer to the given array.
    //        /// </summary>
    //        /// <param name="dest">A pointer to the destination array.</param>
    //        /// <param name="sourceIndex">The index within the buffer to begin copying.</param>
    //        /// <param name="count">The number of characters to copy.</param>
    //        public void CopyTo(char* dest, int sourceIndex, int count)
    //        {
    //            if (count < 0)
    //                throw new ArgumentOutOfRangeException(nameof(count));
    //            if (sourceIndex + count > currentCount || sourceIndex < 0)
    //                throw new ArgumentOutOfRangeException(nameof(sourceIndex));

    //            fixed (char* s = buffer)
    //            {
    //                var src = s + sourceIndex;
    //                for (int i = 0; i < count; i++)
    //                    *dest++ = *src++;
    //            }
    //        }

    //        /// <summary>
    //        /// Copies the contents of the buffer to the given byte array.
    //        /// </summary>
    //        /// <param name="dest">A pointer to the destination byte array.</param>
    //        /// <param name="sourceIndex">The index within the buffer to begin copying.</param>
    //        /// <param name="count">The number of characters to copy.</param>
    //        /// <param name="encoding">The encoding to use to convert characters to bytes.</param>
    //        /// <returns>The number of bytes written to the destination.</returns>
    //        public int CopyTo(byte* dest, int sourceIndex, int count, Encoding encoding)
    //        {
    //            if (count < 0)
    //                throw new ArgumentOutOfRangeException(nameof(count));
    //            if (sourceIndex + count > currentCount || sourceIndex < 0)
    //                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
    //            if (encoding == null)
    //                throw new ArgumentNullException(nameof(encoding));

    //            fixed (char* s = buffer)
    //                return encoding.GetBytes(s, count, dest, count);
    //        }

    //        /// <summary>
    //        /// Converts the buffer to a string instance.
    //        /// </summary>
    //        /// <returns>A new string representing the characters currently in the buffer.</returns>
    //        public override string ToString()
    //        {
    //            return new string(buffer, 0, currentCount);
    //        }

    //        /// <summary>
    //        /// Appends a character to the current buffer.
    //        /// </summary>
    //        /// <param name="c">The character to append.</param>
    //        public void Append(char c)
    //        {
    //            Append(c, 1);
    //        }

    //        /// <summary>
    //        /// Appends a character to the current buffer several times.
    //        /// </summary>
    //        /// <param name="c">The character to append.</param>
    //        /// <param name="count">The number of times to append the character.</param>
    //        public void Append(char c, int count)
    //        {
    //            if (count < 0)
    //                throw new ArgumentOutOfRangeException(nameof(count));

    //            CheckCapacity(count);
    //            fixed (char* b = &buffer[currentCount])
    //            {
    //                var ptr = b;
    //                for (int i = 0; i < count; i++)
    //                    *ptr++ = c;
    //                currentCount += count;
    //            }
    //        }

    //        /// <summary>
    //        /// Appends the specified string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        public void Append(string value)
    //        {
    //            if (value == null)
    //                throw new ArgumentNullException(nameof(value));

    //            Append(value, 0, value.Length);
    //        }

    //        /// <summary>
    //        /// Appends a string subset to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The string to append.</param>
    //        /// <param name="startIndex">The starting index within the string to begin reading characters.</param>
    //        /// <param name="count">The number of characters to append.</param>
    //        public void Append(string value, int startIndex, int count)
    //        {
    //            if (value == null)
    //                throw new ArgumentNullException(nameof(value));
    //            if (startIndex < 0 || startIndex + count > value.Length)
    //                throw new ArgumentOutOfRangeException(nameof(startIndex));

    //            fixed (char* s = value)
    //                Append(s + startIndex, count);
    //        }

    //        /// <summary>
    //        /// Appends an array of characters to the current buffer.
    //        /// </summary>
    //        /// <param name="values">The characters to append.</param>
    //        /// <param name="startIndex">The starting index within the array to begin reading characters.</param>
    //        /// <param name="count">The number of characters to append.</param>
    //        public void Append(char[] values, int startIndex, int count)
    //        {
    //            if (values == null)
    //                throw new ArgumentNullException(nameof(values));
    //            if (startIndex < 0 || startIndex + count > values.Length)
    //                throw new ArgumentOutOfRangeException(nameof(startIndex));

    //            fixed (char* s = &values[startIndex])
    //                Append(s, count);
    //        }

    //        /// <summary>
    //        /// Appends an array of characters to the current buffer.
    //        /// </summary>
    //        /// <param name="str">A pointer to the array of characters to append.</param>
    //        /// <param name="count">The number of characters to append.</param>
    //        public void Append(char* str, int count)
    //        {
    //            CheckCapacity(count);
    //            fixed (char* b = &buffer[currentCount])
    //            {
    //                var dest = b;
    //                for (int i = 0; i < count; i++)
    //                    *dest++ = *str++;
    //                currentCount += count;
    //            }
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        public void Append(bool value)
    //        {
    //            if (value)
    //                Append(TrueLiteral);
    //            else
    //                Append(FalseLiteral);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(sbyte value, StringView format)
    //        {
    //            Numeric.FormatSByte(this, value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(byte value, StringView format)
    //        {
    //            // widening here is fine
    //            Numeric.FormatUInt32(this, value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(short value, StringView format)
    //        {
    //            Numeric.FormatInt16(this, value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(ushort value, StringView format)
    //        {
    //            // widening here is fine
    //            Numeric.FormatUInt32(this, value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(int value, StringView format)
    //        {
    //            Numeric.FormatInt32(this, value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(uint value, StringView format)
    //        {
    //            Numeric.FormatUInt32(this, value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(long value, StringView format)
    //        {
    //            Numeric.FormatInt64(this, value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(ulong value, StringView format)
    //        {
    //            Numeric.FormatUInt64(this, value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(float value, StringView format)
    //        {
    //            Numeric.FormatSingle(this, value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(double value, StringView format)
    //        {
    //            Numeric.FormatDouble(this, value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the specified value as a string to the current buffer.
    //        /// </summary>
    //        /// <param name="value">The value to append.</param>
    //        /// <param name="format">A format specifier indicating how to convert <paramref name="value"/> to a string.</param>
    //        public void Append(decimal value, StringView format)
    //        {
    //            Numeric.FormatDecimal(this, (uint*)&value, format, culture);
    //        }

    //        /// <summary>
    //        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance.
    //        /// Each format item is replaced by the string representation of a single argument.
    //        /// </summary>
    //        /// <typeparam name="T">The type of argument set being formatted.</typeparam>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="args">The set of args to insert into the format string.</param>
    //        public void AppendArgSet<T>(string format, ref T args) where T : IArgSet
    //        {
    //            if (format == null)
    //                throw new ArgumentNullException(nameof(format));

    //            fixed (char* formatPtr = format)
    //            {
    //                var curr = formatPtr;
    //                var end = curr + format.Length;
    //                var segmentsLeft = false;
    //                var prevArgIndex = 0;
    //                do
    //                {
    //                    CheckCapacity((int)(end - curr));
    //                    fixed (char* bufferPtr = &buffer[currentCount])
    //                        segmentsLeft = AppendSegment(ref curr, end, bufferPtr, ref prevArgIndex, ref args);
    //                }
    //                while (segmentsLeft);
    //            }
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        void CheckCapacity(int count)
    //        {
    //            if (currentCount + count > buffer.Length)
    //                Array.Resize(ref buffer, buffer.Length * 2);
    //        }

    //        bool AppendSegment<T>(ref char* currRef, char* end, char* dest, ref int prevArgIndex, ref T args) where T : IArgSet
    //        {
    //            char* curr = currRef;
    //            char c = '\x0';
    //            while (curr < end)
    //            {
    //                c = *curr++;
    //                if (c == '}')
    //                {
    //                    // check for escape character for }}
    //                    if (curr < end && *curr == '}')
    //                        curr++;
    //                    else
    //                        ThrowError();
    //                }
    //                else if (c == '{')
    //                {
    //                    // check for escape character for {{
    //                    if (curr == end)
    //                        ThrowError();
    //                    else if (*curr == '{')
    //                        curr++;
    //                    else
    //                        break;
    //                }

    //                *dest++ = c;
    //                currentCount++;
    //            }

    //            if (curr == end)
    //                return false;

    //            int index;
    //            if (*curr == '}')
    //                index = prevArgIndex;
    //            else
    //                index = ParseNum(ref curr, end, MaxArgs);
    //            if (index >= args.Count)
    //                throw new FormatException(string.Format(SR.ArgIndexOutOfRange, index));

    //            // check for a spacing specifier
    //            c = SkipWhitespace(ref curr, end);
    //            var width = 0;
    //            var leftJustify = false;
    //            var oldCount = currentCount;
    //            if (c == ',')
    //            {
    //                curr++;
    //                c = SkipWhitespace(ref curr, end);

    //                // spacing can be left-justified
    //                if (c == '-')
    //                {
    //                    leftJustify = true;
    //                    curr++;
    //                    if (curr == end)
    //                        ThrowError();
    //                }

    //                width = ParseNum(ref curr, end, MaxSpacing);
    //                c = SkipWhitespace(ref curr, end);
    //            }

    //            // check for format specifier
    //            curr++;
    //            if (c == ':')
    //            {
    //                var specifierBuffer = stackalloc char[MaxSpecifierSize];
    //                var specifierEnd = specifierBuffer + MaxSpecifierSize;
    //                var specifierPtr = specifierBuffer;

    //                while (true)
    //                {
    //                    if (curr == end)
    //                        ThrowError();

    //                    c = *curr++;
    //                    if (c == '{')
    //                    {
    //                        // check for escape character for {{
    //                        if (curr < end && *curr == '{')
    //                            curr++;
    //                        else
    //                            ThrowError();
    //                    }
    //                    else if (c == '}')
    //                    {
    //                        // check for escape character for }}
    //                        if (curr < end && *curr == '}')
    //                            curr++;
    //                        else
    //                        {
    //                            // found the end of the specifier
    //                            // kick off the format job
    //                            var specifier = new StringView(specifierBuffer, (int)(specifierPtr - specifierBuffer));
    //                            args.Format(this, index, specifier);
    //                            break;
    //                        }
    //                    }

    //                    if (specifierPtr == specifierEnd)
    //                        ThrowError();
    //                    *specifierPtr++ = c;
    //                }
    //            }
    //            else
    //            {
    //                // no specifier. make sure we're at the end of the format block
    //                if (c != '}')
    //                    ThrowError();

    //                // format without any specifier
    //                args.Format(this, index, StringView.Empty);
    //            }

    //            // finish off padding, if necessary
    //            var padding = width - (currentCount - oldCount);
    //            if (padding > 0)
    //            {
    //                if (leftJustify)
    //                    Append(' ', padding);
    //                else
    //                {
    //                    // copy the recently placed chars up in memory to make room for padding
    //                    CheckCapacity(padding);
    //                    for (int i = currentCount - 1; i >= oldCount; i--)
    //                        buffer[i + padding] = buffer[i];

    //                    // fill in padding
    //                    for (int i = 0; i < padding; i++)
    //                        buffer[i + oldCount] = ' ';
    //                    currentCount += padding;
    //                }
    //            }

    //            prevArgIndex = index + 1;
    //            currRef = curr;
    //            return true;
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        internal void AppendGeneric<T>(T value, StringView format)
    //        {
    //            // this looks gross, but T is known at JIT-time so this call tree
    //            // gets compiled down to a direct call with no branching
    //            if (typeof(T) == typeof(sbyte))
    //                Append(*(sbyte*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(byte))
    //                Append(*(byte*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(short))
    //                Append(*(short*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(ushort))
    //                Append(*(ushort*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(int))
    //                Append(*(int*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(uint))
    //                Append(*(uint*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(long))
    //                Append(*(long*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(ulong))
    //                Append(*(ulong*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(float))
    //                Append(*(float*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(double))
    //                Append(*(double*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(decimal))
    //                Append(*(decimal*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(bool))
    //                Append(*(bool*)UnsafeMemoryManager.AsPointer(ref value));
    //            else if (typeof(T) == typeof(char))
    //                Append(*(char*)UnsafeMemoryManager.AsPointer(ref value), format);
    //            else if (typeof(T) == typeof(string))
    //                Append(UnsafeMemoryManager.As<string>(value));
    //            else
    //            {
    //                // first, check to see if it's a value type implementing IStringFormattable
    //                var formatter = ValueHelper<T>.Formatter;
    //                if (formatter != null)
    //                    formatter(this, value, format);
    //                else
    //                {
    //                    // We could handle this case by calling ToString() on the object and paying the
    //                    // allocation, but presumably if the user is using us instead of the built-in
    //                    // formatting utilities they would rather be notified of this case, so we'll throw.
    //                    throw new InvalidOperationException(string.Format(SR.TypeNotFormattable, typeof(T)));
    //                }
    //            }
    //        }

    //        static int ParseNum(ref char* currRef, char* end, int maxValue)
    //        {
    //            char* curr = currRef;
    //            char c = *curr;
    //            if (c < '0' || c > '9')
    //                ThrowError();

    //            int value = 0;
    //            do
    //            {
    //                value = value * 10 + c - '0';
    //                curr++;
    //                if (curr == end)
    //                    ThrowError();
    //                c = *curr;
    //            } while (c >= '0' && c <= '9' && value < maxValue);

    //            currRef = curr;
    //            return value;
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        static char SkipWhitespace(ref char* currRef, char* end)
    //        {
    //            char* curr = currRef;
    //            while (curr < end && *curr == ' ') curr++;

    //            if (curr == end)
    //                ThrowError();

    //            currRef = curr;
    //            return *curr;
    //        }

    //        static void ThrowError()
    //        {
    //            throw new FormatException(SR.InvalidFormatString);
    //        }

    //        static StringBuffer Acquire(int capacity)
    //        {
    //            if (capacity <= MaxCachedSize)
    //            {
    //                var buffer = CachedInstance;
    //                if (buffer != null)
    //                {
    //                    CachedInstance = null;
    //                    buffer.Clear();
    //                    buffer.CheckCapacity(capacity);
    //                    return buffer;
    //                }
    //            }

    //            return new StringBuffer(capacity);
    //        }

    //        static void Release(StringBuffer buffer)
    //        {
    //            if (buffer.buffer.Length <= MaxCachedSize)
    //                CachedInstance = buffer;
    //        }

    //        [ThreadStatic]
    //        static StringBuffer CachedInstance;

    //        static readonly CachedCulture CachedInvariantCulture = new CachedCulture(CultureInfo.InvariantCulture);
    //        static readonly CachedCulture CachedCurrentCulture = new CachedCulture(CultureInfo.CurrentCulture);

    //        const int DefaultCapacity = 32;
    //        const int MaxCachedSize = 360;  // same as BCL's StringBuilderCache
    //        const int MaxArgs = 256;
    //        const int MaxSpacing = 1000000;
    //        const int MaxSpecifierSize = 32;

    //        const string TrueLiteral = "True";
    //        const string FalseLiteral = "False";

    //        // The point of this class is to allow us to generate a direct call to a known
    //        // method on an unknown, unconstrained generic value type. Normally this would
    //        // be impossible; you'd have to cast the generic argument and introduce boxing.
    //        // Instead we pay a one-time startup cost to create a delegate that will forward
    //        // the parameter to the appropriate method in a strongly typed fashion.
    //        static class ValueHelper<T>
    //        {
    //            public static Action<StringBuffer, T, StringView> Formatter = Prepare();

    //            static Action<StringBuffer, T, StringView> Prepare()
    //            {
    //                // we only use this class for value types that also implement IStringFormattable
    //                var type = typeof(T);
    //                if (!typeof(IStringFormattable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
    //                    return null;

    //                var result = typeof(ValueHelper<T>)
    //                    .GetTypeInfo()
    //                    .GetDeclaredMethod("Assign")
    //                    .MakeGenericMethod(type)
    //                    .Invoke(null, null);
    //                return (Action<StringBuffer, T, StringView>)result;
    //            }

    //            public static Action<StringBuffer, U, StringView> Assign<U>() where U : IStringFormattable
    //            {
    //                return (f, u, v) => u.Format(f, v);
    //            }
    //        }
    //    }

    //    // TODO: clean this up
    //    public unsafe struct StringView
    //    {
    //        public static readonly StringView Empty = new StringView();

    //        public readonly char* Data;
    //        public readonly int Length;

    //        public bool IsEmpty
    //        {
    //            get { return Length == 0; }
    //        }

    //        public StringView(char* data, int length)
    //        {
    //            Data = data;
    //            Length = length;
    //        }

    //        public static bool operator ==(StringView lhs, string rhs)
    //        {
    //            var count = lhs.Length;
    //            if (count != rhs.Length)
    //                return false;

    //            fixed (char* r = rhs)
    //            {
    //                var lhsPtr = lhs.Data;
    //                var rhsPtr = r;
    //                for (int i = 0; i < count; i++)
    //                {
    //                    if (*lhsPtr++ != *rhsPtr++)
    //                        return false;
    //                }
    //            }

    //            return true;
    //        }

    //        public static bool operator !=(StringView lhs, string rhs)
    //        {
    //            return !(lhs == rhs);
    //        }
    //    }

    //    // caches formatting information from culture data
    //    // some of the accessors on NumberFormatInfo allocate copies of their data
    //    sealed class CachedCulture
    //    {
    //        public readonly CultureInfo Culture;

    //        public readonly NumberFormatData CurrencyData;
    //        public readonly NumberFormatData FixedData;
    //        public readonly NumberFormatData NumberData;
    //        public readonly NumberFormatData ScientificData;
    //        public readonly NumberFormatData PercentData;

    //        public readonly string CurrencyNegativePattern;
    //        public readonly string CurrencyPositivePattern;
    //        public readonly string CurrencySymbol;

    //        public readonly string NumberNegativePattern;
    //        public readonly string NumberPositivePattern;

    //        public readonly string PercentNegativePattern;
    //        public readonly string PercentPositivePattern;
    //        public readonly string PercentSymbol;

    //        public readonly string NegativeSign;
    //        public readonly string PositiveSign;

    //        public readonly string NaN;
    //        public readonly string PositiveInfinity;
    //        public readonly string NegativeInfinity;

    //        public readonly int DecimalBufferSize;

    //        public CachedCulture(CultureInfo culture)
    //        {
    //            Culture = culture;

    //            var info = culture.NumberFormat;
    //            CurrencyData = new NumberFormatData(
    //                info.CurrencyDecimalDigits,
    //                info.NegativeSign,
    //                info.CurrencyDecimalSeparator,
    //                info.CurrencyGroupSeparator,
    //                info.CurrencyGroupSizes,
    //                info.CurrencySymbol.Length
    //            );

    //            FixedData = new NumberFormatData(
    //                info.NumberDecimalDigits,
    //                info.NegativeSign,
    //                info.NumberDecimalSeparator,
    //                null,
    //                null,
    //                0
    //            );

    //            NumberData = new NumberFormatData(
    //                info.NumberDecimalDigits,
    //                info.NegativeSign,
    //                info.NumberDecimalSeparator,
    //                info.NumberGroupSeparator,
    //                info.NumberGroupSizes,
    //                0
    //            );

    //            ScientificData = new NumberFormatData(
    //                6,
    //                info.NegativeSign,
    //                info.NumberDecimalSeparator,
    //                null,
    //                null,
    //                info.NegativeSign.Length + info.PositiveSign.Length * 2 // for number and exponent
    //            );

    //            PercentData = new NumberFormatData(
    //                info.PercentDecimalDigits,
    //                info.NegativeSign,
    //                info.PercentDecimalSeparator,
    //                info.PercentGroupSeparator,
    //                info.PercentGroupSizes,
    //                info.PercentSymbol.Length
    //            );

    //            CurrencyNegativePattern = NegativeCurrencyFormats[info.CurrencyNegativePattern];
    //            CurrencyPositivePattern = PositiveCurrencyFormats[info.CurrencyPositivePattern];
    //            CurrencySymbol = info.CurrencySymbol;
    //            NumberNegativePattern = NegativeNumberFormats[info.NumberNegativePattern];
    //            NumberPositivePattern = PositiveNumberFormat;
    //            PercentNegativePattern = NegativePercentFormats[info.PercentNegativePattern];
    //            PercentPositivePattern = PositivePercentFormats[info.PercentPositivePattern];
    //            PercentSymbol = info.PercentSymbol;
    //            NegativeSign = info.NegativeSign;
    //            PositiveSign = info.PositiveSign;
    //            NaN = info.NaNSymbol;
    //            PositiveInfinity = info.PositiveInfinitySymbol;
    //            NegativeInfinity = info.NegativeInfinitySymbol;
    //            DecimalBufferSize =
    //                NumberFormatData.MinBufferSize +
    //                info.NumberDecimalSeparator.Length +
    //                (NegativeSign.Length + PositiveSign.Length) * 2;
    //        }

    //        static readonly string[] PositiveCurrencyFormats = {
    //        "$#", "#$", "$ #", "# $"
    //    };

    //        static readonly string[] NegativeCurrencyFormats = {
    //        "($#)", "-$#", "$-#", "$#-",
    //        "(#$)", "-#$", "#-$", "#$-",
    //        "-# $", "-$ #", "# $-", "$ #-",
    //        "$ -#", "#- $", "($ #)", "(# $)"
    //    };

    //        static readonly string[] PositivePercentFormats = {
    //        "# %", "#%", "%#", "% #"
    //    };

    //        static readonly string[] NegativePercentFormats = {
    //        "-# %", "-#%", "-%#",
    //        "%-#", "%#-",
    //        "#-%", "#%-",
    //        "-% #", "# %-", "% #-",
    //        "% -#", "#- %"
    //    };

    //        static readonly string[] NegativeNumberFormats = {
    //        "(#)", "-#", "- #", "#-", "# -",
    //    };

    //        static readonly string PositiveNumberFormat = "#";
    //    }

    //    // contains format information for a specific kind of format string
    //    // e.g. (fixed, number, currency)
    //    sealed class NumberFormatData
    //    {
    //        readonly int bufferLength;
    //        readonly int perDigitLength;

    //        public readonly int DecimalDigits;
    //        public readonly string NegativeSign;
    //        public readonly string DecimalSeparator;
    //        public readonly string GroupSeparator;
    //        public readonly int[] GroupSizes;

    //        public NumberFormatData(int decimalDigits, string negativeSign, string decimalSeparator, string groupSeparator, int[] groupSizes, int extra)
    //        {
    //            DecimalDigits = decimalDigits;
    //            NegativeSign = negativeSign;
    //            DecimalSeparator = decimalSeparator;
    //            GroupSeparator = groupSeparator;
    //            GroupSizes = groupSizes;

    //            bufferLength = MinBufferSize;
    //            bufferLength += NegativeSign.Length;
    //            bufferLength += DecimalSeparator.Length;
    //            bufferLength += extra;

    //            if (GroupSeparator != null)
    //                perDigitLength = GroupSeparator.Length;
    //        }

    //        public int GetBufferSize(ref int maxDigits, int scale)
    //        {
    //            if (maxDigits < 0)
    //                maxDigits = DecimalDigits;

    //            var digitCount = scale >= 0 ? scale + maxDigits : 0;
    //            long len = bufferLength;

    //            // calculate buffer size
    //            len += digitCount;
    //            len += perDigitLength * digitCount;
    //            return checked((int)len);
    //        }

    //        internal const int MinBufferSize = 105;
    //    }

    //    // this file contains the custom numeric formatting routines split out from the Numeric.cs file
    //    partial class Numeric
    //    {
    //        static void NumberToCustomFormatString(StringBuffer formatter, ref Number number, StringView specifier, CachedCulture culture)
    //        {
    //        }
    //    }

    //    // Most of the implementation of this file was ported from the native versions built into the CLR
    //    // See: https://github.com/dotnet/coreclr/blob/838807429a0828a839958e3b7d392d65886c8f2e/src/classlibnative/bcltype/number.cpp
    //    // Also see: https://github.com/dotnet/coreclr/blob/02084af832c2900cf6eac2a168c41f261409be97/src/mscorlib/src/System/Number.cs
    //    // Standard numeric format string reference: https://msdn.microsoft.com/en-us/library/dwhawy9k%28v=vs.110%29.aspx

    //    unsafe static partial class Numeric
    //    {
    //        public static void FormatSByte(StringBuffer formatter, sbyte value, StringView specifier, CachedCulture culture)
    //        {
    //            if (value < 0 && !specifier.IsEmpty)
    //            {
    //                // if we're negative and doing a hex format, mask out the bits for the conversion
    //                char c = specifier.Data[0];
    //                if (c == 'X' || c == 'x')
    //                {
    //                    FormatUInt32(formatter, (uint)(value & 0xFF), specifier, culture);
    //                    return;
    //                }
    //            }

    //            FormatInt32(formatter, value, specifier, culture);
    //        }

    //        public static void FormatInt16(StringBuffer formatter, short value, StringView specifier, CachedCulture culture)
    //        {
    //            if (value < 0 && !specifier.IsEmpty)
    //            {
    //                // if we're negative and doing a hex format, mask out the bits for the conversion
    //                char c = specifier.Data[0];
    //                if (c == 'X' || c == 'x')
    //                {
    //                    FormatUInt32(formatter, (uint)(value & 0xFFFF), specifier, culture);
    //                    return;
    //                }
    //            }

    //            FormatInt32(formatter, value, specifier, culture);
    //        }

    //        public static void FormatInt32(StringBuffer formatter, int value, StringView specifier, CachedCulture culture)
    //        {
    //            int digits;
    //            var fmt = ParseFormatSpecifier(specifier, out digits);

    //            // ANDing with 0xFFDF has the effect of uppercasing the character
    //            switch (fmt & 0xFFDF)
    //            {
    //                case 'G':
    //                    if (digits > 0)
    //                        goto default;
    //                    else
    //                        goto case 'D';

    //                case 'D':
    //                    Int32ToDecStr(formatter, value, digits, culture.NegativeSign);
    //                    break;

    //                case 'X':
    //                    // fmt-('X'-'A'+1) gives us the base hex character in either
    //                    // uppercase or lowercase, depending on the casing of fmt
    //                    Int32ToHexStr(formatter, (uint)value, fmt - ('X' - 'A' + 10), digits);
    //                    break;

    //                default:
    //                    var number = new Number();
    //                    var buffer = stackalloc char[MaxNumberDigits + 1];
    //                    number.Digits = buffer;
    //                    Int32ToNumber(value, ref number);
    //                    if (fmt != 0)
    //                        NumberToString(formatter, ref number, fmt, digits, culture);
    //                    else
    //                        NumberToCustomFormatString(formatter, ref number, specifier, culture);
    //                    break;
    //            }
    //        }

    //        public static void FormatUInt32(StringBuffer formatter, uint value, StringView specifier, CachedCulture culture)
    //        {
    //            int digits;
    //            var fmt = ParseFormatSpecifier(specifier, out digits);

    //            // ANDing with 0xFFDF has the effect of uppercasing the character
    //            switch (fmt & 0xFFDF)
    //            {
    //                case 'G':
    //                    if (digits > 0)
    //                        goto default;
    //                    else
    //                        goto case 'D';

    //                case 'D':
    //                    UInt32ToDecStr(formatter, value, digits);
    //                    break;

    //                case 'X':
    //                    // fmt-('X'-'A'+1) gives us the base hex character in either
    //                    // uppercase or lowercase, depending on the casing of fmt
    //                    Int32ToHexStr(formatter, value, fmt - ('X' - 'A' + 10), digits);
    //                    break;

    //                default:
    //                    var number = new Number();
    //                    var buffer = stackalloc char[MaxNumberDigits + 1];
    //                    number.Digits = buffer;
    //                    UInt32ToNumber(value, ref number);
    //                    if (fmt != 0)
    //                        NumberToString(formatter, ref number, fmt, digits, culture);
    //                    else
    //                        NumberToCustomFormatString(formatter, ref number, specifier, culture);
    //                    break;
    //            }
    //        }

    //        public static void FormatInt64(StringBuffer formatter, long value, StringView specifier, CachedCulture culture)
    //        {
    //            int digits;
    //            var fmt = ParseFormatSpecifier(specifier, out digits);

    //            // ANDing with 0xFFDF has the effect of uppercasing the character
    //            switch (fmt & 0xFFDF)
    //            {
    //                case 'G':
    //                    if (digits > 0)
    //                        goto default;
    //                    else
    //                        goto case 'D';

    //                case 'D':
    //                    Int64ToDecStr(formatter, value, digits, culture.NegativeSign);
    //                    break;

    //                case 'X':
    //                    // fmt-('X'-'A'+1) gives us the base hex character in either
    //                    // uppercase or lowercase, depending on the casing of fmt
    //                    Int64ToHexStr(formatter, (ulong)value, fmt - ('X' - 'A' + 10), digits);
    //                    break;

    //                default:
    //                    var number = new Number();
    //                    var buffer = stackalloc char[MaxNumberDigits + 1];
    //                    number.Digits = buffer;
    //                    Int64ToNumber(value, ref number);
    //                    if (fmt != 0)
    //                        NumberToString(formatter, ref number, fmt, digits, culture);
    //                    else
    //                        NumberToCustomFormatString(formatter, ref number, specifier, culture);
    //                    break;
    //            }
    //        }

    //        public static void FormatUInt64(StringBuffer formatter, ulong value, StringView specifier, CachedCulture culture)
    //        {
    //            int digits;
    //            var fmt = ParseFormatSpecifier(specifier, out digits);

    //            // ANDing with 0xFFDF has the effect of uppercasing the character
    //            switch (fmt & 0xFFDF)
    //            {
    //                case 'G':
    //                    if (digits > 0)
    //                        goto default;
    //                    else
    //                        goto case 'D';

    //                case 'D':
    //                    UInt64ToDecStr(formatter, value, digits);
    //                    break;

    //                case 'X':
    //                    // fmt-('X'-'A'+1) gives us the base hex character in either
    //                    // uppercase or lowercase, depending on the casing of fmt
    //                    Int64ToHexStr(formatter, value, fmt - ('X' - 'A' + 10), digits);
    //                    break;

    //                default:
    //                    var number = new Number();
    //                    var buffer = stackalloc char[MaxNumberDigits + 1];
    //                    number.Digits = buffer;
    //                    UInt64ToNumber(value, ref number);
    //                    if (fmt != 0)
    //                        NumberToString(formatter, ref number, fmt, digits, culture);
    //                    else
    //                        NumberToCustomFormatString(formatter, ref number, specifier, culture);
    //                    break;
    //            }
    //        }

    //        public static void FormatSingle(StringBuffer formatter, float value, StringView specifier, CachedCulture culture)
    //        {
    //            int digits;
    //            int precision = FloatPrecision;
    //            var fmt = ParseFormatSpecifier(specifier, out digits);

    //            // ANDing with 0xFFDF has the effect of uppercasing the character
    //            switch (fmt & 0xFFDF)
    //            {
    //                case 'G':
    //                    if (digits > 7)
    //                        precision = 9;
    //                    break;

    //                case 'E':
    //                    if (digits > 6)
    //                        precision = 9;
    //                    break;
    //            }

    //            var number = new Number();
    //            var buffer = stackalloc char[MaxFloatingDigits + 1];
    //            number.Digits = buffer;
    //            DoubleToNumber(value, precision, ref number);

    //            if (number.Scale == ScaleNaN)
    //            {
    //                formatter.Append(culture.NaN);
    //                return;
    //            }

    //            if (number.Scale == ScaleInf)
    //            {
    //                if (number.Sign > 0)
    //                    formatter.Append(culture.NegativeInfinity);
    //                else
    //                    formatter.Append(culture.PositiveInfinity);
    //                return;
    //            }

    //            if (fmt != 0)
    //                NumberToString(formatter, ref number, fmt, digits, culture);
    //            else
    //                NumberToCustomFormatString(formatter, ref number, specifier, culture);
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        public static void FormatDouble(StringBuffer formatter, double value, StringView specifier, CachedCulture culture)
    //        {
    //            int digits;
    //            int precision = DoublePrecision;
    //            var fmt = ParseFormatSpecifier(specifier, out digits);

    //            // ANDing with 0xFFDF has the effect of uppercasing the character
    //            switch (fmt & 0xFFDF)
    //            {
    //                case 'G':
    //                    if (digits > 15)
    //                        precision = 17;
    //                    break;

    //                case 'E':
    //                    if (digits > 14)
    //                        precision = 17;
    //                    break;
    //            }

    //            var number = new Number();
    //            var buffer = stackalloc char[MaxFloatingDigits + 1];
    //            number.Digits = buffer;
    //            DoubleToNumber(value, precision, ref number);

    //            if (number.Scale == ScaleNaN)
    //            {
    //                formatter.Append(culture.NaN);
    //                return;
    //            }

    //            if (number.Scale == ScaleInf)
    //            {
    //                if (number.Sign > 0)
    //                    formatter.Append(culture.NegativeInfinity);
    //                else
    //                    formatter.Append(culture.PositiveInfinity);
    //                return;
    //            }

    //            if (fmt != 0)
    //                NumberToString(formatter, ref number, fmt, digits, culture);
    //            else
    //                NumberToCustomFormatString(formatter, ref number, specifier, culture);
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        public static void FormatDecimal(StringBuffer formatter, uint* value, StringView specifier, CachedCulture culture)
    //        {
    //            int digits;
    //            var fmt = ParseFormatSpecifier(specifier, out digits);

    //            var number = new Number();
    //            var buffer = stackalloc char[MaxNumberDigits + 1];
    //            number.Digits = buffer;
    //            DecimalToNumber(value, ref number);
    //            if (fmt != 0)
    //                NumberToString(formatter, ref number, fmt, digits, culture, isDecimal: true);
    //            else
    //                NumberToCustomFormatString(formatter, ref number, specifier, culture);
    //        }

    //        static void NumberToString(StringBuffer formatter, ref Number number, char format, int maxDigits, CachedCulture culture, bool isDecimal = false)
    //        {
    //            // ANDing with 0xFFDF has the effect of uppercasing the character
    //            switch (format & 0xFFDF)
    //            {
    //                case 'C':
    //                    {
    //                        var cultureData = culture.CurrencyData;
    //                        var bufferSize = cultureData.GetBufferSize(ref maxDigits, number.Scale);
    //                        RoundNumber(ref number, number.Scale + maxDigits);

    //                        var buffer = stackalloc char[bufferSize];
    //                        var ptr = FormatCurrency(
    //                            buffer,
    //                            ref number,
    //                            maxDigits,
    //                            cultureData,
    //                            number.Sign > 0 ? culture.CurrencyNegativePattern : culture.CurrencyPositivePattern,
    //                            culture.CurrencySymbol
    //                        );

    //                        formatter.Append(buffer, (int)(ptr - buffer));
    //                        break;
    //                    }

    //                case 'F':
    //                    {
    //                        var cultureData = culture.FixedData;
    //                        var bufferSize = cultureData.GetBufferSize(ref maxDigits, number.Scale);
    //                        RoundNumber(ref number, number.Scale + maxDigits);

    //                        var buffer = stackalloc char[bufferSize];
    //                        var ptr = buffer;
    //                        if (number.Sign > 0)
    //                            AppendString(&ptr, cultureData.NegativeSign);

    //                        ptr = FormatFixed(ptr, ref number, maxDigits, cultureData);
    //                        formatter.Append(buffer, (int)(ptr - buffer));
    //                        break;
    //                    }

    //                case 'N':
    //                    {
    //                        var cultureData = culture.NumberData;
    //                        var bufferSize = cultureData.GetBufferSize(ref maxDigits, number.Scale);
    //                        RoundNumber(ref number, number.Scale + maxDigits);

    //                        var buffer = stackalloc char[bufferSize];
    //                        var ptr = FormatNumber(
    //                            buffer,
    //                            ref number,
    //                            maxDigits,
    //                            number.Sign > 0 ? culture.NumberNegativePattern : culture.NumberPositivePattern,
    //                            cultureData
    //                        );

    //                        formatter.Append(buffer, (int)(ptr - buffer));
    //                        break;
    //                    }

    //                case 'E':
    //                    {
    //                        var cultureData = culture.ScientificData;
    //                        var bufferSize = cultureData.GetBufferSize(ref maxDigits, number.Scale);
    //                        maxDigits++;

    //                        RoundNumber(ref number, maxDigits);

    //                        var buffer = stackalloc char[bufferSize];
    //                        var ptr = buffer;
    //                        if (number.Sign > 0)
    //                            AppendString(&ptr, cultureData.NegativeSign);

    //                        ptr = FormatScientific(
    //                            ptr,
    //                            ref number,
    //                            maxDigits,
    //                            format, // TODO: fix casing
    //                            cultureData.DecimalSeparator,
    //                            culture.PositiveSign,
    //                            culture.NegativeSign
    //                        );

    //                        formatter.Append(buffer, (int)(ptr - buffer));
    //                        break;
    //                    }

    //                case 'P':
    //                    {
    //                        number.Scale += 2;
    //                        var cultureData = culture.PercentData;
    //                        var bufferSize = cultureData.GetBufferSize(ref maxDigits, number.Scale);
    //                        RoundNumber(ref number, number.Scale + maxDigits);

    //                        var buffer = stackalloc char[bufferSize];
    //                        var ptr = FormatPercent(
    //                            buffer,
    //                            ref number,
    //                            maxDigits,
    //                            cultureData,
    //                            number.Sign > 0 ? culture.PercentNegativePattern : culture.PercentPositivePattern,
    //                            culture.PercentSymbol
    //                        );

    //                        formatter.Append(buffer, (int)(ptr - buffer));
    //                        break;
    //                    }

    //                case 'G':
    //                    {
    //                        var enableRounding = true;
    //                        if (maxDigits < 1)
    //                        {
    //                            if (isDecimal && maxDigits == -1)
    //                            {
    //                                // if we're formatting a decimal, default to 29 digits precision
    //                                // only for G formatting without a precision specifier
    //                                maxDigits = DecimalPrecision;
    //                                enableRounding = false;
    //                            }
    //                            else
    //                                maxDigits = number.Precision;
    //                        }

    //                        var bufferSize = maxDigits + culture.DecimalBufferSize;
    //                        var buffer = stackalloc char[bufferSize];
    //                        var ptr = buffer;

    //                        // round for G formatting only if a precision is given
    //                        // we need to handle the minus zero case also
    //                        if (enableRounding)
    //                            RoundNumber(ref number, maxDigits);
    //                        else if (isDecimal && number.Digits[0] == 0)
    //                            number.Sign = 0;

    //                        if (number.Sign > 0)
    //                            AppendString(&ptr, culture.NegativeSign);

    //                        ptr = FormatGeneral(
    //                            ptr,
    //                            ref number,
    //                            maxDigits,
    //                            (char)(format - ('G' - 'E')),
    //                            culture.NumberData.DecimalSeparator,
    //                            culture.PositiveSign,
    //                            culture.NegativeSign,
    //                            !enableRounding
    //                        );

    //                        formatter.Append(buffer, (int)(ptr - buffer));
    //                        break;
    //                    }

    //                default:
    //                    throw new FormatException(string.Format(SR.UnknownFormatSpecifier, format));
    //            }
    //        }

    //        static char* FormatCurrency(char* buffer, ref Number number, int maxDigits, NumberFormatData data, string currencyFormat, string currencySymbol)
    //        {
    //            for (int i = 0; i < currencyFormat.Length; i++)
    //            {
    //                char c = currencyFormat[i];
    //                switch (c)
    //                {
    //                    case '#': buffer = FormatFixed(buffer, ref number, maxDigits, data); break;
    //                    case '-': AppendString(&buffer, data.NegativeSign); break;
    //                    case '$': AppendString(&buffer, currencySymbol); break;
    //                    default: *buffer++ = c; break;
    //                }
    //            }

    //            return buffer;
    //        }

    //        static char* FormatNumber(char* buffer, ref Number number, int maxDigits, string format, NumberFormatData data)
    //        {
    //            for (int i = 0; i < format.Length; i++)
    //            {
    //                char c = format[i];
    //                switch (c)
    //                {
    //                    case '#': buffer = FormatFixed(buffer, ref number, maxDigits, data); break;
    //                    case '-': AppendString(&buffer, data.NegativeSign); break;
    //                    default: *buffer++ = c; break;
    //                }
    //            }

    //            return buffer;
    //        }

    //        static char* FormatPercent(char* buffer, ref Number number, int maxDigits, NumberFormatData data, string format, string percentSymbol)
    //        {
    //            for (int i = 0; i < format.Length; i++)
    //            {
    //                char c = format[i];
    //                switch (c)
    //                {
    //                    case '#': buffer = FormatFixed(buffer, ref number, maxDigits, data); break;
    //                    case '-': AppendString(&buffer, data.NegativeSign); break;
    //                    case '%': AppendString(&buffer, percentSymbol); break;
    //                    default: *buffer++ = c; break;
    //                }
    //            }

    //            return buffer;
    //        }

    //        static char* FormatGeneral(
    //            char* buffer, ref Number number, int maxDigits, char expChar,
    //            string decimalSeparator, string positiveSign, string negativeSign,
    //            bool suppressScientific)
    //        {

    //            var digitPos = number.Scale;
    //            var scientific = false;
    //            if (!suppressScientific)
    //            {
    //                if (digitPos > maxDigits || digitPos < -3)
    //                {
    //                    digitPos = 1;
    //                    scientific = true;
    //                }
    //            }

    //            var digits = number.Digits;
    //            if (digitPos <= 0)
    //                *buffer++ = '0';
    //            else
    //            {
    //                do
    //                {
    //                    *buffer++ = *digits != 0 ? *digits++ : '0';
    //                } while (--digitPos > 0);
    //            }

    //            if (*digits != 0 || digitPos < 0)
    //            {
    //                AppendString(&buffer, decimalSeparator);
    //                while (digitPos < 0)
    //                {
    //                    *buffer++ = '0';
    //                    digitPos++;
    //                }

    //                while (*digits != 0)
    //                    *buffer++ = *digits++;
    //            }

    //            if (scientific)
    //                buffer = FormatExponent(buffer, number.Scale - 1, expChar, positiveSign, negativeSign, 2);

    //            return buffer;
    //        }

    //        static char* FormatScientific(
    //            char* buffer, ref Number number, int maxDigits, char expChar,
    //            string decimalSeparator, string positiveSign, string negativeSign)
    //        {

    //            var digits = number.Digits;
    //            *buffer++ = *digits != 0 ? *digits++ : '0';
    //            if (maxDigits != 1)
    //                AppendString(&buffer, decimalSeparator);

    //            while (--maxDigits > 0)
    //                *buffer++ = *digits != 0 ? *digits++ : '0';

    //            int e = number.Digits[0] == 0 ? 0 : number.Scale - 1;
    //            return FormatExponent(buffer, e, expChar, positiveSign, negativeSign, 3);
    //        }

    //        static char* FormatExponent(char* buffer, int value, char expChar, string positiveSign, string negativeSign, int minDigits)
    //        {
    //            *buffer++ = expChar;
    //            if (value < 0)
    //            {
    //                AppendString(&buffer, negativeSign);
    //                value = -value;
    //            }
    //            else if (positiveSign != null)
    //                AppendString(&buffer, positiveSign);

    //            var digits = stackalloc char[11];
    //            var ptr = Int32ToDecChars(digits + 10, (uint)value, minDigits);
    //            var len = (int)(digits + 10 - ptr);
    //            while (--len >= 0)
    //                *buffer++ = *ptr++;

    //            return buffer;
    //        }

    //        static char* FormatFixed(char* buffer, ref Number number, int maxDigits, NumberFormatData data)
    //        {
    //            var groups = data.GroupSizes;
    //            var digits = number.Digits;
    //            var digitPos = number.Scale;
    //            if (digitPos <= 0)
    //                *buffer++ = '0';
    //            else if (groups != null)
    //            {
    //                var groupIndex = 0;
    //                var groupSizeCount = groups[0];
    //                var groupSizeLen = groups.Length;
    //                var newBufferSize = digitPos;
    //                var groupSeparatorLen = data.GroupSeparator.Length;
    //                var groupSize = 0;

    //                // figure out the size of the result
    //                if (groupSizeLen != 0)
    //                {
    //                    while (digitPos > groupSizeCount)
    //                    {
    //                        groupSize = groups[groupIndex];
    //                        if (groupSize == 0)
    //                            break;

    //                        newBufferSize += groupSeparatorLen;
    //                        if (groupIndex < groupSizeLen - 1)
    //                            groupIndex++;

    //                        groupSizeCount += groups[groupIndex];
    //                        if (groupSizeCount < 0 || newBufferSize < 0)
    //                            throw new ArgumentOutOfRangeException(SR.InvalidGroupSizes);
    //                    }

    //                    if (groupSizeCount == 0)
    //                        groupSize = 0;
    //                    else
    //                        groupSize = groups[0];
    //                }

    //                groupIndex = 0;
    //                var digitCount = 0;
    //                var digitLength = StrLen(digits);
    //                var digitStart = digitPos < digitLength ? digitPos : digitLength;
    //                var ptr = buffer + newBufferSize - 1;

    //                for (int i = digitPos - 1; i >= 0; i--)
    //                {
    //                    *(ptr--) = i < digitStart ? digits[i] : '0';

    //                    // check if we need to add a group separator
    //                    if (groupSize > 0)
    //                    {
    //                        digitCount++;
    //                        if (digitCount == groupSize && i != 0)
    //                        {
    //                            for (int j = groupSeparatorLen - 1; j >= 0; j--)
    //                                *(ptr--) = data.GroupSeparator[j];

    //                            if (groupIndex < groupSizeLen - 1)
    //                            {
    //                                groupIndex++;
    //                                groupSize = groups[groupIndex];
    //                            }
    //                            digitCount = 0;
    //                        }
    //                    }
    //                }

    //                buffer += newBufferSize;
    //                digits += digitStart;
    //            }
    //            else
    //            {
    //                do
    //                {
    //                    *buffer++ = *digits != 0 ? *digits++ : '0';
    //                }
    //                while (--digitPos > 0);
    //            }

    //            if (maxDigits > 0)
    //            {
    //                AppendString(&buffer, data.DecimalSeparator);
    //                while (digitPos < 0 && maxDigits > 0)
    //                {
    //                    *buffer++ = '0';
    //                    digitPos++;
    //                    maxDigits--;
    //                }

    //                while (maxDigits > 0)
    //                {
    //                    *buffer++ = *digits != 0 ? *digits++ : '0';
    //                    maxDigits--;
    //                }
    //            }

    //            return buffer;
    //        }

    //        static void Int32ToDecStr(StringBuffer formatter, int value, int digits, string negativeSign)
    //        {
    //            if (digits < 1)
    //                digits = 1;

    //            var maxDigits = digits > 15 ? digits : 15;
    //            var bufferLength = maxDigits > 100 ? maxDigits : 100;
    //            var negativeLength = 0;

    //            if (value < 0)
    //            {
    //                negativeLength = negativeSign.Length;
    //                if (negativeLength > bufferLength - maxDigits)
    //                    bufferLength = negativeLength + maxDigits;
    //            }

    //            var buffer = stackalloc char[bufferLength];
    //            var p = Int32ToDecChars(buffer + bufferLength, value >= 0 ? (uint)value : (uint)-value, digits);
    //            if (value < 0)
    //            {
    //                // add the negative sign
    //                for (int i = negativeLength - 1; i >= 0; i--)
    //                    *(--p) = negativeSign[i];
    //            }

    //            formatter.Append(p, (int)(buffer + bufferLength - p));
    //        }

    //        static void UInt32ToDecStr(StringBuffer formatter, uint value, int digits)
    //        {
    //            var buffer = stackalloc char[100];
    //            if (digits < 1)
    //                digits = 1;

    //            var p = Int32ToDecChars(buffer + 100, value, digits);
    //            formatter.Append(p, (int)(buffer + 100 - p));
    //        }

    //        static void Int32ToHexStr(StringBuffer formatter, uint value, int hexBase, int digits)
    //        {
    //            var buffer = stackalloc char[100];
    //            if (digits < 1)
    //                digits = 1;

    //            var p = Int32ToHexChars(buffer + 100, value, hexBase, digits);
    //            formatter.Append(p, (int)(buffer + 100 - p));
    //        }

    //        static void Int64ToDecStr(StringBuffer formatter, long value, int digits, string negativeSign)
    //        {
    //            if (digits < 1)
    //                digits = 1;

    //            var sign = (int)High32((ulong)value);
    //            var maxDigits = digits > 20 ? digits : 20;
    //            var bufferLength = maxDigits > 100 ? maxDigits : 100;

    //            if (sign < 0)
    //            {
    //                value = -value;
    //                var negativeLength = negativeSign.Length;
    //                if (negativeLength > bufferLength - maxDigits)
    //                    bufferLength = negativeLength + maxDigits;
    //            }

    //            var buffer = stackalloc char[bufferLength];
    //            var p = buffer + bufferLength;
    //            var uv = (ulong)value;
    //            while (High32(uv) != 0)
    //            {
    //                p = Int32ToDecChars(p, Int64DivMod(ref uv), 9);
    //                digits -= 9;
    //            }

    //            p = Int32ToDecChars(p, Low32(uv), digits);
    //            if (sign < 0)
    //            {
    //                // add the negative sign
    //                for (int i = negativeSign.Length - 1; i >= 0; i--)
    //                    *(--p) = negativeSign[i];
    //            }

    //            formatter.Append(p, (int)(buffer + bufferLength - p));
    //        }

    //        static void UInt64ToDecStr(StringBuffer formatter, ulong value, int digits)
    //        {
    //            if (digits < 1)
    //                digits = 1;

    //            var buffer = stackalloc char[100];
    //            var p = buffer + 100;
    //            while (High32(value) != 0)
    //            {
    //                p = Int32ToDecChars(p, Int64DivMod(ref value), 9);
    //                digits -= 9;
    //            }

    //            p = Int32ToDecChars(p, Low32(value), digits);
    //            formatter.Append(p, (int)(buffer + 100 - p));
    //        }

    //        static void Int64ToHexStr(StringBuffer formatter, ulong value, int hexBase, int digits)
    //        {
    //            var buffer = stackalloc char[100];
    //            char* ptr;
    //            if (High32(value) != 0)
    //            {
    //                Int32ToHexChars(buffer + 100, Low32(value), hexBase, 8);
    //                ptr = Int32ToHexChars(buffer + 100 - 8, High32(value), hexBase, digits - 8);
    //            }
    //            else
    //            {
    //                if (digits < 1)
    //                    digits = 1;
    //                ptr = Int32ToHexChars(buffer + 100, Low32(value), hexBase, digits);
    //            }

    //            formatter.Append(ptr, (int)(buffer + 100 - ptr));
    //        }

    //        static char* Int32ToDecChars(char* p, uint value, int digits)
    //        {
    //            while (value != 0)
    //            {
    //                *--p = (char)(value % 10 + '0');
    //                value /= 10;
    //                digits--;
    //            }

    //            while (--digits >= 0)
    //                *--p = '0';
    //            return p;
    //        }

    //        static char* Int32ToHexChars(char* p, uint value, int hexBase, int digits)
    //        {
    //            while (--digits >= 0 || value != 0)
    //            {
    //                var digit = value & 0xF;
    //                *--p = (char)(digit + (digit < 10 ? '0' : hexBase));
    //                value >>= 4;
    //            }
    //            return p;
    //        }

    //        static char ParseFormatSpecifier(StringView specifier, out int digits)
    //        {
    //            if (specifier.IsEmpty)
    //            {
    //                digits = -1;
    //                return 'G';
    //            }

    //            char* curr = specifier.Data;
    //            char first = *curr++;
    //            if ((first >= 'A' && first <= 'Z') || (first >= 'a' && first <= 'z'))
    //            {
    //                int n = -1;
    //                char c = *curr++;
    //                if (c >= '0' && c <= '9')
    //                {
    //                    n = c - '0';
    //                    c = *curr++;
    //                    while (c >= '0' && c <= '9')
    //                    {
    //                        n = n * 10 + c - '0';
    //                        c = *curr++;
    //                        if (n >= 10)
    //                            break;
    //                    }
    //                }

    //                if (c == 0)
    //                {
    //                    digits = n;
    //                    return first;
    //                }
    //            }

    //            digits = -1;
    //            return (char)0;
    //        }

    //        static void Int32ToNumber(int value, ref Number number)
    //        {
    //            number.Precision = Int32Precision;
    //            if (value >= 0)
    //                number.Sign = 0;
    //            else
    //            {
    //                number.Sign = 1;
    //                value = -value;
    //            }

    //            var buffer = stackalloc char[Int32Precision + 1];
    //            var ptr = Int32ToDecChars(buffer + Int32Precision, (uint)value, 0);
    //            var len = (int)(buffer + Int32Precision - ptr);
    //            number.Scale = len;

    //            var dest = number.Digits;
    //            while (--len >= 0)
    //                *dest++ = *ptr++;
    //            *dest = '\0';
    //        }

    //        static void UInt32ToNumber(uint value, ref Number number)
    //        {
    //            number.Precision = UInt32Precision;
    //            number.Sign = 0;

    //            var buffer = stackalloc char[UInt32Precision + 1];
    //            var ptr = Int32ToDecChars(buffer + UInt32Precision, value, 0);
    //            var len = (int)(buffer + UInt32Precision - ptr);
    //            number.Scale = len;

    //            var dest = number.Digits;
    //            while (--len >= 0)
    //                *dest++ = *ptr++;
    //            *dest = '\0';
    //        }

    //        static void Int64ToNumber(long value, ref Number number)
    //        {
    //            number.Precision = Int64Precision;
    //            if (value >= 0)
    //                number.Sign = 0;
    //            else
    //            {
    //                number.Sign = 1;
    //                value = -value;
    //            }

    //            var buffer = stackalloc char[Int64Precision + 1];
    //            var ptr = buffer + Int64Precision;
    //            var uv = (ulong)value;
    //            while (High32(uv) != 0)
    //                ptr = Int32ToDecChars(ptr, Int64DivMod(ref uv), 9);

    //            ptr = Int32ToDecChars(ptr, Low32(uv), 0);
    //            var len = (int)(buffer + Int64Precision - ptr);
    //            number.Scale = len;

    //            var dest = number.Digits;
    //            while (--len >= 0)
    //                *dest++ = *ptr++;
    //            *dest = '\0';
    //        }

    //        static void UInt64ToNumber(ulong value, ref Number number)
    //        {
    //            number.Precision = UInt64Precision;
    //            number.Sign = 0;

    //            var buffer = stackalloc char[UInt64Precision + 1];
    //            var ptr = buffer + UInt64Precision;
    //            while (High32(value) != 0)
    //                ptr = Int32ToDecChars(ptr, Int64DivMod(ref value), 9);

    //            ptr = Int32ToDecChars(ptr, Low32(value), 0);

    //            var len = (int)(buffer + UInt64Precision - ptr);
    //            number.Scale = len;

    //            var dest = number.Digits;
    //            while (--len >= 0)
    //                *dest++ = *ptr++;
    //            *dest = '\0';
    //        }

    //        static void DoubleToNumber(double value, int precision, ref Number number)
    //        {
    //            number.Precision = precision;

    //            uint sign, exp, mantHi, mantLo;
    //            ExplodeDouble(value, out sign, out exp, out mantHi, out mantLo);

    //            if (exp == 0x7FF)
    //            {
    //                // special value handling (infinity and NaNs)
    //                number.Scale = (mantLo != 0 || mantHi != 0) ? ScaleNaN : ScaleInf;
    //                number.Sign = (int)sign;
    //                number.Digits[0] = '\0';
    //            }
    //            else
    //            {
    //                // convert the digits of the number to characters
    //                if (value < 0)
    //                {
    //                    number.Sign = 1;
    //                    value = -value;
    //                }

    //                var digits = number.Digits;
    //                var end = digits + MaxFloatingDigits;
    //                var p = end;
    //                var shift = 0;
    //                double intPart;
    //                double reducedInt;
    //                var fracPart = ModF(value, out intPart);

    //                if (intPart != 0)
    //                {
    //                    // format the integer part
    //                    while (intPart != 0)
    //                    {
    //                        reducedInt = ModF(intPart / 10, out intPart);
    //                        *--p = (char)((int)((reducedInt + 0.03) * 10) + '0');
    //                        shift++;
    //                    }
    //                    while (p < end)
    //                        *digits++ = *p++;
    //                }
    //                else if (fracPart > 0)
    //                {
    //                    // normalize the fractional part
    //                    while ((reducedInt = fracPart * 10) < 1)
    //                    {
    //                        fracPart = reducedInt;
    //                        shift--;
    //                    }
    //                }

    //                // concat the fractional part, padding the remainder with zeros
    //                p = number.Digits + precision;
    //                while (digits <= p && digits < end)
    //                {
    //                    fracPart *= 10;
    //                    fracPart = ModF(fracPart, out reducedInt);
    //                    *digits++ = (char)((int)reducedInt + '0');
    //                }

    //                // round the result if necessary
    //                digits = p;
    //                *p = (char)(*p + 5);
    //                while (*p > '9')
    //                {
    //                    *p = '0';
    //                    if (p > number.Digits)
    //                        ++*--p;
    //                    else
    //                    {
    //                        *p = '1';
    //                        shift++;
    //                    }
    //                }

    //                number.Scale = shift;
    //                *digits = '\0';
    //            }
    //        }

    //        static void DecimalToNumber(uint* value, ref Number number)
    //        {
    //            // bit 31 of the decimal is the sign bit
    //            // bits 16-23 contain the scale
    //            number.Sign = (int)(*value >> 31);
    //            number.Scale = (int)((*value >> 16) & 0xFF);
    //            number.Precision = DecimalPrecision;

    //            // loop for as long as the decimal is larger than 32 bits
    //            var buffer = stackalloc char[DecimalPrecision + 1];
    //            var p = buffer + DecimalPrecision;
    //            var hi = *(value + 1);
    //            var lo = *(value + 2);
    //            var mid = *(value + 3);

    //            while ((mid | hi) != 0)
    //            {
    //                // keep dividing down by one billion at a time
    //                ulong n = hi;
    //                hi = (uint)(n / OneBillion);
    //                n = (n % OneBillion) << 32 | mid;
    //                mid = (uint)(n / OneBillion);
    //                n = (n % OneBillion) << 32 | lo;
    //                lo = (uint)(n / OneBillion);

    //                // format this portion of the number
    //                p = Int32ToDecChars(p, (uint)(n % OneBillion), 9);
    //            }

    //            // finish off with the low 32-bits of the decimal, if anything is left over
    //            p = Int32ToDecChars(p, lo, 0);

    //            var len = (int)(buffer + DecimalPrecision - p);
    //            number.Scale = len - number.Scale;

    //            var dest = number.Digits;
    //            while (--len >= 0)
    //                *dest++ = *p++;
    //            *dest = '\0';
    //        }

    //        static void RoundNumber(ref Number number, int pos)
    //        {
    //            var digits = number.Digits;
    //            int i = 0;
    //            while (i < pos && digits[i] != 0) i++;
    //            if (i == pos && digits[i] >= '5')
    //            {
    //                while (i > 0 && digits[i - 1] == '9') i--;
    //                if (i > 0)
    //                    digits[i - 1]++;
    //                else
    //                {
    //                    number.Scale++;
    //                    digits[0] = '1';
    //                    i = 1;
    //                }
    //            }
    //            else
    //            {
    //                while (i > 0 && digits[i - 1] == '0')
    //                    i--;
    //            }

    //            if (i == 0)
    //            {
    //                number.Scale = 0;
    //                number.Sign = 0;
    //            }

    //            digits[i] = '\0';
    //        }

    //        static void AppendString(char** buffer, string value)
    //        {
    //            fixed (char* pinnedString = value)
    //            {
    //                var length = value.Length;
    //                for (var src = pinnedString; src < pinnedString + length; (*buffer)++, src++)
    //                    **buffer = *src;
    //            }
    //        }

    //        static int StrLen(char* str)
    //        {
    //            int count = 0;
    //            while (*str++ != 0)
    //                count++;

    //            return count;
    //        }

    //        static uint Int64DivMod(ref ulong value)
    //        {
    //            var rem = (uint)(value % 1000000000);
    //            value /= 1000000000;
    //            return rem;
    //        }

    //        static double ModF(double value, out double intPart)
    //        {
    //            intPart = Math.Truncate(value);
    //            return value - intPart;
    //        }

    //        static void ExplodeDouble(double value, out uint sign, out uint exp, out uint mantHi, out uint mantLo)
    //        {
    //            var bits = *(ulong*)&value;
    //            if (BitConverter.IsLittleEndian)
    //            {
    //                mantLo = (uint)(bits & 0xFFFFFFFF);         // bits 0 - 31
    //                mantHi = (uint)((bits >> 32) & 0xFFFFF);    // bits 32 - 51
    //                exp = (uint)((bits >> 52) & 0x7FF);         // bits 52 - 62
    //                sign = (uint)((bits >> 63) & 0x1);          // bit 63
    //            }
    //            else
    //            {
    //                sign = (uint)(bits & 0x1);                  // bit 0
    //                exp = (uint)((bits >> 1) & 0x7FF);          // bits 1 - 11
    //                mantHi = (uint)((bits >> 12) & 0xFFFFF);    // bits 12 - 31
    //                mantLo = (uint)(bits >> 32);                // bits 32 - 63
    //            }
    //        }

    //        static uint Low32(ulong value)
    //        {
    //            return (uint)value;
    //        }

    //        static uint High32(ulong value)
    //        {
    //            return (uint)((value & 0xFFFFFFFF00000000) >> 32);
    //        }

    //        struct Number
    //        {
    //            public int Precision;
    //            public int Scale;
    //            public int Sign;
    //            public char* Digits;

    //            // useful for debugging
    //            public override string ToString()
    //            {
    //                return new string(Digits);
    //            }
    //        }

    //        const int MaxNumberDigits = 50;
    //        const int MaxFloatingDigits = 352;
    //        const int Int32Precision = 10;
    //        const int UInt32Precision = 10;
    //        const int Int64Precision = 19;
    //        const int UInt64Precision = 20;
    //        const int FloatPrecision = 7;
    //        const int DoublePrecision = 15;
    //        const int DecimalPrecision = 29;
    //        const int ScaleNaN = unchecked((int)0x80000000);
    //        const int ScaleInf = 0x7FFFFFFF;
    //        const int OneBillion = 1000000000;
    //    }

    //    // currently just contains some hardcoded exception messages
    //    static class SR
    //    {
    //        public const string InvalidGroupSizes = "Invalid group sizes in NumberFormatInfo.";
    //        public const string UnknownFormatSpecifier = "Unknown format specifier '{0}'.";
    //        public const string ArgIndexOutOfRange = "No format argument exists for index '{0}'.";
    //        public const string TypeNotFormattable = "Type '{0}' is not a built-in type, does not implement IStringFormattable, and no custom formatter was found for it.";
    //        public const string InvalidFormatString = "Invalid format string.";
    //    }

    //    /// <summary>
    //    /// A low-allocation version of the built-in <see cref="StringBuilder"/> type.
    //    /// </summary>
    //    partial class StringBuffer
    //    {
    //        /// <summary>
    //        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance. Each format item is replaced by the string representation of a single argument.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>   
    //        public void AppendFormat<T0>(string format, T0 arg0)
    //        {
    //            var args = new Arg1<T0>(arg0);
    //            AppendArgSet(format, ref args);
    //        }

    //        /// <summary>
    //        /// Converts the value of objects to strings based on the formats specified and inserts them into another string.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>   
    //        public static string Format<T0>(string format, T0 arg0)
    //        {
    //            var buffer = Acquire(format.Length + 8);
    //            buffer.AppendFormat(format, arg0);
    //            var result = buffer.ToString();
    //            Release(buffer);
    //            return result;
    //        }

    //        /// <summary>
    //        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance. Each format item is replaced by the string representation of a single argument.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>   
    //        public void AppendFormat<T0, T1>(string format, T0 arg0, T1 arg1)
    //        {
    //            var args = new Arg2<T0, T1>(arg0, arg1);
    //            AppendArgSet(format, ref args);
    //        }

    //        /// <summary>
    //        /// Converts the value of objects to strings based on the formats specified and inserts them into another string.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>   
    //        public static string Format<T0, T1>(string format, T0 arg0, T1 arg1)
    //        {
    //            var buffer = Acquire(format.Length + 16);
    //            buffer.AppendFormat(format, arg0, arg1);
    //            var result = buffer.ToString();
    //            Release(buffer);
    //            return result;
    //        }

    //        /// <summary>
    //        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance. Each format item is replaced by the string representation of a single argument.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>   
    //        public void AppendFormat<T0, T1, T2>(string format, T0 arg0, T1 arg1, T2 arg2)
    //        {
    //            var args = new Arg3<T0, T1, T2>(arg0, arg1, arg2);
    //            AppendArgSet(format, ref args);
    //        }

    //        /// <summary>
    //        /// Converts the value of objects to strings based on the formats specified and inserts them into another string.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>   
    //        public static string Format<T0, T1, T2>(string format, T0 arg0, T1 arg1, T2 arg2)
    //        {
    //            var buffer = Acquire(format.Length + 24);
    //            buffer.AppendFormat(format, arg0, arg1, arg2);
    //            var result = buffer.ToString();
    //            Release(buffer);
    //            return result;
    //        }

    //        /// <summary>
    //        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance. Each format item is replaced by the string representation of a single argument.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>
    //        /// <param name="arg3">A value to format.</param>   
    //        public void AppendFormat<T0, T1, T2, T3>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    //        {
    //            var args = new Arg4<T0, T1, T2, T3>(arg0, arg1, arg2, arg3);
    //            AppendArgSet(format, ref args);
    //        }

    //        /// <summary>
    //        /// Converts the value of objects to strings based on the formats specified and inserts them into another string.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>
    //        /// <param name="arg3">A value to format.</param>   
    //        public static string Format<T0, T1, T2, T3>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    //        {
    //            var buffer = Acquire(format.Length + 32);
    //            buffer.AppendFormat(format, arg0, arg1, arg2, arg3);
    //            var result = buffer.ToString();
    //            Release(buffer);
    //            return result;
    //        }

    //        /// <summary>
    //        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance. Each format item is replaced by the string representation of a single argument.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>
    //        /// <param name="arg3">A value to format.</param>
    //        /// <param name="arg4">A value to format.</param>   
    //        public void AppendFormat<T0, T1, T2, T3, T4>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    //        {
    //            var args = new Arg5<T0, T1, T2, T3, T4>(arg0, arg1, arg2, arg3, arg4);
    //            AppendArgSet(format, ref args);
    //        }

    //        /// <summary>
    //        /// Converts the value of objects to strings based on the formats specified and inserts them into another string.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>
    //        /// <param name="arg3">A value to format.</param>
    //        /// <param name="arg4">A value to format.</param>   
    //        public static string Format<T0, T1, T2, T3, T4>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    //        {
    //            var buffer = Acquire(format.Length + 40);
    //            buffer.AppendFormat(format, arg0, arg1, arg2, arg3, arg4);
    //            var result = buffer.ToString();
    //            Release(buffer);
    //            return result;
    //        }

    //        /// <summary>
    //        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance. Each format item is replaced by the string representation of a single argument.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>
    //        /// <param name="arg3">A value to format.</param>
    //        /// <param name="arg4">A value to format.</param>
    //        /// <param name="arg5">A value to format.</param>   
    //        public void AppendFormat<T0, T1, T2, T3, T4, T5>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    //        {
    //            var args = new Arg6<T0, T1, T2, T3, T4, T5>(arg0, arg1, arg2, arg3, arg4, arg5);
    //            AppendArgSet(format, ref args);
    //        }

    //        /// <summary>
    //        /// Converts the value of objects to strings based on the formats specified and inserts them into another string.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>
    //        /// <param name="arg3">A value to format.</param>
    //        /// <param name="arg4">A value to format.</param>
    //        /// <param name="arg5">A value to format.</param>   
    //        public static string Format<T0, T1, T2, T3, T4, T5>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    //        {
    //            var buffer = Acquire(format.Length + 48);
    //            buffer.AppendFormat(format, arg0, arg1, arg2, arg3, arg4, arg5);
    //            var result = buffer.ToString();
    //            Release(buffer);
    //            return result;
    //        }

    //        /// <summary>
    //        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance. Each format item is replaced by the string representation of a single argument.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>
    //        /// <param name="arg3">A value to format.</param>
    //        /// <param name="arg4">A value to format.</param>
    //        /// <param name="arg5">A value to format.</param>
    //        /// <param name="arg6">A value to format.</param>   
    //        public void AppendFormat<T0, T1, T2, T3, T4, T5, T6>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    //        {
    //            var args = new Arg7<T0, T1, T2, T3, T4, T5, T6>(arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    //            AppendArgSet(format, ref args);
    //        }

    //        /// <summary>
    //        /// Converts the value of objects to strings based on the formats specified and inserts them into another string.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>
    //        /// <param name="arg3">A value to format.</param>
    //        /// <param name="arg4">A value to format.</param>
    //        /// <param name="arg5">A value to format.</param>
    //        /// <param name="arg6">A value to format.</param>   
    //        public static string Format<T0, T1, T2, T3, T4, T5, T6>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    //        {
    //            var buffer = Acquire(format.Length + 56);
    //            buffer.AppendFormat(format, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    //            var result = buffer.ToString();
    //            Release(buffer);
    //            return result;
    //        }

    //        /// <summary>
    //        /// Appends the string returned by processing a composite format string, which contains zero or more format items, to this instance. Each format item is replaced by the string representation of a single argument.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>
    //        /// <param name="arg3">A value to format.</param>
    //        /// <param name="arg4">A value to format.</param>
    //        /// <param name="arg5">A value to format.</param>
    //        /// <param name="arg6">A value to format.</param>
    //        /// <param name="arg7">A value to format.</param>   
    //        public void AppendFormat<T0, T1, T2, T3, T4, T5, T6, T7>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    //        {
    //            var args = new Arg8<T0, T1, T2, T3, T4, T5, T6, T7>(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    //            AppendArgSet(format, ref args);
    //        }

    //        /// <summary>
    //        /// Converts the value of objects to strings based on the formats specified and inserts them into another string.
    //        /// </summary>
    //        /// <param name="format">A composite format string.</param>
    //        /// <param name="arg0">A value to format.</param>
    //        /// <param name="arg1">A value to format.</param>
    //        /// <param name="arg2">A value to format.</param>
    //        /// <param name="arg3">A value to format.</param>
    //        /// <param name="arg4">A value to format.</param>
    //        /// <param name="arg5">A value to format.</param>
    //        /// <param name="arg6">A value to format.</param>
    //        /// <param name="arg7">A value to format.</param>   
    //        public static string Format<T0, T1, T2, T3, T4, T5, T6, T7>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    //        {
    //            var buffer = Acquire(format.Length + 64);
    //            buffer.AppendFormat(format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
    //            var result = buffer.ToString();
    //            Release(buffer);
    //            return result;
    //        }
    //    }

    //    unsafe struct Arg1<T0> : IArgSet
    //    {
    //        T0 t0;

    //        public int Count => 1;

    //        public Arg1(T0 t0)
    //        {
    //            this.t0 = t0;
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        public void Format(StringBuffer buffer, int index, StringView format)
    //        {
    //            switch (index)
    //            {
    //                case 0: buffer.AppendGeneric(t0, format); break;
    //            }
    //        }
    //    }

    //    unsafe struct Arg2<T0, T1> : IArgSet
    //    {
    //        T0 t0;
    //        T1 t1;

    //        public int Count => 2;

    //        public Arg2(T0 t0, T1 t1)
    //        {
    //            this.t0 = t0;
    //            this.t1 = t1;
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        public void Format(StringBuffer buffer, int index, StringView format)
    //        {
    //            switch (index)
    //            {
    //                case 0: buffer.AppendGeneric(t0, format); break;
    //                case 1: buffer.AppendGeneric(t1, format); break;
    //            }
    //        }
    //    }

    //    unsafe struct Arg3<T0, T1, T2> : IArgSet
    //    {
    //        T0 t0;
    //        T1 t1;
    //        T2 t2;

    //        public int Count => 3;

    //        public Arg3(T0 t0, T1 t1, T2 t2)
    //        {
    //            this.t0 = t0;
    //            this.t1 = t1;
    //            this.t2 = t2;
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        public void Format(StringBuffer buffer, int index, StringView format)
    //        {
    //            switch (index)
    //            {
    //                case 0: buffer.AppendGeneric(t0, format); break;
    //                case 1: buffer.AppendGeneric(t1, format); break;
    //                case 2: buffer.AppendGeneric(t2, format); break;
    //            }
    //        }
    //    }

    //    unsafe struct Arg4<T0, T1, T2, T3> : IArgSet
    //    {
    //        T0 t0;
    //        T1 t1;
    //        T2 t2;
    //        T3 t3;

    //        public int Count => 4;

    //        public Arg4(T0 t0, T1 t1, T2 t2, T3 t3)
    //        {
    //            this.t0 = t0;
    //            this.t1 = t1;
    //            this.t2 = t2;
    //            this.t3 = t3;
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        public void Format(StringBuffer buffer, int index, StringView format)
    //        {
    //            switch (index)
    //            {
    //                case 0: buffer.AppendGeneric(t0, format); break;
    //                case 1: buffer.AppendGeneric(t1, format); break;
    //                case 2: buffer.AppendGeneric(t2, format); break;
    //                case 3: buffer.AppendGeneric(t3, format); break;
    //            }
    //        }
    //    }

    //    unsafe struct Arg5<T0, T1, T2, T3, T4> : IArgSet
    //    {
    //        T0 t0;
    //        T1 t1;
    //        T2 t2;
    //        T3 t3;
    //        T4 t4;

    //        public int Count => 5;

    //        public Arg5(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4)
    //        {
    //            this.t0 = t0;
    //            this.t1 = t1;
    //            this.t2 = t2;
    //            this.t3 = t3;
    //            this.t4 = t4;
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        public void Format(StringBuffer buffer, int index, StringView format)
    //        {
    //            switch (index)
    //            {
    //                case 0: buffer.AppendGeneric(t0, format); break;
    //                case 1: buffer.AppendGeneric(t1, format); break;
    //                case 2: buffer.AppendGeneric(t2, format); break;
    //                case 3: buffer.AppendGeneric(t3, format); break;
    //                case 4: buffer.AppendGeneric(t4, format); break;
    //            }
    //        }
    //    }

    //    unsafe struct Arg6<T0, T1, T2, T3, T4, T5> : IArgSet
    //    {
    //        T0 t0;
    //        T1 t1;
    //        T2 t2;
    //        T3 t3;
    //        T4 t4;
    //        T5 t5;

    //        public int Count => 6;

    //        public Arg6(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
    //        {
    //            this.t0 = t0;
    //            this.t1 = t1;
    //            this.t2 = t2;
    //            this.t3 = t3;
    //            this.t4 = t4;
    //            this.t5 = t5;
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        public void Format(StringBuffer buffer, int index, StringView format)
    //        {
    //            switch (index)
    //            {
    //                case 0: buffer.AppendGeneric(t0, format); break;
    //                case 1: buffer.AppendGeneric(t1, format); break;
    //                case 2: buffer.AppendGeneric(t2, format); break;
    //                case 3: buffer.AppendGeneric(t3, format); break;
    //                case 4: buffer.AppendGeneric(t4, format); break;
    //                case 5: buffer.AppendGeneric(t5, format); break;
    //            }
    //        }
    //    }

    //    unsafe struct Arg7<T0, T1, T2, T3, T4, T5, T6> : IArgSet
    //    {
    //        T0 t0;
    //        T1 t1;
    //        T2 t2;
    //        T3 t3;
    //        T4 t4;
    //        T5 t5;
    //        T6 t6;

    //        public int Count => 7;

    //        public Arg7(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
    //        {
    //            this.t0 = t0;
    //            this.t1 = t1;
    //            this.t2 = t2;
    //            this.t3 = t3;
    //            this.t4 = t4;
    //            this.t5 = t5;
    //            this.t6 = t6;
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        public void Format(StringBuffer buffer, int index, StringView format)
    //        {
    //            switch (index)
    //            {
    //                case 0: buffer.AppendGeneric(t0, format); break;
    //                case 1: buffer.AppendGeneric(t1, format); break;
    //                case 2: buffer.AppendGeneric(t2, format); break;
    //                case 3: buffer.AppendGeneric(t3, format); break;
    //                case 4: buffer.AppendGeneric(t4, format); break;
    //                case 5: buffer.AppendGeneric(t5, format); break;
    //                case 6: buffer.AppendGeneric(t6, format); break;
    //            }
    //        }
    //    }

    //    unsafe struct Arg8<T0, T1, T2, T3, T4, T5, T6, T7> : IArgSet
    //    {
    //        T0 t0;
    //        T1 t1;
    //        T2 t2;
    //        T3 t3;
    //        T4 t4;
    //        T5 t5;
    //        T6 t6;
    //        T7 t7;

    //        public int Count => 8;

    //        public Arg8(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
    //        {
    //            this.t0 = t0;
    //            this.t1 = t1;
    //            this.t2 = t2;
    //            this.t3 = t3;
    //            this.t4 = t4;
    //            this.t5 = t5;
    //            this.t6 = t6;
    //            this.t7 = t7;
    //        }

    //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //        public void Format(StringBuffer buffer, int index, StringView format)
    //        {
    //            switch (index)
    //            {
    //                case 0: buffer.AppendGeneric(t0, format); break;
    //                case 1: buffer.AppendGeneric(t1, format); break;
    //                case 2: buffer.AppendGeneric(t2, format); break;
    //                case 3: buffer.AppendGeneric(t3, format); break;
    //                case 4: buffer.AppendGeneric(t4, format); break;
    //                case 5: buffer.AppendGeneric(t5, format); break;
    //                case 6: buffer.AppendGeneric(t6, format); break;
    //                case 7: buffer.AppendGeneric(t7, format); break;
    //            }
    //        }
    //    }
    //}
}