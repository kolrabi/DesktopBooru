using System;
using Cairo;
using System.Collections.Generic;

namespace Booru
{
	public sealed class TagsOverlay
	{
		static readonly Color COLOR_IMAGE_INFO = new Color(1, 1, 1);
		static readonly Color COLOR_SCORE_BAR_BG_NEG = new Color(.3, 0, 0);
		static readonly Color COLOR_SCORE_BAR_BG_POS = new Color(0, .3, 0);
		static readonly Color COLOR_SCORE_BAR = new Color(1, 1, 1);
		static readonly Color COLOR_SCORE_BAR_NEG = new Color(1, 0, 0);
		static readonly Color COLOR_SCORE_BAR_POS = new Color(0, 1, 0);
		static readonly Color COLOR_BLACK = new Color(0, 0, 0);

		static Color GetFadeColor(Color color, double alpha)
		{
			return new Color(color.R * alpha, color.G * alpha, color.B * alpha, color.A * alpha);
		}

		bool active = false;
		public bool IsActive {
			get { return this.active; }
			set { this.active = value; this.drawingArea.QueueDraw (); }
		}

		public Image OpponentImage;

		readonly Gtk.DrawingArea drawingArea;

		public TagsOverlay (Gtk.DrawingArea drawingArea)
		{
			this.drawingArea = drawingArea;
			this.drawingArea.Drawn += this.DrawOverlay;
		}

		private double currentFadeOpacity;
		private Image lastImage = null;

		private class TagBlockColumn
		{
			public double Width { get; private set; }
			public double Height { get; private set; }

			private readonly List<TagBlock> tagBlocks = new List<TagBlock>();
			public IEnumerable<TagBlock> TagBlocks { get { return this.tagBlocks; } }

			public int Count { get { return this.tagBlocks.Count; } }

			public double OffsetX { get; private set; }

			public TagBlockColumn(double offsetX)
			{
				this.OffsetX = offsetX;
			}

			public void AddTagBlock(TagBlock block)
			{
				block.Position.X += this.OffsetX;
				block.Position.Y += this.Height;

				this.tagBlocks.Add (block);

				this.Width = Math.Max (this.Width, block.Width);
				this.Height += block.Height;
			}

			public void Clear()
			{
				this.tagBlocks.Clear();
				this.Width = this.Height = this.OffsetX = 0.0;
			}

			public void Move(double newOffsetX)
			{
				foreach (var block in this.tagBlocks) {
					block.Position.X += newOffsetX - this.OffsetX;
				}
				this.OffsetX = newOffsetX;					
			}

			public void Draw(Cairo.Context cr, double alpha)
			{
				foreach (var block in this.tagBlocks) {
					block.Draw (cr, alpha);
				}
			}
		}

		private class TagBlock 
		{
			public string Text { get; private set; }
			public Color Color { get; private set; }
			public double Width { get; private set; }
			public double Height { get; private set; }
			public bool IsSiteTag { get; private set; }

			public Cairo.PointD Position;

			public TagBlock(Cairo.Context cr, string tag)
			{
				bool isKnownOn = tag.StartsWith ("known_on_");
				bool isNotOn = tag.StartsWith ("not_on_");

				if (isKnownOn || isNotOn) {
					this.IsSiteTag = true;
					this.Text = tag.Replace ("known_on_", "").Replace ("not_on_", "");
					this.Color = isKnownOn ? new Color (.7, 1, .7) : new Color (1, .7, .7);
				} else {
					this.IsSiteTag = false;
					this.Text = tag;

					var tagDetails = BooruApp.BooruApplication.Database.GetTag (tag);
					if (tagDetails == null) {
						this.Color = new Color (1, 1, 1);
					} else {
						this.Color = TagDetails.GetTagTypeColor (tagDetails.Type);
					}
				}

				Cairo.TextExtents extents = cr.TextExtents(this.Text);
				this.Position = new PointD(0.0, 0.0);
				this.Width = extents.XAdvance;
				this.Height = cr.FontExtents.Height;
			}

			public void Draw(Cairo.Context cr, double alpha)
			{
				double x = this.Position.X;
				double y = this.Position.Y;

				if (this.IsSiteTag) {
					// right align
					x -= this.Width;
				}

				cr.DrawStringAt (x, y, this.Text, GetFadeColor(COLOR_BLACK, alpha), GetFadeColor(this.Color, alpha));
			}
		}

		private readonly List<TagBlockColumn> tagBlockColumns = new List<TagBlockColumn>();
		private TagBlockColumn tagBlockSiteColumn = new TagBlockColumn (0.0);

		void UpdateTagBlocks(Cairo.Context cr, Image image, double offsetY, double paddingBottom)
		{
			this.tagBlockColumns.Clear ();
			this.tagBlockSiteColumn.Clear ();

			if (image == null)
				return;

			cr.SelectTagOverlayFont ();

			var column = new TagBlockColumn (8.0);
			foreach (string tag in image.Tags) {
				var tagBlock = new TagBlock (cr, tag);

				tagBlock.Position.Y = offsetY;

				if (tagBlock.IsSiteTag) {
					this.tagBlockSiteColumn.AddTagBlock (tagBlock);
				} else {
					if (tagBlock.Height + column.Height + offsetY > this.canvasSize.Y - paddingBottom) {
						if (column.Count == 0) {
							// height is too small to fit even a single item
							break;
						}
						this.tagBlockColumns.Add (column);
						column = new TagBlockColumn (column.OffsetX + column.Width);
					}
					column.AddTagBlock (tagBlock);
				}
			}
			if (column.Count > 0)
				this.tagBlockColumns.Add (column);

			this.tagBlockSiteColumn.Move (this.canvasSize.X - 8);
		}

