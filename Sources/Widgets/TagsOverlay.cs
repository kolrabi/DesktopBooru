using System;
using Cairo;

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


		private Cairo.Context currentContext;
		private double currentFadeOpacity;


		#region text drawing
		int cursorX, cursorY;
		Color currentTextColor = COLOR_IMAGE_INFO;

		void GotoXY(int x, int y)
		{
			this.cursorX = x;
			this.cursorY = y;
		}

		void GotoX(int x)
		{
			this.cursorX = x;
		}

		void GotoRow(int y)
		{
			this.cursorY = y;
		}

		void NewLine()
		{
			this.cursorX = 0;
			this.cursorY++;
		}

		int ColToX(int x)
		{
			return x * (int)this.currentContext.FontExtents.Height;
		}

		int LineToY(int y)
		{
			return y * (int)this.currentContext.FontExtents.Height + (int)this.currentContext.FontExtents.Ascent;
		}

		void DrawString(string text)
		{
			float x = 4+ColToX(this.cursorX);
			float y = 4+LineToY (this.cursorY);

			Color background = this.currentTextColor;
			Color foreground = new Color (0, 0, 0);

			this.currentContext.DrawStringAt (x, y, text, foreground, background);

			this.cursorX += (int)this.currentContext.TextExtents (text).XAdvance / (int)this.currentContext.FontExtents.Height;
		}

		void DrawStringNL(string text)
		{
			this.DrawString (text);
			this.NewLine ();
		}

		void DrawStringAt(int x, int y, string text)
		{
			this.GotoXY (x, y);
			this.DrawString (text);
		}

		void SetTextColor(Color color)
		{
			var a = color.A * this.currentFadeOpacity;
			this.currentContext.SetSourceRGBA (color.R * a, color.G * a, color.B * a, a);
			this.currentTextColor = color;
		}

		void ResetText()
		{
			this.GotoXY (0, 0);
			this.currentContext.SelectFontFace ("Noto Mono", FontSlant.Normal, FontWeight.Normal);
			this.currentContext.SetFontSize (12.0);
			this.currentContext.Antialias = Cairo.Antialias.Gray;
			this.SetTextColor (COLOR_IMAGE_INFO);
		}
		#endregion
			
		void DrawOverlay(System.Object o, Gtk.DrawnArgs args)
		{
			var imageViewWidget = this.drawingArea as ImageViewWidget;

			if (imageViewWidget == null || imageViewWidget.Image == null || !this.IsActive)
				return;

			var image = imageViewWidget.Image;

			var canvasSize = new Point2D (this.drawingArea.Allocation.Width, this.drawingArea.Allocation.Height);
			var canvasRect = new Rectangle2D (canvasSize);

			this.currentFadeOpacity = imageViewWidget.Alpha;

			if (this.currentFadeOpacity < 1.0)
				return;

			this.currentContext = args.Cr;

			this.ResetText ();

			// draw image info
			this.DrawStringAt (0, 0, System.IO.Path.GetFileName (image.Details.Path));
			this.DrawStringAt (0, 1, System.IO.Path.GetDirectoryName (image.Details.Path));
			this.DrawStringAt (0, 2, image.Details.MD5);
			this.DrawStringAt (0, 4, "ELO:");
			this.DrawStringAt (4, 4, image.Details.ELO.ToString ("F2"));
			this.DrawStringAt (8, 4, "Votes:");
			this.DrawStringAt (12, 4, string.Format ("{0} {1}:{2}", image.Details.Wins + image.Details.Losses, image.Details.Wins, image.Details.Losses));
			this.DrawStringAt (0, 5, "Size:");
			this.DrawStringAt (4, 5, image.Details.Size.ToString ());
			//this.DrawStringAt (0, 6, "TagScore");
			//this.DrawStringAt (6, 6, image.TotalTagScore + " (" + image.AvgTagScore + ")");

			// draw tag list
			int tagX = 0;
			int tagY = 8;
			int farRight = 0;
			foreach (string tag in image.Tags) {
				var tagDetails = BooruApp.BooruApplication.Database.GetTag (tag);
				double score = tagDetails.Score;

				if (tagDetails != null) {
					this.SetTextColor (TagDetails.GetTagTypeColor (tagDetails.Type));
				} else {
					this.SetTextColor (new Color (1, 1, 1));
				}

				if (this.LineToY (this.cursorY) > canvasSize.Y - this.LineToY (4)) {
					tagY = 8;
					tagX = farRight + 2;
				}
				this.cursorX = tagX;
				this.cursorY = tagY;
				this.DrawString (tag);
				tagY++;
				farRight = Math.Max (farRight, this.cursorX);
			}

			// draw relative score bars
			var scoreBarSize = new Point2D(canvasSize.X, 12);
			var scoreBarRect = new Rectangle2D (canvasRect.LowerRight - scoreBarSize, scoreBarSize);
			var scoreBarCenter = scoreBarRect.Position + scoreBarSize / 2;

			this.SetTextColor (COLOR_SCORE_BAR_BG_NEG);
			this.currentContext.Rectangle (new Rectangle (scoreBarRect.Position.X, scoreBarRect.Position.Y, scoreBarSize.X/2, scoreBarSize.Y));
			this.currentContext.Fill ();

			this.SetTextColor (COLOR_SCORE_BAR_BG_POS);
			this.currentContext.Rectangle (new Rectangle (scoreBarCenter.X, scoreBarRect.Position.Y, scoreBarSize.X/2, scoreBarSize.Y));
			this.currentContext.Fill ();

			double elo = image.Details.ELO;
			double winElo = elo + Image.GetEloOffset (image, OpponentImage);
			double loseElo = elo - Image.GetEloOffset (OpponentImage, image);
			double eloScale = 8.0;

			this.SetTextColor(COLOR_SCORE_BAR);
			this.currentContext.Rectangle (new Rectangle (
				scoreBarCenter.X + loseElo*eloScale, 
				scoreBarRect.LowerRight.Y-(scoreBarSize.Y-1), 
				(winElo-loseElo)*eloScale, 
				scoreBarSize.Y-2
			));
			this.currentContext.Fill ();

			if (elo < 0) {
				this.SetTextColor (COLOR_SCORE_BAR_NEG);
			} else {
				this.SetTextColor (COLOR_SCORE_BAR_POS);
			}
			this.currentContext.Rectangle (new Rectangle (scoreBarCenter.X, scoreBarRect.Position.Y+2, elo*eloScale, scoreBarSize.Y-4));
			this.currentContext.Fill ();

			this.SetTextColor (COLOR_SCORE_BAR);
			this.currentContext.MoveTo (scoreBarCenter.X, scoreBarRect.Position.Y);
			this.currentContext.LineTo (scoreBarCenter.X, scoreBarRect.LowerRight.Y);
			this.currentContext.Stroke ();
		}
	}
}
	