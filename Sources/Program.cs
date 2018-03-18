using System;
using System.Collections.Generic;
using System.Threading;

namespace Booru
{
	public class BooruApp
	{
		public static BooruApp BooruApplication  { get; private set; }

		public BooruSettings Settings { get; private set; }
		public Database Database { get; private set; }
		public MainWindow MainWindow { get; private set; }
		public BooruEventCenter EventCenter { get; private set; }
		public BooruLog Log { get; private set; }
		public TaskRunner TaskRunner { get; private set; }
		public Network Network { get; private set; }
		public PluginLoader PluginLoader { get; private set; }

		public string DBFile { get; private set; }
		public int MainThreadId { get; private set; }
		public bool IsMainThread { get { return MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId; } }

		public static void Main (string[] args)
		{
			BooruApp.BooruApplication = new BooruApp ();
			BooruApp.BooruApplication.Init ();
			BooruApp.BooruApplication.Run ();
		}

		private void Init()
		{
			// general gtk initialization
			Gtk.Application.Init ();

			MainThreadId = Thread.CurrentThread.ManagedThreadId;

			// load styles
			var provider = new Gtk.CssProvider ();
			provider.LoadFromData (Resources.LoadResourceString(Resources.ID_STYLES_SCREEN_CSS));
			Gtk.StyleContext.AddProviderForScreen (Gdk.Screen.Default, provider, 600);

			// install handler for glib exceptions
			GLib.ExceptionManager.UnhandledException += (exargs) => PrintUnhandledException(exargs.ExceptionObject);

			// initialize
			this.Log = new BooruLog();
			this.EventCenter = new BooruEventCenter();
			this.TaskRunner = new TaskRunner ();
			this.Settings = new BooruSettings ();
			this.Network = new Network ();
			this.Database = new Database ();
			this.PluginLoader = new PluginLoader ();
			this.PluginLoader.LoadPlugins ();

			// create gui
			this.MainWindow = MainWindow.Create ();
		}

		private static void PrintUnhandledException(object o)
		{
			var ex = o as Exception;
			if (ex != null) {
				Console.WriteLine ("Unhandled " + ex.GetType () + ": " + ex.Message);
				Console.WriteLine (ex.StackTrace);
				while(ex.InnerException != null) {
					ex = ex.InnerException;
					Console.WriteLine ("Inner exception: " + ex.GetType () + ": " + ex.Message);
					Console.WriteLine (ex.StackTrace);
				}
			} else {
				Console.WriteLine ("Unhandled exception: " + o);
			}
		}

		private void Run()
		{
			try {
				// start opening last used database
				string lastDB = this.Settings.Get ("last_used_db");

				if (!string.IsNullOrEmpty (lastDB) && System.IO.File.Exists (lastDB)) {
					this.OpenDatabaseFile(lastDB);
				} else {
					this.Settings.Set ("last_used_db", null);
				}

				// show gui and run
				this.MainWindow.Show ();
				Gtk.Application.Run ();
			} catch (Exception ex) {
				PrintUnhandledException (ex);
			}
		}

		public bool Quit()
		{
			if (this.EventCenter.Quit ()) {
				foreach (var plugin in this.PluginLoader.LoadedPlugins) {
					plugin.OnUnload ();
				}
				Booru.DatabaseQuery.DumpTimes ();
				Gtk.Application.Quit ();
				return true;
			} else {
				return false;
			}
		}

		public void OpenDatabaseFile(string dbFile)
		{
			this.DBFile = dbFile;

			var connection = new Mono.Data.Sqlite.SqliteConnection("Data Source="+dbFile);
			BooruApp.BooruApplication.Database.OpenDatabase(connection);
			BooruApp.BooruApplication.Settings.Set("last_used_db", dbFile);
		}

		public void CreateDatabaseFile(string dbFile)
		{
			this.DBFile = dbFile;

			var connection = new Mono.Data.Sqlite.SqliteConnection("Data Source="+dbFile);

			BooruApp.BooruApplication.Database.CreateDatabase(connection);
			BooruApp.BooruApplication.Settings.Set ("last_used_db", dbFile);
		}
	}
}
