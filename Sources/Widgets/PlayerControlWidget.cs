using System;
using UI = Gtk.Builder.ObjectAttribute;
using System.Threading;

namespace Booru {

	public sealed class PlayerControlWidget : LoadableWidget
	{
		[UI] readonly Gtk.Button PlayButton;
		[UI] readonly Gtk.Button PauseButton;
		[UI] readonly Gtk.Scale PositionScale;
		[UI] readonly Gtk.VolumeButton VolumeButton;
		[UI] readonly Gtk.Label PositionLabel;

		public System.Diagnostics.Process PlayerPocess;

		Thread updateThread;

		bool isInUpdate = false;

		bool isPaused;
		float totalLength;
		float position;
		float volume;

		uint updateTimeout = 0;

		bool wantPause = false;

		public static PlayerControlWidget Create ()
		{
			return LoadableWidget.Create<PlayerControlWidget> ();
		}

		PlayerControlWidget (Gtk.Builder builder, IntPtr handle) : base (builder, handle)
		{
			this.PlayButton.Show ();
			this.PauseButton.Hide ();

			this.PlayButton.Clicked += (s,e) => { this.Pause(); };
			this.PauseButton.Clicked += (s,e) => { this.Pause(); };

			this.PositionScale.RestrictToFillLevel = false;
			this.PositionScale.ValueChanged += (sender, e) => {
				if (!this.isInUpdate) {
					this.PlayerPocess.StandardInput.WriteLine ("seek "+this.PositionScale.Value+" 2");
				}
			};

			this.VolumeButton.ValueChanged += (sender, e) => {
				if (!this.isInUpdate) {
					this.volume = (float)this.VolumeButton.Value;
					this.PlayerPocess.StandardInput.WriteLine ("volume "+this.volume*100+" 1");
				}
			};

			BooruApp.BooruApplication.EventCenter.WillQuit += OnWillQuit;

			this.updateThread = new Thread (new ThreadStart (UpdateThreadProc));
			this.updateThread.Start ();

			this.updateTimeout = GLib.Timeout.Add(200, UpdateControls);
		}

		void OnWillQuit ()
		{
			this.updateThread.Abort ();
			GLib.Timeout.Remove (this.updateTimeout);
		}

		void UpdateThreadProc()
		{
			while(true) {
				var proc = this.PlayerPocess;
				if (proc == null) {
					Thread.Sleep (10);
					continue;
				} else {
					string str = proc.StandardOutput.ReadLine();
					if (str != null && str.StartsWith("ANS_")) {
						string[] pair = str.Substring (4).Split ("=".ToCharArray (), 2);
						this.HandleProperty (pair [0], pair [1]);
					}
				}
			}
		}

		bool UpdateControls()
		{
			this.isInUpdate = true;
			if (this.PlayerPocess != null) {
				this.Visible = true;

				if (this.wantPause) {
					this.PlayerPocess.StandardInput.WriteLine ("pause");
					this.wantPause = false;
				}
				this.PlayerPocess.StandardInput.WriteLine ("get_property pause");
				this.PlayerPocess.StandardInput.WriteLine ("get_property length");
				this.PlayerPocess.StandardInput.WriteLine ("get_property time_pos");
				this.PlayerPocess.StandardInput.WriteLine ("get_property volume");

				this.PlayButton.Visible = this.isPaused;
				this.PauseButton.Visible = !this.isPaused;

				this.PositionScale.SetRange (0, this.totalLength);
				this.PositionScale.Adjustment.Lower = 0;
				this.PositionScale.Adjustment.Upper = this.totalLength;
				this.PositionScale.Value = this.position;

				TimeSpan span = TimeSpan.FromSeconds (this.position);
				this.PositionLabel.Text = string.Format ("{0:hh\\:mm\\:ss}", span);
			} else {
				this.Visible = false;
			}
			this.isInUpdate = false;
			return true;
		}
			

		void HandleProperty(string name, string value)
		{
			if (name == "pause") {
				this.isPaused = value == "yes";
			} else if (name == "length") {
				this.totalLength = float.Parse (value);
			} else if (name == "time_pos") {
				this.position = float.Parse (value);
			} else if (name == "volume") {
				//this.volume = float.Parse (value);
			}
		}

		void Pause()
		{
			this.wantPause = true;
		}
	}

}