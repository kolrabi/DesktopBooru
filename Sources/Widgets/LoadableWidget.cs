using System;
using System.Reflection;

namespace Booru
{
	public class LoadableWidget : Gtk.Container
	{
		protected LoadableWidget(Gtk.Builder builder, IntPtr handle) : base(handle)
		{
			builder.Autoconnect (this);
		}

		protected static T Create<T>()
		{
			var type = typeof(T);

			var resourceName = "Booru.Resources.GUI." + type.Name + ".glade";
			var widgetName = type.Name;

			var ctor = type.GetConstructor ( 
				BindingFlags.NonPublic | BindingFlags.Instance, 
				Type.DefaultBinder, 
				new Type[] { typeof(Gtk.Builder), typeof(IntPtr) },
				null
			);

			var builder = new Gtk.Builder (resourceName);
			var handle = builder.GetObject (widgetName).Handle;

			var widget  = (T)ctor.Invoke(new object[] { builder, handle } );
			return widget;
		}
	}
}

