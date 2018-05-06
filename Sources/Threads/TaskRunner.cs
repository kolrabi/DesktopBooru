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
		private Thread[] asyncTaskExecuterThreads;
		private bool asyncTaskExecuterStopWanted;

		private long totalCount = 0;

		private SemaphoreSlim queueSema = new SemaphoreSlim(0);

		public TaskRunner ()
		{
			this.asyncTaskExecuterStopWanted = false;
			this.asyncTaskExecuterThreads = new Thread[4];

			for (int i = 0; i < this.asyncTaskExecuterThreads.Length; i++) {
				this.asyncTaskExecuterThreads [i] = new Thread (new ThreadStart (this.ExecuteAsync));
				this.asyncTaskExecuterThreads [i].Name = string.Format ("Booru.TaskRunner {0}", i);
				this.asyncTaskExecuterThreads[i].Start ();
			}

			BooruApp.BooruApplication.EventCenter.WillQuit += this.FlushQueues;
		}

		void StartMainIdle()
		{
			// lock to prevent multiple idles to be added
			lock (this) {
				if (this.mainThreadTaskIdle == 0)
					this.mainThreadTaskIdle = GLib.Idle.Add (MainThreadIdle);
			}
		}

		bool MainThreadIdle()
		{
			// lock to prevent other threads from adding tasks
			lock (this) {
				// try executing next task, keep idle active as long as there is work to be done
				while (this.ExecuteMainThread ())
					;

				// nothing more to be done, remove idle
				GLib.Idle.Remove (this.mainThreadTaskIdle);
				this.mainThreadTaskIdle = 0;
				return false;
			}
		}

		// add task to be executed in the main thread next when there is time
		public void StartTaskMainThread(Action a) 
		{
			System.Diagnostics.Debug.Assert(a != null);
			System.Diagnostics.Debug.Assert(!this.asyncTaskExecuterStopWanted);

			if (this.asyncTaskExecuterStopWanted) {
				BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, BooruLog.Severity.Warning, "TaskRunner rejecting main thread task. We are shutting down!");
				return;
			}

			this.runningTasksMainThread.Add (new Task(a));
			this.StartMainIdle ();
			Interlocked.Increment (ref this.totalCount);
		}

		// add task to be executed in one of the worker threads
		public void StartTaskAsync(Action a) 
		{
			System.Diagnostics.Debug.Assert(a != null);

			this.runningTasksAsync.Add (new Task(a));
			this.queueSema.Release ();
			Interlocked.Increment (ref this.totalCount);
		}

		void ExecuteAsync()
		{
			while (!this.asyncTaskExecuterStopWanted || this.runningTasksAsync.Count > 0) {
				this.queueSema.Wait ();
				BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, BooruLog.Severity.Debug, "Task runner woke up");
				this.ExecuteNextTask (this.runningTasksAsync);
			}
		}

		bool ExecuteMainThread() 
		{
			System.Diagnostics.Debug.Assert(BooruApp.BooruApplication.IsMainThread);

			BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, BooruLog.Severity.Debug, "Main thread is idle");

			return ExecuteNextTask (this.runningTasksMainThread);
		}

		void FlushQueues() 
		{
			System.Diagnostics.Debug.Assert(BooruApp.BooruApplication.IsMainThread);

			BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, BooruLog.Severity.Info, "Flushing " + this.QueueLength + " running tasks...");

			// signal thread to finish
			this.asyncTaskExecuterStopWanted = true;

			lock (this) {
				// stop executing idle 
				GLib.Idle.Remove (this.mainThreadTaskIdle);
				this.mainThreadTaskIdle = 0;

				// flush main thread tasks, we should be in main thread here
				while(this.runningTasksMainThread.Count > 0)
					this.ExecuteNextTask(this.runningTasksMainThread);
			}

			// wait for async tasks to finish
			while(this.runningTasksAsync.Count > 0) {
				Thread.Yield();
			}

			// wake up threads, stop wanted was signalled, so each should only wait for one release
			for (int i=0; i<this.asyncTaskExecuterThreads.Length; i++)
				this.queueSema.Release ();

			// wait for threads to finish
			for (int i=0; i<this.asyncTaskExecuterThreads.Length; i++)
				this.asyncTaskExecuterThreads[i].Join();
		}

		bool ExecuteNextTask(ConcurrentBag<Task> queue)
		{
			Task task;

			if (queue.Count > 0 && queue.TryTake (out task)) {
				System.Diagnostics.Debug.Assert (!task.IsCompleted);

				try {
					System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
						
					BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, BooruLog.Severity.Debug, "Running task "+task.Id);

					stopwatch.Start();
					task.RunSynchronously ();
					stopwatch.Stop();

					BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, BooruLog.Severity.Debug, "Task "+task.Id+" took "+stopwatch.ElapsedMilliseconds+"ms");

					if (task.Exception != null)
						throw task.Exception;
				} catch(Exception ex) {
					BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, BooruLog.Severity.Error, "Caught exception while running task: "+ex.Message);
					BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, BooruLog.Severity.Error, ex.StackTrace);
				}

				Interlocked.Decrement (ref this.totalCount);
				return true;
			} else {
				return false;
			}
		}

	}
}

