using System;
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

		public Image ActiveImage { get { return this.imageView.Image; } }

		private readonly ThumbStore store;
		private readonly ImageViewWidget imageView;
		private readonly TagsOverlay tagsOverlay;

		private ImageFinder finder;

		private ImageExporter exporter;

		private int foundImageCount = 0;

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

			this.ImageThumbView.Model = this.store;
			this.ImageThumbView.TooltipColumn = ThumbStore.THUMB_STORE_COLUMN_TOOLTIP;

			this.ImageThumbView.Model.RowInserted += on_ImageThumbView_Model_RowInserted;
			this.ImageThumbView.Model.RowChanged += on_ImageThumbView_Model_RowChanged;

			this.StopButton.Sensitive = true;
			this.Spinner.Active = true;
			this.ExportButton.Sensitive = false;
			this.MarkButton.Sensitive = false;
			this.DeleteButton.Sensitive = false;
		}

		ImagesResultWidget Init(string searchString)
		{
			// finder for image search string
			this.finder = new ImageFinder (searchString, this.store);
			this.store.LastImageAdded+= () => Gtk.Application.Invoke((s,a)=>{
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

			// update image count label 
			this.ResultCountLabel.Text = this.foundImageCount.ToString();
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

		void on_ImageThumbView_move_cursor(object obj, Gtk.MoveCursorArgs args)
		{
			if (this.ImageThumbView.SelectedItems.Length == 0)
				return;
			
			Gtk.TreePath path = this.ImageThumbView.SelectedItems [0];

			var image = this.store.GetImage (new Gtk.TreeRowReference(this.store, path));
			var oldImage = this.ActiveImage;

			if (image == oldImage)
				return;

			// unload current imag
			if (oldImage != null) {
				this.imageView.Image = null;
				oldImage.Dispose ();
			}

			// get newly selected image
			this.imageView.Image = image;

			// clear tags entry to not confuse user
			this.TagsEntry.Text = "";
			this.UpdateButtons ();
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
		/// <summary>
		/// Check if hitting the delete button should mark selection for deletion instead of physically deleting.
		/// </summary>
		/// <returns><c>true</c> if tagging only; otherwise, <c>false</c>.</returns>
		bool IsDeleteTaggingOnly()
		{			
			// all selected images need to be tagged already to be deleted, so if any image
			// doesn't have the tag, delete button should only tag
			foreach (var path in this.ImageThumbView.SelectedItems) {
				var rowRef = new Gtk.TreeRowReference (this.store, path);
				var image = this.store.GetImage (rowRef);

				//System.Diagnostics.Debug.Assert (image != null);
				if (image == null)
					continue;

				System.Diagnostics.Debug.Assert (image.Tags != null);
				if (image.Tags == null)
					continue;

				if (!image.Tags.Contains ("deleteme"))
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
					try {
						// TODO: get alternative paths
						if (System.IO.File.Exists (image.Details.Path)) {
							if (moveImages) {
								System.IO.File.Move (image.Details.Path, targetPath + "/" + System.IO.Path.GetFileName (image.Details.Path));
								BooruApp.BooruApplication.Log.Log (BooruLog.Category.Files, BooruLog.Severity.Info, "Moved file " + image.Details.Path + " to " + targetPath);
							} else {
								System.IO.File.Delete (image.Details.Path);
								BooruApp.BooruApplication.Log.Log (BooruLog.Category.Files, BooruLog.Severity.Info, "Deleted file " + image.Details.Path);
							}
						}
						BooruApp.BooruApplication.Database.RemoveImage (image.Details.MD5);
					} catch (Exception ex) {
						BooruApp.BooruApplication.Log.Log (BooruLog.Category.Files, BooruLog.Severity.Error, "Could not delete file " + image.Details.Path + ": " + ex.Message);
						Console.WriteLine ("Could not delete file " + image.Details.Path + ": " + ex.Message);
						Console.WriteLine (ex.StackTrace);
					}
				});
			}

			this.UpdateButtons ();
		}

		void on_MarkButton_clicked(object sender, EventArgs args)
		{
			bool anyMarked = this.IsAnySelectedMarkedForDelete ();
			if (anyMarked) {
				foreach (var path in this.ImageThumbView.SelectedItems) {
					var image = this.store.GetImage (new Gtk.TreeRowReference (this.store, path));
					image.RemoveTag ("deleteme");
				}
			} else {
				foreach (var path in this.ImageThumbView.SelectedItems) {
					var image = this.store.GetImage (new Gtk.TreeRowReference (this.store, path));
					image.AddTag ("deleteme");
				}
			}
			this.UpdateButtons ();
		}

		void on_OpenExternalButton_clicked(object sender, EventArgs args)
		{
			if (this.ActiveImage != null)
				this.ActiveImage.ViewExternal ();
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

			var dlg = new Gtk.FileChooserDialog (
				"Select export directory", 
				BooruApp.BooruApplication.MainWindow, 
				Gtk.FileChooserAction.SelectFolder, 
				"Export Here", Gtk.ResponseType.Ok, 
				"Cancel", Gtk.ResponseType.Cancel
			);
			dlg.SetFilename (BooruApp.BooruApplication.Database.Config.GetString ("export.lastpath")+"/x");

			if (dlg.Run () == (int)Gtk.ResponseType.Ok) {
				string folderPath = dlg.Filename;

				if (!System.IO.Directory.Exists(folderPath)) {
					try {
						System.IO.Directory.CreateDirectory(folderPath);
					} catch(Exception ex) {
						var messageDlg = new Gtk.MessageDialog (
							BooruApp.BooruApplication.MainWindow, 
							Gtk.DialogFlags.Modal, 
							Gtk.MessageType.Error, 
							Gtk.ButtonsType.Ok, 
							"Invalid folder path!"
						);
						messageDlg.SecondaryText = ex.Message;
						messageDlg.Run ();
						messageDlg.Destroy ();
						dlg.Destroy ();
						return;
					}
				
				}

				BooruApp.BooruApplication.Database.Config.SetString ("export.lastpath", folderPath);

				int exportedCount = 0;
				this.exporter = new ImageExporter (this.store, folderPath);
				this.exporter.Exported += () =>  {
					exportedCount++;

					Gtk.Application.Invoke((o,a)=> {
						this.ExportProgress.Fraction = exportedCount / (float)this.foundImageCount;
					});
				};
				this.exporter.Finished += () => {
					this.exporter = null;
				};
				this.exporter.Start ();
			}
			dlg.Destroy ();
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

		void ToggleFullscreen(bool isFullscreen)
		{
			this.Margin = isFullscreen ? 0 : 8;
			this.ThumbBox.Visible = !isFullscreen;
			this.ButtonBox.Visible = !isFullscreen;
		}
	}
}
