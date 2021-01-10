#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections;
using System.Text;

namespace TinyJson
{
    public class JsonBuilder
    {
        private readonly StringBuilder builder = new StringBuilder();
        private int level;
        private readonly bool pretty;

        public JsonBuilder()
        {
        }

        public JsonBuilder(bool pretty)
        {
            this.pretty = pretty;
        }

        private void AppendPrettyLineBreak()
        {
            builder.Append("\n");

            for (int i = 0; i < level; i++)
            {
                builder.Append("\t");
            }
        }

        private bool HasPrettyLineBreak()
        {
            return builder.ToString().EndsWith("\t", StringComparison.InvariantCulture) || builder.ToString().EndsWith("\n", StringComparison.InvariantCulture);
        }

        private void RemovePrettyLineBreak()
        {
            while (HasPrettyLineBreak())
            {
                builder.Remove(builder.Length - 1, 1);
            }
        }

        public void AppendBeginObject()
        {
            level++;
            builder.Append("{");

            if (pretty)
            {
                AppendPrettyLineBreak();
            }
        }

        public void AppendEndObject()
        {
            level--;

            if (pretty)
            {
                RemovePrettyLineBreak();
            }

            if (pretty)
            {
                AppendPrettyLineBreak();
            }

            builder.Append("}");

            if (pretty)
            {
                AppendPrettyLineBreak();
            }
        }

        public void AppendBeginArray()
        {
            level++;
            builder.Append("[");

            if (pretty)
            {
                AppendPrettyLineBreak();
            }
        }

        public void AppendEndArray()
        {
            level--;

            if (pretty)
            {
                RemovePrettyLineBreak();
            }

            if (pretty)
            {
                AppendPrettyLineBreak();
            }

            builder.Append("]");

            if (pretty)
            {
                AppendPrettyLineBreak();
            }
        }

        public void AppendSeperator()
        {
            if (pretty)
            {
                RemovePrettyLineBreak();
            }

            builder.Append(",");

            if (pretty)
            {
                AppendPrettyLineBreak();
            }
        }

        public void AppendNull()
        {
            builder.Append("null");
        }

        public void AppendBool(bool b)
        {
            builder.Append(b ? "true" : "false");
        }

        public void AppendNumber(object number)
        {
            if (number != null)
            {
                string numberString = number.ToString();

                if (number.GetType().IsFloatingPoint())
                {
                    numberString = numberString.Replace(',', '.');

                    if (!numberString.Contains("."))
                    {
                        numberString += ".0";
                    }
                }

                builder.Append(numberString);
            }
            else
            {
                AppendNull();
            }
        }

        public void AppendString(string str)
        {
            if (str != null)
            {
                builder.Append('\"');

                foreach (char c in str)
                {
                    switch (c)
                    {
                        case '"':
                            builder.Append("\\\"");

                            break;

                        case '\\':
                            builder.Append("\\\\");

                            break;

                        case '\b':
                            builder.Append("\\b");

                            break;

                        case '\f':
                            builder.Append("\\f");

                            break;

                        case '\n':
                            builder.Append("\\n");

                            break;

                        case '\r':
                            builder.Append("\\r");

                            break;

                        case '\t':
                            builder.Append("\\t");

                            break;

                        default:
                            int codepoint = Convert.ToInt32(c);

                            if (pretty || codepoint >= 32 && codepoint <= 126)
                            {
                                builder.Append(c);
                            }
                            else
                            {
                                builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                            }

                            break;
                    }
                }

                builder.Append('\"');
            }
            else
            {
                AppendNull();
            }
        }

        public void AppendArray(IEnumerable enumerable)
        {
            if (enumerable != null)
            {
                AppendBeginArray();
                bool first = true;

                foreach (object item in enumerable)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        AppendSeperator();
                    }

                    AppendValue(item);
                }

                AppendEndArray();
            }
            else
            {
                AppendNull();
            }
        }

        public void AppendDictionary(IDictionary dict)
        {
            if (dict != null)
            {
                AppendBeginObject();
                bool first = true;

                foreach (DictionaryEntry entry in dict)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        AppendSeperator();
                    }

                    AppendString(entry.Key.ToString());
                    builder.Append(pretty ? " : " : ":");
                    AppendValue(entry.Value);
                }

                AppendEndObject();
            }
            else
            {
                AppendNull();
            }
        }

        public void AppendValue(object value)
        {
            if (value == null)
            {
                AppendNull();
            }
            else if (value is bool)
            {
                AppendBool((bool) value);
            }
            else if (value is string)
            {
                AppendString((string) value);
            }
            else if (value is char)
            {
                AppendString("" + value);
            }
            else if (IsEnum(value))
            {
                AppendNumber((int) value);
            }
            else if (IsNumber(value))
            {
                AppendNumber(value);
            }
            else
            {
                Console.WriteLine("Type " + value.GetType() + " not supported!");
            }
        }

        public void AppendName(string name)
        {
            AppendString(name);
            builder.Append(pretty ? " : " : ":");
        }

        internal static bool IsNumber(object value)
        {
            return value != null && value.GetType().IsNumeric();
        }

        internal static bool IsEnum(object value)
        {
            return value != null && value.GetType().IsEnum;
        }

        internal static bool IsSupported(object obj)
        {
            if (obj == null)
            {
                return true;
            }

            if (obj is bool)
            {
                return true;
            }

            if (obj is string)
            {
                return true;
            }

            if (obj is char)
            {
                return true;
            }

            if (IsEnum(obj))
            {
                return true;
            }

            if (IsNumber(obj))
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}