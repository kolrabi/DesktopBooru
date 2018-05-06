using System;
using System.Data.Common;
using System.Collections.Generic;

namespace Booru
{
	public class ImageReader
	{
		DatabaseCursor<ImageDetails> imageCursor;

		readonly BooruImageType type; 

		public ImageReader (BooruImageType type)
		{
			this.type = type;
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
			
		void QueryImages()
		{
			if (this.imageCursor != null)
				this.imageCursor.Close ();

			this.imageCursor = BooruApp.BooruApplication.Database.QueryRandomImagesForVoting (this.type);
		}
	}
}

