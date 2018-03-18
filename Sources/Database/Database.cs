using System;
using System.Data.Common;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;

namespace Booru
{
	public class Database : IDisposable
	{
		public Gtk.ListStore TagEntryCompletionStore = new Gtk.ListStore (typeof(string), typeof(int));
		public readonly BooruLog.Logger Logger;

		public Configuration Config;
		public DbConnection Connection { get; private set; }
		public bool IsLoaded { get; private set; }

		private readonly IDictionary<string, long> tagIds = new Dictionary<string, long> ();
		private readonly IDictionary<long, TagDetails> usedTagList = new Dictionary<long, TagDetails> ();
		private readonly IDictionary<long, IList<long>> tagImplications = new Dictionary<long, IList<long>> ();
		private double maxCount = .0;

		public Database ()
		{
			this.IsLoaded = false;
			this.Logger = new BooruLog.Logger (BooruLog.Category.Database);

		}

		#region Management
		private const int DATABASE_VERSION = 1;

		private DbCommand LoadSQL(DbConnection connection, string name)
		{
			var cmd = connection.CreateCommand ();
			cmd.CommandText = Resources.LoadResourceString (name);
			return cmd;
		}

		public void CreateDatabase(DbConnection connection)
		{
			try {
				Logger.Log(BooruLog.Severity.Info, "Creating database "+BooruApp.BooruApplication.DBFile+"...");

				connection.Open();

				Logger.Log(BooruLog.Severity.Info, "Creating tables...");

				var cmd = this.LoadSQL(connection, "Booru.Resources.SQL.schema.sql");

				var transaction = connection.BeginTransaction ();
				cmd.ExecuteNonQuery ();
				transaction.Commit ();
				connection.Close();

				Logger.Log(BooruLog.Severity.Info, "Opening created database...");

				this.OpenDatabase (connection);
			} catch(Exception ex) {
				Logger.Log(BooruLog.Severity.Error, "Could not create database: " + ex.Message);
			}
		}

		public void OpenDatabase(DbConnection connection)
		{
			this.Close ();
			this.IsLoaded = false;

			BooruApp.BooruApplication.EventCenter.BeginChangeDatabase ();

			var loadThread = new Thread (new ThreadStart (() => {
				Logger.Log(BooruLog.Severity.Info, "Opening database "+BooruApp.BooruApplication.DBFile+"...");
				this.Connection = connection;

				try {
					this.Connection.Open ();
					this.TagEntryCompletionStore = new Gtk.ListStore(typeof(string), typeof(int));

					this.Config = new Configuration (this);

					Logger.Log(BooruLog.Severity.Info, "Checking database version...");
					var versionString = this.Config.GetString("$version", null);
					if (string.IsNullOrEmpty(versionString)) {
						Logger.Log(BooruLog.Severity.Error, "Database version not found. Not a booru database!");
						this.Connection.Close();
						BooruApp.BooruApplication.EventCenter.FinishChangeDatabase(false);
						return;
					}

					var version = this.Config.GetInt("$version");
					if (version != DATABASE_VERSION) {
						this.UpgradeDatabase(version);
					}
				
					Logger.Log(BooruLog.Severity.Info, "Caching tags...");
					this.CacheTags ();

					Logger.Log(BooruLog.Severity.Info, "Checking for obsolete path references...");
					var allPaths = this.GetAllPaths();
					var deletedPaths = new List<string>();
					foreach(var path in allPaths)
					{
						if (!System.IO.File.Exists(path)) {
							deletedPaths.Add(path);
							this.RemoveImagePath(path);
						}
					}
					if (deletedPaths.Count > 0) {
						Logger.Log(BooruLog.Severity.Info, "Removed "+deletedPaths.Count+" path references:");
						foreach(var path in deletedPaths)
							Logger.Log(BooruLog.Severity.Info, "    "+path);
					}
				
					Logger.Log(BooruLog.Severity.Info, "Database successfully opened.");
					BooruApp.BooruApplication.EventCenter.FinishChangeDatabase (true);
					this.IsLoaded = true;
				} catch (System.Data.Common.DbException ex) {
					Logger.Log(BooruLog.Severity.Error, "Could not open database: "+ex.Message);
					BooruApp.BooruApplication.EventCenter.FinishChangeDatabase(false);
				}
			}));
			loadThread.Name = "Database Load";
			loadThread.Start ();
 		}

