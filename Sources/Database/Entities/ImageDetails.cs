using System;

using Property = Booru.DatabaseReadable.PropertyAttribute;

namespace Booru
{
	public class ImageDetails : DatabaseReadable {
		[Property("md5sum")]
		private byte[] md5 = new byte[16];

		[Property]
		private string path = "";

		[Property]
		private double elo = 0.0;

		[Property]
		private long votes = 0;

		[Property]
		private long wins = 0;

		[Property]
		private long losses = 0;

		[Property("type")]
		private string typeString = "?";

		[Property]
		private long width = 0;

		[Property]
		private long height = 0;

		[Property]
		private long added = 0;

		[Property]
		private long updated = 0;

		public string MD5 { get { return MD5Helper.BlobToMD5 (this.md5); } }
		public string Path { get { return this.path; } }
		public double ELO { get { return this.elo; } }
		public long Votes { get { return votes; } }
		public long Wins { get { return wins; } }
		public long Losses { get { return losses; } }
		public BooruImageType type { get { return this.TypeFromString (this.typeString); } }
		public Point2D Size { get { return new Point2D((int)this.width, (int)this.height); } }

		public DateTime Added { get { return new DateTime (this.added); } }
		public DateTime Updated { get { return new DateTime (this.updated); } }

		public ImageDetails()
		{
		}

		private BooruImageType TypeFromString(string typeString)
		{
			switch (typeString) {
			case "A":
				return BooruImageType.Animation;
			case "C":
				return BooruImageType.Comix;
			case "I":
				return BooruImageType.Image;
			case "V":
				return BooruImageType.Video;
			default:
				return BooruImageType.Unknown;
			}		
		}

		public void UpdateElo(double offset)
		{
			this.elo += offset;
		}
	}
}

