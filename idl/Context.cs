using System;
using System.Net;

namespace IMVU.IDL
{
	public class Context : IContext
	{
		public Context(HttpListenerContext ctx)
		{
			this.ctx = ctx;
		}
		
		HttpListenerContext ctx;
		
		public string GetCookie(string name)
		{
			Cookie c = ctx.Request.Cookies[name];
			if (c == null || c.Expired)
			{
				return null;
			}
			return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(c.Value));
		}
		
		public void SetCookie(string name, string val)
		{
			DateTime then = DateTime.Now.AddDays(60);
			string date = then.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss") + " GMT";
			ctx.Response.Headers.Add("Set-Cookie", String.Format(
			    "{0}={1}; path=\"/\"; expires=\"{2}\"; HttpOnly", name, Convert.ToBase64String(
			        System.Text.Encoding.UTF8.GetBytes(val)), date));
		}

		public IPAddress PeerAddress { get { return ctx.Request.RemoteEndPoint.Address; } }
		
		public UserSession Session { get; set; }
		
		public HttpListenerContext Http { get { return ctx; } }
		
		public void VerifyPermission(string permission, string key, string keyName, string opName)
		{
			if (String.IsNullOrEmpty(permission))
			{
				return;
			}
			UserSession sess = Session;
			if (sess == null)
			{
				throw new UnauthorizedAccessException("Permission " + permission + " is required");
			}
			string[] split = permission.Split('|');
			string self = null;
			if (key != null && keyName != null)
			{
				self = "self(" + keyName + ")";
			}
			foreach (string s in split)
			{
				if (s == self && key == sess.userName)
				{
					return;
				}
				if (sess.HasPermission(s))
				{
					return;
				}
			}
			throw new UnauthorizedAccessException("Permission '" + permission + "' is required for " + opName);
		}
	}
	
}

