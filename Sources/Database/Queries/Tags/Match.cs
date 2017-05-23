using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace Booru.Queries.Tags
{
	public class Match : DatabaseQuery
	{
		private Match (string pattern) : base(
			"SELECT id " +
			"  FROM " + TagsTableName + " " +
			" WHERE tag LIKE @pattern"
		)
		{
			this.AddParameter (DbType.String, "pattern", pattern);
			this.Prepare ();
		}

		public static List<int> Execute(string tag)
		{
			List<int> ids = new List<int> ();

			string pattern = tag.Replace ('*', '%');

			using (DatabaseReader reader = new DatabaseReader (new Match(pattern).ExecuteReader ())) 
			{
				while (reader.Read ()) {
					ids.Add ((int)(long)reader ["id"]);
				}
			}

			return ids;
		}
	}
}

