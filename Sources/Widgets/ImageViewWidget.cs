using System;
using Cairo;
using System.Collections.Generic;

namespace Booru
{
	public sealed class ImageViewWidget : Gtk.DrawingArea
	{
		public PlayerControlWidget Controls;

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

				if (value != null) {
					value.AddRef ();
					value.SubImage = -1;
				}

				if (this.image != null) {
					this.image.Release ();
				}

				if (this.Controls != null && this.Controls.PlayerPocess != null) {
					if (!this.Controls.PlayerPocess.HasExited)
						this.Controls.PlayerPocess.Kill ();
					this.Controls.PlayerPocess = null;
					//this.Window.ThawUpdates ();
				}

				this.image = value;
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
			GLib.Timeout.Add (10, () => {
				bool wasFading = this.IsFading;
				if (wasFading) {
					float targetAlpha = this.TargetAlpha; 
					this.fadeAlpha += (targetAlpha - this.fadeAlpha) * 0.5f;
					if (Math.Abs(targetAlpha - this.fadeAlpha) < 0.01)
						this.fadeAlpha = this.TargetAlpha;
				}

				if (hasFrameBeenQueued)
					return true;

				if (this.image != null && this.image.Anim != null && (!IsPaused || wasFading || !this.image.Anim.IsStaticImage)) {
					this.UpdateImage(this.IsFading || !this.image.Anim.IsStaticImage);
				}

				if (wasFading || this.IsFading)
					this.QueueFrame();

				return true;
			});

			this.Events = this.Events | Gdk.EventMask.ButtonPressMask;

			this.Show ();
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			this.Window.EnsureNative ();
			this.windowId = Native.GetDrawableNativeId (this.Window).ToInt32();
		}

		int windowId = 0;

		public override void Destroy()
		{
			this.Image = null;
			base.Destroy ();
		}

		private bool Resize(ref int imageWidth, ref int imageHeight)
		{
			if (this.areaSize.X <= 2 || this.areaSize.Y <= 2)
				return false;

			return true;
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) {
				if (this.image.Details.type == BooruImageType.Video && windowId != 0 && this.Controls != null) {
					if (this.Controls.PlayerPocess == null) {
						var proc = new System.Diagnostics.Process ();
						proc.StartInfo.RedirectStandardInput = true;
						proc.StartInfo.RedirectStandardOutput = true;
						proc.StartInfo.UseShellExecute = false;
						proc.StartInfo.Arguments = string.Format ("-slave -loop 0 -vo gl -volume 0 -wid {1} {0}", "\"" + this.image.Details.Path + "\"", windowId);
						proc.StartInfo.FileName = "mplayer";
						proc.Start (); 
						this.Controls.PlayerPocess = proc;
					} else {
						try {
							this.Controls.PlayerPocess.Kill ();
						} catch(Exception ex) {
							BooruApp.BooruApplication.Log.Log (BooruLog.Category.Image, ex, "Caught exception killing mplayer");
						}
						this.Controls.PlayerPocess = null;
						this.QueueDraw ();
					}
				}
			}
			return base.OnButtonPressEvent (evnt);
		}

		Gdk.Pixbuf unscaledPixBuf;

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
			bool newAnimFrame = false;

			if (animation.IsStaticImage) {
				if (forceUpdate)
					unscaledPixBuf = animation.StaticImage;
			} else {
				if (this.animIter == null) {
					animIter = animation.GetIter (IntPtr.Zero);
				}
				if (animIter != null && animIter.Advance (IntPtr.Zero)) {
					unscaledPixBuf = animIter.Pixbuf;
					newAnimFrame = true;
				}
			}
				
			var imageSize = new Cairo.PointD (animation.Width, animation.Height);
			if (imageSize.X <= 2 || imageSize.Y <= 2)
				return;
			
			// scale preserving aspect ratio
			var scale = Math.Min (this.areaSize.X / imageSize.X, this.areaSize.Y / imageSize.Y);
			var scaledImageSize = new Cairo.PointD (imageSize.X * scale, imageSize.Y * scale);

			if (unscaledPixBuf == null)
				return;

			if (newAnimFrame || unscaledPixBuf != this.unscaledPixBuf || scaledImageSize.X != this.scaledImageSize.X || scaledImageSize.Y != this.scaledImageSize.Y) {
				if (this.ScaledPixbuf != null)
					this.ScaledPixbuf.Dispose ();

				Gdk.Pixbuf scaledPixBuf = unscaledPixBuf.ScaleSimple ((int)scaledImageSize.X, (int)scaledImageSize.Y, Gdk.InterpType.Hyper);
				this.ScaledPixbuf = scaledPixBuf;
				this.scaledImageSize = scaledImageSize;
				this.unscaledPixBuf = unscaledPixBuf;
			}

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
			if (this.Controls != null && this.Controls.PlayerPocess != null) {
				return true;
			}

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

			cr.SelectFontFace ("Noto Mono", FontSlant.Normal, FontWeight.Normal);
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

			// cr.DrawStringAt (4, 4 + cr.FontExtents.Height, System.IO.Path.GetFileName (this.image.Details.Path), black, white);

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

