using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AcoustID;
using AcoustID.Web;
using Fingerprinter.Audio;
using System.Diagnostics;
using System.Globalization;
using System.IO;


namespace RockBox
{


    /// <summary>
    /// Interaction logic for TagDialog.xaml
    /// </summary>
    public partial class TagDialog : Window
    {

        IAudioDecoder decoder;
        Database.Song _row;
        int _index;
        string _fingerprint;

        bool _canFingerprint = true;

        public TagDialog()
        {
            InitializeComponent();
            decoder = new NAudioDecoder();
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        public void LoadFile(Database.Song sr, int index)
        {
            this._row = sr;
            this._index = index;

            string filename = sr.Path + "\\" + sr.Filename;

            if (System.IO.File.Exists(filename))
            {
                ResetAll();
                txtFile.Text = filename;
                decoder.Load(filename);

                int bits = decoder.SourceBitDepth;
                int channels = decoder.SourceChannels;

                if (decoder.Ready)
                {
                    lbAudio.Content = String.Format("{0}Hz, {1}bit{2}, {3}",
                        decoder.SourceSampleRate, bits, bits != 16 ? " (not supported)" : "",
                        channels == 2 ? "stereo" : (channels == 1 ? "mono" : "multi-channel"));

                    lbDuration.Content = decoder.Duration.ToString();

                    LoadTags(filename);

                    btnFingerprint.IsEnabled = true;
                }
                else
                {
                    lbAudio.Content = "Failed to load audio";
                    lbDuration.Content = String.Empty;
                }
            }
        }

        private void doFileOpen(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Filter = "MP3 files (*.mp3)|*.mp3|WAV files (*.wav)|*.wav";

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFile.Text = dlg.FileName;

                ResetAll();

                decoder.Load(dlg.FileName);

                int bits = decoder.SourceBitDepth;
                int channels = decoder.SourceChannels;

                if (decoder.Ready)
                {
                    lbAudio.Content = String.Format("{0}Hz, {1}bit{2}, {3}",
                        decoder.SourceSampleRate, bits, bits != 16 ? " (not supported)" : "",
                        channels == 2 ? "stereo" : (channels == 1 ? "mono" : "multi-channel"));

                    lbDuration.Content = decoder.Duration.ToString();

                    LoadTags(dlg.FileName);

                    btnFingerprint.IsEnabled = true;
                }
                else
                {
                    lbAudio.Content = "Failed to load audio";
                    lbDuration.Content = String.Empty;
                }
            }

        }

        private void LoadTags(string filename)
        {
            TagLib.File f = TagLib.File.Create(filename);
            TagLib.Tag t = f.Tag;

            txtArtist.Text = t.FirstAlbumArtist;
            txtAlbum.Text = t.Album;
            txtTitle.Text = t.Title;
            txtYear.Text = t.Year.ToString();
            txtGenre.Text = t.FirstGenre;
            txtTrack.Text = t.Track.ToString();
            txtComments.Text = t.Comment;
        }

        private void ResetAll()
        {
            _canFingerprint = false;
            lvRecordings.Items.Clear();
            lvResults.Items.Clear();
            ClearText();
            _canFingerprint = true;
        }

        private void doFingerprint(object sender, RoutedEventArgs e)
        {
            if (File.Exists(txtFile.Text))
            {
                if (decoder.Ready)
                {
                    //btnOpen.Enabled = false;
                    btnFingerprint.IsEnabled = false;


                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    ChromaContext context = new ChromaContext();
                    context.Start(decoder.SampleRate, decoder.Channels);
                    decoder.Decode(context.Consumer, 120);
                    context.Finish();

                    stopwatch.Stop();

                    ProcessFileCallback(context.GetFingerprint(), stopwatch.ElapsedMilliseconds);
                }
            }
        }

