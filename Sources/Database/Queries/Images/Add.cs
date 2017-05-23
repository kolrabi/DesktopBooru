using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Images
{
	public class Add : DatabaseQuery
	{
		private Add (byte[] md5blob, string typeString, long added) : base(
			"INSERT OR IGNORE INTO " + ImagesTableName +" " +
			"                      (md5sum,  type, added) " +
			"               VALUES (@md5sum, @type, @added) " +
			";"
		)
		{
			this.AddParameter (DbType.Object, "md5sum", md5blob);
			this.AddParameter (DbType.StringFixedLength, "type", typeString);
			this.AddParameter (DbType.Int64, "added", added);
			this.Prepare ();
		}

		public static void Execute(string md5, BooruImageType type)
		{
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			string typeString = "I";
			switch (type) {
			case BooruImageType.Animation:
				typeString = "A";
				break;
			case BooruImageType.Comix:
				typeString = "C";
				break;
			case BooruImageType.Video:
				typeString = "V";
				break;
			case BooruImageType.Image:
				typeString = "I";
				break;
			}

			new Add(md5blob, typeString, DateTime.Now.Ticks).ExecuteNonQuery();
		}
	}
}

