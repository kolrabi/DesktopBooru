using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace Booru.Queries.ImageTags
{
	public class Get : DatabaseQuery
	{
		private Get (byte[] md5blob) : base(
			"  SELECT tag " +
			"    FROM " + ImageTagsTableName + " AS image_tags " +
			"    JOIN " + TagsTableName + " AS tags " +
			"      ON " + ImageTagsTableName + ".tagid = tags.id " +
			"   WHERE " + ImageTagsTableName + ".md5sum = @md5sum " +
			"ORDER BY tag " +
			";"
		)
		{
			this.AddParameter (System.Data.DbType.Object, "@md5sum", md5blob);
			this.Prepare ();
		}

		public static List<string> Execute(string md5)
		{	
			byte[] md5blob = MD5Helper.MD5ToBlob (md5);
			var tags = new List<string> ();

			using (var reader = new DatabaseReader (new Get (md5blob).ExecuteReader ())) {
				while (reader.Read ()) {
					tags.Add ((string)reader ["tag"]);
				}
			}

			return tags;
		}
	}
}

