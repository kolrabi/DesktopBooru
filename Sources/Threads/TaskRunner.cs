using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Booru
{
	public class TaskRunner
	{
		public long QueueLength { get { return this.totalCount; } }

		private readonly ConcurrentBag<Task> runningTasksMainThread = new ConcurrentBag<Task>();
		private readonly ConcurrentBag<Task> runningTasksAsync = new ConcurrentBag<Task>();

		private uint mainThreadTaskIdle = 0;
		private Thread asyncTaskExecuterThread;
		private bool asyncTaskExecuterStopWanted;

		private long totalCount = 0;

		public TaskRunner ()
		{
			this.asyncTaskExecuterThread = new Thread (new ThreadStart (this.ExecuteAsync));
			this.asyncTaskExecuterStopWanted = false;
			this.asyncTaskExecuterThread.Start ();

			BooruApp.BooruApplication.EventCenter.WillQuit += this.FlushQueues;
		}

		private void StartMainIdle()
		{
			if (this.mainThreadTaskIdle == 0)
			this.mainThreadTaskIdle = GLib.Idle.Add (() => { 
				if (this.ExecuteMainThread())
					return true;

				GLib.Idle.Remove(this.mainThreadTaskIdle);
				this.mainThreadTaskIdle = 0;
				return false;
			});
		}

		public void StartTaskMainThread(Action a) 
		{
			System.Diagnostics.Debug.Assert(a != null);

			this.runningTasksMainThread.Add (new Task(a));
			this.StartMainIdle ();
			Interlocked.Increment (ref this.totalCount);
		}

		public void StartTaskAsync(Action a) 
		{
			System.Diagnostics.Debug.Assert(a != null);

			this.runningTasksAsync.Add (new Task(a));
			Interlocked.Increment (ref this.totalCount);
		}

		private void ExecuteAsync()
		{
			while (!this.asyncTaskExecuterStopWanted || this.runningTasksAsync.Count > 0) {
				if (this.ExecuteNextTask (this.runningTasksAsync)) {
					Interlocked.Decrement (ref this.totalCount);
				} else {
					Thread.Sleep (100);
				}
			}
		}

		private bool ExecuteMainThread() 
		{
			System.Diagnostics.Debug.Assert(BooruApp.BooruApplication.IsMainThread);

			return ExecuteNextTask (this.runningTasksMainThread);
		}

		private void FlushQueues() 
		{
			System.Diagnostics.Debug.Assert(BooruApp.BooruApplication.IsMainThread);

			BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, BooruLog.Severity.Info, "Flushing " + this.QueueLength + " running tasks...");

			// signal thread to finish
			this.asyncTaskExecuterStopWanted = true;

			// stop executing idle 
			GLib.Idle.Remove( this.mainThreadTaskIdle );
			this.mainThreadTaskIdle = 0;

			// flush main thread tasks, we should be in main thread here
			while(this.runningTasksMainThread.Count > 0)
				this.ExecuteNextTask(this.runningTasksMainThread);

			// wait for async tasks to finish
			while(this.runningTasksAsync.Count > 0) {
				Thread.Sleep(100);
			}
			this.asyncTaskExecuterThread.Join();
		}

		private bool ExecuteNextTask(ConcurrentBag<Task> queue)
		{
			Task task;
			if (queue.Count > 0 && queue.TryTake (out task)) {
				System.Diagnostics.Debug.Assert (!task.IsCompleted);
				task.RunSynchronously ();
				return true;
			} else {
				return false;
			}
		}

	}
}

