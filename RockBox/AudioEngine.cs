using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AcoustID;
using AcoustID.Web;
using Fingerprinter.Audio;

namespace RockBox
{

    public class AudioEngine
    {

        public BitmapImage DefaultImage;
        Database dbDataStore;
        Database.Song activeItem;
        NAudioEngine engine;
        ItemInformation iteminfo;
        bool bRepeat = false;
        int iSelectedIndex = -1;
        //WARNFIX
        //string title;
        //string album;
        //string artist;
        //string year;
        //string trackalbum;
        bool isPaused = false;
        float fVolume = 1.0f;
        public delegate void ImageLoadHandler(AudioEngine sender, ImageData e);
        public event ImageLoadHandler ImageLoad;
        public delegate void TagLoadHandler(AudioEngine sender, TagData e);
        public event TagLoadHandler TagLoad;
        Database.Playlist playlist = new Database.Playlist();
        Database.SongCollection playlistcopy = new Database.SongCollection();
        bool firstshuffle = true;
        public delegate void TrackChangeHandler(AudioEngine sender, TrackChangeData e);
        public event TrackChangeHandler TrackChange;
        static Random random = new Random();

        public AudioEngine()
        {
            engine = NAudioEngine.Instance;
            dbDataStore = new Database();
            dbDataStore.Deserialize();
            BitmapImage b = new BitmapImage();
            b.BeginInit();
            Uri uri = new System.Uri(RockBox.Properties.Resources.DefaultAlbumArtURI);
            b.UriSource = uri;
            b.EndInit();
            b.Freeze();
            this.DefaultImage = b;
            iteminfo = new ItemInformation("artist", "album", "title", "year", "00", DefaultImage);

        }


        #region FingerPrinting

