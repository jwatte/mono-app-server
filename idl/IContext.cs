using System;
using System.Net;

namespace IMVU.IDL
{
	public interface IContext
	{
		string GetCookie(string name);
		void SetCookie(string name, string val);
		IPAddress PeerAddress { get; }
		UserSession Session { get; set; }
		HttpListenerContext Http { get; }
	}
}

