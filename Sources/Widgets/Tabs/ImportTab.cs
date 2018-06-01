using System;
using System.Linq;
using Gtk;
using Cairo;
using System.Collections.Generic;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class ImportTab : LoadableWidget
	{
		[UI] Gtk.TreeView ImageEntryView;

		private ImageImporter importer;
		private IDictionary<ImageImporter.ImageEntry, Gtk.TreeRowReference> entries = new Dictionary<ImageImporter.ImageEntry, Gtk.TreeRowReference>();
		private Gtk.ListStore entryStore = new Gtk.ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

		public static ImportTab Create ()
		{
			return LoadableWidget.Create<ImportTab> ();
		}

		ImportTab (Builder builder, IntPtr handle) : base (builder, handle)
		{
			//this.AddColumnPixbuf ("Preview", 0);
			this.AddColumnText ("Path", 1, false, true);
			this.AddColumnText ("MD5", 2);
			this.AddColumnText ("Status", 3);
			this.AddColumnText ("LastUpdated", 5);
			this.AddColumnText ("Sites", 6);
			this.AddColumnText ("Tags", 4, true);

			this.ImageEntryView.Model = this.entryStore;

			this.importer = new ImageImporter ();
			this.importer.UpdateEntry += HandleUpdateEntry;

			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted   += this.OnDatabaseUnloaded;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadFailed    += this.OnDatabaseUnloaded;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += this.OnDatabaseLoaded;

			BooruApp.BooruApplication.EventCenter.WillQuit += OnWillQuit;

			GLib.Timeout.Add(100, UpdateScrolling);
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

		void AddColumnText(string name, int index, bool wrap = false, bool ellipsis = false) 
		{
			CellRendererText textRenderer = new CellRendererText ();
			if (ellipsis) {
				textRenderer.Ellipsize = Pango.EllipsizeMode.Middle;
			}

			textRenderer.Alignment = Pango.Alignment.Left;

			TreeViewColumn column = new TreeViewColumn (name, textRenderer);
			column.Resizable = true;
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
			dlg.SelectMultiple = true;

			if (!string.IsNullOrEmpty (lastPath))
				dlg.SetCurrentFolder (lastPath);

			if (dlg.Run() == (int)Gtk.ResponseType.Ok) {
				BooruApp.BooruApplication.Database.SetConfig ("import.mru", dlg.CurrentFolder);
				BooruApp.BooruApplication.TaskRunner.StartTaskAsync ("Import add folders", () => {
					foreach (var path in dlg.Filenames) {
						this.AddFolder (path);
					}
				});
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

		bool UpdateScrolling()
		{
			if (this.scrollToPath != null && this.scrollToPath != lastScrollToPath) {
				if (lastScrollToPath == null || this.scrollToPath.Indices [0] > this.lastScrollToPath.Indices [0]) {
					this.ImageEntryView.ScrollToCell (this.scrollToPath, this.ImageEntryView.Columns [0], false, 0, 0);
					this.lastScrollToPath = scrollToPath;
				}
			}
			return true;
		}

		Gtk.TreePath lastScrollToPath = null;
		Gtk.TreePath scrollToPath = null;

		void HandleUpdateEntry (ImageImporter.ImageEntry entry)
		{
			var tagString = entry.TagString;
			List<String> tags = new List<string>(tagString.Split(" ".ToCharArray()));
			List<String> siteTags = tags.FindAll((s) => s.StartsWith("known_on_") || s.StartsWith("not_on_"));
			tags.RemoveAll((s) => siteTags.Contains(s));

			siteTags.RemoveAll((s) => s.StartsWith("not_on_"));
			for (int i=0; i<siteTags.Count; i++) {
				siteTags[i] = siteTags[i].Replace("known_on_", "");
			}

			string sites = string.Join(" ", siteTags);
			tagString = string.Join(" ", tags);

			string entryPath = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(entry.path)) + "/" + System.IO.Path.GetFileName(entry.path);

			//Gtk.Application.Invoke ((o,a) => {
			BooruApp.BooruApplication.TaskRunner.StartTaskMainThread("Import update entry", ()=> {
				lock (entries) {
					if (entries.ContainsKey (entry)) {
						var rowRef = entries[entry];
						Gtk.TreeIter iter;
						if (rowRef.Model.GetIter(out iter, entries[entry].Path)) {
							entryStore.SetValues(iter, 
								entry.Preview, 
								entryPath,
								entry.MD5, 
								entry.Status, 
								tagString, 
								entry.LastUpdated, 
								sites
							);
							scrollToPath = this.ImageEntryView.Model.GetPath (iter);
						} else {
							BooruApp.BooruApplication.Log.Log(BooruLog.Category.Application, BooruLog.Severity.Warning, "Could not update import entry "+entry.MD5+", row reference was invalid");
						}
						scrollToPath = this.ImageEntryView.Model.GetPath (iter);
					} else {
						var iter = entryStore.AppendValues (
							entry.Preview, 
							entryPath, 
							entry.MD5, 
							entry.Status, 
							tagString, 
							entry.LastUpdated, 
							sites
						);
						entries [entry] = new Gtk.TreeRowReference(entryStore, entryStore.GetPath(iter));
					}
				}
			});
		}
	}

}