﻿using System;
using System.IO.Compression;

namespace Booru
{
	public class PixbufLoader
	{
		/// <summary>
		/// Tries loading animation directly from file.
		/// </summary>
		/// <returns>The pixbuf animation from image file.</returns>
		/// <param name="path">Path.</param>
		private static Gdk.PixbufAnimation LoadPixbufAnimationFromImageFile(string path)
		{
			if (!System.IO.File.Exists (path))
				return null;
			
			try {
				Gdk.PixbufAnimation animation = new Gdk.PixbufAnimation (path);
				return animation;
			} catch(Exception ex) {
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Image, ex, "Caught exception trying to load " + path + " as an image");
				return null;
			}
		}

		/// <summary>
		/// Loads the pixbuf animation for image, tries loading from file directly, falls back
		/// on vidthumbs path.
		/// </summary>
		/// <returns>The pixbuf animation for image.</returns>
		/// <param name="path">Path.</param>
		/// <param name="md5">Md5.</param>
		public static Gdk.PixbufAnimation LoadPixbufAnimationForImage(string path, string md5)
		{
			Gdk.PixbufAnimation animation = null;

			var configVidthumbsPath = BooruApp.BooruApplication.Database.Config.GetString ("vidthumbs.path");
			if (!string.IsNullOrEmpty (configVidthumbsPath)) {
				animation = animation ?? PixbufLoader.LoadPixbufAnimationFromImageFile (configVidthumbsPath + "/" + md5 + ".jpg");
				animation = animation ?? PixbufLoader.LoadPixbufAnimationFromImageFile (configVidthumbsPath + "/" + md5 + ".png");
			}

			var configVidthumbsPath2 = BooruApp.BooruApplication.Database.Config.GetString ("vidthumbs.path2");
			if (!string.IsNullOrEmpty (configVidthumbsPath2)) {
				animation = animation ?? PixbufLoader.LoadPixbufAnimationFromImageFile (configVidthumbsPath2 + "/" + md5 + ".jpg");
				animation = animation ?? PixbufLoader.LoadPixbufAnimationFromImageFile (configVidthumbsPath2 + "/" + md5 + ".png");
			}

			animation = animation ?? PixbufLoader.LoadPixbufAnimationFromImageFile (path);
			animation = animation ?? new Gdk.PixbufAnimation (null, Resources.ID_PIXBUFS_NOPREVIEW);

			return animation;		
		}

		public static Gdk.PixbufAnimation LoadPixbufAnimationForImage(ImageDetails data)
		{
			return PixbufLoader.LoadPixbufAnimationForImage (data.Path, data.MD5);
		}

	}
}

