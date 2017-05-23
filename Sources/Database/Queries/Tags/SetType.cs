using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Tags
{
	public class SetType : DatabaseQuery
	{
		private SetType (long tagId, int tagType) : base(
			"  UPDATE tags "+
			"     SET type = @type " +
			"   WHERE id = @id "
		)
		{
			this.AddParameter (DbType.Int64, "id", tagId);
			this.AddParameter (DbType.Int32, "type", tagType);
			this.Prepare ();
		}

		public static void Execute(long tagId, TagDetails.TagType type)
		{
			new SetType (tagId, (int)type).ExecuteNonQuery ();
		}
	}
}

