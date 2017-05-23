using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;
using System.Xml;
using System.Text;
using System.Linq;
using Gdk;
using System.Net;

namespace Booru
{
	public class ImageImporter
	{
		public delegate void UpdateEntryDelegate(ImageEntry entry);
		public event UpdateEntryDelegate UpdateEntry;

		public class ImageEntry 
		{
			public readonly string path;
			public string MD5 = "";
			public string Status = "Pending";
			public string LastUpdated = "";
			public string TagString = "";
			public Gdk.Pixbuf Preview;
			public bool IsImage = false;
			public bool IsAnim = false;
			public bool ForceUpdateTags = false;

			public List<string> Tags = new List<string>();

			public ImageEntry(string path) 
			{
				this.path = path;
			}
		}

		private List<ImageEntry> entries = new List<ImageEntry>();
		private int nextImportEntry = 0;

		private Thread importThread;

		public ImageImporter ()
		{
		}

		public ImageEntry AddImage(string path)
		{
			lock (this.entries) {
				ImageEntry entry = new ImageEntry (path);
				this.entries.Add (entry);
				CallUpdate (entry);
				return entry;
			}
		}

		private void CallUpdate(ImageEntry entry)
		{
			if (UpdateEntry != null)
				UpdateEntry (entry);
		}

		public void Start() 
		{
			this.Abort ();

			var start = new ThreadStart (Import);
			this.importThread = new Thread (start);
			this.importThread.Name = "Image Importer";
			this.importThread.Start ();
		}

		public void Abort()
		{
			if (this.importThread != null) {
				this.importThread.Abort ();
				this.importThread = null;
				this.entries.Clear ();
			}
			this.nextImportEntry = 0;
		}

		private string ReadableTimeSpan(DateTime date)
		{
			if (date.Ticks == 0)
				return "never";

			var span = DateTime.Now.Subtract (date);
			if (span.TotalDays >= 365) {
				return string.Format ("{0:D} years ago", (int)(span.TotalDays / 365));
			} else if (span.TotalHours >= 24) {
				return string.Format ("{0:D} days ago", (int)span.TotalDays);
			} else if (span.TotalMinutes >= 60) {
				return string.Format ("{0:D} hours ago", (int)span.TotalHours);
			} else if (span.TotalSeconds >= 60) {
				return string.Format ("{0:D} minutes ago", (int)span.TotalMinutes);
			} else {
				return string.Format ("{0:D} seconds ago", (int)span.TotalSeconds);
			}
		}

		private void Import()
		{
			while (true) {
				ImageEntry entry = null;
				lock (this.entries) {
					if (this.entries.Count > this.nextImportEntry)
						entry = this.entries [this.nextImportEntry];
				}
				if (entry == null) {
					Thread.Sleep (1000);
				} else {
					this.ImportEntry (entry);
					this.nextImportEntry++;
				}
			}
		}

		public void ImportEntry(ImageEntry entry)
		{
			entry.Status = "Getting File Type";
			CallUpdate (entry);

			bool callBooru = false;
			BooruImageType fileType = BooruImageTypeHelper.IdentifyType (entry.path, out callBooru);
			switch (fileType) {
			case BooruImageType.Unknown:
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Files, BooruLog.Severity.Warning, "Unsupported file type: " + entry.path);
				entry.Status = "Unsupported file type";
				CallUpdate (entry);
				return;
			case BooruImageType.Image:
				entry.IsImage = true;
				entry.IsAnim = false;
				break;
			case BooruImageType.Animation:
				entry.IsImage = true;
				entry.IsAnim = true;
				break;
			case BooruImageType.Video:
				entry.IsImage = false;
				entry.IsAnim = true;
				break;
			}
				
			entry.Status = "Getting MD5";
			CallUpdate (entry);
			entry.MD5 = GetEntryMD5 (entry);

			List<string> tags;
			bool tagMeExpired = false;
			ImageDetails details = BooruApp.BooruApplication.Database.GetImageDetails (entry.MD5);
			if (details != null) {
				tags = BooruApp.BooruApplication.Database.GetImageTags (entry.MD5);
				tags.Sort ();
				entry.TagString = string.Join (" ", tags);

				var timeSinceUpdate = DateTime.Now.Subtract (details.Updated);
				double daysSinceUpdate = timeSinceUpdate.TotalDays;
				entry.LastUpdated = ReadableTimeSpan (details.Updated);
				// has image a tagme tag and last update was some time ago, ask servers again
				if (tags.Contains ("tagme") && daysSinceUpdate > 30) {
					tagMeExpired = true;
				}
			} else {
				entry.LastUpdated = "never";
				tags = new List<string>();
			}
			entry.Tags = tags;
			CallUpdate (entry);

			bool askedServer = false;

			var useGelbooru = callBooru && BooruApp.BooruApplication.Database.Config.GetBool ("gelbooru.enable");
			var gelbooruUser = BooruApp.BooruApplication.Database.Config.GetString ("gelbooru.user");
			var gelbooruPass = BooruApp.BooruApplication.Database.Config.GetString ("gelbooru.pass");
			var gelbooruURL = BooruApp.BooruApplication.Database.Config.GetString ("gelbooru.url");
			var knownOnGelbooru = tags.Contains ("known_on_gelbooru");
			var isGelbooruStatusKnown = !tagMeExpired && (knownOnGelbooru || tags.Contains ("not_on_gelbooru"));

