using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Images
{
	public class SetType : DatabaseQuery
	{
		private SetType (byte[] md5blob, string typeString) : base(
			"UPDATE " + ImagesTableName +" " +
			"   SET type = @type " +
			" WHERE md5sum = @md5sum "
		)
		{
			this.AddParameter (DbType.Object, "md5sum", md5blob);
			this.AddParameter (DbType.StringFixedLength, "type", typeString);
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

			new SetType(md5blob, typeString).ExecuteNonQuery ();
		}
	}
}

