using System;
using System.Data;
using System.Data.Common;

namespace Booru.Queries.Files
{
	public class Remove : DatabaseQuery
	{
		private Remove () : base(
					"DELETE FROM " + FilesTableName + " " +
					"WHERE path = @path"
				)
		{
				this.AddParameter (DbType.String, "path");
		}

		public static void Execute(string path)
		{
			new Remove().ExecuteNonQuery(path);
		}
	}
}

