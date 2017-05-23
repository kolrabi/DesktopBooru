using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Images
{
	public class UpdateUpdated : DatabaseQuery
	{
		private UpdateUpdated () : base(
			"UPDATE " + ImagesTableName +" " +
			"   SET updated = @updated " +
			" WHERE md5sum = @md5sum "
		)
		{
			this.AddParameter (DbType.Int64, "updated");
			this.AddParameter (DbType.Object, "md5sum");
			this.Prepare ();
		}

		public static void Execute(string md5)
		{
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			new UpdateUpdated().ExecuteNonQuery (DateTime.Now.Ticks, md5blob);
		}
	}
}

