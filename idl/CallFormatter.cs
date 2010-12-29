
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;

namespace IMVU.IDL
{
	public abstract class CallFormatter
	{
		public CallFormatter()
		{
		}
		
		public abstract IMVU.IDL.Buffer Format(dict val);
	}
	
	public class JSONFormatter : CallFormatter
	{
		public JSONFormatter()
		{
		}
		
		public static JSONFormatter Instance = new JSONFormatter();
		
		public override IMVU.IDL.Buffer Format(dict val)
		{
			StringBuilder sb = new StringBuilder();
			FormatObj(val, sb);
			byte[] buf = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
			IMVU.IDL.Buffer ret = new IMVU.IDL.Buffer(buf, 0, buf.Length);
			return ret;
		}
		
		public string FormatAny(object obj)
		{
			StringBuilder sb = new StringBuilder();
			FormatAny(obj, sb);
			return sb.ToString();
		}
		
		public dict Parse(IMVU.IDL.Buffer buf)
		{
			if (buf.data[buf.offset] != '{')
			{
				throw new ArgumentException("buf is not a proper JSON object");
			}
			int offset = buf.offset + 1;
			return ParseObj(buf.data, ref offset, buf.offset + buf.length);
		}
		
		static bool IsWhitespace(byte b)
		{
			return b == 9 || b == 10 || b == 13 || b == 32;
		}
		
		static int SkipWhitespace(byte[] data, int offset, int end)
		{
			while (offset < end && IsWhitespace(data[offset]))
			{
				++offset;
			}
			if (offset >= end)
			{
				throw new ArgumentException("truncated JSON data found");
			}
			return offset;
		}
		
		static dict ParseObj(byte[] data, ref int off, int end)
		{
			int offset = off;
			dict ret = new dict();
			while (true)
			{
				offset = SkipWhitespace(data, offset, end);
				if (data[offset] == '}')
				{
					++offset;
					break;
				}
				string key = ParseStr(data, ref offset, end);
				offset = SkipWhitespace(data, offset, end);
				if (data[offset] != ':')
				{
					throw new ArgumentException("malformed JSON found; expected ':' after key {0}", key);
				}
				offset += 1;
				object val = ParseAny(data, ref offset, end);
				ret.Add(key, val);
				offset = SkipWhitespace(data, offset, end);
				if (data[offset] == '}')
				{
					++offset;
					break;
				}
				if (data[offset] != ',')
				{
					throw new ArgumentException("malformed JSON found; expected ',' after value after key {0}", key);
				}
			}
			off = offset;
			//	todo: if this is at the outer level, then any data after the first 
			//	correct form will be ignored.
			return ret;
		}
		
		public object ParseAny(string s)
		{
			byte[] data = Encoding.UTF8.GetBytes(s);
			int offset = 0;
			return ParseAny(data, ref offset, data.Length);
		}
		
		static object ParseAny(byte[] data, ref int off, int end)
		{
			int offset = SkipWhitespace(data, off, end);
			object ret = null;
			if (data[offset] == (byte)'[')
			{
				ret = ParseList(data, ref offset, end);
			}
			else if (data[offset] == (byte)'{')
			{
				offset += 1;
				ret = ParseObj(data, ref offset, end);
			}
			else if (data[offset] == (byte)'"')
			{
				ret = ParseStr(data, ref offset, end);
			}
			else if (data[offset] == 't')
			{
				ParseExpect(data, ref offset, end, "true");
				ret = true;
			}
			else if (data[offset] == 'f')
			{
				ParseExpect(data, ref offset, end, "false");
				ret = false;
			}
			else
			{
				ret = ParseNumber(data, ref offset, end);
			}
			off = offset;
			return ret;
		}
		
		static List<object> ParseList(byte[] data, ref int off, int end)
		{
			int offset = SkipWhitespace(data, off, end);
			List<Object> ret = new List<Object>();
			while (true)
			{
				if (data[off] == (byte)']')
				{
					break;
				}
				object obj = ParseAny(data, ref offset, end);
				ret.Add(obj);
				offset = SkipWhitespace(data, offset, end);
				if (data[off] != (byte)',')
				{
					if (data[off] != (byte)']')
					{
						throw new ArgumentException("Bad JSON format: expected ',' or ']' in list");
					}
					break;
				}
				else
				{
					offset = SkipWhitespace(data, offset + 1, end);
				}
			}
			off = offset + 1;
			return ret;
		}
		
