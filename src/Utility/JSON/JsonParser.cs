using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace TinyJson
{

	public class JsonParser : IDisposable {

		enum Token { None, CurlyOpen, CurlyClose, SquareOpen, SquareClose, Colon, Comma, String, Number, BoolOrNull };

		StringReader json;

		// Temporary allocated
		StringBuilder sb = new StringBuilder();

		public static object ParseValue(string jsonString) {
			using (JsonParser parser = new JsonParser(jsonString)) {
				return parser.ParseValue();
			}
		}

		internal JsonParser(string jsonString) {
			json = new StringReader(jsonString);
		}

		public void Dispose() {
			json.Dispose();
			json = null;
		}

		//** Reading Token **//

		bool EndReached() {
			return json.Peek() == -1;
		}

		bool PeekWordbreak() {
			char c = PeekChar();
			return c == ' ' || c == ',' || c == ':' || c == '\"' || c == '{' || c == '}' || c == '[' || c == ']' || c == '\t' || c == '\n' || c == '\r';
		}

		bool PeekWhitespace() {
			char c = PeekChar();
			return c == ' ' || c == '\t' || c == '\n' || c == '\r';
		}

		char PeekChar() {
			return Convert.ToChar(json.Peek());
		}
		
		char ReadChar() {
			return Convert.ToChar(json.Read());
		}

		string ReadWord() {
			sb.Clear();
			while (!PeekWordbreak() && !EndReached()) {
				sb.Append(ReadChar());
			}
			return EndReached() ? null : sb.ToString();
		}

		void EatWhitespace() {
			while (PeekWhitespace()) {
				json.Read();
			}
		}

		Token PeekToken() {
			EatWhitespace();
			if (EndReached()) return Token.None;
			switch (PeekChar()) {
				case '{':
					return Token.CurlyOpen;
				case '}':
					return Token.CurlyClose;
				case '[':
					return Token.SquareOpen;
				case ']':
					return Token.SquareClose;
				case ',':
					return Token.Comma;
				case '"':
					return Token.String;
				case ':':
					return Token.Colon;
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
				case '-':
					return Token.Number;
				case 't':
				case 'f':
				case 'n':
					return Token.BoolOrNull;
				default:
					return Token.None;
			}
		}
	
		//** Parsing Parts **//

		object ParseBoolOrNull() {
			if (PeekToken() == Token.BoolOrNull) {
				string boolValue = ReadWord();
				if (boolValue == "true") return true;
				if (boolValue == "false") return false;
				if (boolValue == "null") return null;
				Console.WriteLine("Unexpected bool value: " + boolValue);
				return null;
			} else {
				Console.WriteLine("Unexpected bool token: " + PeekToken());
				return null;
			}
		}

		object ParseNumber() {
			if (PeekToken() == Token.Number) {
				string number = ReadWord();
				if (number.Contains(".")) {
					double parsed;
					if (Double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) return parsed;
				} else { 
					long parsed;
					if (Int64.TryParse(number, out parsed)) return parsed;
				}
				Console.WriteLine("Unexpected number value: " + number);
				return null;
			} else {
				Console.WriteLine("Unexpected number token: " + PeekToken());
				return null;
			}
		}

		string ParseString() {
			if (PeekToken() == Token.String) {
				ReadChar(); // ditch opening quote

				sb.Clear();
				char c;
				while (true) {
					if (EndReached()) return null;
					
					c = ReadChar();
					switch (c) {
						case '"':
							return sb.ToString();
						case '\\':
							if (EndReached()) return null;
							
							c = ReadChar();
							switch (c) {
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
									string hex = String.Concat(ReadChar(), ReadChar(), ReadChar(), ReadChar());
									sb.Append((char) Convert.ToInt32(hex, 16));
									break;
							}
							break;
						default:
							sb.Append(c);
							break;
					}
				}
			} else {
				Console.WriteLine("Unexpected string token: " + PeekToken());
				return null;
			}
		}

		//** Parsing Objects **//

		Dictionary<string, object> ParseObject() {
			if (PeekToken() == Token.CurlyOpen) {
				json.Read(); // ditch opening brace

				Dictionary<string, object> table = new Dictionary<string, object>();
				while (true) {
					switch (PeekToken()) {
					case Token.None:
						return null;
					case Token.Comma:
						json.Read();
						continue;
					case Token.CurlyClose:
						json.Read();
						return table;
					default:
						string name = ParseString();
						if (string.IsNullOrEmpty(name)) return null;

						if (PeekToken() != Token.Colon) return null;
						json.Read(); // ditch the colon
						
						table[name] = ParseValue();
						break;
					}
				}
			} else {
				Console.WriteLine("Unexpected object token: " + PeekToken());
				return null;
			}
		}
		
		List<object> ParseArray() {
			if (PeekToken() == Token.SquareOpen) {
				json.Read(); // ditch opening brace

				List<object> array = new List<object>();				
				while (true) {
					switch (PeekToken()) {
					case Token.None:			
						return null;
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
			} else {
				Console.WriteLine("Unexpected array token: " + PeekToken());
				return null;
			}
		}

		object ParseValue() {
			switch (PeekToken()) {
			case Token.String:		
				return ParseString();
			case Token.Number:		
				return ParseNumber();
			case Token.BoolOrNull:		
				return ParseBoolOrNull();
			case Token.CurlyOpen:	
				return ParseObject();
			case Token.SquareOpen:	
				return ParseArray();
			}
			Console.WriteLine("Unexpected value token: " + PeekToken());
			return null;
		}
	}
}

