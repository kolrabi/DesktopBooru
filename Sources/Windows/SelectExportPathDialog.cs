using System;

namespace Booru
{
	public class SelectExportPathDialog : Gtk.FileChooserDialog
	{
		public SelectExportPathDialog () : base(
			"Select export directory", 
			BooruApp.BooruApplication.MainWindow, 
			Gtk.FileChooserAction.SelectFolder, 
			"Export Here", Gtk.ResponseType.Ok, 
			"Cancel", Gtk.ResponseType.Cancel
		)
		{
			this.SetCurrentFolder (BooruApp.BooruApplication.Database.Config.GetString ("export.lastpath"));
		}

		public static bool SelectPath(out string folderPath)
		{
			var dlg = new SelectExportPathDialog ();
			if (dlg.Run () != (int)Gtk.ResponseType.Ok) {
				dlg.Destroy ();
				folderPath = null;
				return false;
			}

			if (!System.IO.Directory.Exists (dlg.Filename)) {
				dlg.Destroy ();
				folderPath = null;
				return false;
			}

			BooruApp.BooruApplication.Database.Config.SetString ("export.lastpath", dlg.CurrentFolder);
			folderPath = dlg.CurrentFolder;
			dlg.Destroy ();
			return true;
		}
	}
}

