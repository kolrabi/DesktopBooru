using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace Booru.Queries.Tags
{
	public class UpdateScore : DatabaseQuery
	{
		private UpdateScore (long tagId, double offset) : base(
			"  UPDATE " + TagsTableName + " " +
			"     SET score = COALESCE( score + @offset, @offset ) " +
			"   WHERE id = @tagid "
		)
		{
			this.AddParameter (DbType.Int64, "tagid", tagId);
			this.AddParameter (DbType.Double, "offset", offset);
			this.Prepare ();
		}

		private UpdateScore (List<long> tagIDs, double offset) : base (
			"  UPDATE " + TagsTableName + " " +
			"     SET score = COALESCE( score + @offset, @offset ) " +
			"   WHERE id IN ( "+string.Join(",", tagIDs)+" ) "
		)
		{
			this.AddParameter (DbType.Double, "offset", offset);
			this.Prepare ();
		}

		public static void Execute(long tagId, double offset)
		{
			new UpdateScore (tagId, offset).ExecuteNonQuery ();
		}

		public static void Execute(List<long> tagIds, double offset)
		{
			new UpdateScore (tagIds, offset).ExecuteNonQuery ();
		}
	}
}

