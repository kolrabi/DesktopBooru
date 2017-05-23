using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Tags
{
	public class Add : DatabaseQuery
	{
		private Add (string tagString) : base(
			"INSERT OR IGNORE INTO " + TagsTableName + " " +
			"                      (tag) " +
			"               VALUES (@tag)" +
			";"
		)
		{
			this.AddParameter (DbType.String, "tag", tagString);
			this.Prepare ();
		}

		public static int Execute(string tag)
		{
			object id = new Add (tag).ExecuteScalar ();
			if (id == null)
				return -1;

			return (int)(long)id;
		}
	}
}

