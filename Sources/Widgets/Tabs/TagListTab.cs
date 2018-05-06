using System;
using Gtk;
using Cairo;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public partial class TagListTab : LoadableWidget
	{
		[UI] Gtk.TreeView TagTreeView;
		[UI] Gtk.Label TagLabel;
		[UI] Gtk.Box TagTypeBox;
		[UI] Gtk.Entry ReplaceEntry;
		[UI] Gtk.Box SimilarTagBox;

		[UI] Gtk.TreeView ImpliesTreeView;
		[UI] Gtk.Entry ImpliesEntry;

		[UI] Gtk.Entry FilterEntry;

		private readonly Gtk.ListStore impliesStore, allTagsStore;
		private readonly Gtk.TreeModelFilter filter;

		private Thread loadThread;
		private TagDetails activeTag;

		private string filterString = "";

		private bool loadingTag = false;

		private List<Gtk.RadioButton> typeButtons = new List<RadioButton>();

		public static TagListTab Create ()
		{
			return LoadableWidget.Create<TagListTab> ();
		}

		protected TagListTab (Builder builder, IntPtr handle) : base (builder, handle)
		{
			builder.Autoconnect (this);

			foreach (var typeValue in Enum.GetValues(typeof(TagDetails.TagType))) {
				TagDetails.TagType type = (TagDetails.TagType)typeValue;
				Gtk.RadioButton button;

				if (this.typeButtons.Count == 0) {
					button = new Gtk.RadioButton ("");
				} else {
					button = new Gtk.RadioButton (this.typeButtons [0]);
				}
				button.Label = type.ToString ();
				this.typeButtons.Add (button);
				this.TagTypeBox.PackStart (button, true, true, 2);
				button.ShowAll ();
				button.CanFocus = false;
				button.Toggled += (sender, e) => {
					if (!this.loadingTag)
						this.SetTagType(type);
				};
			}

			this.ImpliesTreeView.AppendColumn ("Tag", new CellRendererText (), "text", 0);
			this.impliesStore = new ListStore (typeof(string));
			this.ImpliesTreeView.Model = this.impliesStore;

			this.allTagsStore = new ListStore(typeof (int), typeof(string), typeof(float), typeof(int), typeof(string), typeof(TagDetails));

			AddColumnText ("ID", 0);
			AddColumnText ("Usage", 3);
			AddColumnText ("Tag", 1);
			AddColumnText ("Score", 2);
			AddColumnText ("Type", 4);

			this.filter = new TreeModelFilter (this.allTagsStore, null);
			this.filter.VisibleFunc = ((ITreeModel model, TreeIter iter) => {
				if (string.IsNullOrWhiteSpace(this.filterString))
					return true;

				lock(model) {
					string tagString = model.GetValue(iter, 1) as string;
					if (string.IsNullOrEmpty(tagString))
						return true;
				
					return tagString.Contains(filterString);
				}

			});

			this.TagTreeView.HeadersClickable = true;
			this.TagTreeView.Model = this.allTagsStore; //this.filter;

			this.TagTreeView.CursorChanged += OnTagsCursorChanged;
				
			this.Sensitive = false;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted += this.OnDatabaseLoadStarted;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += this.OnDatabaseLoadSucceeded;
			BooruApp.BooruApplication.EventCenter.WillQuit += this.OnWillQuit;
		}

		private bool UnfilteredIterFromPath(out TreeIter iter, TreePath path)
		{
			TreeIter filteredIter;

			iter = default(TreeIter);

			if (!this.TagTreeView.Model.GetIter (out filteredIter, path))
				return false;

			if (this.TagTreeView.Model == this.allTagsStore) {
				iter = filteredIter;
				return true;
			}
			
			if (!this.allTagsStore.GetIterFirst (out iter))
				return false;

			TagDetails filteredDetails = (TagDetails)this.TagTreeView.Model.GetValue (filteredIter, 5);

			do {
				TagDetails unfilteredDetails = (TagDetails)this.allTagsStore.GetValue (iter, 5);
				if (unfilteredDetails.ID == filteredDetails.ID) {
					return true;
				}

			} while(this.allTagsStore.IterNext (ref iter));

			return false;
		}

		private readonly ConcurrentQueue<TagDetails> addedTags = new ConcurrentQueue<TagDetails>();
		private uint loaderIdle = 0;
		private bool isFinished = true;


		private bool ProcessQueue()
		{
			if (this.addedTags.Count > 0) {
				TagDetails tag;
				if (this.addedTags.TryDequeue(out tag)) {
					this.allTagsStore.AppendValues (tag.ID, tag.Tag, tag.Score, tag.Count, tag.Type.ToString(), tag);
				}
			} else if (this.isFinished) {
				return false;
			}				

			return true;
		}

		private void OnDatabaseLoadStarted()
		{
			this.AbortLoading ();
			this.Sensitive = false;
		}

		private void OnDatabaseLoadSucceeded()
		{
			this.AbortLoading ();

			this.isFinished = false;
			this.loaderIdle = GLib.Idle.Add (this.ProcessQueue);

			this.loadThread = new Thread (new ThreadStart (() => {
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Database, BooruLog.Severity.Info, "Getting tags list...");
				var tags = BooruApp.BooruApplication.Database.GetUsedTagList ();
				BooruApp.BooruApplication.Log.Log(BooruLog.Category.Database, BooruLog.Severity.Info, "Got information about "+tags.Count+" tags.");

				this.allTagsStore.Clear();

				foreach (var tag in tags) 
					this.addedTags.Enqueue(tag);

				this.isFinished = true;

				BooruApp.BooruApplication.TaskRunner.StartTaskMainThread(()=> this.Sensitive = true);
			}));
			this.loadThread.Name = "Tag List Load";
			this.loadThread.Start ();
		}

		private void OnWillQuit()
		{
			this.AbortLoading();
		}

		private void AbortLoading()
		{
			if (this.loaderIdle != 0) {
				GLib.Idle.Remove (this.loaderIdle);
				this.loaderIdle = 0;
			}
			if (this.loadThread!=null)
				this.loadThread.Abort();
		}

		private Stack<SimilarTagWidget> similarTagWidgetPool = new Stack<SimilarTagWidget>();

		private SimilarTagWidget GetSimilarTagWidget(string tag)
		{
			SimilarTagWidget widget;

			if (similarTagWidgetPool.Count > 0) {
				widget = similarTagWidgetPool.Pop ();
			} else {
				widget = SimilarTagWidget.Create (this);
			}

			widget.Tag = tag;
			return widget;
		}

		private void RecycleSimilarTagWidget(Gtk.Widget widget)
		{
			if (!(widget is SimilarTagWidget))
				return;

			similarTagWidgetPool.Push (widget as SimilarTagWidget);
		}

		private void OnTagsCursorChanged(object o, EventArgs args)
		{
			TreePath path;
			TreeViewColumn column;

			this.TagTreeView.GetCursor(out path, out column);

			TreeIter iter;
			if (this.TagTreeView.Model.GetIter(out iter, path)) {
				this.loadingTag = true;

				this.activeTag = (TagDetails)this.TagTreeView.Model.GetValue(iter, 5);
				TagLabel.Text = this.activeTag.Tag;

				var similar = BooruApp.BooruApplication.Database.FindSimilarTags(this.activeTag.Tag, Math.Min(5, Math.Max(1, TagLabel.Text.Length-1))).Where(x => x.Tag!=this.activeTag.Tag).Distinct().ToList();

				var gridChldren = this.SimilarTagBox.Children;
				foreach (var child in gridChldren) {
					this.SimilarTagBox.Remove (child);
					this.RecycleSimilarTagWidget (child);
				}

				if (similar.Count == 0) {
					Gtk.Button emptyLabelButton = new Gtk.Button ();
					emptyLabelButton.Label = "No similar tags found...";
					emptyLabelButton.SetAlignment (0.0f, 0.5f);
					emptyLabelButton.FocusOnClick = false;
					emptyLabelButton.Relief = ReliefStyle.None;
					emptyLabelButton.Sensitive = false;

					this.SimilarTagBox.PackStart (emptyLabelButton, false, false, 0);
				} else {
					for (int i = 0; i < similar.Count; i++) {
						this.SimilarTagBox.PackStart(this.GetSimilarTagWidget(similar[i].Tag), false, false, 0);
					}
				}

				this.SimilarTagBox.ShowAll ();

				this.ReplaceEntry.StyleContext.RemoveClass ("entryWrong");
				this.typeButtons[(int)this.activeTag.Type].Active = true;

				this.impliesStore.Clear();
				foreach (var implied in BooruApp.BooruApplication.Database.GetTagImplications(this.activeTag.Tag))
					this.impliesStore.AppendValues(implied);

				this.loadingTag = false;
			}
		}
			
		private void AddColumnText(string name, int index)
		{
			CellRenderer textRenderer = new CellRendererText ();
			TreeViewColumn column = new TreeViewColumn (name, textRenderer);
			column.AddAttribute (textRenderer, "text", index);
			column.Resizable = true;
			TagTreeView.AppendColumn (column);
			MakeColumnSortable (column, index);
		}

		private void MakeColumnSortable(Gtk.TreeViewColumn column, int index) 
		{
			column.Clicked += (sender, e) => {
				int oldSortColumn;
				SortType oldSortType;

				this.allTagsStore.GetSortColumnId(out oldSortColumn, out oldSortType);

				SortType sortType = SortType.Ascending;
				if (oldSortColumn == index && oldSortType == SortType.Ascending)
					sortType = SortType.Descending;

				this.allTagsStore.SetSortColumnId(index, sortType);
			};
		}

		protected void SetTagType(TagDetails.TagType type)
		{
			if (this.activeTag == null)
				return;

			BooruApp.BooruApplication.Database.SetTagType (this.activeTag, type);

			TreePath path;
			TreeViewColumn column;

			this.TagTreeView.GetCursor(out path, out column);

			TreeIter iter;
			if (this.UnfilteredIterFromPath(out iter, path)) {
				lock(this.allTagsStore)
					this.allTagsStore.SetValue (iter, 4, type.ToString());
			}
		}

		void SelectTag(string tag)
		{
			TreeIter iter;
			this.FilterEntry.Text = "";
			this.TagTreeView.Model = this.allTagsStore;
			if (!this.TagTreeView.Model.GetIterFirst (out iter))
				return;

			string listTag;

			do {
				listTag = (string)this.allTagsStore.GetValue (iter, 1);
				if (listTag == tag) {
					var path = this.allTagsStore.GetPath(iter);
					this.TagTreeView.SetCursor(path, this.TagTreeView.Columns[0], false);
					break;
				}
			} while(this.allTagsStore.IterNext(ref iter));
		}

		void on_ReplaceButton_clicked(object sender, EventArgs args)
		{
			List<string> newTags = new List<string>(this.ReplaceEntry.Text.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
			if (newTags.Count == 0)
				return;

			var tag = this.activeTag;
			if (!BooruApp.BooruApplication.Database.ReplaceTag (tag.Tag, newTags)) {
				this.ReplaceEntry.StyleContext.AddClass ("entryWrong");
				return;
			}
			this.ReplaceEntry.StyleContext.RemoveClass ("entryWrong");

			this.ReplaceEntry.Text = "";
			this.SelectTag (newTags[0]);
		}

		void on_ImpliesEntry_activate(object sender, EventArgs args) 
		{
			if (this.activeTag == null)
				return;

			if (BooruApp.BooruApplication.Database.AddTagImplication (this.activeTag.Tag, this.ImpliesEntry.Text)) {
				this.impliesStore.AppendValues (this.ImpliesEntry.Text);
				this.ImpliesEntry.Text = "";
			}
		}

		void on_ImpliesTreeView_key_press_event(object sender, KeyPressEventArgs args)
		{
			if (this.activeTag == null)
				return;

			if (args.Event.Key == Gdk.Key.Delete) {
				TreePath path;
				TreeViewColumn column;
				TreeIter iter;

				this.ImpliesTreeView.GetCursor (out path, out column);

				if (this.ImpliesTreeView.Model.GetIter (out iter, path)) {
					var impliedTag = this.ImpliesTreeView.Model.GetValue (iter, 0) as string;
					BooruApp.BooruApplication.Database.RemoveTagImplication (this.activeTag.Tag, impliedTag);
				}
			}
		}

		void on_SearchButton_clicked(object sender, EventArgs args)
		{
			if (this.activeTag == null)
				return;
			
			BooruApp.BooruApplication.EventCenter.ExecuteImageSearch (this.activeTag.Tag);
		}

		public void GotoTag(String tag)
		{
			this.SelectTag (tag);
		}

		public void SelectTagAsMergeTarget(string tag)
		{
			this.ReplaceEntry.Text = tag;
		}

		public void on_FilterEntry_changed(object sender, EventArgs args)
		{
			this.filterString = FilterEntry.Text.Trim();

			if (string.IsNullOrWhiteSpace (this.filterString)) {
				this.TagTreeView.Model = this.allTagsStore;
			} else {
				this.TagTreeView.Model = this.filter;
				this.filter.Refilter ();
			}
		}
	}

}