using System;
using System.Collections.Generic;

namespace Booru
{
	public class AllTagFinderPlugins : TagFinderPluginInterface
	{
		public bool FindTagsForFile(string fileName, string md5, bool tagMeExpired, List<string> tags)
		{
			bool result = false;
			foreach (var plugin in BooruApp.BooruApplication.PluginLoader.LoadedPlugins) {
				var tagFinderIface = plugin as TagFinderPluginInterface;
				if (tagFinderIface == null)
					continue;

				if (tagFinderIface.FindTagsForFile (fileName, md5, tagMeExpired, tags))
					result = true;
			}
			return result;
		}
	}
}

