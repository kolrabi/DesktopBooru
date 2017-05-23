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

		public BooruLog ()
		{
			Model = new Gtk.ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string));

			Gtk.StockItem item;
			if (Gtk.StockManager.LookupItem ("gtk-dialog-warning", out item)) {
			}

		}

		public void Log(Category category, Severity severity, string message)
		{
			Model.AppendValues (null, DateTime.Now.ToString(), category.ToString (), message);
		}
	}

}

