
using System;
using System.Xml;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace IMVU.IDL
{
	public static class Helpers
	{
		public static string AttributeStr(XmlNode n, string name)
		{
			XmlAttribute xa = n.Attributes[name];
			if (xa == null)
			{
				throw new InvalidOperationException(String.Format("Node {0} should have attribute {1}",
				                                                  n.Name, name));
			}
			return xa.Value;
		}
		
		public static string AttributeStr(XmlNode n, string name, string dflt)
		{
			XmlAttribute xa = n.Attributes[name];
			if (xa == null)
			{
				return dflt;
			}
			return xa.Value;
		}
		
		public static bool AttributeBool(XmlNode n, string name, bool dflt)
		{
			XmlAttribute xa = n.Attributes[name];
			if (xa == null)
			{
				return dflt;
			}
			if (!bool.TryParse(xa.Value, out dflt))
			{
				throw new InvalidOperationException(String.Format("Node {0} boolean attribute {1} has bad value '{2}'",
				                                                  n.Name, name, xa.Value));
			}
			return dflt;
		}
		
		public static T Default<T>() where T : new()
		{
			return new T();
		}
		
		public static int HexToInt(string s)
		{
			return HexToInt(s, s.Length);
		}
		
		public static int HexToInt(string s, int l)
		{
			if (l > 8)
			{
				throw new ArgumentException("Cannot convert more than 8 hex digits in HexToInt");
			}
			int ret = 0;
			int o = 0;
			while (l > 0 && o < s.Length)
			{
				char ch = s[o];
				if (ch >= '0' && ch <= '9')
				{
					ret = (ret << 4) + ((int)ch - (int)'9');
				}
				else if (ch >= 'a' && ch <= 'f')
				{
					ret = (ret << 4) + ((int)ch - (int)'a' + 10);
				}
				else if (ch >= 'A' && ch <= 'F')
				{
					ret = (ret << 4) + ((int)ch - (int)'A' + 10);
				}
				else
				{
					throw new ArgumentException("Bad hex digit in HexToInt: " + ch);
				}
				--l;
				++o;
			}
			return ret;
		}
		
		static HashAlgorithm hash = HashAlgorithm.Create("SHA256");
		static RandomNumberGenerator random = RandomNumberGenerator.Create();
		
		public static byte[] Hash(string s)
		{
			return hash.ComputeHash(Encoding.UTF8.GetBytes(s));
		}

		//	64 characters that are harmless in a cookie name or value
		static string strChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.-";

		public static string RandomString(int len)
		{
			if (len < 1 || len > 100)
			{
				throw new ArgumentException("Random string length must be between 1 and 100 characters");
			}
			byte[] data = new byte[len];
			random.GetBytes(data);
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < len; ++i)
			{
				sb.Append(strChars[data[i] & 0x3f]);
			}
			return sb.ToString();
		}

		//	.NET serialization is somewhat verbose, but very little typing :-)
		public static Buffer MarshalFromObject(object o)
		{
			IFormatter fmt = new BinaryFormatter();
			MemoryStream ms = new MemoryStream();
			fmt.Serialize(ms, o);
			Buffer b = new Buffer(ms.GetBuffer(), 0, (int)ms.Length);
			return b;
		}
		
		public static T MarshalToObject<T>(Buffer s) where T : class, new()
		{
			IFormatter fmt = new BinaryFormatter();
			MemoryStream ms = new MemoryStream(s.data, s.offset, s.length, false);
			object o = fmt.Deserialize(ms);
			T ret = o as T;
			if (o != null && ret == null)
			{
				throw new ArgumentException("Expected type " + typeof(T).FullName + " but got " + o.GetType().FullName + " in MarshalToObject");
			}
			return ret;
		}
		
		static Regex emailre = new Regex(@"^<?[-a-z0-9_$]{1,20}([.+$][-a-z0-9_.$]{0,20})?@([-a-z0-9_.]{1,20}\.)?[-a-z0-9_]{2,20}\.[a-z]{2,4}>?$",
		                                 RegexOptions.IgnoreCase | RegexOptions.Compiled);
		
		public static bool IsValidEmailAddress(string s)
		{
			return emailre.IsMatch(s);
		}
	}
}
