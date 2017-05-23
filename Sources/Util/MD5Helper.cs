using System;
using System.Text;

namespace Booru
{
	public class MD5Helper
	{
		// convert md5 string to byte array
		public static byte[] MD5ToBlob (string md5)
		{
			int length = md5.Length;
			byte[] blob = new byte[length / 2];
			for (int i = 0; i < length; i += 2)
				blob [i / 2] = Convert.ToByte (md5.Substring (i, 2), 16);
			return blob;
		}

		// convert md5 byte array to string
		public static string BlobToMD5 (byte[] blob)
		{
			StringBuilder hex = new StringBuilder (blob.Length * 2);
			foreach (byte b in blob)
				hex.AppendFormat ("{0:x2}", b);
			return hex.ToString ();
		}
	}
}

