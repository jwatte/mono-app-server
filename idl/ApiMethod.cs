
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
			Dictionary<string, object> ret = (Dictionary<string, object>)methodInfo.Invoke(owner, args);
			//	todo: verify that the return value matches spec!
			if (ret != null)
			{
				return formatter.Format(ret);
			}
			return null;
		}
		
		public ApiMethod(string name, bool needSession, string[] needPermissions, CallFormatter formatter, ApiParameter[] parameters)
		{
			this.name = name;
			this.needSession = needSession;
			this.needPermissions = needPermissions;
			this.formatter = formatter;
			this.parameters = parameters;
		}
		
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
