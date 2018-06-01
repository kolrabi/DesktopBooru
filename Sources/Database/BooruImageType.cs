using System;
using System.IO;

namespace Booru
{
	public enum BooruImageType
	{
		// Unknown file type
		Unknown = 0,
		Image,
		Animation,
		Comix,
		Video
	}

	public static class BooruImageTypeHelper
	{
		public static BooruImageType IdentifyType(string path, out bool callBooru)
		{		
			try {
				byte[] magic = new byte[16];
				using(var file = File.OpenRead (path)) {
					file.Read (magic, 0, 16);
				}

				if (magic[0] == 0xff && magic[1] == 0xd8 && magic[2] == 0xff) {
					// jpeg
					callBooru = true;
					return BooruImageType.Image;
				}

				if (magic[0] == 0x89 && magic[1] == 0x50 && magic[2] == 0x4e && magic[3] == 0x47) {
					// png
					callBooru = true;
					return BooruImageType.Image;
				}
				if (magic[0] == 0x47 && magic[1] == 0x49 && magic[2] == 0x46) {
					// gif
					callBooru = true;
					try {
						Gdk.PixbufAnimation anim = new Gdk.PixbufAnimation (path);
						var type = anim.IsStaticImage ? BooruImageType.Image : BooruImageType.Animation;
						anim.Dispose();
						return type;
					} catch (Exception ex) {
						BooruApp.BooruApplication.Log.Log(BooruLog.Category.Files, ex, "Caught exception trying to determine image type for "+path);
					}
					return BooruImageType.Image;
				}
				if (magic[0] == 0x42 && magic[1] == 0x4d) {
					// bmp
					callBooru = true;
					return BooruImageType.Image;
				}

				if (magic[0] == 0x1a && magic[1] == 0x45 && magic[2] == 0xdf && magic[3] == 0xa3) {
					// webm/matroshka
					callBooru = true;
					return BooruImageType.Video;
				}
				if (magic[0] == 0x30 && magic[1] == 0x26 && magic[2] == 0xb2 && magic[3] == 0x75) {
					// wmv/wma/asf
					callBooru = false;
					return BooruImageType.Video;
				}
				if (magic[8] == 0x41 && magic[9] == 0x56 && magic[10] == 0x49 && magic[11] == 0x20) {
					// avi
					callBooru = false;
					return BooruImageType.Video;
				}
				if (magic[0] == 0x4f && magic[1] == 0x67 && magic[2] == 0x67 && magic[3] == 0x53) {
					// ogg
					callBooru = false;
					return BooruImageType.Video;
				}
				if (magic[0] == 0x46 && magic[1] == 0x4c && magic[2] == 0x56 && magic[3] == 0x01) {
					// flv
					callBooru = true;
					return BooruImageType.Video;
				}
				if (magic[0] == 0x2e && magic[1] == 0x52 && magic[2] == 0x4d && magic[3] == 0x46) {
					// rmvb
					callBooru = false;
					return BooruImageType.Video;
				}
				if (magic[0] == 0x00 && magic[1] == 0x00 && magic[2] == 0x01) {
					// mpg
					callBooru = false;
					return BooruImageType.Video;
				}
				if (magic[4] == 0x66 && magic[5] == 0x74 && magic[6] == 0x79 && magic[7] == 0x70) {
					// mpeg?
					callBooru = false;
					return BooruImageType.Video;
				}

				if (magic[0] == 0x50 && magic[1] == 0x4b && magic[2] == 0x03 && magic[3] == 0x04) {
					// zip
					callBooru = false;
					return BooruImageType.Comix;
				}

				if (magic[0] == 0x52 && magic[1] == 0x61 && magic[2] == 0x72 && magic[3] == 0x21) {
					// rar
					callBooru = false;
					return BooruImageType.Comix;
				}

				if (magic[0] == 0x37 && magic[1] == 0x7A && magic[2] == 0xBC && magic[3] == 0xAF && magic[4] == 0x27 && magic[5] == 0x1C) {
					// 7z
					callBooru = false;
					return BooruImageType.Comix;
				}

				callBooru = false;
				return BooruImageType.Unknown;
			} catch (System.Threading.ThreadAbortException ex) {
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Files, ex, "Thread was aborted during image type identification");
			} catch (Exception ex) {
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Files, ex, "Caught exception determining file type for "+path);
			}
			callBooru = false;
			return BooruImageType.Unknown;
		}
	}
}

