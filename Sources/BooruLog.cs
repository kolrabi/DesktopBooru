using System;

namespace Booru
{
	public class BooruLog
	{
		public Gtk.ListStore Model { get; private set; }

		public enum Category
		{
			Application,
			Database,
			Image,
			Network,
			Files,
			Plugins
		}

		public enum Severity
		{
			Debug = -1,
			Info = 0,
			Warning,
			Error
		}

		public class Logger
		{
			public readonly Category Category;

			public Logger(Category category)
			{
				this.Category = category;
			}

			public void Log(Severity severity, string message)
			{
				BooruApp.BooruApplication.Log.Log (this.Category, severity, message);
			}

			public void Log(Exception ex, string message)
			{
				BooruApp.BooruApplication.Log.Log (this.Category, ex, message);
			}
		}

		Gdk.Pixbuf DebugPixBuf = null;
		Gdk.Pixbuf InfoPixBuf = null;
		Gdk.Pixbuf WarningPixBuf = null;
		Gdk.Pixbuf ErrorPixBuf = null;

		public BooruLog ()
		{
			Model = new Gtk.ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string));

			Gtk.StyleContext stylecontext = new Gtk.StyleContext ();

			DebugPixBuf = Gtk.IconFactory.LookupDefault ("gtk-dialog-question").RenderIconPixbuf (stylecontext, Gtk.IconSize.Menu);
			InfoPixBuf = Gtk.IconFactory.LookupDefault ("gtk-dialog-info").RenderIconPixbuf (stylecontext, Gtk.IconSize.Menu);
			WarningPixBuf = Gtk.IconFactory.LookupDefault ("gtk-dialog-warning").RenderIconPixbuf (stylecontext, Gtk.IconSize.Menu);
			ErrorPixBuf = Gtk.IconFactory.LookupDefault ("gtk-dialog-error").RenderIconPixbuf (stylecontext, Gtk.IconSize.Menu);
		}

		public void Log(Category category, Severity severity, string message)
		{
			lock (Console.Out) {
				lock (Model) {
					var now = DateTime.Now;
					var nowString = string.Format ("{0:yyyy-MM-dd HH:mm:ss.ffff}", now);
					var categoryString = category.ToString ().PadLeft (16);
					switch (severity) {
					case Severity.Debug:
						Model.AppendValues (DebugPixBuf, nowString, category.ToString (), message);
						Console.WriteLine ("[{0}] [DEBUG {2}] {1}", nowString, message, categoryString);
						break;
					case Severity.Info:
						Model.AppendValues (InfoPixBuf, nowString, category.ToString (), message);
						Console.WriteLine ("[{0}] [INFO  {2}] {1}", nowString, message, categoryString);
						break;
					case Severity.Warning:
						Model.AppendValues (WarningPixBuf, nowString, category.ToString (), message);
						Console.WriteLine ("[{0}] [WARN  {2}] {1}", nowString, message, categoryString);
						break;
					case Severity.Error:
						Model.AppendValues (ErrorPixBuf, nowString, category.ToString (), message);
						Console.WriteLine ("[{0}] [ERROR {2}] {1}", nowString, message, categoryString);
						break;
					}
				}
			}
		}

		public void Log(Category category, Exception ex, string message)
		{
			lock (Console.Out) {
				Log (category, Severity.Error, message);
				Log (category, Severity.Error, ex.Message);
				Log (category, Severity.Error, ex.StackTrace);
				if (ex.InnerException != null)
					Log (category, ex.InnerException, "Inner exception:");
			}
		}
	}

}

