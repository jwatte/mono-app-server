
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using System.Web;

namespace IMVU.IDL
{
	public class ApiMethod
	{
		public bool ValidateUser(UserSession sess)
		{
			if (sess == null)
			{
				if (needSession != false || (needPermissions != null && needPermissions.Length > 0))
				{
					return false;
				}
				return true;
			}
			if (needPermissions != null)
			{
				foreach (string s in needPermissions)
				{
					if (!sess.HasPermission(s))
					{
						return false;
					}
				}
			}
			return true;
		}

		public IMVU.IDL.Buffer Invoke(object owner, UserSession sess, NameValueCollection p, IContext ictx)
		{
            try
            {
                call_counter.Count();
    			int plen = (parameters == null) ? 0 : parameters.Length;
    			object[] args = new object[plen + 1];
    			if (p.Count != plen)
    			{
    				throw new InvalidOperationException("Wrong number of arguments to method " + name + ": " +
    				                                    "expected " + plen.ToString() + " got " + p.Count.ToString());
    			}
    			for (int i = 0; i != plen; ++i)
    			{
    				string val = p[parameters[i].name];
    				if (val == null)
    				{
    					throw new InvalidOperationException("Missing argument " + parameters[i].name + " to method " +
    					                                    name);
    				}
    				args[i + 1] = parameters[i].type.ConvertFromString(val);
    			}
                args[0] = ictx;
    			object ret = null;
                try
                {
                    ret = methodInfo.Invoke(owner, args);
                }
                catch (TargetInvocationException tie)
                {
                    Services.Error(ictx, "TargetInvocationException in " + iname + "." + name);
                    //  the inner exception is better than the generic target invocation
                    if (tie.InnerException != null)
                    {
                        throw tie.InnerException;
                    }
                    //  if no inner, then re-throw what we have
                    throw;
                }
    			//	todo: verify that the return value matches spec!
    			if (ret != null)
    			{
    				return formatter.Format(ret);
    			}
    			return null;
            }
            catch (System.Exception)
            {
                error_counter.Count();
                throw;
            }
		}
		
		public ApiMethod(string iname, string name, bool needSession, string[] needPermissions, CallFormatter formatter, ApiParameter[] parameters)
		{
            this.iname = iname;
			this.name = name;
			this.needSession = needSession;
			this.needPermissions = needPermissions;
			this.formatter = formatter;
			this.parameters = parameters;
            call_counter = new Counter("api." + iname + "." + name + ".call", "number of calls to method (OK or not)");
            error_counter = new Counter("api." + iname + "." + name + ".error", "number of errors in calls to method");
		}

        Counter call_counter;
        Counter error_counter;

        public readonly string iname;
		public readonly string name;
		public readonly bool needSession;
		public readonly string[] needPermissions;
		public readonly CallFormatter formatter;
		public readonly ApiParameter[] parameters;
		
		public MethodInfo methodInfo;
		
		public class ApiParameter
		{
			public ApiParameter(string n, ApiType t)
			{
				this.name = n;
				this.type = t;
			}
			public string name;
			public ApiType type;
		}
	}
}
