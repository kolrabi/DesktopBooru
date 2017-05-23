using System;

namespace Booru
{
	public class DatabaseCursor<TResult> : IDisposable where TResult : DatabaseReadable, new()
	{
		public TResult Value {
			get { 
				TResult result = new TResult();
				result.InitFromReader (this.reader);
				return result;
			}
		}

		private readonly DatabaseReader reader;

		internal DatabaseCursor (DatabaseReader reader)
		{
			this.reader = reader;
		}

		public bool Read()
		{
			return this.reader.Read ();
		}

		public void Close()
		{
			this.reader.Close ();
		}

		public void Dispose()
		{
			this.Close ();
			this.reader.Dispose ();
		}
	}
}

