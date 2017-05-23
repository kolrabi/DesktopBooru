using System;
using System.IO;

namespace Booru
{
	public class BooruSettings
	{
		private static string applicationName = "booru";

		private readonly string settingsFolder;

		public BooruSettings ()
		{
			this.settingsFolder = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData)+"/" + applicationName;
			if (!Directory.Exists(settingsFolder))
				Directory.CreateDirectory (settingsFolder);
		}
			
		public string Get(string name) 
		{
			string path = this.settingsFolder + "/" + name;

			if (!File.Exists(path))
				return null;

			return File.ReadAllText (path);
		}

		public void Set(string name, string value)
		{
			string path = this.settingsFolder + "/" + name;

			if (value == null) {
				if (File.Exists (path))
					File.Delete (path);
			} else {
				File.WriteAllText (path, value);
			}
		}
	}
}

