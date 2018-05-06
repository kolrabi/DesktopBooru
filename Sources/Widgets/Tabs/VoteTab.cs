using System;
using Gtk;
using Cairo;
using System.Collections.Generic;
using UI = Gtk.Builder.ObjectAttribute;

namespace Booru {

	public sealed class VoteTab : LoadableWidget
	{
		[UI] private Gtk.ButtonBox CenterButtonBox;
		[UI] private Gtk.Switch ShowTagsSwitch;
		[UI] private Gtk.ComboBoxText ImageTypeCombo;
		[UI] private Gtk.Spinner LoadSpinner;
		[UI] private Gtk.Box VoteWidgetOld;
		[UI] private Gtk.Box NonFullscreenBox;

		private int nLoadingImages = 0;

		ImageVoteWidget 	LeftImage;
		ImageVoteWidget 	RightImage;

		RandomImageLoader 	ImageLoader;

		public static VoteTab Create ()
		{
			return LoadableWidget.Create<VoteTab> ();
		}

		VoteTab (Builder builder, IntPtr handle) : base (builder, handle)
		{
			this.ImageLoader = new RandomImageLoader ();

			// create voting widgets
			LeftImage = ImageVoteWidget.Create ();
			LeftImage.OnSkipImage += Skip;
			LeftImage.OnImageWinner += Winner;

			RightImage = ImageVoteWidget.Create ();
			RightImage.OnSkipImage += Skip;
			RightImage.OnImageWinner += Winner;

			// reorder things
			this.VoteWidgetOld.Remove (this.CenterButtonBox);
			this.VoteWidgetOld.PackStart (LeftImage, true, true, 0);
			this.VoteWidgetOld.PackStart (this.CenterButtonBox, false, false, 0);
			this.VoteWidgetOld.PackStart (RightImage, true, true, 0);

			// be insensitive until database is loaded
			this.Sensitive = false;

			BooruApp.BooruApplication.EventCenter.DatabaseLoadStarted += OnDatabaseLoadStarted;
			BooruApp.BooruApplication.EventCenter.DatabaseLoadSucceeded += OnDatabaseLoadSucceeded;
			BooruApp.BooruApplication.EventCenter.FullscreenToggled += OnFullscreenToggled;

			// filter image types
			foreach (var e in Enum.GetValues(typeof(BooruImageType))) {
				this.ImageTypeCombo.Append (e.ToString (), e.ToString ());
			}
			this.ImageTypeCombo.ActiveId = BooruImageType.Image.ToString ();

			this.ImageTypeCombo.Changed += (sender, e) => {
				this.ImageLoader.SetFilter((BooruImageType)Enum.Parse(typeof(BooruImageType), this.ImageTypeCombo.ActiveId));
				this.Skip(this.LeftImage);
				this.Skip(this.RightImage);
			};

		}

		void OnDatabaseLoadStarted()
		{
			this.Sensitive = false;
		}

		void OnDatabaseLoadSucceeded()
		{
			// start loading images, enable sensitivity when done
			this.LoadNextImages ();
			this.Sensitive = true;
		}

		void BeginLoading(ImageVoteWidget widget)
		{
			if (this.nLoadingImages == 0) {
				// prevent interaction
				this.Sensitive = false;
			}

			widget.IsPaused = true;

			this.nLoadingImages++;
			this.LoadSpinner.Visible = true;
		}

		void FinishLoading()
		{
			this.nLoadingImages--;
			if (this.nLoadingImages == 0) {
				// check for identical images and skip one of them
				if (this.LeftImage.Image != null && this.RightImage != null && this.LeftImage.Image.Details.MD5 == this.RightImage.Image.Details.MD5) {
					this.Skip (this.RightImage);
					return;
				}

				this.LeftImage.Opponent = this.RightImage.Image;
				this.RightImage.Opponent  = this.LeftImage.Image;

				// enable interaction
				this.Sensitive = true;

				// resume animations
				this.LeftImage.IsPaused = false;
				this.RightImage.IsPaused = false;

				this.LoadSpinner.Visible = false;
			}
		}

		bool isLoading { get { return this.nLoadingImages > 0; } }

		// mark the content of one widget as the winner over the other one
		public void Winner(ImageVoteWidget winnerWidget)
		{
			if (winnerWidget == LeftImage) {
				LeftImage.Image.Win (RightImage.Image);
			} else {
				RightImage.Image.Win (LeftImage.Image);
			}

			this.LeftImage.Opponent = null;
			this.RightImage.Opponent = null;

			this.LoadNextImages ();
		}

		// load next image for widget, without voting
		public void Skip(ImageVoteWidget widget)
		{
			LoadNextImage (widget);
		}

		// create a task for loading the next image
		System.Threading.Tasks.Task LoadNextImage(ImageVoteWidget widget)
		{
			this.BeginLoading (widget);

			var task = new System.Threading.Tasks.Task(async () => {
				// get images until it is a valid image
				Image image = null;
				while(image == null) {
					image = await this.ImageLoader.NextImage ();
				}
					
				System.Threading.SpinWait.SpinUntil(() => !widget.IsFading);

				// update image in gui thread
				BooruApp.BooruApplication.TaskRunner.StartTaskMainThread(()=> { 
					widget.Image = image; 
					image.Release();
					this.FinishLoading ();
				});
			});

			task.Start ();
			return task;
		}

		private async void LoadNextImages()
		{
			this.BeginLoading (this.LeftImage);
			this.BeginLoading (this.RightImage);

			var taskLeft = LoadNextImage (this.LeftImage);
			var taskRight = LoadNextImage (this.RightImage);

			await taskLeft;
			await taskRight;

			this.FinishLoading ();
			this.FinishLoading ();
		}
			
		void on_SkipButton_clicked(object sender, EventArgs args)
		{
			this.Skip (this.LeftImage);
			this.Skip (this.RightImage);
		}
			
		void on_ShowTagsSwitch_state_changed(object o, StateChangedArgs args)
		{
			this.LeftImage.IsOverlayActive = this.ShowTagsSwitch.Active;
			this.RightImage.IsOverlayActive = this.ShowTagsSwitch.Active;
		}

		void on_FullscreenButton_clicked(object sender, EventArgs args)
		{
			BooruApp.BooruApplication.MainWindow.ToggleFullscreen ();
		}

		void OnFullscreenToggled(bool isFullscreen)
		{
			bool showStuff = !isFullscreen;
			this.NonFullscreenBox.Visible = showStuff;
			this.Margin = showStuff ? 32 : 0;
		}
	}

}