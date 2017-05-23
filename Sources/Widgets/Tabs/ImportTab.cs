using System;
using Gtk;
using Cairo;
using System.Collections.Generic;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class ImportTab : LoadableWidget
	{
		[UI] Gtk.TreeView ImageEntryView;

		private ImageImporter importer;
		private IDictionary<ImageImporter.ImageEntry, TreeIter> entries = new Dictionary<ImageImporter.ImageEntry, TreeIter>();
		private Gtk.ListStore entryStore = new Gtk.ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

		public static ImportTab Create ()
		{
			return LoadableWidget.Create<ImportTab> ();
		}

		ImportTab (Builder builder, IntPtr handle) : base (builder, handle)
		{
			//this.AddColumnPixbuf ("Preview", 0);
			this.AddColumnText ("Path", 1);
			this.AddColumnText ("MD5", 2);
			this.AddColumnText ("Status", 3);
			this.AddColumnText ("LastUpdated", 5);
			this.AddColumnText ("Tags", 4);

			this.ImageEntryView.Model = this.entryStore;

			this.importer = new ImageImporter ();
			this.importer.UpdateEntry += HandleUpdateEntry;

			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted   += this.OnDatabaseUnloaded;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadFailed    += this.OnDatabaseUnloaded;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += this.OnDatabaseLoaded;

			BooruApp.BooruApplication.EventCenter.WillQuit += OnWillQuit;
		}

		void OnDatabaseLoaded()
		{
			this.importer.Start();
			this.Sensitive = true;
		}

		void OnDatabaseUnloaded()
		{
			this.importer.Abort();
			this.entryStore.Clear();
			this.Sensitive = false;
		}

		void OnWillQuit()
		{
			this.importer.Abort();
		}

		void AddColumnPixbuf(string name, int index) 
		{
			CellRenderer textRenderer = new CellRendererPixbuf ();
			TreeViewColumn column = new TreeViewColumn (name, textRenderer);
			column.AddAttribute (textRenderer, "pixbuf", index);
			ImageEntryView.AppendColumn (column);
		}

		void AddColumnText(string name, int index) 
		{
			CellRenderer textRenderer = new CellRendererText ();
			TreeViewColumn column = new TreeViewColumn (name, textRenderer);
			column.AddAttribute (textRenderer, "text", index);
			ImageEntryView.AppendColumn (column);
		}

		void on_AddButton_clicked(object sender, EventArgs args)
		{
			string lastPath = BooruApp.BooruApplication.Database.GetConfig ("import.mru");

			FileChooserDialog dlg = new FileChooserDialog ("Choose Images", BooruApp.BooruApplication.MainWindow, FileChooserAction.Open);
			dlg.AddButton ("Open", Gtk.ResponseType.Ok);
			dlg.AddButton ("Cancel", Gtk.ResponseType.Cancel);

			dlg.SelectMultiple = true;

			if (!string.IsNullOrEmpty (lastPath))
				dlg.SetCurrentFolder (lastPath);

			if (dlg.Run() == (int)Gtk.ResponseType.Ok) {
				BooruApp.BooruApplication.Database.SetConfig ("import.mru", dlg.CurrentFolder);
				foreach (var path in dlg.Filenames) {
					this.importer.AddImage (path);
				}
			}

			dlg.Destroy ();
		}

		void on_AddFolderButton_clicked(object sender, EventArgs args)
		{
			string lastPath = BooruApp.BooruApplication.Database.GetConfig ("import.mru");

			FileChooserDialog dlg = new FileChooserDialog ("Choose Image Folder", BooruApp.BooruApplication.MainWindow, FileChooserAction.SelectFolder);
			dlg.AddButton ("Open", Gtk.ResponseType.Ok);
			dlg.AddButton ("Cancel", Gtk.ResponseType.Cancel);

			if (!string.IsNullOrEmpty (lastPath))
				dlg.SetCurrentFolder (lastPath);

			if (dlg.Run() == (int)Gtk.ResponseType.Ok) {
				BooruApp.BooruApplication.Database.SetConfig ("import.mru", dlg.CurrentFolder);
				foreach (var path in dlg.Filenames) {
					this.AddFolder (path);
				}
			}

			dlg.Destroy ();
		}

		void AddFolder(string path)
		{
			if (path.Contains("/."))
				return;
			
			foreach (var sub in System.IO.Directory.EnumerateDirectories(path)) {
				this.AddFolder(sub);
			}

			foreach (var file in System.IO.Directory.EnumerateFiles(path)) {
				this.importer.AddImage (file);
			}
		}

		void HandleUpdateEntry (ImageImporter.ImageEntry entry)
		{
			Gtk.Application.Invoke ((o, e) => {
				lock (entries) {
					if (entries.ContainsKey (entry)) {
						var iter = entries[entry];
						lock(entryStore)
							entryStore.SetValues(iter, entry.Preview, System.IO.Path.GetDirectoryName(entry.path), entry.MD5, entry.Status, entry.TagString, entry.LastUpdated);
						this.ImageEntryView.ScrollToCell(this.ImageEntryView.Model.GetPath(iter), this.ImageEntryView.Columns[0], false, 0,0);
					} else {
						lock(entryStore) {
							var iter = entryStore.AppendValues (null, System.IO.Path.GetDirectoryName(entry.path), entry.MD5, entry.Status, entry.TagString, entry.LastUpdated);
							entries [entry] = iter;
						}
					}
				}
			});
		}
	}

}