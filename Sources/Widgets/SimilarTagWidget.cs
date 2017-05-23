using System;
using Gtk;
using Cairo;
using System.Collections.Generic;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class SimilarTagWidget : LoadableWidget
	{
		public string Tag {
			get { return this.SelectButton.Label; }
			set { this.SelectButton.Label = value; }
		}

		[UI] readonly Gtk.Button SelectButton;

		TagListTab parent;

		public static SimilarTagWidget Create (TagListTab parentTab)
		{
			return LoadableWidget.Create<SimilarTagWidget> ().Init (parentTab);
		}

		SimilarTagWidget (Builder builder, IntPtr handle) : base (builder, handle)
		{
		}

		SimilarTagWidget Init(TagListTab parentTab)
		{
			this.parent = parentTab;
			return this;
		}

		void on_GotoButton_clicked(object sender, EventArgs args)
		{
			this.parent.GotoTag (this.Tag);
		}

		void on_SelectButton_clicked(object sender, EventArgs args)
		{
			this.parent.SelectTagAsMergeTarget (this.Tag);
		}

	}

}