			var useDanbooru = callBooru && BooruApp.BooruApplication.Database.Config.GetBool ("danbooru.enable");
			var danbooruURL = BooruApp.BooruApplication.Database.Config.GetString ("danbooru.url");
			var knownOnDanbooru = tags.Contains ("known_on_danbooru");
			var isDanbooruStatusKnown = !tagMeExpired && (knownOnDanbooru || tags.Contains ("not_on_danbooru"));

			var useProxy = BooruApp.BooruApplication.Database.Config.GetBool ("net.proxy.enable");
			var proxyUrl = BooruApp.BooruApplication.Database.Config.GetString ("net.proxy.url");

			if (tagMeExpired && (knownOnDanbooru || knownOnGelbooru) && (useGelbooru || useDanbooru)) {
				BooruApp.BooruApplication.Database.RemoveImageTag (entry.MD5, "tagme");
			}

			if (useGelbooru && !isGelbooruStatusKnown) {
				entry.Status = "Asking gelbooru...";
				CallUpdate (entry);

				try {
					var webClient = new SocksWebClient(proxyUrl, useProxy);
					if (!string.IsNullOrEmpty(gelbooruUser))
						webClient.Cookies ["user_id"] = gelbooruUser;
					
					if (!string.IsNullOrEmpty(gelbooruPass))
						webClient.Cookies ["pass_hash"] = gelbooruPass;

					var url = string.Format(gelbooruURL, entry.MD5);
					this.ParseGelbooruData (entry, webClient.DownloadString (url));
					askedServer = true;
				} catch (Exception ex) {
					BooruApp.BooruApplication.Log.Log(BooruLog.Category.Network, BooruLog.Severity.Error, "Exception caught while asking gelbooru: " + ex.Message);
				}
			}

			if (useDanbooru && !isDanbooruStatusKnown) {
				entry.Status = "Asking danbooru...";
				CallUpdate (entry);

				try {
					var webClient = new SocksWebClient(proxyUrl, useProxy);

					var url = string.Format(danbooruURL, entry.MD5);
					this.ParseDanbooruData (entry, webClient.DownloadString (url));
					askedServer = true;
				} catch (Exception ex) {
					BooruApp.BooruApplication.Log.Log(BooruLog.Category.Network, BooruLog.Severity.Error, "Exception caught while asking danbooru: " + ex.Message);
				}
			}

			if (details == null) {
				var tmpPixBuf = PixbufLoader.LoadPixbufAnimationForImage (entry.path, entry.MD5);
				if (tmpPixBuf != null) {
					BooruApp.BooruApplication.Database.SetImageSize (entry.MD5, new Point2D(tmpPixBuf.Width, tmpPixBuf.Height));
					tmpPixBuf.Dispose ();
				}
			}

			entry.TagString = string.Join (" ", entry.Tags);

			if (askedServer || details == null || details.Updated.Ticks == 0) {
				entry.Status = (details == null)?"Adding to DB":"Updating";
				CallUpdate (entry);
				if (details == null || askedServer)
					BooruApp.BooruApplication.Database.AddImageIfNew (entry.path, entry.MD5, entry.TagString, fileType);
				Queries.Images.UpdateUpdated.Execute (entry.MD5);
				entry.Status = (details == null)?"Added":"Updated";
				CallUpdate (entry);
			} else {
				entry.Status = "No change";
				CallUpdate (entry);
			}
			if (askedServer)
				Thread.Sleep (100);
		}

		private bool ParseBooruData(ImageEntry entry, string tagData)
		{			
			try {
				XmlDocument doc = new XmlDocument ();
				doc.LoadXml (tagData);

				XmlNode node = doc.SelectSingleNode ("posts/post");
				if (node != null) {
					XmlNode tagsNode = node.Attributes.GetNamedItem ("tags");
					if (tagsNode != null) {
						string tagString = tagsNode.InnerText;
						entry.Tags.AddRange (tagString.Split (" ".ToCharArray (), StringSplitOptions.RemoveEmptyEntries));
					}
					return true;
				} else {
					return false;
				}
			} catch(Exception ex) {
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Network, BooruLog.Severity.Error, "Could not parse booru data: " + ex.Message);
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Network, BooruLog.Severity.Error, "Path: " + entry.path);
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Network, BooruLog.Severity.Error, "Data: " + tagData);
				return false;
			}
		}

		private void ParseDanbooruData(ImageEntry entry, string tagData)
		{
			if (string.IsNullOrWhiteSpace (tagData))
				return;
			
			if (ParseBooruData (entry, tagData)) {
				entry.Tags.Add ("known_on_danbooru");
			} else {
				entry.Tags.Add ("not_on_danbooru");
			}
			entry.Tags = new List<string>(entry.Tags.Distinct ());
			entry.Tags.Sort ();
			CallUpdate (entry);
		}

		private void ParseGelbooruData(ImageEntry entry, string tagData)
		{
			if (string.IsNullOrWhiteSpace (tagData))
				return;

			if (tagData.Contains ("301 Moved"))
				return;
			
			if (ParseBooruData (entry, tagData)) {
				entry.Tags.Add ("known_on_gelbooru");
			} else {
				entry.Tags.Add ("not_on_gelbooru");
			}
			entry.Tags = new List<string>(entry.Tags.Distinct ());
			entry.Tags.Sort ();
			CallUpdate (entry);
		}

		private string GetEntryMD5(ImageEntry entry) 
		{
			string md5sum = BooruApp.BooruApplication.Database.GetPathMD5(entry.path);
			if (!string.IsNullOrEmpty(md5sum))
				return md5sum;

			using (var md5 = MD5.Create())
			{
				using (var stream = File.OpenRead(entry.path))
				{
					return MD5Helper.BlobToMD5(md5.ComputeHash(stream));
				}
			}
		}
	}
}

