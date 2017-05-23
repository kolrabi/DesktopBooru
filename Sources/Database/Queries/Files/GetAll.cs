using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace Booru.Queries.Files
{
	public class GetAll : DatabaseQuery
	{
		private GetAll () : base(
			"  SELECT path " +
			"    FROM files "
		)
		{
			this.Prepare ();
		}

		public static List<string> Execute()
		{
			List<string> paths = new List<string> ();

			using (DatabaseReader reader = new DatabaseReader (new GetAll().ExecuteReader ())) 
			{
				while (reader.Read ()) {
					paths.Add ((string)reader ["path"]);
				}
			}

			return paths;
		}
	}
}

