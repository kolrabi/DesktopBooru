using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Images
{
	public class Get : DatabaseQuery
	{
		private Get (byte[] md5blob) : base(
			"  SELECT files.md5sum, " +
			"         files.path, " +
			"         images.elo," +
			"         images.votes, " +
			"         images.type, " +
			"         images.width, " +
			"         images.height, " +
			"         images.added, " +
			"         images.updated," +
			"         images.wins," +
			"         images.losses " +
			"    FROM files " +
			"    JOIN images " +
			"      ON images.md5sum = files.md5sum " +
			"   WHERE files.md5sum = @md5sum "
		)
		{
			this.AddParameter (DbType.Object, "md5sum", md5blob);
			this.Prepare ();
		}

		public static DatabaseReader Execute(string md5)
		{
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			return new DatabaseReader (new Get(md5blob).ExecuteReader ());
		}
	}
}

