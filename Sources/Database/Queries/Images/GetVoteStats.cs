using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace Booru.Queries.Images
{
	public class GetVoteStats : DatabaseQuery
	{
		private GetVoteStats () : base(
			"  SELECT COUNT(wins + losses) AS vote_count, " +
			"         wins + losses AS votes " +
			"    FROM images " +
			"GROUP BY votes " +
			"ORDER BY votes "
		)
		{
			this.Prepare ();
		}

		// votes -> vote_count
		public static IList<KeyValuePair<int, int>> Execute()
		{
			IList<KeyValuePair<int, int>> result = new List<KeyValuePair<int, int>> ();

			using (var dbReader = new GetVoteStats ().ExecuteReader ()) {
				while (dbReader.Read ()) {
					int voteCount = dbReader.GetInt32 (0);
					int votes = dbReader.GetInt32 (1);
					result.Add (new KeyValuePair<int, int> (votes, voteCount));
				}
			}

			return result;
		}
	}
}

