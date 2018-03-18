using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Files
{
	public class GetMD5 : DatabaseQuery
	{
		private GetMD5 () : base(
			"  SELECT md5sum " +
			"    FROM " + FilesTableName + " AS files " +
			"   WHERE files.path = @path "
		)
		{
			this.AddParameter (DbType.String, "path");
			this.Prepare ();
		}

		public static byte[] Execute(string path)
		{
			return (byte[])new GetMD5().ExecuteScalar (path);
		}
	}
}

