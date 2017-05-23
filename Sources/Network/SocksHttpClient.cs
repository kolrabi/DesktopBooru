using System;
using System.Net;
using System.Net.Http;

namespace Booru
{
	public class SocksHttpClient : HttpClient
	{
		public SocksHttpClient() : base(new SocksHttpMessageHandler())
		{
		}
	}
}

