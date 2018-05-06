using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Net;

namespace Booru
{
	public class PluginLoader
	{
		public readonly List<PluginInterface> LoadedPlugins = new List<PluginInterface>();
		public readonly TagFinderPluginInterface TagFinderPlugins = new AllTagFinderPlugins();

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
					Booru.BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, ex, "Caught exception while loading plugins from " + file);
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
				Booru.BooruApp.BooruApplication.Log.Log (BooruLog.Category.Application, BooruLog.Severity.Info, "Loaded plugin '"+plugin.Name+"'");
				LoadedPlugins.Add (plugin);
			}
		}
	}
}

