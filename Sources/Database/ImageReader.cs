using System;
using System.Data.Common;
using System.Collections.Generic;

namespace Booru
{
	public class ImageReader
	{
		private DatabaseCursor<ImageDetails> imageCursor;
		private readonly BooruImageType type; 
		private readonly bool asc;

		public ImageReader (BooruImageType type, bool asc)
		{
			this.type = type;
			this.asc = asc;
		}

		public void Close()
		{
			if (this.imageCursor != null) {
				this.imageCursor.Close ();
				this.imageCursor = null;
			}
		}

		public ImageDetails GetNextImage()
		{
			if (this.imageCursor == null) {
				this.QueryImages ();
			}

			if (!this.imageCursor.Read ()) {
				this.QueryImages ();
				if (!this.imageCursor.Read ())
					return null;
			}
			return this.imageCursor.Value;
		}
			
		private void QueryImages()
		{
			if (this.imageCursor != null)
				this.imageCursor.Close ();

			this.imageCursor = BooruApp.BooruApplication.Database.QueryRandomImagesForVoting (this.type, this.asc);
		}
	}
}