		public void Dispose()
		{
			this.Close ();
		}

		private void Close()
		{
			if (this.Connection != null) {
				this.Connection.Close ();
				this.Connection.Dispose ();
			}
			this.Connection = null;
			this.IsLoaded = false;
		}

		private void UpgradeDatabase(int curVersion)
		{
			var transaction = this.Connection.BeginTransaction ();
			try {
				while (curVersion < DATABASE_VERSION) {
					var cmd = this.LoadSQL(this.Connection, string.Format("Booru.Resources.SQL.upgrade_{0}.sql", curVersion));

					Logger.Log(BooruLog.Severity.Info, "Upgrading database version from " + curVersion);

					cmd.ExecuteNonQuery();
					curVersion++;
				}
				transaction.Commit ();
			} catch(Exception ex) {
				transaction.Rollback ();
				Console.WriteLine("Could not upgrade database: "+ex.Message);
				Console.WriteLine(ex.StackTrace);
				throw ex;
			}
		}

		#endregion

		#region Images
		public bool DoesImageExist(string md5)
		{
			return Queries.Images.Exists.Execute (md5);
		}

		public bool AddImageIfNew(string path, string md5, string tags, BooruImageType fileType)
		{
			Logger.Log (BooruLog.Severity.Info, md5+": Adding/Updating: "+path);

			List<string> imageTagList = new List<string>(tags.Split (" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));

			lock (BooruApp.BooruApplication.Database.Connection) {
				var transaction = BooruApp.BooruApplication.Database.Connection.BeginTransaction ();
				try {
					Queries.Images.Add.Execute (md5, fileType);
					Queries.Files.Add.Execute (md5, path);

					foreach (string tag in imageTagList) {
						this.AddImageTag (md5, tag); 
					}
					transaction.Commit ();
					return true;
				} catch (Exception ex) {
					transaction.Rollback ();
					Logger.Log (BooruLog.Severity.Error, md5 + ": DB Update threw exception: " + ex.Message);
					Logger.Log (BooruLog.Severity.Error, ex.StackTrace);
					return false;
				}
			}
		}

		public void SetImageType(string md5, BooruImageType type)
		{
			BooruApp.BooruApplication.TaskRunner.StartTaskAsync (() => {
				Queries.Images.SetType.Execute (md5, type);
				Queries.Images.UpdateUpdated.Execute (md5);
			});
		}

		public void SetImageSize(string md5, Point2D size)
		{
			BooruApp.BooruApplication.TaskRunner.StartTaskAsync (() => {
				Queries.Images.SetSize.Execute (md5, size);
				Queries.Images.UpdateUpdated.Execute (md5);
			});
		}

		public void AddImagePath(string md5, string path)
		{
			BooruApp.BooruApplication.TaskRunner.StartTaskAsync (() => Queries.Files.Add.Execute (md5, path));
		}

		public void RemoveImagePath(string path)
		{
			BooruApp.BooruApplication.TaskRunner.StartTaskAsync (() => Queries.Files.Remove.Execute (path));
		}

		public void RemoveImage(string md5) 
		{
			BooruApp.BooruApplication.TaskRunner.StartTaskAsync (() => Queries.Images.Remove.Execute (md5));
		}

		public List<string> GetAllPaths ()
		{
			return Queries.Files.GetAll.Execute ();
		}
			
		public DatabaseCursor<ImageDetails> QueryRandomImagesForVoting(BooruImageType type, bool asc)
		{
			return Queries.Images.NextVoteImages.Execute (type, asc);
		}

		public DatabaseCursor<ImageDetails> QueryImagesWithTags(string tagString)
		{
			return Queries.Images.FindImages.Execute (tagString);
		}

		public List<string> GetImageTags(string md5)
		{
			return Queries.ImageTags.Get.Execute (md5);
		}

		public List<string> GetImagePaths(string md5)
		{
			return Queries.Files.GetForImage.Execute (md5);
		}

		public ImageDetails GetImageDetails(string md5)
		{
			using (var reader = Queries.Images.Get.Execute (md5)) {
				if (reader.Read ()) {
					ImageDetails details = new ImageDetails ();
					details.InitFromReader (reader);
					return details;
				}
			}

			return null;
		}

		public string GetPathMD5(string path)
		{
			byte[] md5blob = Queries.Files.GetMD5.Execute (path);
			if (md5blob == null)
				return null;

			return MD5Helper.BlobToMD5 (md5blob);
		}

		public void AddImageElo(string md5, float offset)
		{
			BooruApp.BooruApplication.TaskRunner.StartTaskAsync (() => Queries.Images.UpdateElo.Execute (md5, offset));
		}

		[Obsolete]
		public void RefreshImage(ImageDetails details)
		{
			using (var reader = Queries.Images.Get.Execute (details.MD5)) {
				details.InitFromReader (reader);
			}
		}
		#endregion

		#region Tags
		public string GetCanonicalTag(string tag) 
		{
			long id = this.GetTagId (tag);
			if (id == -1)
				return tag;

			lock(this.usedTagList)
				return this.usedTagList [id].Tag;
		}

		public long GetTagId(string tag) 
		{
			lock (this.tagIds) {
				if (tagIds.ContainsKey (tag))
					return tagIds [tag];

				if (tagIds.ContainsKey (tag.ToLower ()))
					return tagIds [tag.ToLower ()];
			
				return -1;
			}
		}

		public long GetOrCreateTagId(string tag) 
		{
			long tagId = this.GetTagId(tag);
			if (tagId == -1)
				tagId = this.CreateTag (tag);
			return tagId;
		}

		protected long CreateTag(string tag) 
		{
			try {
				Queries.Tags.Add.Execute(tag);

				long tagId = Queries.Tags.GetID.Execute (tag);

				TagDetails tagData = new TagDetails(tagId, tag, TagDetails.TagType.Normal);

				lock(this.tagIds)
					this.tagIds[tag] = tagId;

				lock(this.usedTagList)
					this.usedTagList[tagId] = tagData;
			
				this.TagEntryCompletionStore.AppendValues (tag, tagData.Count);
			} catch(Exception ex) {
				Logger.Log(BooruLog.Severity.Error, "Could not create tag: " + ex.Message);
			}
			return this.GetTagId (tag);
		}

		public List<int> MatchTag(string tag) 
		{
			return Queries.Tags.Match.Execute (tag);
		}

		public List<TagDetails> GetUsedTagList() 
		{
			lock(this.usedTagList)
				return new List<TagDetails>(this.usedTagList.Values);
		}

		public List<string> GetTagImplications(string tag)
		{
			List<string> result = new List<string> ();

			long tagId = this.GetTagId (tag);

			lock(this.tagImplications)
			if (tagId != -1 && this.tagImplications.ContainsKey (tagId)) {
				foreach (var implies in this.tagImplications[tagId]) {
					long impliedId = Math.Abs (implies);
					string impliedTag = (implies < 0 ? "-" : "") + this.usedTagList [impliedId].Tag;
					result.Add (impliedTag);
				}
			}

			return result;
		}

		public bool AddTagImplication(string tag, string implies)
		{
			bool isneg = implies.StartsWith ("-");
			if (isneg)
				implies = implies.Substring (1);
			
			long tagId = this.GetTagId (tag);
			if (tagId == -1)
				return false;

			long impliesId = this.GetTagId (implies);
			if (impliesId == -1)
				return false;

			lock (this.tagImplications) {
				if (!this.tagImplications.ContainsKey (tagId))
					this.tagImplications [tagId] = new List<long> ();

				if (isneg)
					impliesId = -impliesId;
			
				this.tagImplications [tagId].Add (impliesId);
			}

			Queries.TagImplications.Add.Execute (tagId, impliesId);

			return true;
		}

		public bool RemoveTagImplication(string tag, string implies)
		{
			bool isneg = implies.StartsWith ("-");
			if (isneg)
				implies = implies.Substring (1);
			
			long tagId = this.GetTagId (tag);
			if (tagId == -1)
				return false;

			long impliesId = this.GetTagId (implies);
			if (impliesId == -1)
				return false;

			lock (this.tagImplications) {
				
				if (!this.tagImplications.ContainsKey (tagId))
					return true;

				if (isneg)
					impliesId = -impliesId;

				this.tagImplications.Remove (impliesId);
			}

			Queries.TagImplications.Remove.Execute (tagId, impliesId);

			return true;
		}

		public TagDetails GetTag(string tag) 
		{
			long tagId = this.GetTagId (tag);
			if (tagId == -1)
				return null;

			lock (this.usedTagList) {
				if (this.usedTagList.ContainsKey (tagId)) {
					return this.usedTagList [tagId];
				} else {
					return null;
				}
			}
		}
			
		public void UpdateTagsScore(IEnumerable<string> tags, double offset) 
		{
			List<long> tagIds = new List<long> ();

			foreach (var tag in tags) {
				long tagId = this.GetTagId (tag);
				if (tagId != -1)
					tagIds.Add (tagId);
			}

			lock (this.usedTagList) {
				foreach(long tagId in tagIds)
					this.usedTagList [tagId].UpdateScore (offset);
			}
				
			BooruApp.BooruApplication.TaskRunner.StartTaskAsync(()=> {
				Queries.Tags.UpdateScore.Execute(tagIds, offset);
			});
		}

		public class SimilarTag
		{
			
			public string Tag;
			public long ID;
			public double Distance;

			public SimilarTag(string tag, long id, double distance)
			{
				this.Tag = tag;
				this.ID = id;
				this.Distance = distance;
			}
		}

		private void FindSplitTags(string tag, ref List<SimilarTag> tags)
		{
			List<int> indices = new List<int> ();
			HashSet<string> splitTags = new HashSet<string> ();

			int underscore = tag.IndexOf ('_');
			while (underscore > 0) {
				indices.Add (underscore);
				underscore = tag.IndexOf ('_', underscore + 1);
			}

			if (indices.Count == 0)
				return;

			int count = 2 << indices.Count;
			for (int i = 1; i < count; i++) {
				string newTag = tag;
				for (int j = 0; j < indices.Count; j++) {
					if (((i >> j) & 1) == 1) {
						newTag = newTag.Substring(0, indices [j]) + " " + newTag.Substring(indices [j]+1);
					}
				}

				if (splitTags.Contains (newTag))
					continue;
				
				bool valid = true;
				foreach (string subTag in newTag.Split(' ')) {
					if (GetTagId (subTag) == -1) {
						valid = false;
						break;
					}
				}
				if (valid) {
					tags.Add (new SimilarTag (newTag, -1, 0));
					splitTags.Add (newTag);
				}
			}
		}

		public List<SimilarTag> FindSimilarTags (string tag, int levenThresh) {
			var similar = new List<SimilarTag> ();

			if (levenThresh >= tag.Length) {
				levenThresh = tag.Length - 1;
			}

			similar.Add(new SimilarTag(tag, this.GetTagId (tag), 0));

			FindSplitTags(tag, ref similar);

			var distTags = new Dictionary<int, ConcurrentBag<SimilarTag>>();
			for (int i = 1; i < levenThresh; i++)
				distTags [i] = new ConcurrentBag<SimilarTag> ();
		
			lock(this.tagIds)
			{
				System.Threading.Tasks.Parallel.ForEach (this.tagIds, (kv) => {
					if (!this.usedTagList.ContainsKey(kv.Value))
						return;
					
					if (kv.Key.StartsWith (tag)) {
						lock (similar) {
							similar.Add (new SimilarTag (kv.Key, kv.Value, 1));
						}
						return;
					}

					if (Math.Abs (kv.Key.Length - tag.Length) >= levenThresh)
						return;

					double similarity = StringHelper.CompareStrings(tag, kv.Key);
					//if (similarity <= 0.0)
						//similarity = 1;

					try {
						int ldistance = tag.LevenShteinDistance (kv.Key);
						if (ldistance < levenThresh) {
							long count = this.usedTagList[kv.Value].Count;
							double weightedDistance = ldistance;
							double countWeight = 1 + similarity  + count / this.maxCount;
							weightedDistance = weightedDistance / (countWeight);
							distTags [ldistance].Add (new SimilarTag (kv.Key, kv.Value, 1 + weightedDistance));
							if (kv.Key == "solo" || kv.Key == "blush")
							Console.WriteLine(kv.Key + " <-> " + tag + ": " + weightedDistance + " " + count);
						}
					}catch(Exception ex)
					{
						Console.WriteLine(ex.Message);
						Console.WriteLine(ex.StackTrace);
					}

				});
			}

			for (int i=1; i<levenThresh; i++) {
				similar.AddRange (distTags [i]);
			}
				
			return similar.OrderBy ((x) => x.Distance).ToList();
		}

		public void SetTagType(TagDetails tag, TagDetails.TagType type)
		{
			long tagId = this.GetTagId (tag.Tag);
			if (tagId == -1)
				return;

			lock(this.usedTagList)
				this.usedTagList [tagId].UpdateType (type);

			BooruApp.BooruApplication.TaskRunner.StartTaskAsync(() => Queries.Tags.SetType.Execute (tagId, type));
		}

		public bool ReplaceTag(string fromTag, List<string> toTags)
		{
			long fromTagId = this.GetTagId (fromTag);
			List<long> toTagIds = new List<long> (toTags.Count);

			foreach (string toTag in toTags) {
				long toTagId = this.GetTagId (toTag);
				if (toTagId == -1)
					return false;

				toTagIds.Add(toTagId);
			}

			Queries.ImageTags.Replace.Execute (fromTagId, toTagIds);

			lock (this.usedTagList) {
				TagDetails fromDetails = this.usedTagList [fromTagId];
				foreach (long toTagId in toTagIds) {
					this.usedTagList [toTagId].Replace (fromDetails);
				}
				this.usedTagList.Remove (fromTagId);
			}

			return true;
		}

		public List<string> GetAllTags()
		{
			lock(this.tagIds)
				return new List<string>(this.tagIds.Keys);
		}

		private void CacheTags()
		{
			using (var cursor = Queries.Tags.GetAll.Execute()) {
				while(cursor.Read()) {
					TagDetails details = cursor.Value;
					if (details.Tag != null) {
						lock (this.tagIds)
							this.tagIds [details.Tag] = details.ID;
						lock(this.usedTagList)
							this.usedTagList [details.ID] = details;

						if (details.Count > this.maxCount)
							this.maxCount = details.Count;
						
						this.TagEntryCompletionStore.AppendValues (details.Tag + " ", details.Count);
					}
				}
			}

			var implications = Queries.TagImplications.Get.Execute ();

			lock (this.tagImplications) {
				this.tagImplications.Clear ();
				foreach (var kv in implications) {
					this.tagImplications.Add (kv);
				}
			}
		}

		[Obsolete]
		private void RefreshTag(long id)
		{
			var reader = Queries.Tags.Get.Execute (id);
			lock (this.usedTagList) {
				this.usedTagList [id].InitFromReader (reader);
			}
		}
		#endregion

		#region Image Tags
		public void AddImageTag(string md5, string tag) 
		{
			long id = this.GetOrCreateTagId (tag);

			if (id == -1)
				return;
		
			Queries.ImageTags.Add.Execute (id, md5);
			Queries.Images.UpdateUpdated.Execute (md5);
		}

		public void RemoveImageTag(string md5, string tag) 
		{
			long id = this.GetTagId (tag);

			if (id == -1)
				return;

			Queries.ImageTags.Remove.Execute (id, md5);
			Queries.Images.UpdateUpdated.Execute (md5);
		}
		#endregion

		#region Configuration
		public string GetConfig(string key)
		{
			return Queries.Config.Get.Execute (key);
		}

		public void SetConfig(string key, string value)
		{
			Queries.Config.Set.Execute (key, value);
		}
		#endregion
	}
}

