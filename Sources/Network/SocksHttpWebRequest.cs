using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Org.Mentalis.Network.ProxySocket;

namespace Ditrans
{
	public class SocksHttpWebRequest : WebRequest
	{

		#region Member Variables

		private readonly Uri _requestUri;
		private WebHeaderCollection _requestHeaders;
		private string _method;
		private SocksHttpWebResponse _response;
		private string _requestMessage;
		private byte[] _requestContentBuffer;

		private ProxyTypes _proxyType;

		private IDictionary<string, string> _cookies;

		// darn MS for making everything internal (yeah, I'm talking about you, System.net.KnownHttpVerb)
		static readonly StringCollection validHttpVerbs =
			new StringCollection { "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "OPTIONS" };

		#endregion

		#region Constructor

		private SocksHttpWebRequest(Uri requestUri, ProxyTypes proxyType)
		{
			_requestUri = requestUri;
			_proxyType = proxyType;
		}

		private SocksHttpWebRequest(Uri requestUri, IDictionary<string,string> cookies, ProxyTypes proxyType)
		{
			_requestUri = requestUri;
			_proxyType = proxyType;
			_cookies = cookies; 
		}

		#endregion

		#region WebRequest Members

		public override WebResponse GetResponse()
		{
			if (Proxy == null)
			{
				throw new InvalidOperationException("Proxy property cannot be null.");
			}
			if (String.IsNullOrEmpty(Method))
			{
				throw new InvalidOperationException("Method has not been set.");
			}

			if (RequestSubmitted)
			{
				return _response;
			}
			_response = InternalGetResponse();
			RequestSubmitted = true;
			return _response;
		}

		public override Uri RequestUri
		{
			get { return _requestUri; }
		}

		public override IWebProxy Proxy { get; set; }

		public override WebHeaderCollection Headers
		{
			get
			{
				if (_requestHeaders == null)
				{
					_requestHeaders = new WebHeaderCollection();
				}
				return _requestHeaders;
			}
			set
			{
				if (RequestSubmitted)
				{
					throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
				}
				_requestHeaders = value;
			}
		}

		public bool RequestSubmitted { get; private set; }

		public override string Method
		{
			get
			{
				return _method ?? "GET";
			}
			set
			{
				if (validHttpVerbs.Contains(value))
				{
					_method = value;
				}
				else
				{
					throw new ArgumentOutOfRangeException("value", string.Format("'{0}' is not a known HTTP verb.", value));
				}
			}
		}

		public override long ContentLength { get; set; }

		public override string ContentType { get; set; }

		public override Stream GetRequestStream()
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
			}

			if (_requestContentBuffer == null)
			{
				_requestContentBuffer = new byte[ContentLength];
			}
			else if (ContentLength == default(long))
			{
				_requestContentBuffer = new byte[int.MaxValue];
			}
			else if (_requestContentBuffer.Length != ContentLength)
			{
				Array.Resize(ref _requestContentBuffer, (int) ContentLength);
			}
			return new MemoryStream(_requestContentBuffer);
		}

		#endregion

		#region Methods

		public static WebRequest Create(string requestUri, ProxyTypes proxyType = ProxyTypes.Socks5)
		{
			return new SocksHttpWebRequest(new Uri(requestUri), proxyType);
		}

		public static WebRequest Create(string requestUri, IDictionary<string, string> cookies, ProxyTypes proxyType = ProxyTypes.Socks5)
		{
			return new SocksHttpWebRequest(new Uri(requestUri), cookies, proxyType);
		}

		public static WebRequest Create(Uri requestUri, ProxyTypes proxyType = ProxyTypes.Socks5)
		{
			return new SocksHttpWebRequest(requestUri, proxyType);
		}

		public static WebRequest Create(Uri requestUri, IDictionary<string, string> cookies, ProxyTypes proxyType = ProxyTypes.Socks5)
		{
			return new SocksHttpWebRequest(requestUri, cookies, proxyType);
		}

