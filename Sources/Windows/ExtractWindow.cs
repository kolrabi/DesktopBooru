using System;
using System.Collections.Generic;
using Gtk;
using System.Threading;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru
{
	public class ExtractWindow : Gtk.Window
	{
		[UI] Gtk.Box ImageViewBox;
		[UI] Gtk.Entry TagEntry;
		[UI] Gtk.IconView ImageThumbView;
		[UI] Gtk.Button ButtonSlideshow;
		[UI] Gtk.Button EditTagsButton;
		[UI] Gtk.Spinner Spinner;

		private BooruImage image;
		private ImageViewWidget imageView;
		private readonly Database db;
		private ListStore store;
		private Thread findThread;
		private bool stopThread;

		public static ExtractWindow Create (Database db, Gtk.Window parent)
		{
			Builder builder = new Builder (null, "Booru.interfaces.ExtractWindow.glade", null);
			return new ExtractWindow (builder, builder.GetObject ("window1").Handle, db);
		}

		protected ExtractWindow (Builder builder, IntPtr handle, Database db) : base(handle)
		{
			builder.Autoconnect (this);

			this.db = db;

			this.imageView = new ImageViewWidget ();
			this.ImageViewBox.PackStart(this.imageView, true, true, 2);

			this.imageView.Events |= Gdk.EventMask.ScrollMask;
			this.imageView.ScrollEvent += (o, args) => {
				TreePath path;
				CellRenderer renderer;
				this.ImageThumbView.GetCursor (out path, out renderer);

				TreeIter iter;
				this.store.GetIter (out iter, path);

				if (args.Event.Direction == Gdk.ScrollDirection.Down) {
					if (!this.store.IterNext(ref iter)) {
						this.store.GetIterFirst(out iter);
					}
					path = this.store.GetPath(iter);
				} else {
					this.store.IterPrevious(ref iter);
					path = this.store.GetPath(iter);
				}

				this.ImageThumbView.SetCursor(path, null, false);
				this.ImageThumbView.SelectPath(path);
			};

			this.store = new ListStore(typeof (string), typeof (Gdk.Pixbuf), typeof(BooruImage), typeof(float));
	
			//store.SetSortColumnId(3, SortType.Descending);

			this.ImageThumbView.Model = store;
			this.ImageThumbView.TooltipColumn = 0;
			//this.ImageThumbView.TextColumn = 0;
			this.ImageThumbView.PixbufColumn = 1;
		}

		protected void on_ImageThumbView_key_press_event(object sender, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.e) {
				this.on_EditTagsButton_clicked(null ,null);
			}
		}

		protected void on_window1_delete_event(object sender, Gtk.DeleteEventArgs args)
		{
			if (this.findThread != null) {
				this.stopThread = true;
				this.findThread.Join ();
				this.findThread = null;
			}

			if (this.slideShowTimer != 0) {
				GLib.Timeout.Remove (this.slideShowTimer);
				this.slideShowTimer = 0;
			}
		}

		private uint slideShowTimer = 0;
		protected void on_ButtonSlideshow_clicked(object sender, EventArgs args)
		{
			if (slideShowTimer == 0) {
				ButtonSlideshow.Label = "Stop Slideshow";
				slideShowTimer = GLib.Timeout.Add (5000, () => {
					TreePath path;
					CellRenderer renderer;
					this.ImageThumbView.GetCursor (out path, out renderer);

					TreeIter iter;
					this.store.GetIter (out iter, path);

					if (!this.store.IterNext(ref iter)) {
						this.store.GetIterFirst(out iter);
					}
					path = this.store.GetPath(iter);
					this.ImageThumbView.SetCursor(path, null, false);
					this.ImageThumbView.SelectPath(path);
					return true;
				});
			} else {
				ButtonSlideshow.Label = "Start Slideshow";
				GLib.Timeout.Remove (this.slideShowTimer);
				this.slideShowTimer = 0;
			}
		}

		protected void on_EditTagsButton_clicked(object sender, EventArgs args)
		{			
			var editTagsDlg = EditTagsWindow.Create (this.image, this);
			imageView.Paused = true;
			editTagsDlg.Run ();
			UpdateTagButton ();
			imageView.QueueDraw ();
			imageView.Paused = false;
		}

		private void UpdateTagButton()
		{
			if (this.image == null) {
				this.EditTagsButton.Sensitive = false;
				return;
			}

			this.EditTagsButton.Sensitive = true;
			if (this.image.Tags.Count > 3) {
				this.EditTagsButton.StyleContext.RemoveClass ("red");
				this.EditTagsButton.StyleContext.AddClass ("green");
			} else {
				this.EditTagsButton.StyleContext.RemoveClass ("green");
				this.EditTagsButton.StyleContext.AddClass ("red");
			}
		}

		protected void on_TagEntry_activate(object sender, EventArgs args)
		{
			if (this.findThread != null) {
				this.stopThread = true;
				this.findThread.Join ();
				this.findThread = null;
			}

			this.store.Clear ();
			this.stopThread = false;

			this.store = new ListStore(typeof (string), typeof (Gdk.Pixbuf), typeof(BooruImage), typeof(float));
			//store.SetSortColumnId(3, SortType.Descending);
			this.ImageThumbView.Model = store;
			this.Spinner.Active = true;

			System.IO.Directory.CreateDirectory ("/home/kolrabi/x/extract/" + TagEntry.Text);

			var start = new ThreadStart (() => {
				var reader = this.db.QueryImagesWithTags (TagEntry.Text.Split (" ".ToCharArray (), StringSplitOptions.RemoveEmptyEntries));
				var readerColumns = new Dictionary<string, int> ();
				for (int i = 0; i < reader.FieldCount; i++)
					readerColumns [reader.GetName (i)] = i;

				while (reader.Read () && !this.stopThread) {
					string path = reader.GetString (readerColumns ["path"]);
					object md5obj = reader.GetValue (readerColumns ["md5sum"]);
					float elo = reader.GetFloat (readerColumns ["elo"]);
					int votes = reader.GetInt32 (readerColumns ["votes"]);
					byte[] md5blob = (byte[])md5obj;

					var data = new BooruImageData (MD5Helper.BlobToMD5 (md5blob), path, elo, votes);
					var image = new BooruImage (data, this.db);

					try {
						var info = new Mono.Unix.UnixFileInfo(data.Path);
						info.CreateSymbolicLink("/home/kolrabi/x/extract/" + TagEntry.Text + "/"+data.MD5+System.IO.Path.GetExtension(data.Path));
						//System.IO.File.Copy(data.Path, "/home/kolrabi/x/extract/" + TagEntry.Text + "/"+data.MD5+System.IO.Path.GetExtension(data.Path));
					} catch(Exception ex) {
						Console.WriteLine("Could not copy "+data.Path+": "+ ex.Message);
					}

					Gtk.Application.Invoke((s,a)=>{
						this.store.AppendValues (data.Path+"\n"+string.Join(" ", image.Tags), null, image, data.ELO);
					});
				}
				Gtk.Application.Invoke((s,a)=>{
					this.Spinner.Active=false;
				});
			});
			this.findThread = new Thread (start);
			this.findThread.Start ();
		}
			
		private void on_ImageThumbView_selection_changed(object obj, EventArgs args)
		{
			lock (this.store) {
				TreePath path;
				CellRenderer renderer;
				if (!this.ImageThumbView.GetCursor (out path, out renderer) || path == null)
					return;

				TreeIter iter;
				if (!this.store.GetIter (out iter, path))
					return;

				if (this.image != null)
					this.image.Dispose ();

				BooruImage image = (BooruImage)this.store.GetValue (iter, 2);
				this.imageView.Anim = image.Anim;
				this.image = image;
				UpdateTagButton ();

				SendImage ();
			}
		}

		Thread sendThread;
		System.Net.Sockets.Socket socket;

		private void SendImage()
		{
			if (this.image.ImageType != BooruImageType.Image)
				return;
			
			if (sendThread != null) {
				sendThread.Abort ();
				if (this.socket != null) {
					this.socket.Close ();
					this.socket.Dispose ();
				}
			}
			sendThread = new Thread(() => {
				try {
					this.socket = new System.Net.Sockets.Socket( System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp );
					socket.LingerState = new System.Net.Sockets.LingerOption(true, 3);
					socket.Connect("192.168.192.38", 9966);

					var reader = System.IO.File.OpenRead(this.image.Data.Path);
					int length = (int)reader.Length;
					byte[] dataBytes = new byte[length + 4];
					dataBytes[0] = (byte)((length)&0xff);
					dataBytes[1] = (byte)((length >> 8)&0xff);
					dataBytes[2] = (byte)((length >> 16)&0xff);
					dataBytes[3] = (byte)((length >> 24)&0xff);

					int offset = 0;
					while(offset<length) {
						offset += reader.Read(dataBytes, offset + 4, length - offset);
					}

					offset = 0;
					while(offset<length+4) {
						offset += socket.Send(dataBytes, offset, length+4 - offset, System.Net.Sockets.SocketFlags.None);
					}
				} catch (Exception ex) {
					Console.WriteLine("Exception caught: "+ex.Message);
				}
			});
			sendThread.Start ();
		}

		protected void on_FullScreenButton_clicked(object o, EventArgs args)
		{
			if (this.image != null)
				this.image.ViewExternal ();
		}

	}
}

