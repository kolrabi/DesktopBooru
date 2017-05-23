using System;

namespace Booru {

	// widget for loading and saving boolean database config values
	public class ConfigValueBoolWidget : Gtk.Switch
	{
		public static ConfigValueBoolWidget Create (string key)
		{
			return new ConfigValueBoolWidget (key);
		}

		protected ConfigValueBoolWidget (string key) 
		{
			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += () => { 
				// fill widget with data from database config when database has been loaded
				this.Active = BooruApp.BooruApplication.Database.Config.GetBool (key);
			};

			this.StateChanged += (o, args) => { 
				// ignore state changes in sensitivity
				if (args.PreviousState == Gtk.StateType.Insensitive || this.State == Gtk.StateType.Insensitive)
					return;

				if (this.State == args.PreviousState)
					return;
				
				// update database config when widget is changed
				BooruApp.BooruApplication.Database.Config.SetBool (key, this.Active);
			}; 
		}
	}

}