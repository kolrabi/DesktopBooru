using System;
using System.Collections.Generic;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class ImagesTab : LoadableWidget
	{
		[UI] 
		readonly Gtk.Entry TagEntry;

		[UI] 
		readonly Gtk.Notebook ResultsNotebook;

		[UI] 
		readonly Gtk.Box NonFullscreenBox;

		public static ImagesTab Create ()
		{
			return LoadableWidget.Create<ImagesTab> ();
		}

		ImagesTab (Gtk.Builder builder, IntPtr handle) : base (builder, handle)
		{
			this.Sensitive = false;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted += this.OnDatabaseLoadStarted;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += this.OnDatabaseLoadSucceeded;

			var completion = new Gtk.EntryCompletion ();
			completion.Model = BooruApp.BooruApplication.Database.TagEntryCompletionStore;
			completion.TextColumn = 0;
			completion.MinimumKeyLength = 3;

			this.TagEntry.Completion = completion;

			BooruApp.BooruApplication.EventCenter.ImageSearchRequested += this.ExecuteSearch;
			BooruApp.BooruApplication.EventCenter.FullscreenToggled += this.ToggleFullscreen;
		}

		void OnDatabaseLoadStarted()
		{
			this.Sensitive = false;
		}

		void OnDatabaseLoadSucceeded()
		{
			this.Sensitive = true;
		}

		void on_DeletedItem_activate(object sender, EventArgs args)
		{
			this.TagEntry.Text = "deleteme deleteme #sort:updated";
		}

		void on_Top100Item_activate(object sender, EventArgs args)
		{
			this.TagEntry.Text = "#limit:100 #sort:score";
		}

		void on_TagMe100_activate(object sender, EventArgs args)
		{
			this.TagEntry.Text = "#limit:100 #sort:score #tags<6";
		}

		void on_TagEntry_activate(object sender, EventArgs args)
		{
			BooruApp.BooruApplication.EventCenter.ExecuteImageSearch (this.TagEntry.Text);
		}

		void ExecuteSearch(string tags)
		{
			string searchString = tags.Trim();
			if (string.IsNullOrEmpty (searchString))
				return;
			
			var tab = ImagesResultWidget.Create (searchString);
			var label = ClosableTabLabel.Create(searchString, this.ResultsNotebook, tab);

			int pageNum = this.ResultsNotebook.AppendPage (tab, label);
			this.ResultsNotebook.CurrentPage = pageNum;

			BooruApp.BooruApplication.EventCenter.Fullscreen(BooruApp.BooruApplication.MainWindow.IsFullscreen);
		}

		void ToggleFullscreen(bool isFullscreen)
		{
			bool showStuff = !isFullscreen || this.ResultsNotebook.NPages == 0;
			this.ResultsNotebook.ShowTabs = showStuff;
			this.NonFullscreenBox.Visible = showStuff;
			this.Margin = showStuff ? 32 : 0;
		}
	}

}