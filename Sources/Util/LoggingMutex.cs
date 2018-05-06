using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

namespace Booru
{
	public class LoggingMutex
	{
		private readonly object LockObject;
		private readonly string Name;

		public delegate void CriticalSection();

		private static List<LoggingMutex> allMutexes = new List<Booru.LoggingMutex> ();

		private class Stats 
		{
			public int LockCount;
			public int WaitCount;
			public long totalWaitMS;

			public Stats(int lockCount, int waitCount, long totalWaitMS)
			{
				this.LockCount = lockCount;
				this.WaitCount = waitCount;
				this.totalWaitMS = totalWaitMS;
			}
		}

		private Dictionary<Thread, Stats> stats;

		public LoggingMutex (object lockObject, string name)
		{
			this.LockObject = lockObject;
			this.Name = name;
			this.stats = new Dictionary<Thread, Stats> ();

			lock (allMutexes)
				allMutexes.Add (this);
		}

		public void ExecuteCriticalSection(CriticalSection criticalSection)
		{
			lock (this.stats) {
				if (!this.stats.ContainsKey (Thread.CurrentThread))
					this.stats.Add (Thread.CurrentThread, new Stats (0, 0, 0));
			}
			
			Stopwatch stopWatch = new Stopwatch ();
			stopWatch.Start ();

			if (!Monitor.TryEnter (this.LockObject)) {
				lock (this.stats) this.stats [Thread.CurrentThread].WaitCount++;
				
				//Console.WriteLine ("Logging Mutex {0} is waiting for lock on thread {1}", this.Name, Thread.CurrentThread.Name);
				Monitor.Enter (this.LockObject);
				//Console.WriteLine ("Logging Mutex {0} is got lock on thread {1} after {2}ms", this.Name, Thread.CurrentThread.Name, stopWatch.ElapsedMilliseconds);
				stopWatch.Restart ();
			}

			lock (this.stats) this.stats [Thread.CurrentThread].LockCount++;
			try {
				criticalSection.Invoke ();
			} catch (Exception ex) {
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Application, ex, string.Format("Logging Mutex {0} caught an exception during critical section", this.Name));
			}
			Monitor.Exit (this.LockObject);
			lock (this.stats) 
				this.stats [Thread.CurrentThread].totalWaitMS = this.stats [Thread.CurrentThread].totalWaitMS + stopWatch.ElapsedMilliseconds;
			
			//Console.WriteLine ("Logging Mutex {0} is released lock on thread {1} after {2}ms", this.Name, Thread.CurrentThread.Name, stopWatch.ElapsedMilliseconds);
		}

		protected void DumpStats()
		{
			lock (Console.Out) {
				lock (this.stats) {
					foreach (var stat in this.stats) {
						Console.Write ("Mutex {0} ", this.Name.PadRight(20));
						Console.Write ("Thread {0} ", (stat.Key.Name ?? stat.Key.ManagedThreadId.ToString()).PadRight(20));
						Console.Write ("Locked {0,3} times, ", stat.Value.LockCount);
						Console.Write ("Waited {0:3} times, ", stat.Value.WaitCount);
						Console.Write ("for a total of {0:5}ms", stat.Value.totalWaitMS);
						Console.WriteLine ();
					}
				}
			}
		}

		public static void DumpAllStats()
		{
			lock (allMutexes) {
				foreach (var mutex in allMutexes)
					mutex.DumpStats ();
			}
		}
	}
}

