using System;

namespace Booru {

	// widget for loading and saving string database config values
	public partial class ConfigValueStringWidget : Gtk.Entry
	{
		public static ConfigValueStringWidget Create (string key)
		{
			return new ConfigValueStringWidget (key);
		}

		protected ConfigValueStringWidget (string key) 
		{
			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += () => { 
				// fill widget with data from database config when database has been loaded
				this.Text = BooruApp.BooruApplication.Database.Config.GetString (key) ?? "";
			};

			this.FocusOutEvent += (o, args) => { 
				// update database config when leaving the widget
				BooruApp.BooruApplication.Database.Config.SetString (key, this.Text);
			};
		}
	}

}