using System;
using System.Net;
using System.Collections.Generic;
using Ditrans;
using Org.Mentalis.Network.ProxySocket;

namespace Booru
{
	public class CookieWebClient : WebClient
	{

	}

	public class SocksWebClient : WebClient
	{
		protected ProxyTypes ProxyType;
		public readonly IDictionary<string, string> Cookies = new Dictionary<string, string> ();

		public SocksWebClient(string proxyUrl, bool useProxy) 
		{
			this.Proxy = new WebProxy (proxyUrl);
			this.ProxyType = useProxy ? ProxyTypes.Socks5 : ProxyTypes.None;
		}

		protected override WebRequest GetWebRequest (Uri address)
		{
			var request = SocksHttpWebRequest.Create (address, this.Cookies, this.ProxyType);
			request.Headers = this.Headers;
			request.Proxy = this.Proxy;
			return request;
		}
	}
}

