using System;
using System.Collections.Generic;

namespace Booru
{
	public static class StringHelper
	{
		/// <summary>
		/// Compares the two strings based on letter pair matches
		/// </summary>
		/// <param name="str1"></param>
		/// <param name="str2"></param>
		/// <returns>The percentage match from 0.0 to 1.0 where 1.0 is 100%</returns>
		public static double CompareStrings(string str1, string str2)
		{
			List<string> pairs1 = WordLetterPairs(str1.ToUpper());
			List<string> pairs2 = WordLetterPairs(str2.ToUpper());

			int intersection = 0;
			int union = pairs1.Count + pairs2.Count;

			for (int i = 0; i < pairs1.Count; i++)
			{
				for (int j = 0; j < pairs2.Count; j++)
				{
					if (pairs1[i] == pairs2[j])
					{
						intersection++;
						pairs2.RemoveAt(j);//Must remove the match to prevent "GGGG" from appearing to match "GG" with 100% success

						break;
					}
				}
			}

			return (2.0 * intersection) / union;
		}

		/// <summary>
		/// Gets all letter pairs for each
		/// individual word in the string
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private static List<string> WordLetterPairs(string str)
		{
			List<string> AllPairs = new List<string>();

			// Tokenize the string and put the tokens/words into an array
			string[] Words = str.Split('_');

			// For each word
			for (int w = 0; w < Words.Length; w++)
			{
				if (!string.IsNullOrEmpty(Words[w]))
				{
					// Find the pairs of characters
					String[] PairsInWord = LetterPairs(Words[w]);

					for (int p = 0; p < PairsInWord.Length; p++)
					{
						AllPairs.Add(PairsInWord[p]);
					}
				}
			}

			return AllPairs;
		}

		/// <summary>
		/// Generates an array containing every 
		/// two consecutive letters in the input string
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private static string[] LetterPairs(string str)
		{
			int numPairs = str.Length - 1;

			string[] pairs = new string[numPairs];

			for (int i = 0; i < numPairs; i++)
			{
				pairs[i] = str.Substring(i, 2);
			}

			return pairs;
		}
	

		// calculate levensthein distance between two strings
		public static int LevenShteinDistance(this string s, string t)
		{
			int n = s.Length;
			int m = t.Length;

			int[,] d = new int[n + 1, m + 1];

			// Step 1
			if (n == 0)
			{
				return m;
			}

			if (m == 0)
			{
				return n;
			}

			// Step 2
			for (int i = 0; i <= n; d[i, 0] = i++)
			{
			}

			for (int j = 0; j <= m; d[0, j] = j++)
			{
			}

			// Step 3
			for (int i = 1; i <= n; i++)
			{
				//Step 4
				for (int j = 1; j <= m; j++)
				{
					// Step 5
					int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

					// Step 6
					d[i, j] = Math.Min(
						Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
						d[i - 1, j - 1] + cost);
				}
			}
			// Step 7
			return d[n, m];
		}

		public static int CompareNatural(this string str1, string str2)
		{
			var cultureInfo = System.Globalization.CultureInfo.CurrentUICulture;

			if (str1 == str2) {
				return 0;
			}

			if (str1 == null) {
				return -1;
			}

			if (str2 == null) {
				return 1;
			}

			int index1 = 0;
			int index2 = 0;
			while (index1 < str1.Length && index2 < str2.Length)
			{
				if (char.IsDigit(str1[index1]) && char.IsDigit(str2[index2]))
				{
					// skip 0s
					while (index1 + 1 < str1.Length && char.IsDigit (str1 [index1 + 1]) && str1 [index1] == '0')
						index1++;
					while (index2 + 1 < str2.Length && char.IsDigit (str2 [index2 + 1]) && str2 [index2] == '0')
						index2++;
					
					int numStartIndex1 = index1++;
					int numStartIndex2 = index2++;

					while (index1 < str1.Length && Char.IsDigit(str1[index1]))
						index1++;
					
					while (index2 < str2.Length && Char.IsDigit(str2[index2]))
						index2++;

					int numEndIndex1 = index1;
					int numEndIndex2 = index2;

					int numLength1 = numEndIndex1 - numStartIndex1;
					int numLength2 = numEndIndex2 - numStartIndex2;

					if (numLength1 > numLength2) {
						return 1;
					}
					if (numLength1 < numLength2) {
						return -1;
					}

					int result = cultureInfo.CompareInfo.Compare (str1, numStartIndex1, numLength1, str2, numStartIndex2, numLength2);
					if (result != 0)
						return result;
				}
				else
				{
					int result = cultureInfo.CompareInfo.Compare(str1, index1, 1, str2, index2, 1);
					if (result != 0)
						return result;
					index1++;
					index2++;
				}
			}

			if (index1 < str1.Length)
				return 1;

			return -1;
		}
	}
}

