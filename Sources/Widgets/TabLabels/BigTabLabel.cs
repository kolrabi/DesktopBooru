using System;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class BigTabLabel : LoadableWidget
	{
		[UI] 
		readonly Gtk.Label TextLabel;

		public static BigTabLabel Create (string title)
		{
			return LoadableWidget.Create<BigTabLabel> ().Init (title);
		}

		BigTabLabel (Gtk.Builder builder, IntPtr handle) : base (builder, handle)
		{
		}

		BigTabLabel Init(string title)
		{
			this.TextLabel.Text = title;
			return this;
		}
	}

}