        private void ProcessFileCallback(string fingerprint, long time)
        {
            this._fingerprint = fingerprint;

            lbBenchmark.Content = String.Format("Fingerprint length: {0} (calculated in {1}ms)", fingerprint.Length, time);

            // Release audio file handle.
            decoder.Dispose();

            if (String.IsNullOrEmpty(AcoustID.Configuration.ApiKey))
            {
                // TODO: display a prompt for api key.
                AcoustID.Configuration.ApiKey = "8XaBELgH";
            }

            Lookup(this._fingerprint, Convert.ToInt32(lbDuration.Content));

        }

        private void Lookup(string fingerprint, int duration)
        {

            btnFingerprint.IsEnabled = false;

            LookupService service = new LookupService();

            service.GetAsync((results, e) =>
            {
                btnOpen.IsEnabled = true;

                if (e != null)
                {
                    System.Windows.MessageBox.Show(e.Message, "Webservice error");
                    return;
                }

                if (results.Count == 0)
                {
                    if (String.IsNullOrEmpty(service.Error))
                    {
                        System.Windows.MessageBox.Show("No results for given fingerprint.");
                    }
                    else System.Windows.MessageBox.Show(service.Error, "Webservice error");

                    return;
                }

                foreach (var result in results)
                {
                    var li = new AcoustIdItem();
                    li.AcoustId = result.Id;
                    li.Score = result.Score.ToString();
                    li.Recordings = result.Recordings;

                    lvResults.Items.Add(li);

                }
            }, fingerprint, duration, new string[] { "recordings", "compress" });
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void lvResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_canFingerprint)
            {
                AcoustIdItem selection = lvResults.SelectedItem as AcoustIdItem;
                List<Recording> recordings = selection.Recordings;

                _canFingerprint = false;
                lvRecordings.Items.Clear();
                _canFingerprint = true;

                foreach (Recording record in recordings)
                {
                    string artist = String.Empty;

                    int count = record.Artists.Count;

                    if (count > 0)
                    {
                        artist = record.Artists[0].Name;

                        if (count > 1)
                        {
                            artist += (" (" + (count - 1) + " more)");
                        }
                    }

                    RecordingItem a = new RecordingItem(record.Id, record.Title, record.Duration.ToString(), artist);
                    lvRecordings.Items.Add(a);

                }
            }
        }

