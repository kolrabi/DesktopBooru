using System;
using System.Collections.Generic;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class ConfigTab : LoadableWidget
	{
		public static ConfigTab Create ()
		{
			return LoadableWidget.Create<ConfigTab> ();
		}

		ConfigTab (Gtk.Builder builder, IntPtr handle) : base (builder, handle)
		{
			this.ConnectAllChildren (this);
			this.ShowAll ();

			this.Sensitive = false;

			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted   += OnDatabaseUnloaded;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadFailed    += OnDatabaseUnloaded;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += OnDatabaseLoaded;
		}

		void OnDatabaseLoaded()
		{
			this.Sensitive = true;
		}

		void OnDatabaseUnloaded()
		{
			this.Sensitive = false;
		}

		void ConnectChild(Gtk.Widget widget)
		{
			if (widget == null)
				return;

			string widgetName = widget.Name;

			if (widget is Gtk.Label && !string.IsNullOrEmpty (widgetName)) {
				var widgetNameParts = widgetName.Split (":".ToCharArray (), 2);

				if (widgetNameParts.Length != 2)
					return;

				Gtk.Widget valueWidget = null;
				Gtk.Grid grid = widget.Parent as Gtk.Grid;

				var leftAttach = (int)grid.ChildGetProperty (widget, "left-attach").Val;
				var topAttach = (int)grid.ChildGetProperty (widget, "top-attach").Val;
				var width = 4;

				if (widgetNameParts [0] == "path") {
					valueWidget = ConfigValueFolderWidget.Create (widgetNameParts [1]);
				} else if (widgetNameParts [0] == "string") {
					valueWidget = ConfigValueStringWidget.Create (widgetNameParts [1]);
				} else if (widgetNameParts [0] == "int") {
					widgetNameParts = widgetName.Split (":".ToCharArray (), 4);
					valueWidget = ConfigValueIntWidget.Create (widgetNameParts [3], int.Parse(widgetNameParts[1]), int.Parse(widgetNameParts[2]));
				} else if (widgetNameParts [0] == "bool") {
					valueWidget = ConfigValueBoolWidget.Create (widgetNameParts [1]);
					valueWidget.Expand = false;
					width = 1;
				}

				if (valueWidget != null) {
					valueWidget.TooltipMarkup = widget.TooltipMarkup;
					grid.AttachNextTo (valueWidget, widget, Gtk.PositionType.Right, width, 1);
				}
			}

			if (widget is Gtk.Container)
				this.ConnectAllChildren (widget as Gtk.Container);
		}

		void ConnectAllChildren(Gtk.Container container) {
			foreach (var child in container.AllChildren) {
				this.ConnectChild (child as Gtk.Widget);
			}
		}
	}

}