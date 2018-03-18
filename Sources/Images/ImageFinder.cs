using System;
using System.Threading;
using System.Collections.Concurrent;

namespace Booru
{
	public class ImageFinder
	{
		private readonly string searchString;
		private readonly ThumbStore model;
		private readonly Thread thread;

		public int ResultCount { get; private set; }

		public ImageFinder(string searchString, ThumbStore model)
		{
			this.searchString = searchString;
			this.model = model;
			this.thread = new Thread(new ThreadStart(Run));
			this.thread.Name = "Image Finder";
		}

		public void Start()
		{
			this.thread.Start ();
		}

		public void Abort()
		{
			this.thread.Abort ();
			this.model.EndAdding ();
			this.model.AbortAdding ();
		}

		private void Run()
		{
			BooruApp.BooruApplication.Log.Log (BooruLog.Category.Database, BooruLog.Severity.Info, "Starting search for: " + searchString);

			this.model.BeginAdding ();
			using (var cursor = BooruApp.BooruApplication.Database.QueryImagesWithTags(searchString)) {
				while (cursor.Read ()) {
					this.AddImage (cursor.Value);
				}
			}
			this.model.EndAdding ();

			BooruApp.BooruApplication.Log.Log(BooruLog.Category.Database, BooruLog.Severity.Info, "Found "+this.ResultCount+" images for search: "+searchString);
		}

		private void AddImage(ImageDetails data)
		{
			if (!System.IO.File.Exists(data.Path)) {
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Files, BooruLog.Severity.Warning, "Could not find file "+data.Path);
				//return;
			}

			var image = Image.GetImage (data);
			image.CacheThumbnail();

			this.model.QueueAddedImage (image);
			image.Release ();

			this.ResultCount ++;
		}


	}
}