        internal static void Fingerprint(Database.Song row)
        {
            NAudioDecoder decoder = new NAudioDecoder();
            string filename = row.Path + "\\" + row.Filename;

            if (System.IO.File.Exists(filename))
            {

                decoder.Load(filename);
                string duration;

                int bits = decoder.SourceBitDepth;
                int channels = decoder.SourceChannels;

                if (decoder.Ready)
                {
                    duration = decoder.Duration.ToString();

                    ChromaContext context = new ChromaContext();
                    context.Start(decoder.SampleRate, decoder.Channels);
                    decoder.Decode(context.Consumer, 120);
                    context.Finish();


                    // Release audio file handle.
                    decoder.Dispose();

                    if (String.IsNullOrEmpty(AcoustID.Configuration.ApiKey))
                    {
                        // TODO: display a prompt for api key.
                        AcoustID.Configuration.ApiKey = RockBox.Properties.Resources.AcoustIDApiKey;
                    }

                    LookupService service = new LookupService();
                    List<Recording> recs = new List<Recording>();
                    List<LookupResult> results = service.Get(context.GetFingerprint(), decoder.Duration, new string[] { "recordings", "compress" });

                    foreach (LookupResult res in results)
                    {
                        foreach (Recording rec in res.Recordings)
                        {
                            recs.Add(rec);
                        }
                    }

                    string _lastFmQueryAddress = RockBox.Properties.Resources.LastFmQueryAddress + "?";
                    string _lastFmApiKey = RockBox.Properties.Resources.LastFmApiKey;

                    LastFmApi.Session s = new LastFmApi.Session(_lastFmApiKey, _lastFmQueryAddress);

                    if (recs.Count > 0)
                    {
                        Recording li = recs[0];
                        string artist = "";
                        string title = "";

                        if (li.Artists.Count > 0)
                        {
                            artist = li.Artists[0].Name;
                        }

                        if (li.Title != null)
                        {
                            title = li.Title;
                        }

                        // we kinda have to do it this way because last.fm returns slightly different schema
                        // depending on what data it has (Tags being a name value pair as opposed to a list of pairs, for example)
                        try
                        {
                            LastFmApi.Query.Track.GetInfo q = new LastFmApi.Query.Track.GetInfo(artist, title);
                            LastFmApi.DataTypes.Track.RootObject o = q.Get(s);
                            ProcessTrack(o, row);
                            Console.WriteLine("Processing " + row.Filename);
                        }
                        catch (Exception e)
                        {
                            LastFmApi.Query.Track2.GetInfo q = new LastFmApi.Query.Track2.GetInfo(artist, title);
                            LastFmApi.DataTypes.Track2.RootObject o = q.Get(s);
                            ProcessTrack2(o, row);
                            Console.WriteLine("Processing " + row.Filename);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Couldn't find a recording for " + row.Filename);
                    }
                }
            }
        }

        internal static void Fingerprint(Database.SongCollection playlist)
        {

            foreach (Database.Song row in playlist)
            {
                AudioEngine.Fingerprint(row);
            }
        }

        private static void ProcessTrack2(LastFmApi.DataTypes.Track2.RootObject o, Database.Song row)
        {
            if (o != null && o.track != null)
            {
                if (o.track.album != null)
                {
                    row.Album = o.track.album.title == null ? "" : o.track.album.title;

                    row.Track = o.track.album.attr.position == null ? "" : o.track.album.attr.position;
                }

                if (o.track.artist != null)
                {
                    row.Artist = o.track.artist.name == null ? "" : o.track.artist.name;
                }

                if (o.track.wiki != null)
                {
                    row.Comments = o.track.wiki.summary == null ? "" : o.track.wiki.summary;
                }

                row.Title = o.track.name == null ? "" : o.track.name;

            }
        }

        private static void ProcessTrack(LastFmApi.DataTypes.Track.RootObject o, Database.Song row)
        {
            if (o != null && o.track != null)
            {

                if (o.track.album != null)
                {
                    row.Album = o.track.album.title == null ? "" : o.track.album.title;

                    row.Track = o.track.album.attr.position == null ? "" : o.track.album.attr.position;
                }

                if (o.track.artist != null)
                {
                    row.Artist = o.track.artist.name == null ? "" : o.track.artist.name;
                }

                if (o.track.wiki != null)
                {
                    row.Comments = o.track.wiki.summary == null ? "" : o.track.wiki.summary;
                }

                row.Title = o.track.name == null ? "" : o.track.name;

            }
        }

        #endregion

        #region Internal Classes

        public class TrackChangeData : EventArgs
        {
            public int index { get; set; }
        }

        public class TagData : EventArgs
        {
            public string artist { get; set; }
            public string trackalbum { get; set; }
            public string year { get; set; }
            public string title { get; set; }
        }

        public class ImageData : EventArgs
        {
            private BitmapImage image;
            public BitmapImage Image
            {
                get
                {
                    return this.image;
                }
                set
                {
                    this.image = value;
                }
            }

        }

        public class ItemInformation
        {
            string artist;
            string album;
            string title;
            string year;
            string albumtrack;
            BitmapImage albumart;

            public ItemInformation(string artist, string album, string title, string year, string albumtrack, BitmapImage albumart)
            {
                this.artist = artist;
                this.album = album;
                this.title = title;
                this.year = year;
                this.albumtrack = albumtrack;
                this.albumart = albumart;
            }

            #region Properties

            public string Artist
            {
                get
                {
                    return artist;
                }
            }

            public string Album
            {
                get
                {
                    return album;
                }
            }

            public string AlbumTrack
            {
                get
                {
                    return albumtrack;
                }
            }

            public BitmapImage AlbumArt
            {
                get
                {
                    return albumart;
                }
            }

            public string Title
            {
                get
                {
                    return title;
                }
            }

            public string Year
            {
                get
                {
                    return Year;
                }
            }
            #endregion
        }

        #endregion

        #region Properties

        internal NAudioEngine Engine
        {
            get
            {
                return engine;
            }
        }

        public Database.Song ActiveItem
        {
            get
            {
                return activeItem;
            }
        }

        public double ChannelLength
        {
            get
            {
                return engine.ChannelLength;
            }
        }

        public double ChannelPosition
        {
            get
            {
                return engine.ChannelPosition;
            }
            set
            {
                engine.ChannelPosition = value;
            }
        }

        public Database Datastore
        {
            get
            {
                return dbDataStore;
            }
        }

        public bool IsPaused
        {
            get { return isPaused; }
        }

        public bool IsPlaying
        {
            get
            {
                return engine.IsPlaying;
            }
        }

        public ItemInformation ItemInfo
        {
            get
            {
                return iteminfo;
            }
        }

        public Database.Playlist Playlist
        {
            get
            {
                return playlist;
            }
        }

        public long Position
        {
            get
            {
                return engine.ActiveStream.Position;
            }
        }

        public bool Repeat
        {
            get
            {
                return this.bRepeat;
            }
            set
            {
                bRepeat = value;
            }
        }

        public long SampleReset
        {
            set
            {
                engine.ActiveStream.Position = value;
            }
        }

        public int SelectedIndex
        {
            get
            {
                return iSelectedIndex;
            }
            set
            {
                iSelectedIndex = value;
            }
        }

        public float Volume
        {
            get
            {
                return engine.Volume;
            }
            set
            {
                fVolume = value;
                engine.Volume = value;
            }
        }

        #endregion

        #region Methods

        public void Shuffle()
        {
            if (firstshuffle)
            {
                firstshuffle = false;
                playlistcopy.Clear();

                List<KeyValuePair<int, Database.Song>> list = new List<KeyValuePair<int, Database.Song>>();
                foreach (Database.Song row in playlist)
                {
                    playlistcopy.Add(row);
                    list.Add(new KeyValuePair<int, Database.Song>(random.Next(), row));
                }

                var sorted = from item in list
                             orderby item.Key
                             select item;

                playlist.Clear();

                foreach (KeyValuePair<int, Database.Song> pair in sorted)
                {
                    playlist.Add(pair.Value);
                }

            }
            else
            {
                playlist.Clear();

                foreach (Database.Song row in playlistcopy)
                {
                    playlist.Add(row);
                }

                firstshuffle = true;
            }
        }

        public void Close()
        {
            dbDataStore.Serialize();
        }

        public void PlayNext()
        {
            if (Repeat)
            {
                TrackChangeData e = new TrackChangeData();
                e.index = SelectedIndex;
                TrackChange(this, e);
                PlayItem(SelectedIndex);
            }
            else
            {
                if (SelectedIndex + 1 == Playlist.Count)
                {
                    TrackChangeData e = new TrackChangeData();
                    e.index = 0;
                    TrackChange(this, e);
                    PlayItem(0);
                }
                else
                {
                    TrackChangeData e = new TrackChangeData();
                    e.index = SelectedIndex + 1;
                    TrackChange(this, e);
                    PlayItem(SelectedIndex + 1);
                }
            }
        }

        public void PlayItem(int index)
        {
            if (index > -1 && index < this.Playlist.Count)
            {
                iSelectedIndex = index;
                activeItem = Playlist[index];
                TrackChangeData e = new TrackChangeData();
                e.index = index;
                TrackChange(this, e);
                string file = activeItem.Path + "\\" + activeItem.Filename;
                Console.WriteLine("Trying to play file: " + file);

                engine.OpenFile(file);
                if (engine.CanPlay)
                {
                    TagLib.File f = engine.FileTag;
                    TagLib.Tag tag = f.Tag;

                    string ititle = activeItem.Title == null ? tag.Title : activeItem.Title;
                    string iartist = activeItem.Artist == null ? tag.AlbumArtists[0] : activeItem.Artist;
                    string ialbumtrack = "Track " + (activeItem.Track == null ? tag.Track.ToString() : activeItem.Track) + " on " + (activeItem.Album == null ? tag.Album : activeItem.Album);
                    string iyear = activeItem.Year == null ? tag.Year.ToString() : activeItem.Year;

                    if (iyear != "")
                    {
                        iyear = "released in " + iyear;
                    }

                    ImageData id = new ImageData();
                    id.Image = GetAlbumArt(activeItem.Artist, activeItem.Album);
                    ImageLoad(this, id);
                    TagData td = new TagData();
                    td.artist = iartist;
                    td.title = ititle;
                    td.year = iyear;
                    td.trackalbum = ialbumtrack;
                    TagLoad(this, td);

                    engine.Volume = fVolume;
                    engine.Play();
                    engine.PlaybackStopped += Engine_PlaybackStopped;

                }

            }
        }

        public void Stop()
        {
            engine.Stop();
        }

        public void PlayPrevious()
        {
            if (Repeat)
            {
                TrackChangeData e = new TrackChangeData();
                e.index = SelectedIndex;
                TrackChange(this, e);
                PlayItem(SelectedIndex);
            }
            else
            {
                if (SelectedIndex == 0)
                {
                    TrackChangeData e = new TrackChangeData();
                    e.index = playlist.Count - 1;
                    TrackChange(this, e);
                    PlayItem(playlist.Count - 1);
                }
                else
                {
                    TrackChangeData e = new TrackChangeData();
                    e.index = SelectedIndex - 1;
                    TrackChange(this, e);
                    PlayItem(SelectedIndex - 1);
                }
            }
        }

        public void Play()
        {
            if (iSelectedIndex != -1)
            {
                if (IsPaused)
                {
                    this.Pause();
                }
                else
                {
                    PlayItem(SelectedIndex);
                }
            }
        }

        public void Pause()
        {
            isPaused = !isPaused;
            engine.Pause();

        }

        public BitmapImage GetAlbumArt(string artist, string album)
        {
            BitmapImage bi = null;
            byte[] image = null;

            if (album == " ")
            {
                album = "__NULL__";
            }

            Database.ImageCollection dt = this.dbDataStore.Images.GetDataByArtistAndAlbum(artist, album);

            if (dt.Count > 0)
            {
                if (dt[0].ImageData != null)
                {
                    image = dt[0].ImageData;
                }
            }

            if (image == null)
            {
                bool remote = Convert.ToBoolean(RockBox.Properties.Resources.GetRemoteAlbumInfo);

                if (remote)
                {
                    image = ImageHelper.DownloadAndSaveImage(dbDataStore, artist, album);
                }
            }

            if (image == null)
            {
                bi = this.DefaultImage;
            }
            else
            {
                bi = ImageHelper.ConvertBytesToBitmapImage(image);
                // the image must be frozen so that multithreads play nice.
                bi.Freeze();
            }

            return bi;
        }

        private void Engine_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (engine.ChannelPosition == engine.ChannelLength)
            {
                PlayNext();
            }
        }

        #endregion

    }
}
