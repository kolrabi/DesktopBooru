using System;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru
{
	public class MainWindow : Gtk.Window
	{
		[UI] private Gtk.Notebook MainNotebook;

		public bool IsFullscreen = false;

		public static MainWindow Create ()
		{
			var builder = new Gtk.Builder (null, "Booru.Resources.GUI.MainWindow.glade", null);
			return new MainWindow (builder, builder.GetObject ("MainWindow").Handle);
		}

		protected MainWindow (Gtk.Builder builder, IntPtr handle) : base(handle)
		{
			builder.Autoconnect (this);
		
			this.Icon = new Gdk.Pixbuf (null, "Booru.Resources.Pixbufs.icon.png");

			DeleteEvent += OnDeleteEvent;

			this.MainNotebook.AppendPage (MainTab.Create (), BigTabLabel.Create ("Start"));
			this.MainNotebook.AppendPage (ImagesTab.Create (), BigTabLabel.Create ("Images"));
			this.MainNotebook.AppendPage (VoteTab.Create (), BigTabLabel.Create ("Vote"));
			this.MainNotebook.AppendPage (TagListTab.Create (), BigTabLabel.Create ("Tags"));
			this.MainNotebook.AppendPage (ImportTab.Create (), BigTabLabel.Create ("Import"));
			this.MainNotebook.AppendPage (ConfigTab.Create (), BigTabLabel.Create ("Settings"));

			// when a search is to be executed, select images tab
			BooruApp.BooruApplication.EventCenter.ImageSearchRequested += (arg) => {
				this.MainNotebook.CurrentPage = 1;
			};

			this.KeyPressEvent += (o, args) => {
				if (args.Event.Key == Gdk.Key.F5) {
					this.ToggleFullscreen();
				}
			};

			BooruApp.BooruApplication.EventCenter.Fullscreen (this.IsFullscreen);
		}

		protected void OnDeleteEvent (object sender, Gtk.DeleteEventArgs a)
		{
			a.RetVal = BooruApp.BooruApplication.Quit();
		}

		private void AddGlobalStyleClass(Gtk.Container container, string style)
		{
			if (container == null)
				return;

			foreach (var child in container.AllChildren) {
				var childWidget = child as Gtk.Widget;

				if (childWidget != null) {
					this.AddGlobalStyleClass (childWidget as Gtk.Container, style);
					childWidget.StyleContext.AddClass (style);
				}
			}
		}

		private void RemoveGlobalStyleClass(Gtk.Container container, string style)
		{
			if (container == null)
				return;

			foreach (var child in container.AllChildren) {
				var childWidget = child as Gtk.Widget;

				if (childWidget != null) {
					this.RemoveGlobalStyleClass (childWidget as Gtk.Container, style);
					childWidget.StyleContext.RemoveClass (style);
				}
			}
		}

		public void ToggleFullscreen()
		{
			this.IsFullscreen = !this.IsFullscreen;

			if (this.IsFullscreen) {
				this.Fullscreen ();
				this.AddGlobalStyleClass (this, "fullscreen");
			} else {
				this.Unfullscreen ();
				this.RemoveGlobalStyleClass (this, "fullscreen");
			}

			this.MainNotebook.ShowTabs = !this.IsFullscreen;

			BooruApp.BooruApplication.EventCenter.Fullscreen (this.IsFullscreen);
		}
	}
}

