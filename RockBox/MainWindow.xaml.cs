using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RockBox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        AudioEngine audioEngine;
        StringWriter ConsoleWriter;
        //WARNFIX
        //BitmapImage DefaultImage;
        ErrorConsole wndErrorConsole;
        System.Windows.Forms.Timer timer1;
        bool bUpdatingTrackbar = false;
        About wndAbout;
        Playlist wndPlaylist;
        Library wndLibrary;
        TagEditor wndTagEditor;
        TagDialog wndTagDialog;
        LibraryManager wndLibraryManager;
        bool init = true;


        public MainWindow()
        {
            audioEngine = new AudioEngine();
            audioEngine.ImageLoad += AudioEngine_ImageLoad;
            audioEngine.TagLoad += AudioEngine_TagLoad;
            InitializeComponent();
            wndErrorConsole = new ErrorConsole();
            this.ConsoleWriter = new StringWriter();
            Console.SetOut(this.ConsoleWriter);
            Console.SetError(this.ConsoleWriter);

            this.btn_close.Visibility = Visibility.Visible;

            // sets up a timer which is needed for updating the trackbar.
            this.timer1 = new System.Windows.Forms.Timer();
            this.timer1.Enabled = true;
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);

            //wireup clock
            spectrumAnalyzer.Visibility = System.Windows.Visibility.Visible;
            clockDisplay.Visibility = System.Windows.Visibility.Visible;
            this.AudioEngine.Engine.PropertyChanged += AudioEngine_PropertyChanged;
            this.AudioEngine.TrackChange += AudioEngine_TrackChange;
            spectrumAnalyzer.RegisterSoundPlayer(AudioEngine.Engine);

            init = false;
        }

        #region Properties

        public AudioEngine AudioEngine
        {
            get
            {
                return audioEngine;
            }
        }

        /// <summary>
        /// Gets the Library window reference, instantiates a copy if null.
        /// </summary>
        public Library Library
        {
            get
            {
                if (this.wndLibrary == null)
                {
                    this.wndLibrary = new Library();
                    this.wndLibrary.Owner = this;
                }
                return this.wndLibrary;
            }
        }

        public About About
        {
            get
            {
                if (this.wndAbout == null)
                {
                    this.wndAbout = new About();
                    this.wndAbout.Owner = this;
                }

                return this.wndAbout;
            }
        }


        /// <summary>
        /// Gets the LibraryManager window reference, instantiates a copy if null.
        /// </summary>
        public LibraryManager LibraryManager
        {
            get
            {
                if (this.wndLibraryManager == null)
                {
                    this.wndLibraryManager = new LibraryManager();
                    this.wndLibraryManager.Owner = this;
                }

                return this.wndLibraryManager;
            }
        }

        /// <summary>
        /// Gets the Playlist window reference, instantiates a copy if null.
        /// </summary>
        public Playlist Playlist
        {
            get
            {
                if (this.wndPlaylist == null)
                {
                    this.wndPlaylist = new Playlist();
                    this.wndPlaylist.Owner = this;
                }
                return this.wndPlaylist;
            }
        }

        /// <summary>
        /// Gets the TagEditor window reference, instantiates a copy if null.
        /// </summary>
        public TagEditor TagEditor
        {
            get
            {
                if (this.wndTagEditor == null)
                {
                    this.wndTagEditor = new TagEditor();
                    this.wndTagEditor.Owner = this;
                }

                return this.wndTagEditor;
            }
        }

        /// <summary>
        /// Gets the TagDialog window reference, instantiates a copy if null.
        /// </summary>
        public TagDialog TagDialog
        {
            get
            {
                if (this.wndTagDialog == null)
                {
                    this.wndTagDialog = new TagDialog();
                    this.wndTagDialog.Owner = this;
                }

                return this.wndTagDialog;
            }
        }

        #endregion

        #region Event Handlers

        private void AudioEngine_TrackChange(AudioEngine sender, AudioEngine.TrackChangeData e)
        {
            Playlist.SelectedIndex = e.index;
        }

        private void AudioEngine_TagLoad(AudioEngine sender, AudioEngine.TagData e)
        {
            tb_Artist.Text = e.artist;
            tb_Title.Text = e.title;
            tb_TrackAlbum.Text = e.trackalbum;
            tb_Year.Text = e.year;
        }

        private void AudioEngine_ImageLoad(AudioEngine sender, AudioEngine.ImageData e)
        {
            imgAlbumArt.Source = e.Image;
        }

        /// <summary>
        /// Handles the timer1.Tick event. Allows for the trackbar to function correctly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {

            if (this.wndErrorConsole.IsVisible)
            {
                this.wndErrorConsole.ConsoleText = ConsoleWriter.ToString();

            }

            if (audioEngine != null)
            {
                if (audioEngine.IsPlaying)
                {
                    sl_trackbar.Maximum = audioEngine.ChannelLength;
                    if (!this.bUpdatingTrackbar)
                    {
                        sl_trackbar.Value = audioEngine.ChannelPosition;
                    }
                }
            }
            else
            {
                sl_trackbar.Value = 0;
            }
        }

        /// <summary>
        /// Moves the window and any attached windows.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            this.LocationChanged += MainWindow_LocationChanged;
            this.DragMove();
        }

        /// <summary>
        /// Updates the location of attached windows relative to MainWindow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (this.wndPlaylist != null)
            {
                UpdatePlaylistWindowPosition();
            }
            if (this.wndLibrary != null)
            {
                UpdateLibraryWindowPosition();
            }
        }

        /// <summary>
        /// Updates the library window position relative to MainWindow.
        /// </summary>
        private void UpdateLibraryWindowPosition()
        {
            this.Library.Left = this.Left + this.ActualWidth + 10;
            this.Library.Top = this.Top;
        }

        /// <summary>
        /// Updates the playlist window position relative to MainWindow.
        /// </summary>
        private void UpdatePlaylistWindowPosition()
        {
            this.Playlist.Left = this.Left;
            this.Playlist.Top = this.Top + this.Height + 10;
        }

        /// <summary>
        /// Toggles the visibilty of the About window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_ShowAbout(object sender, RoutedEventArgs e)
        {
            if (this.About.IsVisible)
            {
                this.About.Hide();
            }
            else
            {
                this.About.Show();
            }
        }


        /// <summary>
        /// Toggles the visibilty of the Library window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void Menu_ShowLibrary(object sender, RoutedEventArgs e)
        {
            UpdateLibraryWindowPosition();
            if (this.Library.IsVisible)
            {
                this.Library.Hide();
            }
            else
            {
                this.Library.Show();
            }
        }

        /// <summary>
        /// Toggles the visibility of the playlist window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void Menu_ShowPlaylist(object sender, RoutedEventArgs e)
        {
            UpdatePlaylistWindowPosition();
            if (this.Playlist.IsVisible)
            {
                this.Playlist.Hide();
            }
            else
            {
                this.Playlist.Show();
            }
        }

        /// <summary>
        /// Toggles the visibility of the tagdialog window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void Menu_ShowTagDialog(object sender, RoutedEventArgs e)
        {
            TagDialog t = this.TagDialog;
            t.Owner = this;

            if (t.IsVisible)
            {
                t.Hide();
            }
            else
            {
                t.Show();
            }

        }

        internal void Menu_ShowLibraryManager(object sender, RoutedEventArgs e)
        {
            LibraryManager t = this.LibraryManager;
            t.Owner = this;

            if (t.IsVisible)
            {
                t.Hide();
            }
            else
            {
                t.Show();
            }
        }

        internal void Menu_ShowTagEditor(object sender, RoutedEventArgs e)
        {
            TagEditor t = this.TagEditor;
            t.Owner = this;

            if (t.IsVisible)
            {
                t.Hide();
            }
            else
            {
                t.Show();
            }
        }

        internal void Menu_ShowConsole(object sender, RoutedEventArgs e)
        {
            wndErrorConsole.Show();
        }

        private void Menu_ShowDBFixer(object sender, RoutedEventArgs e)
        {

        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            if (wndPlaylist != null)
            {
                this.wndPlaylist.Close();
            }

            if (wndLibrary != null)
            {
                this.wndLibrary.Close();
            }

            this.audioEngine.Close();

            if (wndErrorConsole != null)
            {
                this.wndErrorConsole.Close();
            }

            this.Close();
        }

        private void sl_trackbar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            audioEngine.ChannelPosition = Convert.ToInt32(sl_trackbar.Value);
            audioEngine.SampleReset = audioEngine.Position;
            this.bUpdatingTrackbar = false;
        }

        private void sl_trackbar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.bUpdatingTrackbar = true;
        }

        private void btn_eject_Click(object sender, RoutedEventArgs e)
        {
            this.Menu_ShowPlaylist(sender, e);

        }

        private void btn_forward_Click(object sender, RoutedEventArgs e)
        {
            this.audioEngine.PlayNext();
        }

        private void btn_pause_Click(object sender, RoutedEventArgs e)
        {
            this.audioEngine.Pause();
        }

        private void btn_play_Click(object sender, RoutedEventArgs e)
        {
            this.audioEngine.Play();
        }

        private void btn_previous_Click(object sender, RoutedEventArgs e)
        {
            this.audioEngine.PlayPrevious();
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            this.audioEngine.Stop();
        }

        private void btn_repeat_Click(object sender, RoutedEventArgs e)
        {
            this.audioEngine.Repeat = !this.audioEngine.Repeat;
        }

        private void btn_shuffle_Click(object sender, RoutedEventArgs e)
        {
            this.audioEngine.Shuffle();
        }

        private void sl_volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // this event is fireed as a part of InitializeComponent();
            // audioEngine will not be bootstrapped yet, so ignore in this case.
            if (!init)
            {
                this.audioEngine.Volume = (float)(e.NewValue / 100);
            }
        }

        private void AudioEngine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ChannelPosition":
                    clockDisplay.Time = TimeSpan.FromSeconds(audioEngine.ChannelPosition);
                    break;

                default:
                    // Do Nothing
                    break;
            }
        }

        #endregion

        #region Methods

        internal void DetachTagEditor()
        {
            this.wndTagEditor = null;
        }

        internal void DetachLibraryManager()
        {
            this.wndLibraryManager = null;
        }


        #endregion

    }
}
