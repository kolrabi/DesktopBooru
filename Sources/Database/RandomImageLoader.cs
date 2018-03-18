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
		private ImageReader[] readers = new ImageReader[2];
		private BooruImageType type = BooruImageType.Image;
		private int idx = 0;
	
		public RandomImageLoader ()
		{
			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted += OnDatabaseLoadSucceeded;
		}

		private void OnDatabaseLoadSucceeded()
		{
			this.SetFilter (this.type);
		}

		private Random rnd = new Random();

		private Task<Image> LoadImage()
		{
			var loadingTask = new Task<Image> (() => {
				ImageDetails data = null;
				while (data == null) {
					lock(this.readers) {
						data = this.readers[rnd.Next()%2].GetNextImage();
						//this.idx = 1-this.idx;
					}
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

				lock (this.readers) {
					for (int i=0; i<2; i++)
					{
						if (this.readers [i] != null)
							this.readers [i].Close ();

						this.readers[i] = new ImageReader (type, i == 0);
					}
				}

				var queue = new ConcurrentQueue<Task<Image>>();
				for (int i = 0; i < 2; i++)
					queue.Enqueue (LoadImage ());
				this.taskQueue = queue;
			}
		}
	}
}

