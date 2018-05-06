using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Data.Common;

namespace Booru
{
	public class RandomImageLoader
	{
		ConcurrentQueue<Task<Image>> taskQueue = new ConcurrentQueue<Task<Image>>();
		ImageReader reader = new ImageReader(BooruImageType.Image);
		BooruImageType type = BooruImageType.Image;
	
		public RandomImageLoader ()
		{
			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted += OnDatabaseLoadSucceeded;
		}

		void OnDatabaseLoadSucceeded()
		{
			this.SetFilter (this.type);
		}
			
		Task<Image> LoadImage()
		{
			var loadingTask = new Task<Image> (GetNextImageProc);
			loadingTask.Start ();
			return loadingTask;
		}

		Image GetNextImageProc()
		{
			ImageDetails data = null;
			while (data == null) {
				lock(this.reader) {
					data = this.reader.GetNextImage();
				}
			}
			return Image.GetImage(data);
		}

		public async Task<Image> NextImage()
		{
			Task<Image> task;
			lock (this.taskQueue) {
				// queue next image to be loaded
				taskQueue.Enqueue (LoadImage ());

				// and take one out
				taskQueue.TryDequeue (out task);	
			}
			return await task;
		}

		public void SetFilter(BooruImageType type)
		{
			lock (this.taskQueue) {
				// clear out queue
				while (!this.taskQueue.IsEmpty) {
					Task<Image> task;
					this.taskQueue.TryDequeue (out task);
				}

				// replace reader with reader for new type
				this.reader.Close ();
				this.reader = new ImageReader (type);

				// replace old queue with new one, add two tasks
				var queue = new ConcurrentQueue<Task<Image>>();
				queue.Enqueue (LoadImage ());
				queue.Enqueue (LoadImage ());
				this.taskQueue = queue;
			}
		}
	}
}

