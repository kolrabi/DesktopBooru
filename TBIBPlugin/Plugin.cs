using System;
using System.Collections.Generic;
using System.Xml;

namespace TBIBPlugin
{
	public class Plugin : Booru.PluginInterface, Booru.TagFinderPluginInterface
	{
		private static List<Booru.ConfigEntryDefinition> configEntries = new List<Booru.ConfigEntryDefinition>()
		{
			new Booru.ConfigEntryDefinition("bool:tbib.enable", "Enable TBIB Support", "<markup><span>Enable support for asking the <i>TBIB</i> server about image tags during image import.</span></markup>"),
			//new Booru.ConfigEntryDefinition("string:tbib.user", "TBIB API User ID", "<markup><span>User ID to use when using the <i>TBIB</i> API. You can find this settings in your cookies after loggin in to the <i>TBIB</i> website.</span></markup>"),
			//new Booru.ConfigEntryDefinition("string:tbib.pass", "TBIB API Password Hash", "<markup><span>Password hash to use when using the <i>TBIB</i> API. You can find this settings in your cookies after loggin in to the <i>TBIB</i> website.</span></markup>"),
			new Booru.ConfigEntryDefinition("string:tbib.url", "TBIB API URL", "<markup><span>API URL to use for requesting information about an image. <tt>{0}</tt> will be replaced by the image's md5sum.</span></markup>")
		};

		public string Name { get { return "TBIB Support"; } }
		public string ConfigDesc { get { return "This program can connect to the TBIB website to gather information about images during import. These settings specify how to access tbib."; } }
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
			var useTBIB = App.Database.Config.GetBool ("tbib.enable");
			var tbibUser = App.Database.Config.GetString ("tbib.user");
			var tbibPass = App.Database.Config.GetString ("tbib.pass");
			var tbibURL = App.Database.Config.GetString ("tbib.url");
			var knownOnTBIB = tags.Contains ("known_on_tbib");
			var isTBIBStatusKnown = !tagMeExpired && (knownOnTBIB || tags.Contains ("not_on_tbib"));

			if (!useTBIB || isTBIBStatusKnown)
				return false;

			try {
				IDictionary<string, string> cookies = new Dictionary<string, string>();
				if (!string.IsNullOrEmpty(tbibUser))
					cookies ["user_id"] = tbibUser;

				if (!string.IsNullOrEmpty(tbibPass))
					cookies ["pass_hash"] = tbibPass;

				var url = string.Format(tbibURL, md5);
				var postData = App.Network.DownloadText(url, cookies);
				if (!this.ParseData (postData, tags))
					return false;
			} catch (Exception ex) {
				App.Log.Log(Booru.BooruLog.Category.Plugins, ex, "Exception caught while asking tbib");
 				return false;
			}

			if (tagMeExpired && knownOnTBIB && useTBIB) {
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
				tags.Add ("known_on_tbib");
			} else {
				tags.Add ("not_on_tbib");
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
				App.Log.Log(Booru.BooruLog.Category.Plugins, ex, "Exception caught while parsing booru data");
				App.Log.Log(Booru.BooruLog.Category.Plugins, Booru.BooruLog.Severity.Error, "Data was:" + tagData);
				return false;
			}
		}
	}
}

