using System;
using System.Collections.Generic;

namespace Booru
{
	public abstract class FileArchive : IDisposable
	{
		public abstract class FileArchiveEntry
		{
			public readonly string Name;

			public FileArchiveEntry(string name)
			{
				this.Name = name;
			}

			public abstract System.IO.Stream Open();
		}

		public abstract IEnumerable<FileArchiveEntry> GetEntries();
		public abstract FileArchiveEntry GetEntry(int index);
		public abstract int GetEntryCount();
		public abstract void Dispose();
	}
}

