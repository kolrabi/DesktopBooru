using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace Booru.Queries.Tags
{
	public class MatchType : DatabaseQuery
	{
		private MatchType (int tagType) : base(
			"SELECT id " +
			"  FROM " + TagsTableName + " " +
			" WHERE type = @tagType"
		)
		{
			this.AddParameter (DbType.Int32, "tagType", tagType);
			this.Prepare ();
		}

		public static List<int> Execute(TagDetails.TagType tagType)
		{
			List<int> ids = new List<int> ();

			using (DatabaseReader reader = new DatabaseReader (new MatchType((int)tagType).ExecuteReader ())) 
			{
				while (reader.Read ()) {
					ids.Add ((int)(long)reader ["id"]);
				}
			}

			return ids;
		}
	}
}

