using System;
using Gtk;
using Cairo;
using System.Collections.Generic;

namespace Booru
{
	public class TagsEntryWidget : DrawingArea
	{
		private readonly List<string> tags;
		public IEnumerable<string> Tags { get { return this.tags; } }

		protected const int FONT_HEIGHT = 12;

		protected const int ENTRY_PADDING = 5;
		protected const int TAGBOX_PADDING = 4;
		protected const int TAGBOX_SEPARATION = 5;
		protected const int TAGBOX_XOFFSET = 2;
		protected const int TAGBOX_XSIZE = FONT_HEIGHT;
		protected const int TAGBOX_HEIGHT = FONT_HEIGHT  + TAGBOX_PADDING;

		private class TagBox
		{
			public string Tag;
			public Rectangle Rect;
			public TextExtents Extents;
		}

		private readonly List<TagBox> boxen;
		private Rectangle allocRect;
		private bool isDirty;

		public TagsEntryWidget ()
		{
			this.CanFocus = true;
			this.Sensitive = true;

			this.tags = new List<string> ();
			this.boxen = new List<TagBox> ();
			this.isDirty = true;

			this.AddTag ("foo");
			this.AddTag ("bar");
		}

		public void AddTag(string tag)
		{
			if (this.tags.Contains (tag))
				return;

			tags.Add (tag);
			this.Rebuild ();
		}

		public void SetTags(IEnumerable<string> tags)
		{
			this.tags.Clear ();
			this.tags.AddRange (tags);
			this.Rebuild ();
		}

		protected void Rebuild()
		{
			this.isDirty = true;

			this.tags.Sort ();
			this.boxen.Clear ();

			if (this.Window == null)
				return;

			var cr = Gdk.CairoHelper.Create (this.Window);
			cr.SetFontSize (FONT_HEIGHT);
			cr.SelectFontFace ("Mono", FontSlant.Normal, FontWeight.Normal);

			int left = ENTRY_PADDING;
			int top = ENTRY_PADDING;
			foreach (string rawTag in this.tags) {
				var tag = rawTag.Replace ('_', ' ');
				var box = new TagBox ();
				box.Extents = cr.TextExtents (tag);
				box.Tag = tag;
				box.Rect = new Rectangle (left, top, box.Extents.Width + 3 * TAGBOX_PADDING + TAGBOX_XSIZE, FONT_HEIGHT + 2 * TAGBOX_PADDING);

				this.boxen.Add (box);
				left += (int)(TAGBOX_SEPARATION + box.Rect.Width);
			}

			cr.Dispose ();

			this.isDirty = false;

			this.QueueDraw ();
		}

		protected override void OnGetPreferredHeight (out int minimum_height, out int natural_height)
		{
			int min = 0;
			int natural = 0;

			base.OnGetPreferredHeight (out min, out natural);
			minimum_height = Math.Max (FONT_HEIGHT + 2 * (ENTRY_PADDING + TAGBOX_PADDING), min);
			natural_height = min;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			this.allocRect = new Rectangle (this.Allocation.Left, this.Allocation.Top, this.Allocation.Width, this.Allocation.Height);
		}

		protected override void OnStateChanged (StateType previous_state)
		{
			base.OnStateChanged (previous_state);
		}

		private void RoundedRect(Cairo.Context cr, Cairo.Rectangle rect, double r)
		{
			var rightAngle = Math.PI / 2.0;
			cr.NewPath ();
			cr.Arc(rect.X + rect.Width - r, rect.Y + r, r, -rightAngle, 0);
			cr.Arc(rect.X + rect.Width - r, rect.Y + rect.Height - r, r, 0, rightAngle);
			cr.Arc(rect.X + r, rect.Y + rect.Height - r, r, rightAngle, rightAngle * 2);
			cr.Arc(rect.X + r, rect.Y + r, r, rightAngle*2, rightAngle * 3);
			cr.ClosePath ();
		}

		protected override bool OnDrawn (Cairo.Context cr)
		{
			if (this.isDirty)
				this.Rebuild ();
			
			bool baseDrawnResult = base.OnDrawn (cr);

			cr.SetFontSize (FONT_HEIGHT);
			cr.SelectFontFace ("Mono", FontSlant.Normal, FontWeight.Normal);

			cr.Rectangle (0.0, 0.0, this.allocRect.Width, this.allocRect.Height);
			cr.SetSourceRGB (1, 1, 1);
			cr.Fill ();

			var gradient = new LinearGradient (0.0, 0.0, 0.0, this.allocRect.Height);
			gradient.AddColorStopRgb (0.0, new Color (0.4, 0.4, 0.4));
			gradient.AddColorStopRgb (0.1, new Color (1.0, 1.0, 1.0));
			gradient.AddColorStopRgb (0.2, new Color (0.6, 0.6, 0.6));
			gradient.AddColorStopRgb (1.0, new Color (0.1, 0.1, 0.1));

			cr.LineWidth = 1;

			foreach (var box in this.boxen) {
				this.RoundedRect(cr, box.Rect, 4.0);
				cr.SetSource (gradient);
				cr.FillPreserve ();
				cr.SetSourceRGB (0,0,0);
				cr.Stroke ();


				int x = (int)(box.Rect.X + TAGBOX_PADDING * 2 + TAGBOX_XSIZE);
				int y = (int)(box.Rect.Y + box.Rect.Height/2 + cr.FontExtents.Height/2 - cr.FontExtents.Descent/2);
				cr.MoveTo (x, y);
				cr.TextPath (box.Tag);
				cr.SetSourceRGB (1.0, 1.0, 1.0);
				cr.Fill ();

				cr.MoveTo (box.Rect.X + TAGBOX_PADDING + TAGBOX_XOFFSET, box.Rect.Y + TAGBOX_PADDING + TAGBOX_XOFFSET);
				cr.RelLineTo (TAGBOX_XSIZE - TAGBOX_XOFFSET*2, TAGBOX_XSIZE - TAGBOX_XOFFSET*2);
				cr.MoveTo (box.Rect.X + TAGBOX_PADDING + TAGBOX_XOFFSET, box.Rect.Y + TAGBOX_PADDING + TAGBOX_XSIZE - TAGBOX_XOFFSET);
				cr.RelLineTo (TAGBOX_XSIZE - TAGBOX_XOFFSET*2, -TAGBOX_XSIZE + TAGBOX_XOFFSET*2);
				cr.Stroke ();
			}

			return baseDrawnResult;
		}
	}
}

