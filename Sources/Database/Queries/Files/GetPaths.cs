using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace Booru.Queries.Files
{
	public class GetForImage : DatabaseQuery
	{
		private GetForImage (byte[] md5blob) : base(
			"  SELECT files.path " +
			"    FROM files " +
			"   WHERE files.md5sum = @md5sum "
		)
		{
			this.AddParameter (DbType.Object, "md5sum", md5blob);
			this.Prepare ();
		}

		public static List<string> Execute(string md5)
		{
			List<string> paths = new List<string> ();
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);

			using (DatabaseReader reader = new DatabaseReader (new GetForImage(md5blob).ExecuteReader ())) 
			{
				while (reader.Read ()) {
					paths.Add ((string)reader ["path"]);
				}
			}

			return paths;
		}
	}
}

