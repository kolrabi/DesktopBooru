using System;

namespace Booru
{
	public class BooruApp
	{
		public static BooruApp BooruApplication;

		public BooruSettings Settings { get; private set; }
		public Database Database { get; private set; }
		public MainWindow MainWindow { get; private set; }
		public BooruEventCenter EventCenter { get; private set; }
		public BooruLog Log { get; private set; }
		public TaskRunner TaskRunner { get; private set; }

		public int MainThreadId { get; private set; }
		public bool IsMainThread { get { return MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId; } }

		public string DBFile;

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

			MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

			// load styles
			var provider = new Gtk.CssProvider ();
			provider.LoadFromData (Resources.LoadResourceString(Resources.ID_STYLES_SCREEN_CSS));

			Gtk.StyleContext.AddProviderForScreen (Gdk.Screen.Default, provider, 600);

			// install handler for glib exceptions
			GLib.ExceptionManager.UnhandledException += (exargs) => {
				var ex = exargs.ExceptionObject as Exception;
				if (ex != null) {
					Console.WriteLine ("Unhandled " + ex.GetType () + ": " + ex.Message);
					Console.WriteLine (ex.StackTrace);
					if (ex.InnerException != null) {
						Console.WriteLine (ex.InnerException.Message);
						Console.WriteLine (ex.InnerException.StackTrace);
					}
				} else {
					Console.WriteLine ("Unhandled exception: " + exargs.ExceptionObject);
				}
			};

			// initialize
			this.Log = new BooruLog();
			this.EventCenter = new BooruEventCenter();
			this.TaskRunner = new TaskRunner ();
			this.Settings = new BooruSettings ();
			this.Database = new Database ();

			// create gui
			this.MainWindow = MainWindow.Create ();
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
				Console.WriteLine ("Global exeption: " + ex.Message);
				Console.WriteLine (ex.StackTrace);
				if (ex.InnerException != null) {
					Console.WriteLine (ex.InnerException.Message);
					Console.WriteLine (ex.InnerException.StackTrace);
				}
			}
		}

		public bool Quit()
		{
			bool ok = this.EventCenter.Quit ();

			if (ok) {
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
