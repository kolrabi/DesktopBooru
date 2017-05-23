using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Images
{
	public class Remove : DatabaseQuery
	{
		private Remove (byte[] md5blob) : base(
			"DELETE FROM " + FilesTableName + " "+
			"WHERE md5sum = @md5sum;" +
			"DELETE FROM " + ImageTagsTableName + " "+
			"WHERE md5sum = @md5sum;" +
			"DELETE FROM " + ImagesTableName + " " +
			"WHERE md5sum = @md5sum;" 
		)
		{
			this.AddParameter (DbType.Object, "md5sum", md5blob);
			this.Prepare ();
		}

		public static void Execute(string md5)
		{
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			new Remove (md5blob).ExecuteNonQuery ();
		}
	}
}

