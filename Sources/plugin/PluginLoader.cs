using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Net;

namespace Booru
{
	public class PluginLoader
	{
		public List<PluginInterface> LoadedPlugins = new List<PluginInterface>();

		public PluginLoader ()
		{
		}

		public void LoadPlugins()
		{
			var thisAsm = Assembly.GetExecutingAssembly ();
			var thisDir = Path.GetDirectoryName (new Uri(thisAsm.CodeBase).AbsolutePath);

			foreach (var file in Directory.EnumerateFiles (thisDir, "*.dll")) {
				try {
					Assembly fileAsm = Assembly.LoadFile(file);
					this.LoadPluginsFromAssembly(fileAsm);
				} catch(Exception ex) {
				}
			}
		}

		private void LoadPluginsFromAssembly(Assembly asm)
		{
			var asmTypes = asm.GetTypes ();
			foreach (var asmType in asmTypes) {
				var asmIface = asmType.GetInterface (typeof(PluginInterface).FullName);
				if (asmIface != null)
					LoadPlugin (asmType);
			}
		}

		private void LoadPlugin(Type type)
		{
			var ctor = type.GetConstructor (new Type[0]);
			if (ctor != null) {
				PluginInterface plugin = ctor.Invoke (new object[0]) as PluginInterface;
				plugin.OnLoad (BooruApp.BooruApplication);
				LoadedPlugins.Add (plugin);
			}
		}
	}
}

