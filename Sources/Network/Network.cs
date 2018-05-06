using System;
using System.Collections.Generic;
using System.Net;

namespace Booru
{
	public class Network
	{
		public byte[] DownloadData(string url, IDictionary<string, string> cookies = null)
		{
			var useProxy = BooruApp.BooruApplication.Database.Config.GetBool ("net.proxy.enable");
			var proxyUrl = BooruApp.BooruApplication.Database.Config.GetString ("net.proxy.url");

			var webClient = new SocksWebClient(proxyUrl, useProxy);
			if (cookies != null) {
				foreach (var cookie in cookies)
					webClient.Cookies [cookie.Key] = cookie.Value;
			}

			webClient.Headers ["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; rv:52.0) Gecko/20100101 Firefox/52.0";
			webClient.Headers ["Accept-Language"] = "en-US,en;q=0.5";
			// webClient.Headers ["Accept-Encoding"] = "gzip, deflate, br";
			webClient.Headers ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
			webClient.Headers ["Connection"] = "close";

			byte[] data = webClient.DownloadData (url);
			bool isChunked = false;
			foreach (var respHeader in webClient.ResponseHeaders.AllKeys) {
				string value = webClient.ResponseHeaders.Get(respHeader);
				if (respHeader == "Set-Cookie") {
					var c = value.Split (";".ToCharArray ());
					var cf = c [0].Split ("=".ToCharArray ());

					webClient.Cookies [cf [0]] = cf [1];
					if (cookies != null)
						cookies [cf [0]] = cf [1];
				}
				if (respHeader == "Transfer-Encoding") {
					isChunked = value == "chunked";
				}
			}

			if (isChunked) {
				List<byte[]> unchunked = new List<byte[]> ();
				int pos = 0;
				int chunkLength = 0;
				int totalLength = 0;
				while (pos < data.Length) {
					byte b = data [pos];
					if (b >= 0x30 && b <= 0x39) {
						chunkLength *= 16;
						chunkLength += b - 0x30;
						pos++;
					} else if (b >= 0x61 && b <= 0x66) {
							chunkLength *= 16;
							chunkLength += b - 0x61 + 10;
							pos++;
					} else if (b == 10) {
						pos++;

						if (chunkLength > 0) {
							byte[] chunk = new byte[chunkLength];
							Array.Copy (data, pos, chunk, 0, chunkLength);
							unchunked.Add (chunk);
						}
						totalLength += chunkLength;
						pos += chunkLength + 2;
						chunkLength = 0;
					} else {
						pos++;
					}
				}

				data = new byte[totalLength];
				pos = 0;
				foreach (var chunk in unchunked) {
					Array.Copy (chunk, 0, data, pos, chunk.Length);
					pos += chunk.Length;
				}
			}

			//string dataString = System.Text.Encoding.ASCII.GetString (data);
			return data;
		}

		public string DownloadText(string url, IDictionary<string, string> cookies = null)
		{
			return this.DownloadText (url, System.Text.Encoding.UTF8, cookies);
		}

		public string DownloadText(string url, System.Text.Encoding encoding, IDictionary<string, string> cookies = null)
		{
			byte[] data = this.DownloadData (url, cookies);
			return encoding.GetString (data);
		}
	}
}

