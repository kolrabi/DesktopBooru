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
			Files
		}

		public enum Severity
		{
			Error,
			Warning,
			Info
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

		}

		private Gdk.Pixbuf InfoPixBuf = null;
		private Gdk.Pixbuf WarningPixBuf = null;
		private Gdk.Pixbuf ErrorPixBuf = null;

		public BooruLog ()
		{
			Model = new Gtk.ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string));

			Gtk.StyleContext stylecontext = new Gtk.StyleContext ();

			InfoPixBuf = Gtk.IconFactory.LookupDefault ("gtk-dialog-info").RenderIconPixbuf (stylecontext, Gtk.IconSize.Menu);
			WarningPixBuf = Gtk.IconFactory.LookupDefault ("gtk-dialog-warning").RenderIconPixbuf (stylecontext, Gtk.IconSize.Menu);
			ErrorPixBuf = Gtk.IconFactory.LookupDefault ("gtk-dialog-error").RenderIconPixbuf (stylecontext, Gtk.IconSize.Menu);
		}

		public void Log(Category category, Severity severity, string message)
		{
			lock (Model) {
				switch (severity) {
				case Severity.Info:
					Model.AppendValues (InfoPixBuf, DateTime.Now.ToString (), category.ToString (), message);
					break;
				case Severity.Warning:
					Model.AppendValues (WarningPixBuf, DateTime.Now.ToString (), category.ToString (), message);
					break;
				case Severity.Error:
					Model.AppendValues (ErrorPixBuf, DateTime.Now.ToString (), category.ToString (), message);
					break;
				}
			}
		}
	}

}