		static string ParseStr(byte[] data, ref int offset, int end)
		{
			int off = offset;
			Debug.Assert(data[0] == (byte)'"');
			++off;
			StringBuilder sb = new StringBuilder();
			int prev = off;
			while (data[off] != (byte)'"')
			{
				if (data[off] == (byte)'\\')
				{
					if (prev < off)
					{
						sb.Append(Encoding.UTF8.GetString(data, prev, off - prev));
					}
					++off;
					switch (data[off])
					{
					case (byte)'n':
						sb.Append('\n');
						break;
					case (byte)'r':
						sb.Append('\r');
						break;
					case (byte)'u':
					case (byte)'U':
						if (off + 4 >= end)
						{
							throw new ArgumentException("Bad hex data after u escape in string");
						}
						sb.Append((char)Helpers.HexToInt(Encoding.UTF8.GetString(data, off+1, 4)));
						off += 4;
						break;
					default:
						sb.Append((char)data[off]);
						break;
					}
					prev = off + 1;
				}
				++off;
			}
			if (prev < off)
			{
				sb.Append(Encoding.UTF8.GetString(data, prev, off - prev));
			}
			Debug.Assert(data[off] == (byte)'"');
			++off;
			offset = off;
			return sb.ToString();
		}
		
		static void ParseExpect(byte[] data, ref int off, int end, string token)
		{
			foreach (char ch in token)
			{
				if (off >= end || data[off] != (byte)ch)
				{
					throw new ArgumentException("Expected " + token + " in input");
				}
				++off;
			}
		}

		static Regex re = new Regex(@"^[-+]?[0-9]*(\.[0-9]*([eE][-+]?[0-9]+)?)?", RegexOptions.Compiled);
		
		static object ParseNumber(byte[] data, ref int off, int end)
		{
			int len = end - off;
			if (len > 24)
			{
				len = 24;
			}
			//	don't cut the middle of an UTF8 character
			while (off + len < end && ((data[off + len] & 0xc0) == 0x80))
			{
				--len;
			}
			string s = Encoding.UTF8.GetString(data, off, len);
			Match m = re.Match(s);
			if (m == null)
			{
				throw new ArgumentException("This is not a number: " + s);
			}
			off += m.Length;
			s = m.Groups[0].Value;
			if (s.Contains(".") || s.Contains("e") || s.Contains("E"))
			{
				return double.Parse(s);
			}
			return long.Parse(s);
		}
		
		static void FormatObj(dict val, StringBuilder sb)
		{
			sb.Append("{");
			string comma = "";
			foreach (KeyValuePair<string, object> kvp in val)
			{
				sb.Append(comma);
				comma = ",";
				FormatStr(kvp.Key, sb);
				sb.Append(":");
				FormatAny(kvp.Value, sb);
			}
			sb.Append("}");
		}
		
		static void FormatStr(string str, StringBuilder sb)
		{
			sb.Append("\"");
			sb.Append(str.Replace("\\", "\\\\").Replace("\"", "\\\""));
			sb.Append("\"");
		}
		
		static void FormatAny(object obj, StringBuilder sb)
		{
			if (obj == null)
			{
				sb.Append("null");
				return;
			}
			if (obj is bool)
			{
				//	dumb runtime outputs "True" and "False," which JSON doesn't like
				sb.Append(((bool)obj) ? "true" : "false");
				return;
			}
			if (obj is byte || obj is sbyte || obj is char || obj is short || obj is ushort || obj is int ||
			    obj is uint || obj is long || obj is ulong || obj is float || obj is double)
			{
				sb.Append(obj.ToString());
				return;
			}
			if (obj is string)
			{
				FormatStr((string)obj, sb);
				return;
			}
			if (obj is IJSONString)
			{
				FormatStr(obj.ToString(), sb);
				return;
			}
			if (obj is dict)
			{
				FormatObj((dict)obj, sb);
				return;
			}
			//	any enumerable turns into a list
			if (obj is System.Collections.IEnumerable)
			{
				string comma = "";
				sb.Append("[");
				foreach (object q in (obj as System.Collections.IEnumerable))
				{
					sb.Append(comma);
					comma = ",";
					FormatAny(q, sb);
				}
				sb.Append("]");
				return;
			}
			throw new InvalidOperationException("Unsupported type in JSONFormatter: " + obj.GetType().Name);
		}
	}
}
