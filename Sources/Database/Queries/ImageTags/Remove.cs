using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.ImageTags
{
	public class Remove : DatabaseQuery
	{
		private Remove (long tagId, byte[] md5blob) : base(
			"DELETE FROM " + ImageTagsTableName + " " +
			"      WHERE md5sum = @md5sum "+
			"        AND tagid  = @tagid " +
			";"
		)
		{
			this.AddParameter (DbType.Int64, "tagid", tagId);
			this.AddParameter (DbType.Object, "md5sum", md5blob);
		}

		public static void Execute(long tagId, string md5)
		{
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			new Remove(tagId, md5blob).ExecuteNonQuery();
		}
	}
}

