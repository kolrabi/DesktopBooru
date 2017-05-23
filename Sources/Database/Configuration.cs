using System;
using System.Collections.Generic;

namespace Booru
{
	public class Configuration
	{

		private readonly Database db;

		private readonly IDictionary<string, string> cache;

		public Configuration (Database db)
		{
			this.db = db;
			this.cache = new Dictionary<string, string> ();

			// set some defaults
			GetString ("gelbooru.url", "http://gelbooru.com/index.php?page=dapi&s=post&q=index&tags=md5%3a{0}");
			GetString ("danbooru.url", "https://danbooru.donmai.us/post/index.xml?limit=1&tags=md5%3a{0}");
			GetString ("net.proxy.url", "socks5h://localhost:9050");

			GetString ("thumbs.size", "128");
			GetString ("slideshow.timeout", "5000");

			GetString ("deletemove.enable", "false");
		}

		private void CacheValue(string key)
		{
			if (this.cache.ContainsKey (key))
				return;

			this.cache [key] = this.db.GetConfig (key);
		}

		private void StoreValue(string key, string value)
		{
			lock (this.cache) {
				if (this.cache.ContainsKey (key) && this.cache [key] == value)
					return;
			
				this.cache [key] = value;
				this.db.SetConfig (key, value);
			}
		}

		public string GetString(string key, string defaultValue = null)
		{
			lock (this.cache) {
				CacheValue (key);

				if (this.cache [key] == null)
					this.cache [key] = defaultValue;

				return this.cache [key];
			}
		}

		public void SetString(string key, string value)
		{
			this.StoreValue (key, value);
		}

		public bool GetBool(string key, bool defaultValue = false)
		{
			string stringValue = this.GetString (key);
			bool value = defaultValue;
			if (!string.IsNullOrEmpty (stringValue))
				bool.TryParse (stringValue, out value);

			return value;
		}

		public void SetBool(string key, bool value)
		{
			this.SetString (key, value.ToString ());
		}

		public int GetInt(string key, int defaultValue = 0)
		{
			string stringValue = this.GetString (key);
			int value = defaultValue;
			if (!string.IsNullOrEmpty (stringValue))
				int.TryParse (stringValue, out value);

			return value;
		}

		public void SetBool(string key, int value)
		{
			this.SetString (key, value.ToString ());
		}
	}
}

