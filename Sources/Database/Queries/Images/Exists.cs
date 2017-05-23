using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Images
{
	public class Exists : DatabaseQuery
	{
		private Exists (byte[] md5blob) : base(
			"SELECT COUNT(*) "+
			"  FROM " + ImagesTableName + " " +
			" WHERE md5sum = @md5sum"
		)
		{
			this.AddParameter (DbType.Object, "md5sum", md5blob);
			this.Prepare ();
		}

		public static bool Execute(string md5)
		{
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			return (long)new Exists(md5blob).ExecuteScalar () > 0;
		}
	}
}

