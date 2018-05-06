using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Booru
{
	public class Image : RefCountedDisposable
	{
		public readonly ImageDetails Details;
		public List<string> Tags;

		public Gdk.PixbufAnimation Anim { get { return this.CacheAnimation(); } }
		public Gdk.Pixbuf Thumbnail { get { return this.GetThumbnail(); } }

		private Gdk.PixbufAnimation cachedAnimation;
		private Gdk.Pixbuf cachedThumbnail;

		public double TotalTagScore { get; private set; }
		public double AvgTagScore { get; private set; }

		FileArchive archive = null;
		private int subImage = -1;
		public int SubImage { get { return subImage; } set { subImage = value; SelectSubImage (); } }
		private int maxImage = -1;
		public int MaxImage { get { return this.maxImage; } }
		public string SubImageName { get; private set; }

		private static IDictionary<string, Image> imageCache = new Dictionary<string, Image>();

		public static Image GetImage(ImageDetails details)
		{
			lock (imageCache) {
				if (imageCache.ContainsKey (details.MD5)) {
					return imageCache [details.MD5];
				}
				Image image = new Image (details, details);
				imageCache [details.MD5] = image;
				return image;
			}
		}

		private Image (ImageDetails details, ImageDetails details2)
		{
			this.Details = details;
			this.SubImageName = "";
			this.ReloadTags ();
		}
			
		public override void Dispose()
		{
			System.Diagnostics.Debug.Assert (this.IsDisposed);

			lock (imageCache) {
				if (imageCache.ContainsKey (Details.MD5)) {
					imageCache.Remove (Details.MD5);
				}
				//Console.WriteLine ("{0} images left in cache", imageCache.Count);
			}
				
			if (this.cachedAnimation != null)
				this.cachedAnimation.Dispose ();
			this.cachedAnimation = null;

			if (this.cachedThumbnail != null)
				this.cachedThumbnail.Dispose ();
			this.cachedThumbnail = null;

			if (this.archive != null)
				this.archive.Dispose ();
			this.archive = null;
		}

		void OpenArchive()
		{
			if (this.archive != null)
				return;
			
			var file = File.OpenRead (this.Details.Path);
			byte[] magic = new byte[16];
			file.Read (magic, 0, 16);
			file.Seek (0, SeekOrigin.Begin);

			this.maxImage = -1;
			this.subImage = -1;

			if (magic[0] == 0x50 && magic[1] == 0x4b && magic[2] == 0x03 && magic[3] == 0x04) {
				// zip
				this.archive = new ZipArchive(file);
			} else if (magic[0] == 0x52 && magic[1] == 0x61 && magic[2] == 0x72 && magic[3] == 0x21) {
				// rar
				// TODO: this.OpenRar(file);
			}

			if (this.archive != null) {
				this.maxImage = archive.GetEntryCount () - 1;
			}
		}
			
		void SelectBaseImage()
		{
			this.subImage = -1;

			if (this.cachedAnimation != null)
				this.cachedAnimation.Dispose ();
			this.cachedAnimation = null;
			this.CacheAnimation ();
		}

		void SelectSubImage()
		{
			if (subImage < 0 || subImage > this.maxImage) {
				this.SelectBaseImage ();
				return;
			}

			try {
				int subImage = this.subImage;
				this.OpenArchive();
				this.subImage = subImage;
			} catch(Exception ex) {
				BooruApp.BooruApplication.Log.Log (BooruLog.Category.Image, ex, "Caught exception opening zip for sub image");
				this.SelectBaseImage ();
				return;
			}

			try {
				var entry = this.archive.GetEntry(this.subImage);
				using (var stream = entry.Open()) {
					if (this.cachedAnimation != null)
						this.cachedAnimation.Dispose ();
					this.cachedAnimation = new Gdk.PixbufAnimation (stream);
					this.SubImageName = entry.Name;
				}
			} catch(Exception ex) {
				BooruApp.BooruApplication.Log.Log (BooruLog.Category.Image, ex, "Caught exception finding zip entry for sub image");
				this.cachedAnimation = Resources.LoadResourcePixbufAnimation (Resources.ID_PIXBUFS_NOPREVIEW);
			}
		}

		#region Thumbnails
		public void CacheThumbnail()
		{
			if (this.cachedThumbnail != null)
				return;

			if (this.LoadThumbnail ())
				return;

			this.CreateThumbnail ();
			this.SaveThumbnail ();
		}

		private Gdk.Pixbuf GetThumbnail()
		{
			this.CacheThumbnail ();
			return this.cachedThumbnail;
		}

		private bool LoadThumbnail()
		{
			var dbConfig = BooruApp.BooruApplication.Database.Config;

			var configThumbsPath = dbConfig.GetString ("thumbs.path");
			var configThumbsEnable = dbConfig.GetBool ("thumbs.enable");
			var configThumbSize = dbConfig.GetInt ("thumbs.size");

			if (!configThumbsEnable)
				return false; 

			if (string.IsNullOrEmpty (configThumbsPath))
				return false;
			
			string thumbnailPath = configThumbsPath + "/" + this.Details.MD5 + ".thumb.jpg";
			if (!File.Exists (thumbnailPath))
				return false;
			
			try {

				var cachedThumbnail = new Gdk.Pixbuf(thumbnailPath);

				if (cachedThumbnail.Width == configThumbSize || cachedThumbnail.Height == configThumbSize) {
					this.cachedThumbnail = cachedThumbnail;
				} else {
					this.CreateThumbnail();
				}

				return true;
			} catch (Exception ex) {
				BooruApp.BooruApplication.Log.Log (BooruLog.Category.Image, ex, "Caught exception loading thumbnail "+thumbnailPath);
				return false;
			}
		}

		private void SaveThumbnail()
		{
			if (this.cachedThumbnail == null)
				return;
			
			var dbConfig = BooruApp.BooruApplication.Database.Config;

			var configThumbsPath = dbConfig.GetString ("thumbs.path");
			var configThumbsEnable = dbConfig.GetBool ("thumbs.enable");

			if (!configThumbsEnable)
				return; 

			if (string.IsNullOrEmpty (configThumbsPath))
				return;

			string thumbnailPath = configThumbsPath + "/" + this.Details.MD5 + ".thumb.jpg";
			this.cachedThumbnail.Save (thumbnailPath, "jpeg");
		}

		private void CreateThumbnail()
		{
			Gdk.PixbufAnimation animation = this.LoadAnimation ();
			Gdk.Pixbuf staticImage = animation.StaticImage;

			var dbConfig = BooruApp.BooruApplication.Database.Config;
			float thumbSize = dbConfig.GetInt ("thumbs.size");

			float scale = Math.Min (thumbSize / staticImage.Width, thumbSize / staticImage.Height);
			int w = (int)(staticImage.Width * scale);
			int h = (int)(staticImage.Height * scale);

			this.cachedThumbnail = staticImage.ScaleSimple (w, h, Gdk.InterpType.Hyper);

			if (staticImage.Width > 0 && staticImage.Height > 0) {
				if (this.Details.Size.IsZero) {
					BooruApp.BooruApplication.Database.SetImageSize (this.Details.MD5, new Point2D(staticImage.Width, staticImage.Height));
				}
			}

			// only dispose if not from cached animation
			if (this.cachedAnimation != animation) {
				animation.Dispose ();
				staticImage.Dispose ();
			}
		}

		public void DisposeThumbnail()
		{
			if (this.cachedThumbnail == null)
				return;

			this.cachedThumbnail.Dispose ();
			this.cachedThumbnail = null;
		}

		#endregion

		#region Animation
		private Gdk.PixbufAnimation LoadAnimation()
		{
			if (this.cachedAnimation != null)
				return this.cachedAnimation;

			return PixbufLoader.LoadPixbufAnimationForImage (this.Details);
		}

		private Gdk.PixbufAnimation CacheAnimation()
		{
			if (this.archive == null && this.cachedAnimation == null && this.Details.type == BooruImageType.Comix) {
				try {
					this.OpenArchive ();
				} catch (Exception ex) {
					BooruApp.BooruApplication.Log.Log (BooruLog.Category.Image, ex, "Caught exception opening zip "+this.Details.Path+" to cache");
				}
			}

			if (this.cachedAnimation == null)
				this.cachedAnimation = this.LoadAnimation ();
	
			return this.cachedAnimation;
		}
		#endregion
					
		#region Voting
		public static float GetEloOffset (Image winner, Image loser)
		{
			if (winner == null || loser == null)
				return 0;

			float Ew = (float)(1.0f / (1.0f + Math.Pow (10.0f, (loser.Details.ELO - winner.Details.ELO) * 0.1f)));

			float d = 10.0f * (1.0f - Ew);
			return d;
		}

		public void Win(Image opponent) 
		{
			float offset = GetEloOffset (this, opponent);
			BooruApp.BooruApplication.Log.Log (BooruLog.Category.Image, BooruLog.Severity.Info, this.Details.MD5 + ": Won " + offset + " elo. Now " + this.Details.ELO);

			this.Details.UpdateElo (offset);
			BooruApp.BooruApplication.Database.AddImageElo (this.Details.MD5, offset);
			BooruApp.BooruApplication.Database.UpdateTagsScore (this.Tags, +1);

			opponent.Details.UpdateElo (-offset);
			BooruApp.BooruApplication.Database.AddImageElo (opponent.Details.MD5, -offset);
			BooruApp.BooruApplication.Database.UpdateTagsScore (opponent.Tags, -1);
		}

		#endregion

		#region Tags

		public void AddTag(string tag) 
		{
			if (tag.StartsWith ("-")) {
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Image, BooruLog.Severity.Info, this.Details.MD5+": Remove tag "+tag.Substring(1));
				this.RemoveTag (tag.Substring (1));
				return;
			}

			List<string> impliedTags = BooruApp.BooruApplication.Database.GetTagImplications (tag);
			if (!this.Tags.Contains (tag)) {
				BooruApp.BooruApplication.Database.AddImageTag (this.Details.MD5, tag);
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Image, BooruLog.Severity.Info, this.Details.MD5+": Add tag "+tag);

				this.Tags.Add (tag);

				foreach (var implied in impliedTags) {
					if (!implied.StartsWith("-"))
						BooruApp.BooruApplication.Log.Log(BooruLog.Category.Image, BooruLog.Severity.Info, this.Details.MD5+": Add implied tag "+implied);
					this.AddTag (implied);
				}
			}

			this.Tags.Sort ();
		}

		public void RemoveTag(string tag)
		{
			BooruApp.BooruApplication.Database.RemoveImageTag (this.Details.MD5, tag);
			if (this.Tags.Contains (tag))
				this.Tags.Remove (tag);
		}

		public void ReloadTags() 
		{
			this.Tags = BooruApp.BooruApplication.Database.GetImageTags (this.Details.MD5);

			// calculate score
			this.TotalTagScore = 0;
			this.AvgTagScore = 0;

			foreach (string tagString in this.Tags) {
				var tag = BooruApp.BooruApplication.Database.GetTag (tagString);
				double score = tag == null ? 0.0 : tag.Score;
				this.TotalTagScore += score;
			}
			if (this.Tags.Count > 0) {
				this.AvgTagScore = this.TotalTagScore / this.Tags.Count;
			}
		}
		#endregion

		public void ViewExternal(int windowId)
		{
			string viewer = "";
			switch (this.Details.type) {
				case BooruImageType.Image:				viewer = "viewer.image";				break;
				case BooruImageType.Animation:			viewer = "viewer.animation";			break;
				case BooruImageType.Comix:				viewer = "viewer.comix";				break;
				case BooruImageType.Video:				viewer = "viewer.video";				break;
			}

			string[] viewerParts = BooruApp.BooruApplication.Database.Config.GetString (viewer).Split (" ".ToCharArray(), 2);
			System.Diagnostics.Process.Start(viewerParts[0], string.Format(viewerParts[1], "\"" + this.Details.Path + "\"", windowId));
		}
	}
}

