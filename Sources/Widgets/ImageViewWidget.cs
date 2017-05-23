using System;
using Cairo;
using System.Collections.Generic;

namespace Booru
{
	public sealed class ImageViewWidget : Gtk.DrawingArea
	{
		public bool IsPaused = false;
		public bool IsFading { get { return this.Alpha != this.TargetAlpha; } }

		private float fadeAlpha = 1.0f;
		public float Alpha { get { return this.fadeAlpha; } }

		public float TargetAlpha { get { return !IsPaused ? 1.0f : 0.0f; } }

		private Image image;
		public Image Image { 
			get {
				return this.image;
			}
			set { 
				if (this.image == value)
					return;

				this.image = value;
				if (this.image != null)
					this.image.SubImage = -1;
				this.animIter = null;
				this.UpdateImage (true);
				this.QueueFrame ();
			}
		}

		private bool hasFrameBeenQueued = false;
		private Cairo.PointD areaSize;
		private Cairo.PointD scaledImageSize;
		public Cairo.Rectangle TargetRect;

		public Gdk.Pixbuf ScaledPixbuf { get; private set; }

		private Gdk.PixbufAnimationIter animIter;

		public ImageViewWidget ()
		{
			// frame update
			GLib.Timeout.Add (33, () => {
				if (hasFrameBeenQueued)
					return true;
				
				bool wasFading = this.IsFading;
				if (this.IsFading) {
					this.fadeAlpha += (this.TargetAlpha - this.Alpha) * 0.5f;
					if (Math.Abs(this.TargetAlpha - this.Alpha) < 0.01)
						this.fadeAlpha = this.TargetAlpha;
				}

				if (this.image != null && this.image.Anim != null &&  !hasFrameBeenQueued && (!IsPaused || wasFading)) {
					this.UpdateImage(this.IsFading);
				}

				if (wasFading || this.IsFading)
					this.QueueFrame();

				return true;
			});

			this.Show ();
		}

		private bool Resize(ref int imageWidth, ref int imageHeight)
		{
			if (this.areaSize.X <= 2 || this.areaSize.Y <= 2)
				return false;

			return true;
		}

		private void UpdateImage(bool forceUpdate)
		{
			if (this.image == null || this.image.Anim == null)
				return;

			Gdk.PixbufAnimation animation;

			animation = this.image.Anim;

			// 
			if (this.areaSize.X <= 2 || this.areaSize.Y <= 2)
				return;
			
			Gdk.Pixbuf unscaledPixBuf = null;

			if (animation.IsStaticImage) {
				if (forceUpdate)
					unscaledPixBuf = animation.StaticImage;
			} else {
				if (this.animIter == null) {
					animIter = animation.GetIter (IntPtr.Zero);
				}
				if (animIter != null && animIter.Advance (IntPtr.Zero)) {
					unscaledPixBuf = animIter.Pixbuf;
				}
			}

			if (unscaledPixBuf == null)
				return;

			var imageSize = new Cairo.PointD (animation.Width, animation.Height);

			if (imageSize.X <= 2 || imageSize.Y <= 2)
				return;

			// scale preserving aspect ratio
			var scale = Math.Min (this.areaSize.X / imageSize.X, this.areaSize.Y / imageSize.Y);
			var scaledImageSize = new Cairo.PointD (imageSize.X * scale, imageSize.Y * scale);

			if (this.ScaledPixbuf != null)
				this.ScaledPixbuf.Dispose ();
			
			this.ScaledPixbuf = unscaledPixBuf.ScaleSimple ((int)scaledImageSize.X, (int)scaledImageSize.Y, Gdk.InterpType.Hyper);
			this.scaledImageSize = scaledImageSize;

			// where to draw the pixbuf
			this.TargetRect = new Cairo.Rectangle (
				(this.areaSize.X - this.scaledImageSize.X) / 2,
				(this.areaSize.Y - this.scaledImageSize.Y) / 2,
				this.scaledImageSize.X,
				this.scaledImageSize.Y
			);

			this.QueueFrame ();
		}
		
		protected override bool OnDrawn (Cairo.Context cr)
		{
			bool baseDrawnResult = base.OnDrawn (cr);

			var scaledPixbuf = this.ScaledPixbuf;
			if (scaledPixbuf == null) {
				this.hasFrameBeenQueued = false;
				return baseDrawnResult;
			}

			// black background in fullscreen mode
			if (BooruApp.BooruApplication.MainWindow.IsFullscreen) {
				cr.SetSourceRGB (0, 0, 0);
				cr.Rectangle (new Rectangle(0,0, this.Allocation.Width, this.Allocation.Height));
				cr.Fill ();
			}

			cr.Rectangle (this.TargetRect);
			Gdk.CairoHelper.SetSourcePixbuf (cr, scaledPixbuf, TargetRect.X, TargetRect.Y);
			if (this.fadeAlpha < 1.0) {
				cr.PaintWithAlpha (this.fadeAlpha);
				cr.NewPath ();
			} else {
				cr.Fill ();
			}

			cr.SelectFontFace ("Noto Sans", FontSlant.Normal, FontWeight.Normal);
			cr.SetFontSize (12.0);
			cr.LineWidth = 3.5;

			if (this.fadeAlpha < 1.0) {
				var ext = cr.TextExtents ("Loading...");
				var center = new Point2D (this.Allocation.Width / 2 - (int)ext.Width / 2, this.Allocation.Height / 2 - (int)ext.Height / 2);
				var whiteInverse = new Color (1, 1, 1, 1 - this.fadeAlpha);
				var blackInverse = new Color (0, 0, 0, 1 - this.fadeAlpha);
				cr.DrawStringAt (center.X, center.Y, "Loading...", blackInverse, whiteInverse);
			}

			var white = new Cairo.Color (1, 1, 1, this.fadeAlpha);
			var black = new Cairo.Color (0, 0, 0, this.fadeAlpha);

			cr.DrawStringAt (4, 4 + cr.FontExtents.Height, System.IO.Path.GetFileName (this.image.Details.Path), black, white);

			if (this.image.MaxImage > -1) {
				string text;
				if (this.image.SubImage < 0) {
					text = string.Format ("{0:D} images in file", this.image.MaxImage);
				} else {
					text = string.Format ("{0:D3}/{1:D3} {2}", this.image.SubImage, this.image.MaxImage, this.image.SubImageName);
				}
				cr.DrawStringAt (4, this.Allocation.Height - cr.FontExtents.Height - 4, text, black, white);
			}

			this.hasFrameBeenQueued = false;
			return baseDrawnResult;
		}

		protected override bool OnConfigureEvent (Gdk.EventConfigure evnt)
		{
			// handle resize
			var newAreaSize = new Cairo.PointD(evnt.Width, evnt.Height);

			if (!newAreaSize.Equals(this.areaSize)) {
				this.areaSize = newAreaSize;

				if (this.image != null && this.image.Anim != null) {
					this.UpdateImage (true);
				}
			}

			this.QueueFrame ();
			return base.OnConfigureEvent (evnt);
		}

		private void QueueFrame()
		{
			if (this.animIter != null) {
				var data = this.animIter.Data;
			}
			this.hasFrameBeenQueued = true;
			this.QueueDraw();
		}

		public bool AdvanceSubImage(bool forward)
		{
			if (this.image.MaxImage < 0)
				return false;

			if (forward) {
				this.image.SubImage = this.image.SubImage + 1;
			} else {
				this.image.SubImage = this.image.SubImage - 1;
			}

			if (this.image.SubImage == -1) 
				return false;
		
			this.UpdateImage (true);
			this.QueueDraw ();
			return true;
		}

	}
}

