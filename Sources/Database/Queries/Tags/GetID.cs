using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Tags
{
	public class GetID : DatabaseQuery
	{
		private GetID (string tagString) : base(
			"SELECT id FROM " + TagsTableName + " " +
			" WHERE tag = @tag "
		)
		{
			this.AddParameter (DbType.String, "tag", tagString);
			this.Prepare ();
		}

		public static long Execute(string tag)
		{
			object id = new GetID(tag).ExecuteScalar();

			if (id == null)
				return -1;

			return (long)id;
		}
	}
}

