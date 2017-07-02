using System;

namespace Booru
{
	public class Resources
	{
		public const string ID_PIXBUFS_NOPREVIEW = "Booru.Resources.Pixbufs.nopreview.png";
		public const string ID_PIXBUFS_BUTTON_PLAY = "Booru.Resources.GUI.play.png";
		public const string ID_PIXBUFS_BUTTON_STOP = "Booru.Resources.GUI.stop.png";
		public const string ID_PIXBUFS_BUTTON_TAG = "Booru.Resources.GUI.tag.png";
		public const string ID_PIXBUFS_BUTTON_SHUFFLE = "Booru.Resources.GUI.shuffle.png";
		public const string ID_PIXBUFS_BUTTON_MARK = "Booru.Resources.GUI.mark.png";
		public const string ID_PIXBUFS_BUTTON_UNMARK = "Booru.Resources.GUI.unmark.png";
		public const string ID_PIXBUFS_BUTTON_DELETE = "Booru.Resources.GUI.delete.png";
		public const string ID_PIXBUFS_BUTTON_VIEW_EXTERNAL = "Booru.Resources.GUI.viewexternal.png";
		public const string ID_PIXBUFS_BUTTON_EXPORT = "Booru.Resources.GUI.export.png";
		public const string ID_PIXBUFS_BUTTON_ABORT = "Booru.Resources.GUI.abort.png";

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

		public static Gdk.PixbufAnimation LoadResourcePixbufAnimation(string id)
		{
			return new Gdk.PixbufAnimation (null, id);
		}
	}
}

