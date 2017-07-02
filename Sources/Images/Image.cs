using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace Booru
{
	public class Image : IDisposable
	{
		public readonly ImageDetails Details;
		public List<string> Tags;

		public Gdk.PixbufAnimation Anim { get { return this.CacheAnimation(); } }
		public Gdk.Pixbuf Thumbnail { get { return this.GetThumbnail(); } }

		private Gdk.PixbufAnimation cachedAnimation;
		private Gdk.Pixbuf cachedThumbnail;

		public double TotalTagScore { get; private set; }
		public double AvgTagScore { get; private set; }

		private int subImage = -1;
		public int SubImage { get { return subImage; } set { subImage = value; SelectSubImage (); } }
		private int maxImage = -1;
		public int MaxImage { get { return this.maxImage; } }
		public string SubImageName { get; private set; }

		private static IDictionary<string, WeakReference<Image>> imageCache = new Dictionary<string, WeakReference<Image>>();
		public static Image GetImage(ImageDetails details)
		{
			lock (imageCache) {
				Image image;
				if (imageCache.ContainsKey (details.MD5)) {
					if (imageCache [details.MD5].TryGetTarget (out image))
						return image;
				}
				image = new Image (details);
				imageCache [details.MD5] = new WeakReference<Image> (image);
				return image;
			}
		}

		private Image (ImageDetails details)
		{
			this.Details = details;
			this.SubImageName = "";
			this.ReloadTags ();
		}

		public void Dispose()
		{
			if (this.cachedAnimation != null)
				this.cachedAnimation.Dispose ();
			this.cachedAnimation = null;

			if (this.cachedThumbnail != null)
				this.cachedThumbnail.Dispose ();
			this.cachedThumbnail = null;

			if (this.zipFile != null)
				this.zipFile.Close ();
		}

		private ZipFile zipFile;
		private List<ZipEntry> zipEntries = new List<ZipEntry> ();

		void SelectBaseImage()
		{
			this.subImage = -1;

			if (this.cachedAnimation != null)
				this.cachedAnimation.Dispose ();
			this.cachedAnimation = null;
			this.CacheAnimation ();

			if (this.zipFile != null)
				this.zipFile.Close ();
			this.zipFile = null;
			this.zipEntries.Clear ();
		}

		void SelectSubImage()
		{
			if (this.subImage < 0 || this.subImage > this.maxImage) {
				this.SelectBaseImage ();
				return;
			}

			try {
				if (this.zipFile == null) {
					this.zipFile = new ZipFile(this.Details.Path);
					this.zipEntries.Clear();
					var e = this.zipFile.GetEnumerator();
					while(e.MoveNext()) {
						var entry = (ZipEntry)e.Current;
						if (entry.IsFile)
							this.zipEntries.Add(entry);
					}
					this.zipEntries.Sort((a,b) => {
						return a.Name.CompareNatural(b.Name);
					});
				}
			} catch(Exception ex) {
				Console.WriteLine (ex.Message);
				this.SelectBaseImage ();
				return;
			}

			try {
				var entry = this.zipEntries[this.subImage];
				using (var stream = this.zipFile.GetInputStream (entry)) {
					if (this.cachedAnimation != null)
						this.cachedAnimation.Dispose ();
					this.cachedAnimation = new Gdk.PixbufAnimation (stream);
					this.SubImageName = entry.Name;
				}
			} catch(Exception ex) {
				Console.WriteLine (ex.Message);
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
				System.Console.Out.WriteLine ("Could not load thumbnail " + thumbnailPath + ": " + ex.Message);
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
			if (this.zipFile == null && this.cachedAnimation == null && this.Details.type == BooruImageType.Comix) {
				try {
					this.zipFile = new ZipFile(this.Details.Path);
					var e = zipFile.GetEnumerator ();
					this.maxImage = -1;
					while(e.MoveNext()) {
						this.maxImage++;
					}
					if (this.maxImage != -1)
					this.subImage = this.subImage % (1+this.maxImage);
				} catch(Exception ex) {
					Console.WriteLine (ex.Message);
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

		public void ViewExternal()
		{
			string viewer = "";
			switch (this.Details.type) {
				case BooruImageType.Image:				viewer = "viewer.image";				break;
				case BooruImageType.Animation:			viewer = "viewer.animation";			break;
				case BooruImageType.Comix:				viewer = "viewer.comix";				break;
				case BooruImageType.Video:				viewer = "viewer.video";				break;
			}

			string[] viewerParts = BooruApp.BooruApplication.Database.Config.GetString (viewer).Split (" ".ToCharArray(), 2);
			System.Diagnostics.Process.Start(viewerParts[0], string.Format(viewerParts[1], "\"" + this.Details.Path + "\""));
		}
	}
}

