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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace TinyJson
{
    public class JsonParser : IDisposable
    {
        private StringReader json;

        // Temporary allocated
        private readonly StringBuilder sb = new StringBuilder();

        internal JsonParser(string jsonString)
        {
            json = new StringReader(jsonString);
        }

        public void Dispose()
        {
            json.Dispose();
            json = null;
        }

        public static object ParseValue(string jsonString)
        {
            using (JsonParser parser = new JsonParser(jsonString))
            {
                return parser.ParseValue();
            }
        }

        //** Reading Token **//

        private bool EndReached()
        {
            return json.Peek() == -1;
        }

        private bool PeekWordbreak()
        {
            char c = PeekChar();

            return c == ' ' || c == ',' || c == ':' || c == '\"' || c == '{' || c == '}' || c == '[' || c == ']' || c == '\t' || c == '\n' || c == '\r';
        }

        private bool PeekWhitespace()
        {
            char c = PeekChar();

            return c == ' ' || c == '\t' || c == '\n' || c == '\r';
        }

        private char PeekChar()
        {
            return Convert.ToChar(json.Peek());
        }

        private char ReadChar()
        {
            return Convert.ToChar(json.Read());
        }

        private string ReadWord()
        {
            sb.Clear();

            while (!PeekWordbreak() && !EndReached())
            {
                sb.Append(ReadChar());
            }

            return EndReached() ? null : sb.ToString();
        }

        private void EatWhitespace()
        {
            while (PeekWhitespace())
            {
                json.Read();
            }
        }

        private Token PeekToken()
        {
            EatWhitespace();

            if (EndReached())
            {
                return Token.None;
            }

            switch (PeekChar())
            {
                case '{': return Token.CurlyOpen;

                case '}': return Token.CurlyClose;

                case '[': return Token.SquareOpen;

                case ']': return Token.SquareClose;

                case ',': return Token.Comma;

                case '"': return Token.String;

                case ':': return Token.Colon;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-': return Token.Number;

                case 't':
                case 'f':
                case 'n': return Token.BoolOrNull;

                default: return Token.None;
            }
        }

        //** Parsing Parts **//

        private object ParseBoolOrNull()
        {
            if (PeekToken() == Token.BoolOrNull)
            {
                string boolValue = ReadWord();

                if (boolValue == "true")
                {
                    return true;
                }

                if (boolValue == "false")
                {
                    return false;
                }

                if (boolValue == "null")
                {
                    return null;
                }

                Console.WriteLine("Unexpected bool value: " + boolValue);

                return null;
            }

            Console.WriteLine("Unexpected bool token: " + PeekToken());

            return null;
        }

        private object ParseNumber()
        {
            if (PeekToken() == Token.Number)
            {
                string number = ReadWord();

                if (number.Contains("."))
                {
                    double parsed;

                    if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                    {
                        return parsed;
                    }
                }
                else
                {
                    long parsed;

                    if (long.TryParse(number, out parsed))
                    {
                        return parsed;
                    }
                }

                Console.WriteLine("Unexpected number value: " + number);

                return null;
            }

            Console.WriteLine("Unexpected number token: " + PeekToken());

            return null;
        }

        private string ParseString()
        {
            if (PeekToken() == Token.String)
            {
                ReadChar(); // ditch opening quote

                sb.Clear();
                char c;

                while (true)
                {
                    if (EndReached())
                    {
                        return null;
                    }

                    c = ReadChar();

                    switch (c)
                    {
                        case '"': return sb.ToString();

                        case '\\':
                            if (EndReached())
                            {
                                return null;
                            }

                            c = ReadChar();

                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    sb.Append(c);

                                    break;

                                case 'b':
                                    sb.Append('\b');

                                    break;

                                case 'f':
                                    sb.Append('\f');

                                    break;

                                case 'n':
                                    sb.Append('\n');

                                    break;

                                case 'r':
                                    sb.Append('\r');

                                    break;

                                case 't':
                                    sb.Append('\t');

                                    break;

                                case 'u':
                                    string hex = string.Concat(ReadChar(), ReadChar(), ReadChar(), ReadChar());
                                    sb.Append((char) Convert.ToInt32(hex, 16));

                                    break;
                            }

                            break;

                        default:
                            sb.Append(c);

                            break;
                    }
                }
            }

            Console.WriteLine("Unexpected string token: " + PeekToken());

            return null;
        }

        //** Parsing Objects **//

        private Dictionary<string, object> ParseObject()
        {
            if (PeekToken() == Token.CurlyOpen)
            {
                json.Read(); // ditch opening brace

                Dictionary<string, object> table = new Dictionary<string, object>();

                while (true)
                {
                    switch (PeekToken())
                    {
                        case Token.None: return null;

                        case Token.Comma:
                            json.Read();

                            continue;

                        case Token.CurlyClose:
                            json.Read();

                            return table;

                        default:
                            string name = ParseString();

                            if (string.IsNullOrEmpty(name))
                            {
                                return null;
                            }

                            if (PeekToken() != Token.Colon)
                            {
                                return null;
                            }

                            json.Read(); // ditch the colon

                            table[name] = ParseValue();

                            break;
                    }
                }
            }

            Console.WriteLine("Unexpected object token: " + PeekToken());

            return null;
        }

        private List<object> ParseArray()
        {
            if (PeekToken() == Token.SquareOpen)
            {
                json.Read(); // ditch opening brace

                List<object> array = new List<object>();

                while (true)
                {
                    switch (PeekToken())
                    {
                        case Token.None: return null;

                        case Token.Comma:
                            json.Read();

                            continue;

                        case Token.SquareClose:
                            json.Read();

                            return array;

                        default:
                            array.Add(ParseValue());

                            break;
                    }
                }
            }

            Console.WriteLine("Unexpected array token: " + PeekToken());

            return null;
        }

        private object ParseValue()
        {
            switch (PeekToken())
            {
                case Token.String: return ParseString();

                case Token.Number: return ParseNumber();

                case Token.BoolOrNull: return ParseBoolOrNull();

                case Token.CurlyOpen: return ParseObject();

                case Token.SquareOpen: return ParseArray();
            }

            Console.WriteLine("Unexpected value token: " + PeekToken());

            return null;
        }

        private enum Token
        {
            None,
            CurlyOpen,
            CurlyClose,
            SquareOpen,
            SquareClose,
            Colon,
            Comma,
            String,
            Number,
            BoolOrNull
        }
    }
}