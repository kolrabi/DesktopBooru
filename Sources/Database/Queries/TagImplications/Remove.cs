using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.TagImplications
{
	public class Remove : DatabaseQuery
	{
		private Remove (long tagId, long impliesId) : base(
			"DELETE FROM " + ImplicationsTableName + " " +
			"      WHERE tagid = @tagid " +
			"        AND implies = @implies "
		)
		{
			this.AddParameter (DbType.Int64, "tagid", tagId);
			this.AddParameter (DbType.Int64, "implies", impliesId);
			this.Prepare ();
		}

		public static void Execute(long tagId, long impliesId)
		{
			new Remove (tagId, Math.Abs(impliesId)).ExecuteNonQuery();
		}
	}
}

