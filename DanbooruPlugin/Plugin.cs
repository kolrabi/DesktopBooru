using System;
using System.Collections.Generic;
using System.Xml;

namespace DanbooruPlugin
{
	public class Plugin : Booru.PluginInterface, Booru.TagFinderPluginInterface
	{
		private static List<Booru.ConfigEntryDefinition> configEntries = new List<Booru.ConfigEntryDefinition>()
		{
			new Booru.ConfigEntryDefinition("bool:danbooru.enable", "Enable Gelbooru Support", "<markup><span>Enable support for asking the <i>Danbooru</i> server about image tags during image import.</span></markup>"),
			new Booru.ConfigEntryDefinition("string:danbooru.url", "Danbooru API URL", "<markup><span>API URL to use for requesting information about an image. <tt>{0}</tt> will be replaced by the image's md5sum.</span></markup>")
		};

		public string Name { get { return "Danbooru Support"; } }
		public string ConfigDesc { get { return "This program can connect to the danbooru website to gather information about images during import. These settings specify how to access danbooru."; } }
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
			var useDanbooru = App.Database.Config.GetBool ("danbooru.enable");
			var danbooruURL = App.Database.Config.GetString ("danbooru.url");
			var knownOnDanbooru = tags.Contains ("known_on_danbooru");
			var isDanbooruStatusKnown = !tagMeExpired && (knownOnDanbooru || tags.Contains ("not_on_danbooru"));

			if (!useDanbooru || isDanbooruStatusKnown)
				return false;

			try {
				var url = string.Format(danbooruURL, md5);
				if (!this.ParseData (App.Network.DownloadText(url, null), tags))
					return false;
			} catch (Exception ex) {
				App.Log.Log(Booru.BooruLog.Category.Network, Booru.BooruLog.Severity.Error, "Exception caught while asking danbooru: " + ex.Message + " " + (ex.InnerException == null ? "no inner exception" : ex.InnerException.Message));
				return false;
			}

			if (tagMeExpired && knownOnDanbooru && useDanbooru) {
				App.Database.RemoveImageTag (md5, "tagme");
			}
			return true;
		}

		private bool ParseData(string tagData, List<string> tags)
		{
			if (string.IsNullOrWhiteSpace (tagData))
				return false;

			if (ParseBooruData (tagData, tags)) {
				tags.Add ("known_on_danbooru");
			} else {
				tags.Add ("not_on_danbooru");
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

