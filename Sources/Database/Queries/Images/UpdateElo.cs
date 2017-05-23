using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Images
{
	public class UpdateElo : DatabaseQuery
	{
		private UpdateElo () : base(
			"UPDATE " + ImagesTableName +" " +
			"   SET elo   = elo   + @offset, " +
			"       votes = votes + 1, " +
			"       wins  = wins  + @wins," +
			"       losses = losses + @losses " +
			" WHERE md5sum = @md5sum " 
		)
		{
			this.AddParameter (DbType.Single, "offset");
			this.AddParameter (DbType.Object, "md5sum");
			this.AddParameter (DbType.Int32, "wins");
			this.AddParameter (DbType.Int32, "losses");
			this.Prepare ();
		}

		public static void Execute(string md5, float offset)
		{
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			new UpdateElo().ExecuteNonQuery (offset, md5blob, offset > 0 ? 1 : 0, offset < 0 ? 1 : 0);
		}
	}
}

