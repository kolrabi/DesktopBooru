using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Config
{
	public class Set : DatabaseQuery
	{
		private Set (string key, string value) : base(
			"INSERT OR REPLACE INTO " + ConfigTableName + " " +
			"                       (name,  value) " +
			"                VALUES (@name, @value)"
		)
		{
			this.AddParameter (DbType.String, "name", key);
			this.AddParameter (DbType.String, "value", value);
			this.Prepare ();
		}

		public static void Execute(string key, string value)
		{
			new Set(key, value).ExecuteNonQuery ();
		}
	}
}

