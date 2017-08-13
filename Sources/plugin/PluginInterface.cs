using System;
using System.Collections.Generic;

namespace Booru
{
	public struct ConfigEntryDefinition
	{
		public readonly string Name;
		public readonly string Label;
		public readonly string Tooltip;

		public ConfigEntryDefinition(string name, string label, string tooltip)
		{
			this.Name = name;
			this.Label = label;
			this.Tooltip = tooltip;
		}
	}

	public interface PluginInterface
	{
		string Name { get; }
		string ConfigDesc { get; }
		IEnumerable<ConfigEntryDefinition> ConfigEntryDefinitions { get; }

		void OnLoad(BooruApp app);
		void OnUnload();
	}

	public interface TagFinderPluginInterface
	{
		bool FindTagsForFile(string fileName, string md5, bool tagMeExpired, List<string> tags);
	}
}

