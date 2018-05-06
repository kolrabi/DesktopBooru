using System;
using System.IO;
using System.Collections.Generic;

namespace Booru
{
	public class ZipArchive : FileArchive, IDisposable
	{
		class Entry : FileArchiveEntry
		{
			public readonly System.IO.Compression.ZipArchiveEntry ZipEntry;

			public Entry(System.IO.Compression.ZipArchiveEntry zipEntry) : base(zipEntry.FullName)
			{
				this.ZipEntry = zipEntry;
			}

			public override Stream Open()
			{
				return this.ZipEntry.Open ();
			}
		}

		System.IO.Compression.ZipArchive zipFile;
		List<Entry> zipEntries;

		public ZipArchive (FileStream fileStream)
		{
			this.zipEntries = new List<Entry> ();
			this.zipFile = new System.IO.Compression.ZipArchive(fileStream);

			foreach(var entry in this.zipFile.Entries) {
				this.zipEntries.Add(new Entry(entry));
			}

			this.zipEntries.Sort((a,b) => {
				return a.Name.CompareNatural(b.Name);
			});
		}

		public override int GetEntryCount ()
		{
			return this.zipEntries.Count;
		}

		public override System.Collections.Generic.IEnumerable<FileArchiveEntry> GetEntries ()
		{
			return this.zipEntries;
		}

		public override FileArchiveEntry GetEntry (int index)
		{
			if (index < 0)
				return null;

			if (index >= this.zipEntries.Count)
				return null;
			
			return this.zipEntries [index];
		}

		public override void Dispose()
		{
			if (this.zipFile != null) {
				this.zipFile.Dispose ();
				this.zipFile = null;
				this.zipEntries.Clear ();
			}
		}
	}
}

