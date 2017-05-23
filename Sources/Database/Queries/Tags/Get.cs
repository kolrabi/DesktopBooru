using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Tags
{
	public class Get : DatabaseQuery
	{
		private Get (long tagId) : base(
			"    SELECT tags.id AS id, tags.tag AS tag, SUM(images.elo) AS score, COUNT(it.md5sum) as usage, tags.type AS type " +
			"    FROM " + TagsTableName + " AS tags " +
			"    JOIN " + ImageTagsTableName + " AS it      ON it.tagid      = tags.id " +
			"    JOIN " + ImagesTableName +    " AS images  ON images.md5sum = it.md5sum " +
			"   WHERE tags.id = @tagid " +
			"GROUP BY tags.id"
		)
		{
			this.AddParameter (DbType.Int64, "tagid", tagId);
			this.Prepare ();
		}

		public static DatabaseReader Execute(long tagid)
		{
			return  new DatabaseReader (new Get (tagid).ExecuteReader ());
		}
	}
}

