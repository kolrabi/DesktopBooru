using System;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class ClosableTabLabel : LoadableWidget
	{
		[UI]
		readonly Gtk.Label TextLabel;

		Gtk.Notebook ParentNotebook;
		Gtk.Widget PageWidget;

		public static ClosableTabLabel Create (string title, Gtk.Notebook notebook, Gtk.Widget pageWidget)
		{
			return LoadableWidget.Create<ClosableTabLabel>().Init(title, notebook, pageWidget);
		}

		ClosableTabLabel (Gtk.Builder builder, IntPtr handle) : base (builder, handle)
		{
		}

		ClosableTabLabel Init(string title, Gtk.Notebook notebook, Gtk.Widget pageWidget)
		{
			this.TextLabel.Text = title;

			this.ParentNotebook = notebook;
			this.PageWidget = pageWidget;

			return this;
		}

		void closeTab()
		{
			this.ParentNotebook.Remove (this.PageWidget);
			this.PageWidget.Destroy ();
		}

		void on_CloseButton_clicked(object sender, EventArgs args)
		{
			// click the x to close
			this.closeTab ();
		}

		void on_ClosableTabLabel_button_release_event(object sender, Gtk.ButtonReleaseEventArgs args)
		{
			// click middle mouse button to close
			if (args.Event.Button == 2)
				this.closeTab ();
		}
	}

}