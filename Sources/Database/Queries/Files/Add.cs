using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Files
{
	public class Add : DatabaseQuery
	{
		private Add () : base(
			"INSERT OR IGNORE INTO " + FilesTableName + " " +
			"(" +
			"  path, " +
			"  md5sum " +
			") " +
			"VALUES " +
			"( " +
			"  @path, " +
			"  @md5sum " +
			");"
		)
		{
			this.AddParameter (DbType.Object, "md5sum");
			this.AddParameter (DbType.String, "path");
			this.Prepare ();
		}

		public static void Execute(string md5, string path)
		{
			try {
				byte[] md5blob = MD5Helper.MD5ToBlob (md5);
				new Add().ExecuteNonQuery(md5blob, path);
			} catch (Exception ex) {
				BooruApp.BooruApplication.Database.Logger.Log(BooruLog.Severity.Error, "Could not add " + path + ": " + ex.Message);
			}
		}
	}
}

