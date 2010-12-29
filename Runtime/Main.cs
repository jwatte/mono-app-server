using System;
using System.Net;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace Runtime
{
	class MainClass
	{
		static AutoResetEvent quitEvent;
		static HttpListener listener;
		static Regex pathParts = new Regex("/([a-zA-Z_0-9]{2,})/([a-zA-Z_0-9]{2,})(\\?.*)?",
		                                   RegexOptions.Compiled | RegexOptions.CultureInvariant);
		static string codepath = ".";
		
		public static void Usage()
		{
			Console.WriteLine("Usage: Runtime [key=value] [config=file]");
			throw new InvalidOperationException("Bad arguments supplied.");
		}
		
		public static void ParseConfig(string s)
		{
			string[] kv = s.Split(new char[] { '=' }, 2);
			if (kv == null || kv.Length != 2)
			{
				Console.WriteLine("Bad argument: {0}", s);
				Usage();
			}
			kv[0] = kv[0].Trim();
			kv[1] = kv[1].Trim();
			if (kv[1].Length > 1 && kv[1][0] == '"' && kv[1][kv[1].Length-1] == '"')
			{
				kv[1] = kv[1].Substring(1, kv[1].Length - 2);
			}
			MethodInfo mi = typeof(MainClass).GetMethod("opt_" + kv[0],
			                                            BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public);
			if (mi == null)
			{
				Console.WriteLine("Argument {0} is not known", kv[0]);
				Usage();
			}
			mi.Invoke(null, new object[] { kv[1] });
		}
		
		public static void opt_config(string s)
		{
			using (StreamReader sr = new StreamReader(s))
			{
				string l;
				while ((l = sr.ReadLine()) != null)
				{
					if (l.Length == 0 || l[0] == '#')
					{
						continue;
					}
					ParseConfig(l);
				}
			}
		}
		
		public static void opt_codepath(string s)
		{
			if (!Directory.Exists(s))
			{
				Console.WriteLine("Could not find codepath directory " + s);
				throw new InvalidDataException("The codepath directory " + s + " does not exist!");
			}
			codepath = s;
		}
		
		public static void Main (string[] args)
		{
			foreach (string s in args)
			{
				ParseConfig(s);
			}
			Console.WriteLine("Codepath is: " + codepath);
			quitEvent = new AutoResetEvent(false);
			listener = new HttpListener();
			listener.Prefixes.Add("http://localhost:8888/");
			listener.Start();
			StartOneGet();
			quitEvent.WaitOne();
			Console.WriteLine("Got quit event");
			listener.Stop();
			Console.WriteLine("Falling off end of Main()");
		}
		
		public static void StartOneGet()
		{
			bool repeat = true;
			while (repeat)
			{
				IAsyncResult ar = listener.BeginGetContext(OnGet, null);
				repeat = ar.CompletedSynchronously;
			}
		}
		
		public static void OnGet(IAsyncResult ar)
		{
			HttpListenerContext ctx = listener.EndGetContext(ar);
            IMVU.IDL.IContext ictx = new IMVU.IDL.Context(ctx);
			if (!ar.CompletedSynchronously)
			{
				StartOneGet();
			}
			try
			{
				HandleRequest(ictx);
				ctx.Response.Close();
			}
			catch(Exception x)
			{
				string msg = x.Message;
				string stack = x.StackTrace;
				while (x.InnerException != null)
				{
					x = x.InnerException;
					msg = msg + ":\n" + x.Message;
					stack = x.StackTrace;
				}
				try
				{
					IMVU.IDL.Services.Error(ictx, msg + "\n" + stack);
				}
				catch (System.Exception y)
				{
					IMVU.IDL.Services.Log("Second exception: {0}\n{1}", y.Message, y.StackTrace);
				}
			}
		}
		
		public static void HandleRequest(IMVU.IDL.IContext ictx)
		{
			IMVU.IDL.Services.Log("Request: {0}", ictx.Http.Request.Url);
			string host = ictx.Http.Request.Url.Host;
			//	todo: verify that the app is allowed to use the service, based on the host
			string str = ictx.Http.Request.Url.PathAndQuery;
			var mc = pathParts.Match(str);
			if (mc.Groups.Count < 3)
			{
				IMVU.IDL.Services.Error(ictx, "Bad API URL: {0} (group count = {1})", str, mc.Groups.Count);
				return;
			}
			string api = mc.Groups[1].Captures[0].Value;
			string method = mc.Groups[2].Captures[0].Value;
			string args = "";
			if (mc.Groups.Count == 4 && mc.Groups[3].Captures.Count > 0)
			{
				args = mc.Groups[3].Captures[0].Value.Substring(1);
			}
			IMVU.IDL.Buffer data = HandleCall(api, method, HttpUtility.ParseQueryString(args), ictx);
			if (data != null)
			{
				ictx.Http.Response.OutputStream.Write(data.data, data.offset, data.length);
			}
		}
		
		public static IMVU.IDL.Buffer HandleCall(string api, string method, NameValueCollection p, IMVU.IDL.IContext ictx)
		{
			ApiInstance ai = ApiInstance.FindWrapper(api, codepath);
			if (ai == null)
			{
				IMVU.IDL.Services.Error(ictx, "Unknown API: {0}", api);
				return null;
			}
			return ai.CallMethod(method, p, ictx);
		}
	}
}
