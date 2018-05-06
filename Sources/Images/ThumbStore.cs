using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Booru
{
	public sealed class ThumbStore : Gtk.ListStore
	{
		// list store column indices
		public const int THUMB_STORE_COLUMN_TOOLTIP = 0;
		public const int THUMB_STORE_COLUMN_THUMBNAIL = 1;
		public const int THUMB_STORE_COLUMN_IMAGE = 2;
		public const int THUMB_STORE_COLUMN_INDEX = 3;

		public delegate void OnLastImageAdded ();
		public event OnLastImageAdded LastImageAdded;

		private readonly ConcurrentQueue<Image> addedImages = new ConcurrentQueue<Image>();
		private uint loaderIdle = 0;
		private bool isFinished = true;

		public bool IsFinished { get { return this.isFinished && this.addedImages.Count == 0; } }

		public ThumbStore () : base(typeof (string), typeof (Gdk.Pixbuf), typeof(Image), typeof(string))
		{
		}

		/// <summary>
		/// Notify store that images will be added, start processing queue.
		/// </summary>
		public void BeginAdding()
		{
			if (!this.isFinished)
				return;
			
			this.isFinished = false;
			this.loaderIdle = GLib.Idle.Add (this.ProcessQueue);
		}

		/// <summary>
		/// Queues image for adding.
		/// </summary>
		/// <param name="image">Image.</param>
		public void QueueAddedImage(Image image)
		{
			if (image == null)
				throw new ArgumentNullException ();

			image.AddRef ();
			this.addedImages.Enqueue (image);
		}

		/// <summary>
		/// Stop adding images, cancel pending.
		/// </summary>
		public void AbortAdding()
		{
			// abort adder
			if (this.loaderIdle != 0)
				GLib.Idle.Remove (this.loaderIdle);
			this.loaderIdle = 0;

			// let observers know
			if (this.LastImageAdded != null)
				this.LastImageAdded ();

			this.isFinished = true;
		}

		/// <summary>
		/// Notify store that the last image has been queued. Keep appending queued images.
		/// </summary>
		public void EndAdding()
		{
			if (this.isFinished)
				return; 

			// if already finished queue, notify observers
			if (this.addedImages.Count == 0) {
				if (this.LastImageAdded != null)
					this.LastImageAdded ();
			}

			this.isFinished = true;
		}

		/// <summary>
		/// Append image data to list.
		/// </summary>
		/// <param name="image">Image.</param>
		private void AppendImage(Image image) 
		{
			if (image == null)
				throw new ArgumentNullException ();
			
			string tagsString = image.Details.Path + "\n" + string.Join (" ", image.Tags);
			tagsString = tagsString.Replace ("&", "&amp;");

			var iter = this.Append ();
			this.SetValues(iter, tagsString, image.Thumbnail, image, this.curIndex++);
		}

		private int curIndex = 1;

		/// <summary>
		/// Gets the image for a given row reference.
		/// </summary>
		/// <returns>The image.</returns>
		/// <param name="rowRef">Row reference.</param>
		public Image GetImage(Gtk.TreeRowReference rowRef)
		{
			if (rowRef == null)
				throw new ArgumentNullException ();
			
			if (rowRef.Model != this)
				throw new InvalidOperationException();
			
			Gtk.TreeIter iter;
			if (!this.GetIter (out iter, rowRef.Path))
				return null;

			return GetImage (iter);
		}

		public Image GetImage(Gtk.TreeIter iter)
		{
			return (Image)this.GetValue (iter, ThumbStore.THUMB_STORE_COLUMN_IMAGE);
		}

		/// <summary>
		/// Process added image queue.
		/// </summary>
		/// <returns><c>true</c>, to continue processing, <c>false</c> when done.</returns>
		private bool ProcessQueue()
		{
			if (this.addedImages.Count > 0) {
				Image image;
				if(this.addedImages.TryDequeue(out image)) {
					this.AppendImage (image);
				}
			} else if (this.isFinished) {
				if (this.LastImageAdded != null)
					this.LastImageAdded ();
				return false;
			}				

			return true;
		}
	}
}

