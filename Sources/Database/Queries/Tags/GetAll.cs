using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Tags
{
	public class GetAll : DatabaseQuery
	{
		private GetAll () : base(
			"  SELECT tags.id AS id, " +
			"         tags.tag AS tag, " +
			"         SUM(images.elo) AS score, " +
			"         COUNT(it.md5sum) as usage, " +
			"         tags.type AS type " +
			"    FROM " + TagsTableName + " AS tags " +
			"    JOIN " + ImageTagsTableName + " AS it " +
			"      ON it.tagid = tags.id " +
			"    JOIN images " +
			"      ON images.md5sum = it.md5sum " +
			"GROUP BY tags.id " +
			"   UNION " +
			"  SELECT tags.id AS id, " +
			"         tags.tag AS tag, " +
			"         0.0 AS score, " +
			"         0 AS usage," +
			"         tags.type AS type " +
			"    FROM " + TagsTableName + " AS tags " +
			"   WHERE tags.id NOT IN ( SELECT DISTINCT tagid FROM " + ImageTagsTableName + " ) "
		)
		{
			this.Prepare ();
		}

		public static DatabaseCursor<TagDetails> Execute()
		{
			return new DatabaseCursor<TagDetails> (new DatabaseReader (new GetAll().ExecuteReader ()));
		}
	}
}

