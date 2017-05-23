using System;
using Gtk;
using Cairo;
using System.Collections.Generic;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class MainTab : LoadableWidget
	{
		[UI] 
		readonly Gtk.Image LogoImage;

		[UI] 
		readonly Gtk.Label DatabaseLabel;

		[UI] 
		readonly Gtk.Label QueueLabel;

		[UI] 
		readonly Gtk.Label VersionLabel;

		[UI]  
		readonly Gtk.Box ButtonBox;

		[UI]  
		readonly Gtk.TreeView LogView;

		uint queueLableIdle = 0;
		long lastQueueCount = 0;

		public static MainTab Create ()
		{
			return LoadableWidget.Create<MainTab> ();
		}

		MainTab (Builder builder, IntPtr handle) : base (builder, handle)
		{
			LogView.AppendColumn ("", new Gtk.CellRendererPixbuf (), "pixbuf", 0);
			LogView.AppendColumn ("Time", new Gtk.CellRendererText (), "text", 1);
			LogView.AppendColumn ("Category", new Gtk.CellRendererText (), "text", 2);
			LogView.AppendColumn ("Message", new Gtk.CellRendererText (), "text", 3);
			LogView.Model = BooruApp.BooruApplication.Log.Model;

			VersionLabel.Text = typeof(BooruApp).Assembly.GetName(false).Version.ToString();

			LogoImage.Pixbuf = (new Gdk.Pixbuf (null, "Booru.Resources.Pixbufs.icon.png"));

			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted   += this.OnDatabaseLoadStarted;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadFailed    += this.OnDatabaseLoadFinished;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += this.OnDatabaseLoadFinished;

			BooruApp.BooruApplication.EventCenter.DatabaseLoadFailed += () => this.DatabaseLabel.Text = "Failed to open '"+BooruApp.BooruApplication.DBFile+"'";
			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted += () => this.DatabaseLabel.Text = "Opening '"+BooruApp.BooruApplication.DBFile+"'...";
			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += () => this.DatabaseLabel.Text = BooruApp.BooruApplication.DBFile;

			this.queueLableIdle = GLib.Timeout.Add (100, () => {
				var count = BooruApp.BooruApplication.TaskRunner.QueueLength;
				if (count != this.lastQueueCount) {
					this.QueueLabel.Text = count.ToString (); 
					this.lastQueueCount = count;
				}
				return true; 
			});
			BooruApp.BooruApplication.EventCenter.WillQuit += () => GLib.Idle.Remove(this.queueLableIdle);
		}

		void OnDatabaseLoadStarted()
		{
			this.ButtonBox.Sensitive = false;
		}

		void OnDatabaseLoadFinished()
		{
			this.ButtonBox.Sensitive = true;
		}

		void on_OpenButton_clicked(object sender, EventArgs args)
		{
			FileChooserDialog dlg = new FileChooserDialog ("Open Database", BooruApp.BooruApplication.MainWindow, FileChooserAction.Open);
			dlg.AddButton ("Open", Gtk.ResponseType.Ok);
			dlg.AddButton ("Cancel", Gtk.ResponseType.Cancel);
			FileFilter filter = new FileFilter ();
			filter.AddPattern ("*.boorudb");
			filter.Name = "Booru Databases";
			dlg.AddFilter (filter);

			if (dlg.Run() == (int)Gtk.ResponseType.Ok) {
				BooruApp.BooruApplication.OpenDatabaseFile (dlg.Filename);
			}

			dlg.Destroy ();
		}

		void on_CreateButton_clicked(object sender, EventArgs args)
		{
			FileChooserDialog dlg = new FileChooserDialog ("Open Database", BooruApp.BooruApplication.MainWindow, FileChooserAction.Save);
			dlg.AddButton ("Create", Gtk.ResponseType.Ok);
			dlg.AddButton ("Cancel", Gtk.ResponseType.Cancel);
			FileFilter filter = new FileFilter ();
			filter.AddPattern ("*.boorudb");
			filter.Name = "Booru Databases";
			dlg.AddFilter (filter);

			if (dlg.Run() == (int)Gtk.ResponseType.Ok) {
				string filename = dlg.Filename;
				if (!filename.EndsWith(".boorudb"))
					filename += ".boorudb";

				BooruApp.BooruApplication.CreateDatabaseFile (filename);
			}

			dlg.Destroy ();
		}
	}

}