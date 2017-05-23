using System;
using System.Data.Common;
using System.Collections.Generic;

namespace Booru
{
	public class DatabaseReader : IDisposable
	{
		private readonly DbDataReader reader;
		private readonly IDictionary<string, int> readerColumns;

		public DatabaseReader(DbDataReader reader)
		{
			this.reader = reader;			
			this.readerColumns = new Dictionary<string,int>();

			for (int i = 0; i < this.reader.FieldCount; i++)
				this.readerColumns [this.reader.GetName (i)] = i;
		}

		public object this[string name]
		{
			get 
			{
				if (!this.readerColumns.ContainsKey (name))
					return null;

				object val = this.reader.GetValue (this.readerColumns [name]);
				//Console.WriteLine(name+": "+val.GetType().Name);
				return val;
			}
		}

		public bool Read()
		{
			return this.reader.Read ();
		}

		public void Close()
		{
			this.reader.Close();
		}

		public void Dispose()
		{
			this.reader.Close ();
		}
	}
}

