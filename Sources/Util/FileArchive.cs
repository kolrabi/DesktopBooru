using System;
using System.Collections.Generic;
using System.IO;

namespace Booru
{
	public class FileArchive : IDisposable
	{
		SharpCompress.Archives.IArchive archive;
		List<Entry> entries;

		public class Entry
		{
			public readonly SharpCompress.Archives.IArchiveEntry archiveEntry;
			public string Name { get { return this.archiveEntry.Key; } }

			public Entry(SharpCompress.Archives.IArchiveEntry archiveEntry)
			{
				this.archiveEntry = archiveEntry;
			}

			public Stream Open()
			{
				return this.archiveEntry.OpenEntryStream ();
			}
		}

		public static FileArchive Open(string path)
		{
			SharpCompress.Archives.IArchive archive = null;
			try {
				if (SharpCompress.Archives.Zip.ZipArchive.IsZipFile(path))
					archive = SharpCompress.Archives.Zip.ZipArchive.Open (path);
				else if (SharpCompress.Archives.Rar.RarArchive.IsRarFile(path))
					archive = SharpCompress.Archives.Rar.RarArchive.Open (path);
				else if (SharpCompress.Archives.SevenZip.SevenZipArchive.IsSevenZipFile(path))
					archive = SharpCompress.Archives.SevenZip.SevenZipArchive.Open (path);
				else if (SharpCompress.Archives.GZip.GZipArchive.IsGZipFile(path))
					archive = SharpCompress.Archives.GZip.GZipArchive.Open (path);
				else if (SharpCompress.Archives.Tar.TarArchive.IsTarFile(path))
					archive = SharpCompress.Archives.Tar.TarArchive.Open (path);
			} catch (Exception ex) {
				BooruApp.BooruApplication.Log.Log (BooruLog.Category.Files, ex, "Caught exception while opening archive " + path);
			}

			if (archive == null)
				return null;

			return new FileArchive (archive);
		}
			
		protected FileArchive (SharpCompress.Archives.IArchive archive)
		{
			this.entries = new List<Entry> ();
			this.archive = archive;

			foreach(var entry in this.archive.Entries) {
				if (entry.IsComplete && !entry.IsDirectory)
					this.entries.Add(new Entry(entry));
			}

			this.entries.Sort((a,b) => {
				return a.Name.CompareNatural(b.Name);
			});
		}

		public int GetEntryCount ()
		{
			return this.entries.Count;
		}

		public System.Collections.Generic.IEnumerable<Entry> GetEntries ()
		{
			return this.entries;
		}

		public Entry GetEntry (int index)
		{
			if (index < 0)
				return null;

			if (index >= this.entries.Count)
				return null;

			return this.entries [index];
		}

		public void Dispose()
		{
			if (this.archive != null) {
				this.archive.Dispose ();
				this.archive = null;
				this.entries.Clear ();
			}
		}
	}
}

