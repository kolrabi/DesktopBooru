using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Images
{
	public class UpdateAdded : DatabaseQuery
	{
		private UpdateAdded () : base(
			"UPDATE " + ImagesTableName +" " +
			"   SET added = @added " +
			" WHERE md5sum = @md5sum "
		)
		{
			this.AddParameter (DbType.Int64, "added");
			this.AddParameter (DbType.Object, "md5sum");
			this.Prepare ();
		}

		public static void Execute(string md5, DateTime time)
		{
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			new UpdateAdded().ExecuteNonQuery (time.Ticks, md5blob);
		}
	}
}

