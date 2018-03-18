using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;

namespace Booru.Queries.Images
{
	public class FindImages : DatabaseQuery
	{
		private FindImages (string tagString) : base(
			""
		)
		{
			this.command.CommandText = this.BuildQueryString (tagString);
			this.Prepare ();
		}

		public static DatabaseCursor<ImageDetails> Execute(string tagString)
		{
			return new DatabaseCursor<ImageDetails> (new DatabaseReader (new FindImages(tagString).ExecuteReader ()));
		}

		private class QueryParams
		{
			public List<string> PathGlobs = new List<string> ();
			public List<string> OrderBy = new List<string>();
			public int Limit = -1;
		}

		private string GetSQLConditionForTag(string tag, QueryParams queryParams)
		{
			if (tag.StartsWith ("-")) 
				return "NOT " + this.GetSQLConditionForTag (tag.Substring (1), queryParams);

			if (tag.StartsWith ("#")) {
				string lowerTag = tag.ToLower ();
				int argPos = lowerTag.IndexOf (':');
				int argInt = 0;
				char argOp = ' ';
				string argString = "";
				string baseTag = lowerTag;

				if (argPos == -1)
					argPos = lowerTag.IndexOf ('=');
				if (argPos == -1)
					argPos = lowerTag.IndexOf ('>');
				if (argPos == -1)
					argPos = lowerTag.IndexOf ('<');

				if (argPos != -1) {
					argString = lowerTag.Substring (argPos + 1);
					argOp = lowerTag [argPos];
					baseTag = tag.Substring (0, argPos);
					int.TryParse (argString, out argInt);
					if (argOp == ':')
						argOp = '=';
				}

				if (baseTag == "#type") {
					switch (argString.ToLower()) {
					case "a":
						return " ( images.type == 'A' ) ";
					case "i":
						return " ( images.type == 'I' ) ";
					case "c":
						return " ( images.type == 'C' ) ";
					case "v":
						return " ( images.type == 'V' ) ";
					}
				}

				if (baseTag == "#score")	return " ( elo "      + argOp + " " + argInt + " ) ";
				if (baseTag == "#votes")	return " ( votes "    + argOp + " " + argInt + " ) ";
				if (baseTag == "#tags")		return " ( tagCount " + argOp + " " + argInt + " ) ";
				if (baseTag == "#wins")		return " ( wins "     + argOp + " " + argInt + " ) ";
				if (baseTag == "#losses")	return " ( losses "     + argOp + " " + argInt + " ) ";
				if (baseTag == "#winratio")	return " ( (wins-losses) "     + argOp + " " + argInt + " ) ";

				if (baseTag == "#path") {
					string pathGlob = argString.Replace ('*', '%').Replace ('?', '_');
					queryParams.PathGlobs.Add ("%"+pathGlob+"%");
					return " ( LOWER(files.path) LIKE LOWER(@pathGlob" + (queryParams.PathGlobs.Count-1) + ") ) ";
				}

				if (baseTag == "#md5") {
					queryParams.PathGlobs.Add (argString);
					return " ( LOWER(HEX(files.md5sum)) = @pathGlob" + (queryParams.PathGlobs.Count-1) + " ) ";
				}

				if (baseTag == "#square") {
					return " ( images.width = images.height ) ";
				}

				if (baseTag == "#landscape") {
					return " ( images.width > images.height ) ";
				}

				if (baseTag == "#portrait") {
					return " ( images.width < images.height ) ";
				}

				if (baseTag == "#unsized") {
					return " (images.width IS NULL OR images.width = 0 OR images.height IS NULL OR images.height = 0) ";
				}

				// -------------------------------------------------------
				// 

				if (baseTag == "#sort" && !string.IsNullOrEmpty (argString)) {
					string sortString = argString;
					string orderString = " DESC ";
					if (sortString.StartsWith ("-")) {
						orderString = " ASC ";
						sortString = sortString.Substring (1);
					}

					switch (sortString) {
					case "tags":
						queryParams.OrderBy.Add (" tagCount " + orderString);
						return null;

					case "votes":
						queryParams.OrderBy.Add (" votes " + orderString);
						return null;

					case "score":
						queryParams.OrderBy.Add (" elo " + orderString);
						return null;

					case "wins":
						queryParams.OrderBy.Add (" wins " + orderString);
						return null;

					case "losses":
						queryParams.OrderBy.Add (" losses " + orderString);
						return null;

					case "winratio":
						queryParams.OrderBy.Add (" (wins-losses) " + orderString);
						return null;

					case "updated":
						queryParams.OrderBy.Add (" updated " + orderString);
						return null;

					case "random":
						queryParams.OrderBy.Add(" RANDOM() ");
						return null;

					case "added":
						queryParams.OrderBy.Add(" added " + orderString);
						return null;

					default:
						BooruApp.BooruApplication.Database.Logger.Log(BooruLog.Severity.Error, "Unknown sorting: " + argString);
						return null;
					}
				}


				if (baseTag == "#limit") {
					queryParams.Limit = argInt;
					return null;
				}
			} else {
				var tagIds = BooruApp.BooruApplication.Database.MatchTag (tag);
				var tagIdString = string.Join (", ", tagIds);
				return string.Format (
					"((SELECT COUNT(*) " +
					" FROM image_tags " +
					"WHERE md5sum = files.md5sum " +
					"  AND image_tags.tagid IN ({0}) ) > 0 ) ", 
					tagIdString
				);
			}

			return null;
		}

		private string BuildQueryString(string tagString)
		{

			var builder = new System.Text.StringBuilder ();
			builder.Append(
				"SELECT   DISTINCT files.md5sum, " +
				"         files.path, " +
				"         images.elo," +
				"         images.votes, " +
				"         images.type AS type, " +
				"         (SELECT COUNT(*) FROM image_tags WHERE md5sum = files.md5sum) AS tagCount, " +
				"         images.width AS width, "+
				"         images.height AS height," +
				"         images.wins AS wins," +
				"         images.losses AS losses "+
				"    FROM files " +
				"    JOIN images " +
				"      ON images.md5sum = files.md5sum "
			);

			var queryParams = new QueryParams ();

			var tags = tagString.Split (" ".ToCharArray());
			var conditionStrings = new List<string> ();
			bool includeDeleteme = false;
			foreach (var tag in tags) {
				if (tag == "deleteme") {
					if (!includeDeleteme) {
						includeDeleteme = true;
						continue;
					}
				}
				var cond = GetSQLConditionForTag (tag, queryParams);
				if (!string.IsNullOrEmpty(cond)) 
					conditionStrings.Add(cond);
			}
			if (!includeDeleteme)
				conditionStrings.Add(this.GetSQLConditionForTag("-deleteme", queryParams));

			if (conditionStrings.Count > 0) {
				builder.Append(" WHERE ");
				builder.Append(string.Join (" AND ", conditionStrings));
			}

			for (int i=0; i<queryParams.PathGlobs.Count; i++) 
				this.AddParameter (DbType.String, "pathGlob" + i, queryParams.PathGlobs [i]);

			if (queryParams.OrderBy.Count > 0)
				builder.Append (" ORDER BY " + string.Join (",", queryParams.OrderBy));

			if (queryParams.Limit > 0)
				builder.Append (" LIMIT " + queryParams.Limit);

			return builder.ToString ();
		}
	}
}

