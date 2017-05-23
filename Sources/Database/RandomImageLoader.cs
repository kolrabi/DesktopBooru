using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Data.Common;

namespace Booru
{
	public class RandomImageLoader
	{
		private ConcurrentQueue<Task<Image>> taskQueue = new ConcurrentQueue<Task<Image>>();
		private ImageReader reader;
		private BooruImageType type = BooruImageType.Image;
	
		public RandomImageLoader ()
		{
			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted += OnDatabaseLoadSucceeded;
		}

		private void OnDatabaseLoadSucceeded()
		{
			this.SetFilter (this.type);
		}

		private Task<Image> LoadImage()
		{
			var loadingTask = new Task<Image> (() => {
				ImageDetails data = null;
				while (data == null) {
					lock(this.reader) {
						data = this.reader.GetNextImage();
					}
					if (data == null)
						System.Threading.Thread.Sleep(1000);
				}
				return Image.GetImage(data);
			});
			loadingTask.Start ();
			return loadingTask;
		}

		public async Task<Image> NextImage()
		{
			Task<Image> task;
			lock (this.taskQueue) {
				taskQueue.Enqueue (LoadImage ());

				taskQueue.TryDequeue (out task);	
			}
			return await task;
		}

		public void SetFilter(BooruImageType type)
		{
			lock (this.taskQueue) {
				while (!taskQueue.IsEmpty) {
					Task<Image> task;
					taskQueue.TryDequeue (out task);	
				}

				if (this.reader != null)
					this.reader.Close ();

				this.reader = new ImageReader (type);
				var queue = new ConcurrentQueue<Task<Image>>();
				for (int i = 0; i < 2; i++)
					queue.Enqueue (LoadImage ());
				this.taskQueue = queue;
			}
		}
	}
}