		private string BuildHttpRequestMessage()
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException("This operation cannot be performed after the request has been submitted.");
			}

			var message = new StringBuilder();
			message.AppendFormat("{0} {1} HTTP/1.0\r\nHost: {2}\r\n", Method, RequestUri.PathAndQuery, RequestUri.Host);

			// add the headers
			foreach (var key in Headers.Keys)
			{
				message.AppendFormat("{0}: {1}\r\n", key, Headers[key.ToString()]);
			}

			if (!string.IsNullOrEmpty(ContentType))
			{
				message.AppendFormat("Content-Type: {0}\r\n", ContentType);
			}
			if (ContentLength > 0)
			{
				message.AppendFormat("Content-Length: {0}\r\n", ContentLength);
			}

			if (_cookies != null && _cookies.Count > 0) 
			{
				List<string> cookieStrings = new List<string> ();
				foreach (var cookie in _cookies) {
					cookieStrings.Add (cookie.Key + "=" + cookie.Value);
				}
				message.AppendFormat("Cookie: "+string.Join (";", cookieStrings) + "\r\n");
			}

			// add a blank line to indicate the end of the headers
			message.Append("\r\n");

			// add content
			if(_requestContentBuffer != null && _requestContentBuffer.Length > 0)
			{
				using (var stream = new MemoryStream(_requestContentBuffer, false))
				{
					using (var reader = new StreamReader(stream))
					{
						message.Append(reader.ReadToEnd());
					}
				}
			}

			return message.ToString();
		}
			
		private Socket CreateSocket()
		{
			if (_proxyType == ProxyTypes.None) {
				return new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			}

			var socket = new ProxySocket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			var proxyUri = Proxy.GetProxy(RequestUri);
			var ipAddress = GetProxyIpAddress(proxyUri);
			socket.ProxyEndPoint = new IPEndPoint(ipAddress, proxyUri.Port);
			socket.ProxyType = _proxyType; // ProxyTypes.Socks5;

			return socket;
		}

		private Stream OpenNetworkStream()
		{
			Socket socket = CreateSocket ();
			if (RequestUri.Scheme == "http") {
				int port = RequestUri.IsDefaultPort ? 80 : RequestUri.Port;
				socket.Connect (RequestUri.Host, port);

				return new NetworkStream (socket);
			} else if (RequestUri.Scheme == "https") {
				int port = RequestUri.IsDefaultPort ? 443 : RequestUri.Port;
				socket.Connect (RequestUri.Host, port);

				var stream = new NetworkStream (socket);
				var sslStream = new System.Net.Security.SslStream (stream);

				sslStream.AuthenticateAsClient (RequestUri.Host);

				return sslStream;
			} else {
				throw new InvalidDataException("unknown uri scheme "+RequestUri.Scheme);
			}
		}

		private SocksHttpWebResponse InternalGetResponse()
		{
			var response = new StringBuilder();
			// open connection
			using (var _stream = OpenNetworkStream ()) 
			{
				// send an HTTP request
				var requestBytes = Encoding.ASCII.GetBytes (RequestMessage);
				_stream.Write (requestBytes, 0, requestBytes.Length);

				// read the HTTP reply
				var buffer = new byte[1024];

				var bytesReceived = _stream.Read (buffer, 0, buffer.Length);
				while (bytesReceived > 0) {
					response.Append (Encoding.ASCII.GetString (buffer, 0, bytesReceived));
					bytesReceived = _stream.Read (buffer, 0, buffer.Length);
				}
			}
			return new SocksHttpWebResponse(response.ToString());
		}

		private static IPAddress GetProxyIpAddress(Uri proxyUri)
		{
			IPAddress ipAddress;
			if (!IPAddress.TryParse(proxyUri.Host, out ipAddress))
			{
				try
				{
					return Dns.GetHostEntry(proxyUri.Host).AddressList[0];
				}
				catch (Exception e)
				{
					throw new InvalidOperationException(
						string.Format("Unable to resolve proxy hostname '{0}' to a valid IP address.", proxyUri.Host), e);
				}
			}
			return ipAddress;
		}

		#endregion

		#region Properties

		public string RequestMessage
		{
			get
			{
				if (string.IsNullOrEmpty(_requestMessage))
				{
					_requestMessage = BuildHttpRequestMessage();
				}
				return _requestMessage;
			}
		}

		#endregion

	}
}