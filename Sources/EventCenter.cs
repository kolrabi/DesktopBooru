using System;

namespace Booru
{
	public class BooruEventCenter
	{
		public delegate void NoArgsEvent();
		public delegate void BoolArgsEvent(bool arg);
		public delegate bool NoArgsBoolResultEvent();
		public delegate void StringArgEvent(string arg);

		public event NoArgsBoolResultEvent CheckQuitOK;
		public event NoArgsEvent WillQuit;

		public event BoolArgsEvent FullscreenToggled;

		public event NoArgsEvent DatabaseLoadStarted;
		public event NoArgsEvent DatabaseLoadSucceeded;
		public event NoArgsEvent DatabaseLoadFailed;

		public event StringArgEvent ImageSearchRequested;

		public BooruEventCenter ()
		{
		}

		public void BeginChangeDatabase()
		{
			BooruApp.BooruApplication.TaskRunner.StartTaskMainThread(()=>{
				if (this.DatabaseLoadStarted != null)
					this.DatabaseLoadStarted();
			});
		}

		public void FinishChangeDatabase(bool success)
		{
			BooruApp.BooruApplication.TaskRunner.StartTaskMainThread(()=>{
				if (success) {
					// fire event
					if (this.DatabaseLoadSucceeded != null)
						this.DatabaseLoadSucceeded();
				} else {
					// show error message
					var dlg = new Gtk.MessageDialog(BooruApp.BooruApplication.MainWindow, Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, "Could not open database");
					dlg.Run();
					dlg.Destroy();

					// clear last used database
					BooruApp.BooruApplication.Settings.Set ("last_used_db", null);

					// fire event
					if (this.DatabaseLoadFailed != null)
						this.DatabaseLoadFailed();
				}
			});
		}

		public bool Quit()
		{
			if (this.CheckQuitOK != null) {
				if (!this.CheckQuitOK ())
					return false;
			}

			if (this.WillQuit != null) {
				this.WillQuit ();
			}
			return true;
		}

		public void ExecuteImageSearch(string tags)
		{
			if (this.ImageSearchRequested != null)
				this.ImageSearchRequested (tags);
		}

		public void Fullscreen(bool isFullscreen)
		{
			if (this.FullscreenToggled != null)
				this.FullscreenToggled (isFullscreen);
		}
	}
}

