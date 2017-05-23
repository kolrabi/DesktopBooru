using System;
using System.Data.Common;
using System.Data;
using System.Threading.Tasks;

namespace Booru.Queries.Images
{
	public class SetSize : DatabaseQuery
	{
		private SetSize (byte[] md5blob, int width, int height) : base(
			"UPDATE " + ImagesTableName +" " +
			"   SET width = @width, height = @height " +
			" WHERE md5sum = @md5sum "
		)
		{
			this.AddParameter (DbType.Object, "md5sum", md5blob);
			this.AddParameter (DbType.Int32, "width", width);
			this.AddParameter (DbType.Int32, "height", height);
		}

		public static void Execute(string md5, Point2D size)
		{
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			new SetSize (md5blob, size.X, size.Y).ExecuteNonQuery ();
		}
	}
}

