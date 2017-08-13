using System;
using System.Collections.Generic;
using System.Xml;

namespace GelbooruPlugin
{
	public class Plugin : Booru.PluginInterface, Booru.TagFinderPluginInterface
	{
		private static List<Booru.ConfigEntryDefinition> configEntries = new List<Booru.ConfigEntryDefinition>()
		{
			new Booru.ConfigEntryDefinition("bool:gelbooru.enable", "Enable Gelbooru Support", "<markup><span>Enable support for asking the <i>Gelbooru</i> server about image tags during image import.</span></markup>"),
			new Booru.ConfigEntryDefinition("string:gelbooru.user", "Gelbooru API User ID", "<markup><span>User ID to use when using the <i>Gelbooru</i> API. You can find this settings in your cookies after loggin in to the <i>Gelbooru</i> website.</span></markup>"),
			new Booru.ConfigEntryDefinition("string:gelbooru.pass", "Gelbooru API Password Hash", "<markup><span>Password hash to use when using the <i>Gelbooru</i> API. You can find this settings in your cookies after loggin in to the <i>Gelbooru</i> website.</span></markup>"),
			new Booru.ConfigEntryDefinition("string:gelbooru.url", "Gelbooru API URL", "<markup><span>API URL to use for requesting information about an image. <tt>{0}</tt> will be replaced by the image's md5sum.</span></markup>")
		};

		public string Name { get { return "Gelbooru Support"; } }
		public string ConfigDesc { get { return "This program can connect to the gelbooru website to gather information about images during import. These settings specify how to access gelbooru."; } }
		public IEnumerable<Booru.ConfigEntryDefinition> ConfigEntryDefinitions { get  { return configEntries; } }

		private Booru.BooruApp App;

		public void OnLoad(Booru.BooruApp app)
		{
			App = app;
		}

		public void OnUnload()
		{
		}

		public bool FindTagsForFile(string fileName, string md5, bool tagMeExpired, List<string> tags)
		{
			var useGelbooru = App.Database.Config.GetBool ("gelbooru.enable");
			var gelbooruUser = App.Database.Config.GetString ("gelbooru.user");
			var gelbooruPass = App.Database.Config.GetString ("gelbooru.pass");
			var gelbooruURL = App.Database.Config.GetString ("gelbooru.url");
			var knownOnGelbooru = tags.Contains ("known_on_gelbooru");
			var isGelbooruStatusKnown = !tagMeExpired && (knownOnGelbooru || tags.Contains ("not_on_gelbooru"));

			if (!useGelbooru || isGelbooruStatusKnown)
				return false;

			try {
				IDictionary<string, string> cookies = new Dictionary<string, string>();
				if (!string.IsNullOrEmpty(gelbooruUser))
					cookies ["user_id"] = gelbooruUser;

				if (!string.IsNullOrEmpty(gelbooruPass))
					cookies ["pass_hash"] = gelbooruPass;

				var url = string.Format(gelbooruURL, md5);
				if (!this.ParseData (App.DownloadText(url, cookies), tags))
					return false;
			} catch (Exception ex) {
				App.Log.Log(Booru.BooruLog.Category.Network, Booru.BooruLog.Severity.Error, "Exception caught while asking gelbooru: " + ex.Message + " " + ex.InnerException == null ? "no inner exception" : ex.InnerException.Message);
				return false;
			}

			if (tagMeExpired && knownOnGelbooru && useGelbooru) {
				App.Database.RemoveImageTag (md5, "tagme");
			}
			return true;
		}

		private bool ParseData(string tagData, List<string> tags)
		{
			if (string.IsNullOrWhiteSpace (tagData))
				return false;

			if (tagData.Contains ("301 Moved"))
				return false;

			if (ParseBooruData (tagData, tags)) {
				tags.Add ("known_on_gelbooru");
			} else {
				tags.Add ("not_on_gelbooru");
			}
			return true;
		}

		private bool ParseBooruData(string tagData, List<string> tags)
		{			
			try {
				XmlDocument doc = new XmlDocument ();
				doc.LoadXml (tagData);

				XmlNode node = doc.SelectSingleNode ("posts/post");
				if (node != null) {
					XmlNode tagsNode = node.Attributes.GetNamedItem ("tags");
					if (tagsNode != null) {
						string tagString = tagsNode.InnerText;
						tags.AddRange (tagString.Split (" ".ToCharArray (), StringSplitOptions.RemoveEmptyEntries));
					}
					return true;
				} else {
					return false;
				}
			} catch(Exception ex) {
				App.Log.Log(Booru.BooruLog.Category.Network, Booru.BooruLog.Severity.Error, "Could not parse booru data: " + ex.Message);
				App.Log.Log(Booru.BooruLog.Category.Network, Booru.BooruLog.Severity.Error, "Data: " + tagData);
				return false;
			}
		}
	}
}