        private void lvRecordings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_canFingerprint)
            {
                //ClearText();

                string _lastFmQueryAddress = RockBox.Properties.Resources.LastFmQueryAddress + "?";
                string _lastFmApiKey = RockBox.Properties.Resources.LastFmApiKey;

                LastFmApi.Session s = new LastFmApi.Session(_lastFmApiKey, _lastFmQueryAddress);
                RecordingItem li = lvRecordings.SelectedItem as RecordingItem;


                // we kinda have to do it this way because last.fm returns slightly different schema
                // depending on what data it has (Tags being a name value pair as opposed to a list of pairs, for example)
                try
                {
                    LastFmApi.Query.Track.GetInfo q = new LastFmApi.Query.Track.GetInfo(li.Artist, li.Title);
                    LastFmApi.DataTypes.Track.RootObject o = q.Get(s);
                    ProcessTrack(o);
                }
                catch (Exception ex)
                {
                    LastFmApi.Query.Track2.GetInfo q = new LastFmApi.Query.Track2.GetInfo(li.Artist, li.Title);
                    LastFmApi.DataTypes.Track2.RootObject o = q.Get(s);
                    ProcessTrack2(o);
                }


            }
        }

        private void ProcessTrack2(LastFmApi.DataTypes.Track2.RootObject o)
        {
            if (o != null && o.track != null)
            {
                if (o.track.album != null)
                {
                    txtAlbum.Text = o.track.album.title == null ? "" : o.track.album.title;

                    txtTrack.Text = o.track.album.attr.position == null ? "" : o.track.album.attr.position;

                    if (o.track.album.image.Count > 0)
                    {
                        ImageHelper.DownloadFile(o.track.album.image[0].text, "temp.jpg");
                        BitmapImage b = new BitmapImage();
                        b.BeginInit();
                        b.UriSource = new Uri("temp.jpg", UriKind.Relative);
                        b.CacheOption = BitmapCacheOption.OnLoad;
                        b.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        b.EndInit();

                        imgArt.Source = b;
                        imgArt.UpdateLayout();
                    }
                }

                if (o.track.artist != null)
                {
                    txtArtist.Text = o.track.artist.name == null ? "" : o.track.artist.name;
                }

                if (o.track.wiki != null)
                {
                    txtComments.Text = o.track.wiki.summary == null ? "" : o.track.wiki.summary;
                }

                txtTitle.Text = o.track.name == null ? "" : o.track.name;

            }
            else
            {
                MessageBox.Show("No match.");
            }
        }

        private void ProcessTrack(LastFmApi.DataTypes.Track.RootObject o)
        {
            if (o != null && o.track != null)
            {

                if (o.track.album != null)
                {
                    txtAlbum.Text = o.track.album.title == null ? "" : o.track.album.title;

                    txtTrack.Text = o.track.album.attr.position == null ? "" : o.track.album.attr.position;

                    if (o.track.album.image.Count > 0)
                    {
                        ImageHelper.DownloadFile(o.track.album.image[0].text, "temp.jpg");
                        BitmapImage b = new BitmapImage();
                        b.BeginInit();
                        b.UriSource = new Uri("temp.jpg", UriKind.Relative);
                        b.CacheOption = BitmapCacheOption.OnLoad;
                        b.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        b.EndInit();

                        imgArt.Source = b;
                    }
                }

                if (o.track.artist != null)
                {
                    txtArtist.Text = o.track.artist.name == null ? "" : o.track.artist.name;
                }

                if (o.track.wiki != null)
                {
                    txtComments.Text = o.track.wiki.summary == null ? "" : o.track.wiki.summary;
                }

                txtTitle.Text = o.track.name == null ? "" : o.track.name;

            }
            else
            {
                MessageBox.Show("No match.");
            }
        }

        private void ClearText()
        {
            txtAlbum.Clear();
            txtArtist.Clear();
            txtComments.Clear();
            txtGenre.Clear();
            txtTitle.Clear();
            txtTrack.Clear();
            txtYear.Clear();
        }

        private void btnSaveClick(object sender, RoutedEventArgs e)
        {
            this._row.Album = txtAlbum.Text;
            this._row.Artist = txtArtist.Text;
            this._row.Comments = txtComments.Text;
            this._row.Genre = txtGenre.Text;
            this._row.Title = txtTitle.Text;
            this._row.Track = txtTrack.Text;
            this._row.Year = txtYear.Text;

            MainWindow owner = this.Owner as MainWindow;

            Database.Song sr = owner.AudioEngine.Datastore.Songs.FindById(this._row.Id);
            if (sr != null)
            {
                sr.Album = this._row.Album;
                sr.Artist = this._row.Artist;
                sr.Comments = this._row.Comments;
                sr.Genre = this._row.Genre;
                sr.Title = this._row.Title;
                sr.Track = this._row.Track;
                sr.Year = this._row.Year;
            }

            owner.AudioEngine.Playlist.RemoveAt(this._index);
            owner.AudioEngine.Playlist.Insert(this._index, sr);

            owner.Playlist.DataContext = owner.AudioEngine.Playlist;

        }
    }

    public class RecordingItem
    {
        public RecordingItem(string id, string title, string duration, string artist)
        {
            this.Id = id;
            this.Title = title;
            this.Duration = duration;
            this.Artist = artist;
        }

        public string Id
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string Duration
        {
            get;
            set;
        }

        public string Artist
        {
            get;
            set;
        }
    }

    public class AcoustIdItem
    {
        public string AcoustId
        {
            get;
            set;
        }

        public string Score
        {
            get;
            set;
        }

        public List<Recording> Recordings
        {
            get;
            set;
        }
    }
}
