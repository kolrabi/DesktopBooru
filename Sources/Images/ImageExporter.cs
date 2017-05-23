using System;
using System.Threading;
using System.Collections.Generic;

namespace Booru
{
	class ImageExporter
	{
		private readonly string exportPath;
		private readonly List<Image> images;

		private readonly Thread thread;

		public delegate void OnFinished ();
		public event OnFinished Finished;

		public delegate void OnExported ();
		public event OnExported Exported;

		public ImageExporter(ThumbStore model, string exportPath)
		{
			this.images = new List<Image>();
			this.exportPath = exportPath;

			lock (model) {
				Gtk.TreeIter iter;
				if (model.GetIterFirst (out iter)) {
					do {
						this.images.Add (model.GetImage (new Gtk.TreeRowReference (model, model.GetPath (iter))));
					} while(model.IterNext (ref iter));
				}
			}

			this.thread = new Thread(new ThreadStart(Run));
			this.thread.Name = "Image Exporter";
		}

		public void Start()
		{
			this.thread.Start ();
		}

		public void Abort()
		{
			this.thread.Abort ();
		}

		public void Run()
		{
			foreach(var image in this.images) {
				try {
					System.IO.File.Copy (image.Details.Path, this.exportPath + "/" + System.IO.Path.GetFileName (image.Details.Path));
					BooruApp.BooruApplication.Log.Log (BooruLog.Category.Files, BooruLog.Severity.Info, "Exported " + image.Details.MD5 + " to " + exportPath);
				} catch (Exception ex) {
					BooruApp.BooruApplication.Log.Log (BooruLog.Category.Files, BooruLog.Severity.Error, "Could not export " + image.Details.MD5 + " to " + exportPath + ": " + ex.Message);
				}
				if (this.Exported != null)
					this.Exported ();
			}

			if (this.Finished != null)
				this.Finished ();
		}
	}
}

