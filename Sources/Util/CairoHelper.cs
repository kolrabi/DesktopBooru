using System;

namespace Booru
{
	public static class CairoHelper
	{
		public const int TAB_WIDTH=64;

		public static void DrawStringAt(this Cairo.Context cr, double x, double y, string text, Cairo.Color foreground, Cairo.Color background)
		{
			string[] lines = text.Split ("\n".ToCharArray ());
			double leftX = x;
			cr.LineWidth = 3.5;
			cr.LineJoin = Cairo.LineJoin.Round;

			foreach (var line in lines) {
				string[] tabs = line.Split ("\t".ToCharArray ());
				x = leftX;

				foreach (var tab in tabs) {
					cr.MoveTo (x, y);
					cr.TextPath (tab);

					if (tabs.Length > 1) {
						var extents = cr.TextExtents (tab);
						x += extents.XAdvance;

						x = (((int)x / TAB_WIDTH) + 1) * TAB_WIDTH;
					}
				}
				y += cr.FontExtents.Height;
			}

			cr.SetSourceColor (background);
			cr.StrokePreserve ();
			cr.SetSourceColor (foreground);
			cr.Fill ();
		}


		public static void SelectTagOverlayFont(this Cairo.Context cr)
		{
			cr.SelectFontFace ("Noto Mono", Cairo.FontSlant.Normal, Cairo.FontWeight.Normal);
			cr.SetFontSize (12.0);
			cr.Antialias = Cairo.Antialias.Gray;
		}
	}
}

