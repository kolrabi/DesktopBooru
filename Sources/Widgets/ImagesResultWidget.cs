using System;
using System.Linq;
using System.Collections.Generic;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class ImagesResultWidget : LoadableWidget
	{
		[UI] Gtk.Box ImageViewBox;
		[UI] Gtk.IconView ImageThumbView;
		[UI] Gtk.Button ButtonSlideshow;
		[UI] Gtk.Button OpenExternalButton;
		[UI] Gtk.Button ExportButton;
		[UI] Gtk.Button DeleteButton;
		[UI] Gtk.Button StopButton;
		[UI] Gtk.ToggleButton ShowTagsButton;
		[UI] Gtk.ToggleButton ShuffleButton;
		[UI] Gtk.Spinner Spinner;
		[UI] Gtk.Entry TagsEntry;
		[UI] Gtk.Label ResultCountLabel;
		[UI] Gtk.ButtonBox ButtonBox;
		[UI] Gtk.Box ThumbBox;
		[UI] Gtk.ProgressBar ExportProgress;
		[UI] Gtk.Button MarkButton;

		Gtk.Image TagImage;
		Gtk.Image PlayImage;
		Gtk.Image StopImage;
		Gtk.Image ShuffleImage;
		Gtk.Image MarkImage;
		Gtk.Image UnmarkImage;
		Gtk.Image DeleteImage;
		Gtk.Image ViewExternalImage;
		Gtk.Image ExportImage;
		Gtk.Image AbortImage;

		public Image ActiveImage;

		private readonly ThumbStore store;
		private readonly ImageViewWidget imageView;
		private readonly TagsOverlay tagsOverlay;

		private ImageFinder finder;

		private ImageExporter exporter;

		private int foundImageCount = 0;
		private int markedImageCount = 0;
		private uint idle = 0;

		public static ImagesResultWidget Create (string searchString)
		{
			return LoadableWidget.Create<ImagesResultWidget> ().Init (searchString);
		}

		ImagesResultWidget (Gtk.Builder builder, IntPtr handle) : base (builder, handle)
		{
			builder.Autoconnect (this);

			BooruApp.BooruApplication.EventCenter.WillQuit += Abort;
			BooruApp.BooruApplication.EventCenter.FullscreenToggled += this.ToggleFullscreen;

			this.PlayImage = new Gtk.Image (Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_BUTTON_PLAY));
			this.StopImage = new Gtk.Image (Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_BUTTON_STOP));
			this.TagImage = new Gtk.Image (Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_BUTTON_TAG));
			this.ShuffleImage = new Gtk.Image (Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_BUTTON_SHUFFLE));
			this.MarkImage = new Gtk.Image (Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_BUTTON_MARK));
			this.UnmarkImage = new Gtk.Image (Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_BUTTON_UNMARK));
			this.DeleteImage = new Gtk.Image (Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_BUTTON_DELETE));
			this.ViewExternalImage = new Gtk.Image (Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_BUTTON_VIEW_EXTERNAL));
			this.ExportImage = new Gtk.Image (Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_BUTTON_EXPORT));
			this.AbortImage = new Gtk.Image (Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_BUTTON_ABORT));

			this.ButtonSlideshow.Image = this.PlayImage;
			this.ShowTagsButton.Image = this.TagImage;
			this.ShuffleButton.Image = this.ShuffleImage;
			this.MarkButton.Image = this.MarkImage;
			this.DeleteButton.Image = this.DeleteImage;
			this.OpenExternalButton.Image = this.ViewExternalImage;
			this.ExportButton.Image = this.ExportImage;
			this.StopButton.Image = this.AbortImage;

			this.Removed += (o, args) => {
				this.Abort ();
			};

			// TODO: add custom tag input widget
			/*
			var tagbox = new TagBoxWidget ();
			ImageViewBox.PackEnd (tagbox, false, false, 0);
			tagbox.Show ();
			*/

			// add image view
			this.imageView = new ImageViewWidget ();
			this.ImageViewBox.PackStart (this.imageView, true, true, 0);

			// enable mouse scrolling
			this.imageView.Events |= Gdk.EventMask.ScrollMask;
			this.imageView.ScrollEvent += (o, args) => {
				this.Advance (args.Event.Direction == Gdk.ScrollDirection.Down);
			};

			// add overlay for tag display
			this.tagsOverlay = new TagsOverlay (this.imageView);

			// setup tag entry autocompletion
			var completion = new Gtk.EntryCompletion ();
			completion.Model = BooruApp.BooruApplication.Database.TagEntryCompletionStore;
			completion.TextColumn = 0;
			completion.MinimumKeyLength = 3;
			this.TagsEntry.Completion = completion;

			// set up thumb list view
			this.store = new ThumbStore ();
			this.ImageThumbView.PixbufColumn = ThumbStore.THUMB_STORE_COLUMN_THUMBNAIL;
			this.ImageThumbView.TooltipColumn = ThumbStore.THUMB_STORE_COLUMN_TOOLTIP;
			this.ImageThumbView.TextColumn = ThumbStore.THUMB_STORE_COLUMN_INDEX;
			this.ImageThumbView.ItemWidth = 64;

			this.ImageThumbView.Model = this.store;

			this.ImageThumbView.Model.RowInserted += on_ImageThumbView_Model_RowInserted;
			this.ImageThumbView.Model.RowChanged += on_ImageThumbView_Model_RowChanged;

			this.ImageThumbView.Events |= Gdk.EventMask.KeyPressMask;
			this.ImageThumbView.KeyPressEvent += on_ImageThumbView_KeyPress;

			this.StopButton.Sensitive = true;
			this.Spinner.Active = true;
			this.ExportButton.Sensitive = false;
			this.MarkButton.Sensitive = false;
			this.DeleteButton.Sensitive = false;

			//var box = (Gtk.Box)this.StopButton.Parent.Parent;
			//this.tagsBox = new TagsEntryWidget ();
			//box.PackEnd (this.tagsBox, false, true, 0);
			//this.tagsBox.Show ();

			this.idle = GLib.Timeout.Add (100, () => {
				if (this.imageView.Image != this.ActiveImage) {
					// get newly selected image
					this.imageView.Image = this.ActiveImage;

					// this.tagsBox.SetTags (image.Tags);

					// clear tags entry to not confuse user
					this.TagsEntry.Text = "";
					this.UpdateButtons ();
				}
				return true;
			});
		}

		ImagesResultWidget Init(string searchString)
		{
			// finder for image search string
			this.finder = new ImageFinder (searchString, this.store);
			this.store.LastImageAdded += () => BooruApp.BooruApplication.TaskRunner.StartTaskMainThread(()=>{
				this.Spinner.Active = false;
				this.StopButton.Visible = false;
				this.ExportButton.Sensitive = true;
			});

			// start finding images
			this.finder.Start ();

			return this;
		}

		private void Abort()
		{
			this.finder.Abort ();

			this.StopSlideShow ();
		}

		public override void Destroy()
		{
			this.Abort ();

			GLib.Timeout.Remove (this.idle);
			this.idle = 0;

			Gtk.TreeIter iter;
			var model = this.ImageThumbView.Model as ThumbStore;

			if (model.GetIterFirst (out iter)) {
				do {
					model.GetImage(iter).Release();
				} while(model.IterNext (ref iter));
			}

			base.Destroy();
		}

		#region Image navigation

		// select next/previous image in list
		private Gtk.TreePath GetNextPath(Gtk.TreePath path, bool forward)
		{
			if (path == null)
				return this.foundImageCount == 0 ? null : new Gtk.TreePath ("0");
			
			int index = path.Indices [0];

			if (forward) {
				index++;
				if (index >= this.foundImageCount) {
					index = 0;
				}
			} else {
				index--;
				if (index < 0) {
					index = this.foundImageCount - 1;
				}
			}
			return new Gtk.TreePath (index.ToString ());
		}

		void Advance(bool forward)
		{
			Gtk.TreePath path = new Gtk.TreePath ();

			if (this.ImageThumbView.SelectedItems.Length > 0) {
				if (forward) {
					foreach (var selPath in this.ImageThumbView.SelectedItems) {
						if (path.Depth == 0 || path.Compare (selPath) < 0)
							path = selPath;
					}
				} else {
					foreach (var selPath in this.ImageThumbView.SelectedItems) {
						if (path.Depth == 0 || path.Compare (selPath) > 0)
							path = selPath;
					}
				}
				if (this.imageView.AdvanceSubImage (forward))
					return;
			}

			path = this.GetNextPath (path, forward);
			this.ImageThumbView.UnselectAll ();
			this.SelectImagePath (path);
		}
			
		void on_ImageThumbView_Model_RowInserted(object sender, Gtk.RowInsertedArgs args)
		{
			System.Diagnostics.Debug.Assert (BooruApp.BooruApplication.IsMainThread);

			foundImageCount ++;
			this.UpdateCountLabel ();
		}

		/// <summary>
		/// Called from the thumb store when a new thumbnail was added. 
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Arguments.</param>
		void on_ImageThumbView_Model_RowChanged(object sender, Gtk.RowChangedArgs args)
		{
			System.Diagnostics.Debug.Assert (BooruApp.BooruApplication.IsMainThread);

			// make sure to select first image
			if (this.ActiveImage == null) {
				this.SelectImagePath (args.Path);
			}

			if (this.store.IsFinished)
				this.ReCount ();
		}

		void ReCount()
		{
			this.markedImageCount = 0;
			this.foundImageCount = 0;

			Gtk.TreeIter iter;
			if (this.store.GetIterFirst (out iter)) {
				do {
					Image image = this.store.GetImage(iter);
					if (image.Tags.Contains("deleteme"))
						this.markedImageCount ++;
					this.foundImageCount++;
				} while (this.store.IterNext(ref iter));
			}

			this.UpdateCountLabel ();
		}

		void UpdateCountLabel()
		{
			// update image count label 
			this.ResultCountLabel.Text = string.Format("{0} results, {1} unmarked", this.foundImageCount, this.foundImageCount - this.markedImageCount);
		}

		void SelectImageRowRef(Gtk.TreeRowReference rowRef)
		{
			// select image in thumb list
			this.ImageThumbView.SetCursor (rowRef.Path, null, false);
			this.ImageThumbView.SelectPath (rowRef.Path);
			this.ImageThumbView.ScrollToPath (rowRef.Path);
		}

		void SelectImagePath(Gtk.TreePath Path)
		{
			this.SelectImageRowRef(new Gtk.TreeRowReference(this.store, Path));
		}
			
		void on_ImageThumbView_selection_changed(object obj, EventArgs args)
		{
			this.on_ImageThumbView_move_cursor (obj, null);
			this.UpdateButtons ();
		}

		private Gtk.TreePath lastSelectionBegin;
		private Gtk.TreePath lastSelectionEnd;

		void on_ImageThumbView_move_cursor(object obj, Gtk.MoveCursorArgs args)
		{
			if (this.ImageThumbView.SelectedItems.Length == 0)
				return;
			
			Gtk.TreePath selectionBegin = null;
			Gtk.TreePath selectionEnd = null;

			foreach (var p in this.ImageThumbView.SelectedItems) {
				if (selectionBegin == null || selectionBegin.Indices [0] > p.Indices [0]) {
					selectionBegin = p;
				}
				if (selectionEnd == null || selectionEnd.Indices [0] < p.Indices [0]) {
					selectionEnd = p;
				}
			}

			Gtk.TreePath newActiveImagePath = null;

			if (this.lastSelectionBegin == null || this.lastSelectionBegin.Indices[0] != selectionBegin.Indices[0]) {
				newActiveImagePath = selectionBegin;
			}
			if (this.lastSelectionEnd == null || this.lastSelectionEnd.Indices[0] != selectionEnd.Indices[0]) {
				newActiveImagePath = selectionEnd;
			}

			this.lastSelectionBegin = selectionBegin;
			this.lastSelectionEnd = selectionEnd;

			this.ActiveImage = this.store.GetImage (new Gtk.TreeRowReference(this.store, newActiveImagePath));
		}

		void on_ImageThumbView_KeyPress(object obj, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete) {
				bool isShift = (args.Event.State & Gdk.ModifierType.ShiftMask) != 0;
				bool anyMarked = this.IsAnySelectedMarkedForDelete ();

				if (anyMarked) {
					if (isShift) {
						// unmark
						this.on_MarkButton_clicked (obj, null);
					} else {
						// delete 
						this.on_DeleteButton_clicked (obj, null);
					}
				} else {
					if (!isShift) {
						// mark
						this.on_MarkButton_clicked(obj, null);
					}
				}
			}
		}
		#endregion

		#region Slideshow

		uint slideShowTimer = 0;
		bool IsSlideShowRunning { get { return slideShowTimer != 0; }}
		readonly Random shuffleRandom = new Random();

		void StartSlideShow()
		{
			if (!this.IsSlideShowRunning) {
				this.ButtonSlideshow.Image = this.StopImage;
				this.ButtonSlideshow.TooltipText = "Stop Slideshow";
			}

			if (this.slideShowTimer != 0)
				GLib.Timeout.Remove (this.slideShowTimer);

			var dbConfig = BooruApp.BooruApplication.Database.Config;
			this.slideShowTimer = GLib.Timeout.Add ((uint)dbConfig.GetInt ("slideshow.timeout"), this.SlideshowTimeout);
		}

		bool SlideshowTimeout()
		{
			if (this.ShuffleButton.Active) {
				this.ImageThumbView.UnselectAll ();
				this.SelectImagePath(new Gtk.TreePath((shuffleRandom.Next()%this.foundImageCount).ToString()));
			} else {
				this.Advance (true);
			}
			this.StartSlideShow ();
			return false;
		}

		void StopSlideShow()
		{
			if (!this.IsSlideShowRunning)
				return;
			
			this.ButtonSlideshow.Image = this.PlayImage;
			this.ButtonSlideshow.TooltipText = "Start Slideshow";
			GLib.Timeout.Remove (this.slideShowTimer);
			this.slideShowTimer = 0;
		}

		void on_ButtonSlideshow_clicked(object sender, EventArgs args)
		{
			if (this.IsSlideShowRunning) {
				this.StopSlideShow ();
			} else {
				this.StartSlideShow ();
			}
		}
		#endregion

		bool AreAllSelectedMarkedForDelete()
		{
			foreach (var path in this.ImageThumbView.SelectedItems) {
				var rowRef = new Gtk.TreeRowReference (this.store, path);
				var image = this.store.GetImage (rowRef);

				if (image == null)
					continue;
				if (image.Tags == null)
					continue;

				if (!image.Tags.Contains ("deleteme"))
					return false;
			}
			return true;
		}
			
		bool IsAnySelectedMarkedForDelete()
		{
			foreach (var path in this.ImageThumbView.SelectedItems) {
				var rowRef = new Gtk.TreeRowReference (this.store, path);
				var image = this.store.GetImage (rowRef);

				if (image == null)
					continue;
				if (image.Tags == null)
					continue;

				if (image.Tags.Contains ("deleteme"))
					return true;
			}
			return false;
		}

		void UpdateButtons()
		{
			if (this.ImageThumbView.SelectedItems.Length == 0) {
				this.MarkButton.Sensitive = false;
				this.DeleteButton.Sensitive = false;
			} else {
				bool allMarked = this.AreAllSelectedMarkedForDelete ();
				bool anyMarked = this.IsAnySelectedMarkedForDelete ();
				this.MarkButton.Sensitive = true;
				this.DeleteButton.Sensitive = allMarked;

				if (anyMarked) {
					this.MarkButton.Image = this.UnmarkImage;
					this.MarkButton.TooltipText = "Unmark Image(s) for Deletion";
				} else {
					this.MarkButton.Image = this.MarkImage;
					this.MarkButton.TooltipText = "Mark Image(s) for Deletion";
				}
			}
			this.ReCount ();
		}

		void on_DeleteButton_clicked(object sender, EventArgs args)
		{
			Queue<Gtk.TreeRowReference> rowRefs = new Queue<Gtk.TreeRowReference> ();
			List<Image> images = new List<Image> ();
			foreach (var path in this.ImageThumbView.SelectedItems) {
				var rowRef = new Gtk.TreeRowReference (this.store, path);
				rowRefs.Enqueue (rowRef);

				var image = this.store.GetImage (rowRef);
				images.Add (image);
			}

			this.ImageThumbView.UnselectAll ();

			while (rowRefs.Count > 0) {
				Gtk.TreePath path = rowRefs.Dequeue().Path;

				Gtk.TreeIter iter;
				if (this.store.GetIter (out iter, path))
					this.store.Remove (ref iter);

				this.foundImageCount--;
			}

			var dbConfig = BooruApp.BooruApplication.Database.Config;
			bool moveImages = dbConfig.GetBool ("deletemove.enable");
			var targetPath = moveImages ? dbConfig.GetString("deletemove.path") : "";
			foreach(var image in images) {
				BooruApp.BooruApplication.TaskRunner.StartTaskAsync (() => {
					var paths = BooruApp.BooruApplication.Database.GetImagePaths(image.Details.MD5);
					foreach (string source in paths) {
						try {
							string target = targetPath + "/" + System.IO.Path.GetFileName (source);
							if (System.IO.File.Exists (source)) {
								if (moveImages && !System.IO.File.Exists(target)) {
									System.IO.File.Move (source, target);
									BooruApp.BooruApplication.Log.Log (BooruLog.Category.Files, BooruLog.Severity.Info, "Moved file " + source + " to " + targetPath);
								} else {
									System.IO.File.Delete (source);
									BooruApp.BooruApplication.Log.Log (BooruLog.Category.Files, BooruLog.Severity.Info, "Deleted file " + source);
								}
							}
						} catch (Exception ex) {
							BooruApp.BooruApplication.Log.Log (BooruLog.Category.Files, ex, "Caught exception trying to delete file " + source);
						}
					}
					BooruApp.BooruApplication.Database.RemoveImage (image.Details.MD5);
				});
			}

			this.UpdateButtons ();
		}

		void on_MarkButton_clicked(object sender, EventArgs args)
		{
			List<Image> images = new List<Image> ();
			foreach (var path in this.ImageThumbView.SelectedItems) {
				var image = this.store.GetImage (new Gtk.TreeRowReference (this.store, path));
				image.AddRef ();
				images.Add (image);
			}

			bool anyMarked = this.IsAnySelectedMarkedForDelete ();
			if (anyMarked) {
				new System.Threading.Thread (new System.Threading.ThreadStart (() => {
					foreach (var image in images) {
						image.RemoveTag ("deleteme");
						image.Release ();
					}
				})).Start ();
			} else {
				new System.Threading.Thread (new System.Threading.ThreadStart (() => {
					foreach (var image in images) {
						image.AddTag ("deleteme");
						image.Release ();
					}
				})).Start ();
			}

			this.Advance (true);
			this.UpdateButtons ();
		}

		void on_OpenExternalButton_clicked(object sender, EventArgs args)
		{
			if (this.ActiveImage != null)
				this.ActiveImage.ViewExternal (this.imageView.Window.Visual.OwnedHandle.ToInt32());
		}

		void on_ShowTagsButton_toggled(object sender, EventArgs args)
		{
			this.tagsOverlay.IsActive = this.ShowTagsButton.Active;
			this.TagsEntry.Visible = this.ShowTagsButton.Active;
		}

		void on_TagsEntry_activate(object sender, EventArgs args)
		{
			if (this.ActiveImage == null)
				return;

			var enteredTags = this.TagsEntry.Text.Split (" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

			if (enteredTags.Length == 0) {
				this.Advance (true);
				return;
			}
				
			foreach (string enteredTag in enteredTags) {
				if (enteredTag.StartsWith ("-")) {
					var tag = BooruApp.BooruApplication.Database.GetCanonicalTag (enteredTag.Substring (1));
					this.ActiveImage.RemoveTag (tag);
				} else {
					var tag = BooruApp.BooruApplication.Database.GetCanonicalTag (enteredTag);
					long tagId = BooruApp.BooruApplication.Database.GetTagId (tag);
					if (tagId == -1) {
						var resolve = ResolveTagWindow.Create (tag);
						resolve.Parent = this;
						if (resolve.Run () == (int)Gtk.ResponseType.Ok) {
							foreach (var t in resolve.Tag.Split(" ".ToCharArray())) {
								BooruApp.BooruApplication.Database.GetOrCreateTagId (t);
								this.ActiveImage.AddTag (t);
							}
						}
						resolve.Hide ();
					} else {
						this.ActiveImage.AddTag (tag);
					}
				}
			}
			this.TagsEntry.Text = "";
			this.imageView.QueueDraw ();
		}

		void on_ExportButton_clicked(object sender, EventArgs args)
		{
			if (this.exporter != null) {
				var messageDlg = new Gtk.MessageDialog (
					BooruApp.BooruApplication.MainWindow, 
					Gtk.DialogFlags.Modal, 
					Gtk.MessageType.Question, 
					Gtk.ButtonsType.YesNo, 
					"Cancel export?"
				);
				messageDlg.SecondaryText = "Do you want to cancel the currently running export?";

				int result = messageDlg.Run ();
				messageDlg.Destroy ();
				if (result == (int)Gtk.ResponseType.Yes) {
					this.exporter.Abort ();
					this.exporter = null;
					this.ExportProgress.Visible = false;
				}
				return;
			}

			string folderPath;
			if (SelectExportPathDialog.SelectPath(out folderPath)) {
				int exportedCount = 0;
				this.exporter = new ImageExporter (this.store, folderPath);
				this.exporter.Exported += () =>  {
					exportedCount++;

					BooruApp.BooruApplication.TaskRunner.StartTaskMainThread(()=>{
						this.ExportProgress.Fraction = exportedCount / (float)this.foundImageCount;
					});
				};
				this.exporter.Finished += () => {
					this.exporter = null;
				};
				this.exporter.Start ();
			}
		}

		void on_StopButton_clicked(object sender, EventArgs args)
		{
			this.finder.Abort ();
		}
			
		void on_TagsEntry_key_press_event(object sender, Gtk.KeyPressEventArgs args)
		{
			if (string.IsNullOrWhiteSpace (this.TagsEntry.Text)) {
				if (args.Event.Key == Gdk.Key.Page_Up)
					this.Advance (false);
				else if (args.Event.Key == Gdk.Key.Page_Down)
					this.Advance (true);
			}
		}

		void on_RenameButton_clicked(object sender, EventArgs args)
		{
		}

		void ToggleFullscreen(bool isFullscreen)
		{
			this.Margin = isFullscreen ? 0 : 8;
			this.ThumbBox.Visible = !isFullscreen;
			this.ButtonBox.Visible = !isFullscreen;
		}
	}
}
