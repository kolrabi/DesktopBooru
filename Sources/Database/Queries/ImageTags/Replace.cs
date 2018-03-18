using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace Booru.Queries.ImageTags
{
	public class Replace : DatabaseQuery
	{
		private Replace (long oldTagId, long newTagId) : base(
			// 1. move score to new tag
			"UPDATE " + TagsTableName + " " +
			"   SET score = COALESCE(tags.score, 0.0) + " +
			"               (SELECT COALESCE(oldtags.score, 0.0) " +
			"                  FROM " + TagsTableName + " AS oldtags " +
			"                 WHERE id = @oldTagID" +
			"               ) " +
			" WHERE id = @newTagID; " +

			// 2. clear old tag score
			"UPDATE " + TagsTableName + " " +
			"   SET score = 0.0 " +
			" WHERE id = @oldTagID; " +

			// 3. add new tag to images with old tag
			"INSERT OR IGNORE INTO " + ImageTagsTableName + " " +
			"(md5sum, tagid) " +
			"SELECT md5sum, @newTagID FROM " + ImageTagsTableName + " WHERE tagid = @oldTagID " +
			";" +

			// 4. remove old assignments
			"DELETE FROM " + ImageTagsTableName + " " +
			" WHERE tagid = @oldTagID; "

		)
		{
			this.AddParameter (DbType.Int64, "oldTagID", oldTagId);
			this.AddParameter (DbType.Int64, "newTagID", newTagId);
			this.Prepare ();
		}

		public static void Execute(long oldTagId, IList<long> newTagIds)
		{
			lock (BooruApp.BooruApplication.Database.Connection) {
				var transaction = BooruApp.BooruApplication.Database.Connection.BeginTransaction ();

				foreach (long newTagId in newTagIds)
					new Replace (oldTagId, newTagId).ExecuteNonQuery ();
	
				transaction.Commit ();
			}
		}
	}
}

