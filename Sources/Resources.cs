using System;

namespace Booru
{
	public class Resources
	{
		public const string ID_PIXBUFS_NOPREVIEW = "Booru.Resources.Pixbufs.nopreview.png";

		public const string ID_STYLES_SCREEN_CSS = "Booru.Resources.Styles.screen.css";

		public static System.IO.Stream OpenResource(string id)
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly ();
			return asm.GetManifestResourceStream (id);
		}

		public static string LoadResourceString(string id)
		{
			using (var stream = Resources.OpenResource(id))
			using (var reader = new System.IO.StreamReader(stream))
			{
				return reader.ReadToEnd();
			}
		}
	}
}

