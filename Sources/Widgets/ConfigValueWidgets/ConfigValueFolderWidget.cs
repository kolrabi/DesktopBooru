using System;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	// widget for loading and saving folder path string database config values
	public partial class ConfigValueFolderWidget : Gtk.FileChooserButton
	{
		private readonly string configKey;

		public static ConfigValueFolderWidget Create (string key)
		{
			return new ConfigValueFolderWidget (key);
		}

		protected ConfigValueFolderWidget (string key) : base(key, Gtk.FileChooserAction.SelectFolder)
		{
			this.configKey = key;

			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += () => { 
				// fill widget with data from database config when database has been loaded
				this.SetFilename( BooruApp.BooruApplication.Database.Config.GetString (key));
			};


			this.CurrentFolderChanged += (o, args) => {
				SaveValue ();
			};
		}
		/*
		private void on_SelectButton_clicked(object sender, EventArgs args)
		{
			// show dialog to select folder
			var dlg = new Gtk.FileChooserDialog ("Choose Folder", BooruApp.BooruApplication.MainWindow, Gtk.FileChooserAction.SelectFolder);
			dlg.AddButton ("Choose", Gtk.ResponseType.Ok);
			dlg.AddButton ("Cancel", Gtk.ResponseType.Cancel);

			// start in already specified folder path
			if (!string.IsNullOrEmpty (this.FileEntry.Text))
				dlg.SetCurrentFolder (this.FileEntry.Text);

			// update and save value after selection
			if (dlg.Run() == (int)Gtk.ResponseType.Ok) {
				this.FileEntry.Text = dlg.CurrentFolder;
				this.SaveValue ();
			}
				
			dlg.Destroy ();
		}
*/
		private void SaveValue()
		{
			// update database config when leaving the widget
			BooruApp.BooruApplication.Database.Config.SetString (this.configKey, this.Filename);
		}
	}

}