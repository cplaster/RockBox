using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace RockBox
{
    /// <summary>
    /// Interaction logic for Library2.xaml
    /// </summary>
    public partial class Library : Window
    {

        AlbumDataItem _DataItem;
        BitmapImage DefaultImage;
        MainWindow wndMainWindow;
        Task searchTask = null;
        System.Collections.ArrayList listView = new ArrayList();
        ListBox targetList;
        bool backload = false;

        //WARNFIX
        //object objAlbumArtLock = new object();

        private int clickCounter = 0;
        private ObservableCollection<AlbumDataItem> albumItems = new ObservableCollection<AlbumDataItem>();
        //WARNFIX
        //private ObservableDictionary<string, BitmapImage> imageItems = new ObservableDictionary<string, BitmapImage>();

        private object albumItemsLock = new object();
        private System.Windows.Forms.Timer timer1;

        public Library()
        {
            InitializeComponent();
            InitializeComponent();
            this.DefaultImage = ImageHelper.ConvertBytesToBitmapImage(ImageHelper.GetDefaultImage());
            this.timer1 = new System.Windows.Forms.Timer();
            this.timer1.Enabled = true;
            this.timer1.Interval = 500;
            this.timer1.Tick += timer1_Tick;
        }

        #region Search Methods

        private void doSearch(object sender, RoutedEventArgs e)
        {
            Database dbDataStore = wndMainWindow.AudioEngine.Datastore;
            ArrayList a = new ArrayList();
            a.Add(cbSearch.SelectedIndex);
            a.Add(txtSearch.Text);
            icResults.ItemsSource = null;
            albumItems.Clear();

            this.searchTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    int index = (int)a[0];
                    string text = (string)a[1];
                    ObservableCollection<AlbumDataItem> items = new ObservableCollection<AlbumDataItem>();

                    //MusicTableAdapters.ImageCacheTableAdapter ict = this.wndMainWindow.ImageCacheTableAdapter;
                    //MusicTableAdapters.SongsTableAdapter sta = this.wndMainWindow.SongsTableAdapter;

                    Database.SongCollection sdt = null;

                    switch (index)
                    {
                        case 0:
                            sdt = dbDataStore.Songs.GetDataByAlbum(text);
                            SmartSearch(items, sdt, DefaultImage);
                            break;

                        case 1:
                            sdt = dbDataStore.Songs.GetDataByArtist(text);
                            SmartSearch(items, sdt, DefaultImage);
                            break;

                        case 2:
                            sdt = GetByTitle(this.wndMainWindow, text);
                            SmartSearch(items, sdt, DefaultImage);
                            break;
                        case 3:
                            sdt = GetByPlaylist(this.wndMainWindow, text);
                            SmartSearchNEW(items, sdt, DefaultImage);
                            break;
                    }

                    lock (albumItemsLock)
                    {
                        albumItems = items;
                    }

                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {

                        if (index == 3)
                        {
                            if (icResults2.ItemsSource != null)
                            {
                                icResults2.Items.Clear();

                            }

                            icResults2.ItemsSource = albumItems;
                            boResults.Visibility = System.Windows.Visibility.Collapsed;
                            boResults2.Visibility = System.Windows.Visibility.Visible;
                            svResults2.ScrollToTop();
                        }
                        else
                        {
                            if (icResults.ItemsSource != null)
                            {
                                icResults.Items.Clear();
                            }

                            icResults.ItemsSource = albumItems;
                            boResults.Visibility = System.Windows.Visibility.Visible;
                            boResults2.Visibility = System.Windows.Visibility.Collapsed;
                            svResults.ScrollToTop();
                        }
                        backload = true;
                    });

                    // this should garbage collect the previously populated stuff.. not sure if we should really do this...
                    GC.Collect();

                    Thread.Sleep(10);
                    return false;
                }
            });
        }

        private Database.SongCollection GetByPlaylist(MainWindow w, string text)
        {
            Database dbDataStore = w.AudioEngine.Datastore;
            Database.SongCollection sdt = new Database.SongCollection();
            List<string> playlists = dbDataStore.FindInPlaylist(text);
            int i = 0;

            foreach (string name in playlists)
            {
                Database.SongsAlbumCollection sadt = w.AudioEngine.Datastore.SongsAlbums.GetDataByAlbumExact(name);

                i++;
                int seed = 1000000 * i;

                if (sadt.Count == 1)
                {
                    Database.SongsAlbum sar = sadt[0];
                    string[] rowIds = sar.RowIds.Split(",".ToCharArray());

                    int tracknum = 0;
                    foreach (string rowId in rowIds)
                    {
                        tracknum++;
                        Database.Song songsrow = dbDataStore.Songs.FindById(Convert.ToInt16(rowId));
                        if (songsrow != null)
                        {

                            Database.Song target = new Database.Song();
                            target = songsrow;
                            target.Album = name;
                            target.Track = tracknum.ToString();
                            dbDataStore.FormatTrackString(target);

                            target.Id = target.Id + seed;

                            try
                            {
                                sdt.Add(target);
                            }
                            catch (Exception e)
                            {
                                //WARNFIX
                                //int j = 0;
                            }
                        }
                    }
                }

            }

            return sdt;

        }

        private Database.SongCollection GetByTitle(MainWindow w, string text)
        {
            Database dbDataStore = w.AudioEngine.Datastore;
            Database.SongCollection sdt = dbDataStore.Songs.GetDataByTitle(text);
            List<string> playlists = dbDataStore.FindInPlaylist(text);
            int i = 0;

            foreach (string name in playlists)
            {
                Database.SongsAlbumCollection sadt = dbDataStore.SongsAlbums.GetDataByAlbumExact(name);
                i++;
                int seed = 1000000 * i;

                if (sadt.Count == 1)
                {
                    Database.SongsAlbum sar = sadt[0];
                    string[] rowIds = sar.RowIds.Split(",".ToCharArray());

                    int tracknum = 0;
                    foreach (string rowId in rowIds)
                    {
                        tracknum++;
                        Database.Song songsrow = dbDataStore.Songs.FindById(Convert.ToInt16(rowId));
                        if (songsrow != null)
                        {

                            Database.Song target = new Database.Song();
                            target = songsrow;
                            target.Album = name;
                            target.Track = tracknum.ToString();
                            FormatTrackString(target);

                            target.Id = target.Id + seed;

                            try
                            {
                                sdt.Add(target);
                            }
                            catch (Exception e)
                            {
                                //WARNFIX
                                //int j = 0;
                            }
                        }
                    }
                }

            }

            return sdt;
        }

        private static void SmartSearch(ObservableCollection<AlbumDataItem> a, Database.SongCollection sdt, BitmapImage image)
        {
            SortedDictionary<string, AlbumDataItem> albums = new SortedDictionary<string, AlbumDataItem>();

            if (sdt.Count > 0)
            {
                foreach (Database.Song sarow in sdt)
                {
                    AlbumDataItem i = null;
                    string akey = sarow.Artist.Trim() + sarow.Album.Trim();

                    FormatTrackString(sarow);

                    if (!albums.ContainsKey(akey))
                    {
                        i = new AlbumDataItem(sarow.Album, sarow.Artist, new Database.SongCollection(), sarow.Year);
                        albums.Add(akey, i);
                    }
                    else
                    {
                        i = albums[akey];
                    }

                    i.Table.Add(sarow);
                }
            }


            foreach (KeyValuePair<string, AlbumDataItem> item in albums)
            {
                item.Value.BitmapImage = image;
                RemoveDuplicateTracks(item);
                a.Add(item.Value);
            }

        }

        private void SmartSearchNEW(ObservableCollection<AlbumDataItem> a, Database.SongCollection sdt, BitmapImage image)
        {
            Dictionary<string, AlbumDataItem> albums = new Dictionary<string, AlbumDataItem>();

            if (sdt.Count > 0)
            {
                foreach (Database.Song sarow in sdt)
                {
                    AlbumDataItem i = null;

                    FormatTrackString(sarow);

                    string album = UcFirst(sarow.Album);


                    if (!albums.ContainsKey(album))
                    {
                        i = new AlbumDataItem(album, "Various Artists", new Database.SongCollection(), sarow.Year);
                        albums.Add(album, i);
                    }
                    else
                    {
                        i = albums[album];
                    }

                    i.Table.Add(sarow);
                }
            }

            foreach (KeyValuePair<string, AlbumDataItem> item in albums)
            {
                item.Value.BitmapImage = image;
                RemoveDuplicateTracks(item);
                a.Add(item.Value);
            }
        }

        private static string UcFirst(string album)
        {
            string[] words = album.Split(" ".ToCharArray());
            string w = "";
            foreach (string s in words)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return string.Empty;
                }
                char[] a = s.ToCharArray();
                a[0] = char.ToUpper(a[0]);
                w += " " + new string(a);
            }
            w = w.Trim();
            return w;
        }

        internal static void FormatTrackString(Database.Song row)
        {

            if (row.Track.Length > 0)
            {
                row.Track = (row.Track.Split("/".ToCharArray()))[0];

                if (row.Track.Length > 1)
                {
                    row.Track = row.Track.Substring(0, 2);
                }
                else
                {
                    row.Track = "0" + row.Track;
                }
            }
        }

        private static void RemoveDuplicateTracks(KeyValuePair<string, AlbumDataItem> item)
        {
            Database.SongCollection dTable = item.Value.Table;

            Hashtable hTable = new Hashtable();
            ArrayList duplicateList = new ArrayList();

            //Add list of all the unique item value to hashtable, which stores combination of key, value pair.
            //And add duplicate item value in arraylist.
            foreach (Database.Song drow in dTable)
            {
                string title = drow.Album.ToString().ToLower() + drow.Title.ToString().ToLower();
                if (hTable.Contains(title))
                {
                    Database.Song h = (Database.Song)hTable[title];

                    if (drow.Bitrate == "")
                    {
                        drow.Bitrate = "96000";
                    }

                    if (Convert.ToInt32(drow.Bitrate) <= Convert.ToInt32(h.Bitrate))
                    {
                        duplicateList.Add(drow);
                    }
                    else
                    {
                        duplicateList.Add(h);
                    }
                }
                else
                {
                    hTable.Add(title, drow);
                }
            }

            //Removing a list of duplicate items from datatable.
            foreach (Database.Song dRow in duplicateList)
            {
                if (dTable.Contains<Database.Song>(dRow))
                {
                    dTable.Remove(dRow);
                }
            }

            item.Value.Table = dTable;
        }

        #endregion

        #region Event Handlers

        void timer1_Tick(object sender, EventArgs e)
        {

            // if there are images to load in the background, we should do it here.
            // anything else that requires eventloop ticks can go here as well.
            if (this.backload)
            {
                this.backload = false;
                this.searchTask = Task.Factory.StartNew(() =>
                {
                    foreach (AlbumDataItem item in this.albumItems)
                    {
                        BitmapImage bi = this.wndMainWindow.AudioEngine.Datastore.LoadImage(item.Artist, item.Name);
                        item.BitmapImage = bi;

                    }

                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        icResults.Items.Refresh();
                    });

                    Thread.Sleep(10);
                    return false;

                });
            }

            timer1.Stop();
            if (this.clickCounter == 2)
            {
                AddAlbumToPlaylist(_DataItem);
                this.clickCounter = 0;
            }
            timer1.Start();

        }

        private void AddAlbumToPlaylist(AlbumDataItem _DataItem)
        {
            // this method can be invoked via the doubleclick timer thread, so we have to 
            // use a dispatcher invoke instead of just calling the method normally.
            Database.SongCollection t = _DataItem.Table;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (Database.Song r in t)
                {
                    this.wndMainWindow.Playlist.Add(r);
                }
            }));
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            this.wndMainWindow = this.Owner as MainWindow;
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                doSearch(sender, new RoutedEventArgs());
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image i = (System.Windows.Controls.Image)sender;
            _DataItem = (AlbumDataItem)i.DataContext;

            this.timer1.Stop();
            this.clickCounter++;
            this.timer1.Start();
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBox lv = (ListBox)sender;
            //System.Data.DataRowView rv = (System.Data.DataRowView)lv.SelectedItem;
            //object o = lv.SelectedItem;

            this.wndMainWindow.Playlist.Add((Database.Song)lv.SelectedItem);
        }

        private void ListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                svResults.LineDown();
            }
            else
            {
                svResults.LineUp();
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.targetList = (ListBox)sender;

            if (listView.Count == 0)
            {
                IEnumerable<ListBox> lv = FindVisualChildren<ListBox>(this);
                this.listView = new ArrayList();
                foreach (ListBox l in lv)
                {
                    this.listView.Add(l);
                }
            }

            foreach (ListBox li in listView)
            {
                if (targetList != li)
                {
                    li.SelectedIndex = -1;
                }
            }
        }

        private void ListItemRightClick(object sender, RoutedEventArgs e)
        {
            //System.Data.DataRowView dr = (System.Data.DataRowView)this.targetList.SelectedItem;
            //object o = this.targetList.SelectedItem;
            this.wndMainWindow.Playlist.Add((Database.Song)this.targetList.SelectedItem);
            //fixme: figure out how to use list as a datarowview
            //this.wndMainWindow.Playlist.Add((Music.SongsRow)dr.Row);
        }

        #endregion

        #region Helper Methods

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        #endregion




    }
}
