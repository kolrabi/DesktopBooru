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
				Console.WriteLine ("tried to add reference to disposed object!");
				return;
			}

			int newRefCount = Interlocked.Increment(ref this.refCount);

			var img = this as Image;
			if (img != null)
				Console.WriteLine ("{0} inc to {1}", img.Details.MD5, newRefCount); 
		}

		public bool Release()
		{
			Debug.Assert (!this.IsDisposed);

			if (this.IsDisposed) {
				Console.WriteLine ("tried to release a disposed object!");
				return false;
			}

			int newRefCount = Interlocked.Decrement (ref this.refCount);

			var img = this as Image;
			if (img != null)
				Console.WriteLine ("{0} dec to {1}", img.Details.MD5, newRefCount); 

			if (newRefCount == 0) {
				((IDisposable)this).Dispose ();
				return true;
			}
			return false;
		}

		public abstract void Dispose();
	}
}

