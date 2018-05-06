using System;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class ImageVoteWidget : LoadableWidget
	{
		public delegate void ActionCallback(ImageVoteWidget widget);

		public event ActionCallback OnImageWinner;
		public event ActionCallback OnSkipImage;

		public bool IsPaused 
		{
			get { return this.ImageWidget.IsPaused; }
			set { this.ImageWidget.IsPaused = value; }
		}

		public bool IsFading 
		{ 
			get { return this.ImageWidget.IsFading; } 
		}

		public Image Opponent
		{
			get { return this.Overlay.OpponentImage; }
			set { this.Overlay.OpponentImage = value; }
		}

		public bool IsOverlayActive
		{
			get { return this.Overlay.IsActive; }
			set { this.Overlay.IsActive = value; }
		}

		private Image image;
		public Image Image {
			get { return this.image; }
			set {
				if (this.image == value)
					return;

				if (value != null) {
					value.AddRef ();
				}

				if (this.image != null) {
					this.image.Release ();
				}
				
				this.image = value;

				this.ImageWidget.Image = value;
			}
		}
			
		[UI] readonly Gtk.Box ImageViewBox;
		[UI] readonly ImageViewWidget ImageWidget;
		[UI] readonly TagsOverlay Overlay;
		[UI] readonly Gtk.Menu ImagePopupMenu;

		public static ImageVoteWidget Create ()
		{
			return LoadableWidget.Create<ImageVoteWidget> ();
		}

		ImageVoteWidget (Gtk.Builder builder, IntPtr handle) : base (builder, handle)
		{
			this.ImageWidget = new ImageViewWidget ();
			this.ImageViewBox.PackStart(this.ImageWidget, true, true, 0);

			// enable mouse scrolling
			this.ImageWidget.Events |= Gdk.EventMask.ScrollMask;
			this.ImageWidget.ScrollEvent += (o, args) => {
				this.ImageWidget.AdvanceSubImage (args.Event.Direction == Gdk.ScrollDirection.Down);
			};

			// enable context menu
			this.ImageWidget.Events |= Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.ButtonPressMask;
			this.ImageWidget.ButtonReleaseEvent += (o, args) => {
				if (args.Event.Button == 3) {
					this.ImagePopupMenu.Popup();
				}
			};

			this.Overlay = new TagsOverlay (this.ImageWidget);
			this.Overlay.IsActive = false;
		}

		void on_WinnerButton_clicked(object o, EventArgs args)
		{
			if (this.OnImageWinner != null)
				this.OnImageWinner (this);
		}

		private void on_DeleteButton_clicked(object o, EventArgs args)
		{
			this.image.AddTag ("deleteme");
			if (this.OnSkipImage != null)
				this.OnSkipImage (this);
		}

		private void on_ViewButton_clicked(object o, EventArgs args)
		{
			BooruApp.BooruApplication.EventCenter.ExecuteImageSearch ("#md5:" + image.Details.MD5);
		}

		private void on_OpenExternallyButton_clicked(object o, EventArgs args)
		{
			if (this.image != null)
				this.image.ViewExternal (this.ImageWidget.Handle.ToInt32());
		}

		private void on_BrowseToButton_clicked(object o, EventArgs args)
		{
			if (this.image != null) {
				var path = System.IO.Path.GetDirectoryName (this.image.Details.Path);
				System.Diagnostics.Process.Start ("file://" + path);
			}
		}

		private void on_ExportButton_clicked(object o, EventArgs args)
		{
			if (this.image != null) {
				string folderPath;
				if (SelectExportPathDialog.SelectPath (out folderPath)) {
					string fromPath = this.image.Details.Path;
					string toPath = folderPath + "/" + System.IO.Path.GetFileName (fromPath);
					try {
						System.IO.File.Copy (fromPath, toPath);
					} catch(Exception ex) {
						BooruApp.BooruApplication.Log.Log (BooruLog.Category.Files, ex, "Caught exception trying to copy " + fromPath + " to " + toPath);
					}
				}
			}
		}

		public override void Destroy()
		{
			this.Image = null;
			base.Destroy ();
		}
	}

}