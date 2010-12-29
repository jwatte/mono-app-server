
using System;
using System.Net;
using System.Collections.Generic;
using System.IO;

namespace IMVU.IDL
{
	public static class Services
	{
		public static void Log(string fmt, params object[] data)
		{
			string f = String.Format(fmt, data);
			Console.WriteLine("LOG: {0}", f);
		}
		
		public static void Raise(string fmt, params object[] data)
		{
			string f = String.Format(fmt, data);
			Console.WriteLine("EXCEPTION: {0}", f);
			throw new InvalidOperationException(f);
		}
		
		public static void Error(IContext ictx, string fmt, params object[] data)
		{
			try
			{
				string f = String.Format(fmt, data);
				Console.WriteLine("ERROR: Request {0} error {1}", ictx.Http.Request.Url, f);
				ictx.Http.Response.ContentType = "text/json";
				ictx.Http.Response.StatusCode = 500;
				Dictionary<string, object> ret = new Dictionary<string, object>();
				ret.Add("message", f);
				ret.Add("success", false);
				IMVU.IDL.Buffer buf = jsonf.Format(ret);
				ictx.Http.Response.Close(buf.data, false);
			}
			catch (System.Exception x)
			{
				Console.WriteLine("Exception when generating error: {0}\n{1}", x.Message, x.StackTrace);
			}
		}

		public static JSONFormatter jsonf = new JSONFormatter();
		
		public static class Types
		{
			public static ApiType t_varchar = new ApiTypeGeneric<varchar>(x => new varchar(x), x => x.ToString());
			public static ApiType t_text = new ApiTypeGeneric<text>(x => new text(x), x => x.ToString());
			public static ApiType t_password = new ApiTypeGeneric<password>(x => new password(x), x => x.ToString());
			public static ApiType t_idstring = new ApiTypeGeneric<idstring>(x => new idstring(x), x => x.ToString());
			public static ApiType t_long = new ApiTypeGeneric<long>(x => long.Parse(x), x => x.ToString());
			public static ApiType t_bool = new ApiTypeGeneric<bool>(x => bool.Parse(x), x => x.ToString().ToLowerInvariant());
		}
	}
	
	public interface ILengthLimit
	{
		int MaxLength { get; }
	}
	
	public interface IJSONString
	{
	}
	
	public class LimitedString<N> : IEquatable<LimitedString<N>>, IJSONString
		where N : class, ILengthLimit, new()
	{
		public static N Empty = new N();
		
		public LimitedString()
		{
			str = "";
		}
		
		public LimitedString(string s)
		{
			if (s == null)
			{
				throw new ArgumentNullException(GetType().Name + " does not allow null string values");
			}
			if (s.Length > Empty.MaxLength)
			{
				Services.Log("Long string: {0} longer than {1}", s, Empty.MaxLength);
				throw new InvalidDataException(GetType().Name + " max length is " + Empty.MaxLength.ToString() + " characters; got " + s.Length.ToString());
			}
			this.str = s;
		}
		
		public override string ToString()
		{
			return str;
		}
		
		public override bool Equals(object obj)
		{
			return obj != null && obj.GetType().IsAssignableFrom(GetType()) && ((LimitedString<N>)obj).str == str;
		}

		public bool Equals(LimitedString<N> other)
		{
			return other != null && str == other.str;
		}
		
		public override int GetHashCode()
		{
			return str.GetHashCode();
		}
		
		public string str;
	}

	public class password : LimitedString<password>, ILengthLimit
	{
		public int MaxLength { get { return 64; } }
		public password() {}
		public password(string s) : base(s) {}
	}
	
	public class idstring : LimitedString<idstring>, ILengthLimit
	{
		public int MaxLength { get { return 32; } }
		public idstring() {}
		public idstring(string s) : base(s) {}
	}
	
	public class varchar : LimitedString<varchar>, ILengthLimit
	{
		public int MaxLength { get { return 255; } }
		public varchar() {}
		public varchar(string s) : base(s) {}
	}
	
	public class text : LimitedString<text>, ILengthLimit
	{
		public int MaxLength { get { return 8191; } }
		public text() {}
		public text(string s) : base(s) {}
	}
}
