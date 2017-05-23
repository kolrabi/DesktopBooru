using System;

namespace Booru
{
	public static class CairoHelper
	{
		public static void DrawStringAt(this Cairo.Context cr, double x, double y, string text, Cairo.Color foreground, Cairo.Color background)
		{
			cr.LineWidth = 3.5;
			cr.LineJoin = Cairo.LineJoin.Round;
			cr.MoveTo (x,y);
			cr.TextPath(text);
			cr.SetSourceColor (background);
			cr.StrokePreserve ();
			cr.SetSourceColor (foreground);
			cr.Fill ();
		}

	}
}

