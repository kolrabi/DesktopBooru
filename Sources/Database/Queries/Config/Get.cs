using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Config
{
	public class Get : DatabaseQuery
	{
		private Get (string key) : base(
			"SELECT value "+
			"  FROM " + ConfigTableName + " " +
			" WHERE name = @name"
		)
		{
			this.AddParameter (DbType.String, "name", key);
			this.Prepare ();
		}

		public static string Execute(string key)
		{
			return (string) new Get(key).ExecuteScalar ();
		}
	}
}

