using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace Booru.Queries.TagImplications
{
	public class Get : DatabaseQuery
	{
		private Get () : base(
			"SELECT tagid, implies, isneg FROM " + ImplicationsTableName
		)
		{
			this.Prepare ();
		}

		public static IDictionary<long, IList<long>> Execute()
		{
			IDictionary<long, IList<long>> result = new Dictionary<long, IList<long>> ();
			using (var reader = new DatabaseReader (new Get().ExecuteReader ())) {
				while (reader.Read ()) {
					var details = new TagImplicationDetails ();
					details.InitFromReader (reader);

					if (!result.ContainsKey (details.TagID))
						result [details.TagID] = new List<long> ();

					result [details.TagID].Add (details.IsNegative ? -details.ImpliedTagID : details.ImpliedTagID);
				}
			}
			return result;
		}
	}
}

