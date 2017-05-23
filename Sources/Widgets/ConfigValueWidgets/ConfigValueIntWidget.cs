using System;

namespace Booru {

	// widget for loading and saving int database config values
	public partial class ConfigValueIntWidget : Gtk.SpinButton
	{
		public static ConfigValueIntWidget Create (string key, int min, int max)
		{
			return new ConfigValueIntWidget (key, min, max);
		}

		protected ConfigValueIntWidget (string key, int min, int max) : base(min,max,1) 
		{
			this.Numeric = true;

			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += () => { 
				// fill widget with data from database config when database has been loaded
				this.Value = int.Parse(BooruApp.BooruApplication.Database.Config.GetString (key));
			};

			this.FocusOutEvent += (o, args) => { 
				// update database config when leaving the widget
				BooruApp.BooruApplication.Database.Config.SetString (key, this.Text);
			};
		}
	}

}