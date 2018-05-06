using System;
using System.Threading;
using System.Diagnostics;

namespace Booru
{
	public abstract class RefCountedDisposable : IDisposable
	{
		public int RefCount { get { return this.refCount; } }
		private int refCount;

		public bool IsDisposed { get { return this.RefCount == 0; } }

		public RefCountedDisposable ()
		{
			this.refCount = 1;
		}

		public void AddRef()
		{
			Debug.Assert (!this.IsDisposed);
			if (this.IsDisposed) {
				Booru.BooruApp.BooruApplication.Log.Log (Booru.BooruLog.Category.Application, BooruLog.Severity.Warning, "Tried to add reference to disposed object!");
				return;
			}

			Interlocked.Increment(ref this.refCount);
		}

		public bool Release()
		{
			Debug.Assert (!this.IsDisposed);

			if (this.IsDisposed) {
				Booru.BooruApp.BooruApplication.Log.Log (Booru.BooruLog.Category.Application, BooruLog.Severity.Warning, "Tried to release disposed object!");
				return false;
			}

			int newRefCount = Interlocked.Decrement (ref this.refCount);

			if (newRefCount == 0) {
				((IDisposable)this).Dispose ();
				return true;
			}
			return false;
		}

		public abstract void Dispose();
	}
}

