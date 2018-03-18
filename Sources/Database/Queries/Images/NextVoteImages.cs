using System;
using System.Data.Common;
using System.Data;

namespace Booru.Queries.Images
{
	public class NextVoteImages : DatabaseQuery
	{
		private NextVoteImages (string typeString, bool asc) : base(
			"  SELECT files.md5sum, " +
			"         files.path, " +
			"         images.elo," +
			"         images.votes, " +
			"         images.type, " +
			"         images.width, " +
			"         images.height," +
			"         images.wins, " +
			"         images.losses " +
			"    FROM files " +
			"    JOIN images " +
			"      ON images.md5sum = files.md5sum " +
			"     WHERE (SELECT COUNT(*) FROM image_tags WHERE image_tags.md5sum = files.md5sum and tagid = (SELECT id FROM tags WHERE tag = 'deleteme') ) = 0 " + 
			"       AND (@type IS NULL OR @type = images.type ) " +
			"GROUP BY images.md5sum " +
			"ORDER BY " +
			"         (images.wins+images.losses) "+(asc?"":"DESC")+", RANDOM() LIMIT 20" 
		)
		{
			this.AddParameter (DbType.AnsiString, "type", typeString);
			this.Prepare ();
		}

		public static DatabaseCursor<ImageDetails> Execute(BooruImageType type, bool asc)
		{
			string t = null;
			if (type != BooruImageType.Unknown)
				t = type.ToString ().Substring(0,1);

			var dbReader = new NextVoteImages (t, true).ExecuteReader ();

			return new DatabaseCursor<ImageDetails> (new DatabaseReader (dbReader));
		}
	}
}

