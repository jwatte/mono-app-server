
using System;
using System.IO;
using System.Reflection;
using IMVU.IDL;
using System.Net;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace Runtime
{
	public class ApiInstance
	{
        static Counter api_counter = new Counter("api", "number of APIs installed");

		//	codebase/idl/idl.somename.dll contains generated API wrapper for somename
		//	codebase/api/api.somename.dll contains actual user-written code for API
		private ApiInstance(string name, string codepath)
		{
			path = Path.Combine(Path.Combine(codepath, "idl"), "idl." + name + ".dll");
			//	todo: for runtime code replacement, use appdomains and refcount active requests
			assy = Assembly.LoadFile(path);
			if (assy == null)
			{
				throw new FileNotFoundException("Could not find assembly: " + path);
			}
			Console.WriteLine("Loaded {0} from {1}", name, path);
			foreach (Type t in assy.GetExportedTypes())
			{
				Console.WriteLine("Examining type {0}", t.Name);
				if ((t.Name == name || t.Name == "idl." + name) && typeof(WrapperBase).IsAssignableFrom(t))
				{
					Console.WriteLine("Found type {0}", t.Name);
					wrapper = (WrapperBase)assy.CreateInstance(t.FullName);
					wrapper.CodePath = codepath;
					wrapper.Initialize();
					break;
				}
			}
			if (wrapper == null)
			{
				throw new FileNotFoundException("Could not instantiate wrapper type: " + path);
			}
            api_counter.Count();
		}
		
		public static long NowTicks()
		{
			return DateTime.Now.Ticks;
		}

		public IMVU.IDL.Buffer CallMethod(string method, NameValueCollection p, IContext ictx)
		{
			string scook = ictx.GetCookie("session");
			UserSession sess = null;
			if (scook != null)
			{
				sess = UserSession.Find(scook, ictx);
                ictx.Session = sess;
			}
			return wrapper.CallMethodForUser(sess, method, p, ictx);
		}
		
		string path;
		Assembly assy;
		WrapperBase wrapper;
		
		static Dictionary<string, ApiInstance> apis = new Dictionary<string, ApiInstance>();
		
		public static ApiInstance FindWrapper(string name, string codepath)
		{
			ApiInstance ret;
			lock (apis)
			{
				if (!apis.TryGetValue(name, out ret))
				{
					try
					{
						Console.WriteLine("Creating new ApiInstance({0})", name);
						ret = new ApiInstance(name, codepath);
					}
					finally
					{
						if (ret == null)
                        {
                            Services.Log("Adding negative cache of interface {0}", name);
                        }
						apis.Add(name, ret);
					}
				}
			}
			return ret;
		}
	}
}
