using System;
using System.Collections.Generic;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru
{
	public class EditTagsWindow : Gtk.Dialog
	{
		private readonly BooruImage image;

		[UI] Gtk.Box ImageViewBox;
		[UI] Gtk.Entry TagEntry;

		private ImageViewWidget imageView;

		private static Gtk.EntryCompletion completion;

		public static EditTagsWindow Create (BooruImage image, Gtk.Window parent)
		{
			Builder builder = new Builder (null, "Booru.interfaces.EditTagsWindow.glade", null);
			return new EditTagsWindow (builder, builder.GetObject ("dialog1").Handle, image);
		}

		protected EditTagsWindow (Builder builder, IntPtr handle, BooruImage image) : base(handle)
		{
			builder.Autoconnect (this);

			var db = image.db;
			List<string> tags = db.GetAllTags ();
			tags.Sort ();

			if (completion == null) {
				var completionStore = new Gtk.ListStore (typeof(string));
				foreach (var tag in tags) {
					completionStore.AppendValues (tag);
					completionStore.AppendValues ("-" + tag);
				}
			
				completion = new Gtk.EntryCompletion ();
				completion.Model = completionStore;
				completion.TextColumn = 0;
				completion.MinimumKeyLength = 3;
			}

			TagEntry.Completion = completion;

			this.image = image;
			this.imageView = new ImageViewWidget ();
			this.imageView.Drawn += (System.Object o, Gtk.DrawnArgs args) => {
				BooruImageWidget.ImageViewTagsOverlay(args.Cr, this.image, this.imageView);
			};
			this.ImageViewBox.PackStart(this.imageView, true, true, 2);
			this.imageView.Anim = image.Anim;

			this.Maximize ();
		}

		protected void on_dialog1_close(object sender, EventArgs args)
		{
			this.Respond (Gtk.ResponseType.Ok);
			this.Hide ();
		}

		protected void on_ReimportButton_clicked(object sender, EventArgs args)
		{
			var importWindow = ImportFilesWindow.Create (this.image.Data.Path, this.image.db);
			importWindow.Run();
			this.image.ReloadTags ();
			this.imageView.QueueDraw ();
		}
		protected void on_OkButton_activate(object sender, EventArgs args)
		{
			this.Respond (Gtk.ResponseType.Ok);
			this.Hide ();
		}

		protected void on_TagEntry_activate(object sender, EventArgs args)
		{
			var enteredTags = TagEntry.Text.Split (" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			if (enteredTags.Length == 0) {
				this.Respond (Gtk.ResponseType.Ok);
				this.Hide ();
				return;
			}
			foreach (string tag in enteredTags) {
				if (tag.StartsWith ("-")) {
					this.image.RemoveTag (tag.Substring (1));
				} else {
					int tagId = this.image.db.GetTagId (tag);
					if (tagId == -1) {
						var resolve = ResolveTagWindow.Create (tag, this.image.db);
						resolve.Parent = this;
						if (resolve.Run () == (int)Gtk.ResponseType.Ok) {
							this.image.db.GetOrCreateTagId (resolve.Tag);
							this.image.AddTag (resolve.Tag);
						}
						resolve.Hide ();
					} else {
						this.image.AddTag (tag);
					}
				}
			}
			TagEntry.Text = "";
			this.imageView.QueueDraw ();
		}

	}
}