		void DrawTagBlocks(Cairo.Context cr, double alpha)
		{
			cr.SelectTagOverlayFont ();
			this.tagBlockSiteColumn.Draw (cr, alpha);
			foreach (var column in this.tagBlockColumns)
				column.Draw (cr, alpha);
		}

		bool UpdateFade(ImageViewWidget imageViewWidget)
		{
			this.currentFadeOpacity = imageViewWidget.Alpha;

			return this.currentFadeOpacity > 0.0;
		}

		void DrawScoreBars(Cairo.Context cr, Image image, Image opponentImage)
		{
			Cairo.PointD scoreBarSize = new Cairo.PointD(this.canvasSize.X, 12.0);
			Cairo.PointD scoreBarPos = new Cairo.PointD (this.canvasSize.X - scoreBarSize.X, this.canvasSize.Y - scoreBarSize.Y);
			Cairo.PointD scoreBarCenter = new Cairo.PointD(scoreBarPos.X + scoreBarSize.X / 2, scoreBarPos.Y + scoreBarSize.Y / 2);

			// draw score bar background
			cr.SetSourceColor(COLOR_SCORE_BAR_BG_NEG);
			cr.Rectangle (scoreBarPos, scoreBarSize.X/2, scoreBarSize.Y);
			cr.Fill ();

			cr.SetSourceColor (COLOR_SCORE_BAR_BG_POS);
			cr.Rectangle (new Rectangle (scoreBarCenter.X, scoreBarPos.Y, scoreBarSize.X/2, scoreBarSize.Y));
			cr.Fill ();

			// calculate elo
			double elo = image.Details.ELO;
			double winElo = elo + Image.GetEloOffset (image, opponentImage);
			double loseElo = elo - Image.GetEloOffset (opponentImage, image);
			double eloScale = 8.0;

			cr.SetSourceColor(COLOR_SCORE_BAR);
			cr.Rectangle (new Rectangle (
				scoreBarCenter.X + loseElo*eloScale, 
				this.canvasSize.Y-(scoreBarSize.Y-1), 
				(winElo-loseElo)*eloScale, 
				scoreBarSize.Y-2
			));
			cr.Fill ();

			if (elo < 0) {
				cr.SetSourceColor (COLOR_SCORE_BAR_NEG);
			} else {
				cr.SetSourceColor (COLOR_SCORE_BAR_POS);
			}
			cr.Rectangle (scoreBarCenter.X, scoreBarPos.Y+2, elo*eloScale, scoreBarSize.Y-4);
			cr.Fill ();

			cr.SetSourceColor (COLOR_SCORE_BAR);
			cr.MoveTo (scoreBarCenter.X, scoreBarPos.Y);
			cr.LineTo (scoreBarCenter.X, this.canvasSize.Y);
			cr.Stroke ();

		}

		Cairo.PointD canvasSize;

		string infoString = "";

		void UpdateInfoString(Image image)
		{
			this.infoString = string.Format (
				"File:\t{0}\n" +
				"Dir:\t{1}\n" +
				"MD5:\t{2}\n" +
				"ELO:\t{3:F2}\n" +
				"Votes:\t{4} ({5}:{6})\n" +
				"Size:\t{7}",
				System.IO.Path.GetFileName (image.Details.Path),
				System.IO.Path.GetDirectoryName (image.Details.Path),
				image.Details.MD5,
				image.Details.ELO,
				image.Details.Wins + image.Details.Losses, image.Details.Wins, image.Details.Losses,
				image.Details.Size
			);
		}

		void DrawInfoString(Cairo.Context cr, Image image, double alpha)
		{
			cr.SelectTagOverlayFont ();
			cr.DrawStringAt (8, 8 + cr.FontExtents.Height, this.infoString, GetFadeColor(COLOR_BLACK, alpha), GetFadeColor(COLOR_IMAGE_INFO, alpha));
		}
			
		void DrawOverlay(System.Object o, Gtk.DrawnArgs args)
		{
			var imageViewWidget = this.drawingArea as ImageViewWidget;
			if (imageViewWidget == null || imageViewWidget.Image == null || !this.IsActive || !this.UpdateFade (imageViewWidget))
				return;

			this.canvasSize = new Cairo.PointD (this.drawingArea.Allocation.Width, this.drawingArea.Allocation.Height);

			var image = imageViewWidget.Image;
			if (image != this.lastImage) {
				this.UpdateTagBlocks (args.Cr, image, 120.0, 32.0);
				this.UpdateInfoString (image);
			}

			this.DrawInfoString (args.Cr, image, this.currentFadeOpacity);
			this.DrawTagBlocks (args.Cr, this.currentFadeOpacity);
			this.DrawScoreBars (args.Cr, image, this.OpponentImage);
		}
	}
}
	