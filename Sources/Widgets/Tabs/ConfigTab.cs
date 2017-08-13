using System;
using System.Collections.Generic;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class ConfigTab : LoadableWidget
	{
		[UI] 
		private Gtk.Grid ConfigGrid;

		public static ConfigTab Create ()
		{
			return LoadableWidget.Create<ConfigTab> ();
		}

		ConfigTab (Gtk.Builder builder, IntPtr handle) : base (builder, handle)
		{
			int n = 100;
			foreach (var plugin in BooruApp.BooruApplication.PluginLoader.LoadedPlugins) {
				Gtk.Label label = new Gtk.Label ("<b>"+plugin.Name+"</b>");
				label.SetAlignment (0.0f, 0.5f);
				label.UseMarkup = true;
				label.MarginTop = 32;
				ConfigGrid.Attach(label, 0, n, 5, 1);
				n++;

				Gtk.HSeparator sep = new Gtk.HSeparator ();
				sep.MarginBottom = 16;
				ConfigGrid.Attach(sep, 0, n, 5, 1);
				n++;

				label = new Gtk.Label (plugin.ConfigDesc);
				label.SetAlignment (0.0f, 0.5f);
				label.UseMarkup = true;
				ConfigGrid.Attach(label, 0, n, 5, 1);
				n++;

				foreach (var configDef in plugin.ConfigEntryDefinitions) {
					Gtk.Label configLabel = new Gtk.Label (configDef.Label);
					configLabel.Name = configDef.Name;
					configLabel.TooltipMarkup = configDef.Tooltip;
					configLabel.SetAlignment (0.0f, 0.5f);
					ConfigGrid.Attach(configLabel, 0, n, 1, 1);
					n++;
				}
			}

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