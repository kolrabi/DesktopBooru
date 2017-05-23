using System;
using System.Data.Common;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace Booru
{
	public class DatabaseQuery
	{
		protected readonly DbCommand command;

		protected static string TagsTableName = "tags";
		protected static string ImagesTableName = "images";
		protected static string FilesTableName = "files";
		protected static string ImageTagsTableName = "image_tags";
		protected static string ConfigTableName = "config";
		protected static string ImplicationsTableName = "tag_implications";

		protected static IDictionary<Type, List<long>> queryTimes = new Dictionary<Type, List<long>>();

		public DatabaseQuery(string sql)
		{
			this.command = BooruApp.BooruApplication.Database.Connection.CreateCommand ();
			this.command.CommandText = sql;
		}

		protected void Prepare()
		{
			this.command.Prepare();
		}
			
		public void AddParameter(DbType type, string name, object value = null)
		{
			DbParameter parameter = this.command.CreateParameter ();
			parameter.DbType = type;
			parameter.ParameterName = name;
			parameter.Value = value;
			this.command.Parameters.Add (parameter);
		}

		protected void SetParameterValues(params object[] paramValues)
		{
			for (int i = 0; i < paramValues.Length; i++) {
				this.command.Parameters [i].Value = paramValues [i];
			}
		}

		private T ExecuteTimed<T>(Func<T> func)
		{
			var stopWatch = new System.Diagnostics.Stopwatch ();
			stopWatch.Start ();

			T ret;
			try {
				ret = func ();
			} catch(Exception ex) {
				Console.WriteLine ("Exception caught executing query of type " + this.GetType ().FullName);
				Console.WriteLine ();

				Console.WriteLine (ex.GetType().Name);
				Console.WriteLine (ex.Message);

				Console.WriteLine ();
				Console.WriteLine ("Command SQL:");
				Console.WriteLine (this.command.CommandText);
				Console.WriteLine ();

				throw ex;
			}

			var elapsed = stopWatch.ElapsedMilliseconds;

			lock (queryTimes) {
				if (!queryTimes.ContainsKey (this.GetType ()))
					queryTimes.Add (this.GetType (), new List<long> ());

				queryTimes [this.GetType ()].Add (elapsed);
			}

			return ret;
		}

		public static void DumpTimes()
		{
			lock (queryTimes) {
				try {
					string log = "";
					foreach (var kv in queryTimes) {
						kv.Value.Sort ();

						long num = kv.Value.Count;
						long min = kv.Value [0];
						long max = kv.Value [kv.Value.Count - 1];
						long avg = (min + max) / 2;
						long med = kv.Value [kv.Value.Count / 2];
						long tot = kv.Value.Sum ();

						log += string.Format("{0,-50}: min: {1,5} max: {2,5} avg: {3,5} med: {4,5} num: {5,5} tot: {6,10}\n", kv.Key.FullName, min, max,avg, med, num, tot);
					}
					System.IO.File.WriteAllText("queries.log", log);
				} catch (Exception ex) {
					Console.WriteLine ("Could not log query statistics: " + ex.Message);
					Console.WriteLine (ex.StackTrace);
				}
			}
		}

	    public object ExecuteScalar(params object[] paramValues)
		{
			lock (this.command) {
				this.SetParameterValues (paramValues);
				return ExecuteTimed(() => this.command.ExecuteScalar ());
			}
		}

		public DbDataReader ExecuteReader(params object[] paramValues)
		{
			lock (this.command) {
				this.SetParameterValues (paramValues);
				return ExecuteTimed (() => this.command.ExecuteReader ());
			}
		}

		public void ExecuteNonQuery(params object[] paramValues)
		{
			lock (this.command) {
				this.SetParameterValues (paramValues);
				ExecuteTimed(() => this.command.ExecuteNonQuery ());
			}
		}

		protected DbTransaction BeginTransaction()
		{
			return this.command.Connection.BeginTransaction ();
		}

	}
}

