using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.TagImplications
{
	public class Add : DatabaseQuery
	{
		private Add (long tagId, long impliesId, bool isNeg) : base(
			"INSERT OR REPLACE INTO " + ImplicationsTableName + " " +
			"( " +
			"  tagid, " +
			"  implies," +
			"  isneg "+
			") " +
			"VALUES " +
			"( " +
			"  @tagid," +
			"  @implies," +
			"  @isneg " +
			");"
		)
		{
			this.AddParameter (DbType.Int64, "tagid", tagId);
			this.AddParameter (DbType.Int64, "implies", impliesId);
			this.AddParameter (DbType.Boolean, "isneg", isNeg);
			this.Prepare ();
		}

		public static void Execute(long tagId, long impliesId)
		{
			new Add (tagId, Math.Abs (impliesId), impliesId < 0).ExecuteNonQuery ();
		}
	}
}

