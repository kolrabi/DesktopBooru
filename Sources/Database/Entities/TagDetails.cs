using System;

using Property = Booru.DatabaseReadable.PropertyAttribute;

namespace Booru
{
	// TODO: define encourage a certain minimal set of tag types to be present on each image

	public class TagDetails : DatabaseReadable
	{
		[Property]
		private long id = 0;

		[Property]
		private string tag = "";

		[Property]
		private double score = 0.0;

		[Property("usage")]
		private long count = 0;

		[Property("type")]
		private long tagType = 0;

		public long ID { get { return this.id; } }
		public string Tag { get { return this.tag; } }
		public double Score { get { return this.score; } }
		public long Count { get { return this.count; } }
		public TagType Type { get { return (TagType)this.tagType; } }

		public enum TagType
		{
			// TODO: make a table in the database for this

			// Catch all for general tags
			Normal = 0,	

			// Series or collection on which the image can be based
			Series = 1,

			// Identifying a person or character in an image
			Person = 2,

			// Identifying a person or group that created an image
			Artist = 3,

			// For tags that may be used to internally organize images
			Meta = 4,

			// Identifying the presence or style of making parts of an image unrecognizable
			Censorship = 5,

			// Identifying the pose in which a person or character is in an image
			Pose = 6,

			// Identifying the location an image depicts
			Location = 7,

			// Identifying the presencs or style of clothing that are depicted in the image
			Clothing = 8,

			// Identifying an ongoing process depicted in the image
			Process = 9,

			// Identifying distinct parts of the image
			Feature = 10
		}

		public TagDetails()
		{
		}

		public TagDetails(long id, string tag, TagType type)
		{
			this.id = id;
			this.tag = tag;
			this.tagType = (int)type;
		}

		public void UpdateScore(double offset)
		{
			this.score += offset;
		}

		public void UpdateType(TagType type)
		{
			this.tagType = (int)type;
		}

		public void Replace(TagDetails replaced)
		{
			this.count += replaced.count;
			this.score += replaced.score;
		}

		public static Cairo.Color GetTagTypeColor(TagType type)
		{
			switch (type) {
			case TagDetails.TagType.Artist: 	return new Cairo.Color (1.00, 0.80, 1.00);
			case TagDetails.TagType.Series: 	return new Cairo.Color (0.80, 1.00, 1.00);
			case TagDetails.TagType.Meta:   	return new Cairo.Color (0.50, 0.50, 0.50);
			case TagDetails.TagType.Censorship: return new Cairo.Color (1.00, 0.80, 0.80);
			case TagDetails.TagType.Feature:  	return new Cairo.Color (0.80, 1.00, 0.80);
			case TagDetails.TagType.Clothing:   return new Cairo.Color (0.80, 0.80, 1.00);
				//case TagDetails.TagType.Normal:
			default:				return new Cairo.Color (1, 1, 1);
			}

		}
	}
}

