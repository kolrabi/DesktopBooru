using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.ImageTags
{
	public class Add : DatabaseQuery
	{
		private Add (long tagId, byte[] md5blob) : base(
			"INSERT OR IGNORE INTO " + ImageTagsTableName + " " +
			"                      (md5sum,  tagid) " +
			"               VALUES (@md5sum, @tagid) "
		)
		{
			this.AddParameter (DbType.Int64, "tagid", tagId);
			this.AddParameter (DbType.Object, "md5sum", md5blob);
			this.Prepare ();
		}

		public static void Execute(long tagId, string md5)
		{
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			new Add (tagId, md5blob).ExecuteNonQuery ();
		}
	}
}

