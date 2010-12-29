
using System;
using System.Net;
using System.Collections.Generic;

namespace IMVU.IDL
{
	[Serializable]
	public class UserSession
	{
		public UserSession()
		{
		}
		
		public static UserSession Find(string sid, IContext ctx)
		{
			Services.Log("UserSession.Find({0})", sid);
			long ver = 0;
			Buffer b = KeyValueStore.FindBuffer(sid, out ver);
			UserSession ret = null;
			if (b == null)
			{
				return null;
			}
			ret = Helpers.MarshalToObject<UserSession>(b);
			ret.lastVersion = ver;
			IPAddress addr = ctx.PeerAddress;
			if (ret.peerAddress.ToString() != addr.ToString())
			{
				Console.WriteLine("Session {2} bound IP address changed: {0} to {1}", ret.peerAddress, addr, ret.sid);
				ret.Invalidate(ctx);
				return null;
			}
			if (ret.expiryTimeTicks < DateTime.Now.Ticks)
			{
				Console.WriteLine("Session {0} expired", ret.sid);
				ret.Invalidate(ctx);
				return null;
			}
			return ret;
		}
		
		public static UserSession Create(IContext ctx, string uName)
		{
			int n = 0;
			UserSession ret = new UserSession();
			ret.peerAddress = ctx.PeerAddress;
			//	six hour session lifetime
			ret.userName = uName;
		again:
			ret.sid = "sid:" + Helpers.RandomString(20);
			ret.UpdateExpiryTime();
			Buffer s = Helpers.MarshalFromObject(ret);
			if (!KeyValueStore.StoreBuffer(ret.sid, s, 0))
			{
				Services.Log("Could not store session (unlikely collision?): ({0}/3) {1}", n, ret.sid);
				++n;
				if (n < 3)
				{
					goto again;
				}
				Services.Raise("Three session id collisions in a row? Is not possible! {0}", ret.sid);
			}
			ret.SetCookie(ctx);
			return ret;
		}
		
		public void Invalidate(IContext ctx)
		{
			KeyValueStore.Erase(this.sid, this.lastVersion);
			ctx.Http.Response.Headers.Add("Set-Cookie", "session=; path=\"/\"; expires=\"Thu, 01 Jan 1970 00:00:00 GMT\"; HttpOnly");
		}
		
		public void Refresh(IContext ctx)
		{
			UpdateExpiryTime();
			Store(ctx);
		}
		
		public bool Store(IContext ctx)
		{
			Buffer s = Helpers.MarshalFromObject(this);
			if (!KeyValueStore.StoreBuffer(sid, s, lastVersion))
			{
				return false;
			}
			lastVersion += 1;
			SetCookie(ctx);
			return true;
		}
		
		void UpdateExpiryTime()
		{
			expiryTimeTicks = DateTime.Now.Ticks + 6 * 60 * 60 * 10000000L;
		}
		
		void SetCookie(IContext ctx)
		{
	        ctx.SetCookie("session", sid);
		}
		
		public bool HasPermission(string perm)
		{
			bool ret = false;
			if (!permissions.TryGetValue(perm, out ret))
			{
				return false;
			}
			return ret;
		}

		//	in-memory session storage
		static Dictionary<string, UserSession> sessions = new Dictionary<string, UserSession>();
		
		[NonSerialized]	//	gotten from KeyValueStore
		public long lastVersion;

		//	session data
		public string sid;
		public IPAddress peerAddress;
		public long expiryTimeTicks;

		//	user data
		public string userName;
		public Dictionary<string, bool> permissions = new Dictionary<string, bool>();
	}
}
