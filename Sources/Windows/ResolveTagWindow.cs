using System;
using System.Collections.Generic;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru
{
	public class ResolveTagWindow : Gtk.Dialog
	{
		[UI] Gtk.TreeView TagTreeView;
		[UI] Gtk.Label WrongLabel;
		[UI] Gtk.Label CorrectLabel;

		public string Tag { get; private set; }

		private List<Database.SimilarTag> similarTags;

		public static ResolveTagWindow Create (string tag)
		{
			Builder builder = new Builder (null, "Booru.Resources.GUI.ResolveTagsWindow.glade", null);
			return new ResolveTagWindow (builder, builder.GetObject ("dialog1").Handle, tag);
		}

		protected ResolveTagWindow (Builder builder, IntPtr handle, string tag) : base(handle)
		{
			builder.Autoconnect (this);

			this.Tag = tag;
			WrongLabel.Text = tag;

			Gtk.CellRenderer tagRenderer = new CellRendererText ();
			Gtk.TreeViewColumn columnTag = new TreeViewColumn ("Tag", tagRenderer);
			columnTag.AddAttribute (tagRenderer, "text", 0);
			this.TagTreeView.AppendColumn (columnTag);

			Gtk.TreeViewColumn columnDistance = new TreeViewColumn ("Distance", tagRenderer);
			columnDistance.AddAttribute (tagRenderer, "text", 1);
			this.TagTreeView.AppendColumn (columnDistance);

			Gtk.ListStore store = new ListStore (typeof(string), typeof(double));

			this.similarTags = BooruApp.BooruApplication.Database.FindSimilarTags (tag, 5);
			foreach (var similarTag in this.similarTags) {
				store.AppendValues (similarTag.Tag, similarTag.Distance);
			}

			this.TagTreeView.Model = store;

			if (this.similarTags.Count > 1) {
				TagTreeView.Selection.SelectPath (new Gtk.TreePath ("1"));
				this.Tag = similarTags [1].Tag;
			}
			this.CorrectLabel.Text = this.Tag;

			this.TagTreeView.RowActivated += delegate(object o, RowActivatedArgs args) {
				int row = args.Path.Indices[0];
				this.Tag = this.similarTags [row].Tag;
				this.CorrectLabel.Text = this.Tag;
				this.on_OkButto_clicked(null, null);
			};
		}

		protected void on_OkButto_clicked(object sender, EventArgs args)
		{
			this.Respond (ResponseType.Ok);
			this.Hide ();
		}

		protected void on_CancelButton_clicked(object sender, EventArgs args)
		{
			this.Respond (ResponseType.Cancel);
			this.Hide ();
		}

		protected void on_TagTreeView_cursor_changed(object sender, EventArgs args)
		{
			var rows = this.TagTreeView.Selection.GetSelectedRows ();
			if (rows.Length > 0 && rows[0].Indices.Length > 0) {
				int row = rows [0].Indices [0];
				this.Tag = this.similarTags [row].Tag;
				this.CorrectLabel.Text = this.Tag;
			}
		}

		protected void on_TagTreeView_key_press_event(object sender, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Return) {
				this.on_OkButto_clicked (null, null);
			}
		}
	}
}

