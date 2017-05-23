using System;

using Property = Booru.DatabaseReadable.PropertyAttribute;

namespace Booru
{
	public class TagImplicationDetails : DatabaseReadable {
		[Property]
		private long tagid = 0;

		[Property]
		private long implies = 0;

		[Property]
		private bool isneg;

		public long TagID { get { return this.tagid; } }
		public long ImpliedTagID { get { return this.implies; } }
		public bool IsNegative { get { return this.isneg; } }

		public TagImplicationDetails()
		{
		}
	}
}

