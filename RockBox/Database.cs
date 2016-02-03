using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Security;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections;
using System.Windows;
using System.Xml.Linq;

namespace RockBox
{

    public static class DataTableExtensions
    {
        private static Dictionary<Type, IList<PropertyInfo>> typeDictionary = new Dictionary<Type, IList<PropertyInfo>>();
        public static IList<PropertyInfo> GetPropertiesForType<T>()
        {
            var type = typeof(T);
            if (!typeDictionary.ContainsKey(typeof(T)))
            {
                typeDictionary.Add(type, type.GetProperties().ToList());
            }
            return typeDictionary[type];
        }

        public static IList<T> ToList<T>(this DataTable table) where T : new()
        {
            IList<PropertyInfo> properties = GetPropertiesForType<T>();
            IList<T> result = new List<T>();

            foreach (var row in table.Rows)
            {
                var item = CreateItemFromRow<T>((DataRow)row, properties);
                result.Add(item);
            }

            return result;
        }

        private static T CreateItemFromRow<T>(DataRow row, IList<PropertyInfo> properties) where T : new()
        {
            T item = new T();
            foreach (var property in properties)
            {
                property.SetValue(item, row[property.Name], null);
            }
            return item;
        }

    }

    public class Database
    {
        ImageCollection _ImageCollection;
        SongCollection _SongCollection;
        SongsAlbumCollection _SongsAlbumCollection;
        int songsNextId = 0;
        int imagesCacheNextId = 0;
        int songsAlbumsNextId = 0;
        BitmapImage DefaultImage;

        public Database()
        {
            this._ImageCollection = new ImageCollection();
            this._SongCollection = new SongCollection();
            this._SongsAlbumCollection = new SongsAlbumCollection();

            BitmapImage b = new BitmapImage();
            b.BeginInit();
            Uri uri = new System.Uri("pack://application:,,,/Resources/jewelcase_medium.png");
            b.UriSource = uri;
            b.EndInit();
            b.Freeze();
            this.DefaultImage = b;

        }

        public void FormatTrackString(Database.Song row)
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

        public List<string> FindInPlaylist(string text)
        {
            List<string> l = new List<string>();
            Database.SongCollection sdt = this.Songs.GetDataByTitle(text);
            foreach (Database.Song row in sdt)
            {
                Database.SongsAlbumCollection sadt = this.SongsAlbums.GetDataByRowId(row.Id);

                if (sadt.Count > 0)
                {
                    foreach (Database.SongsAlbum sar in sadt)
                    {
                        l.Add(sar.Album);
                    }
                }
            }

            return l;
        }

        public BitmapImage LoadImage(string artist, string album)
        {
            BitmapImage bi = null;
            byte[] image = null;

            if (album == " ")
            {
                album = "__NULL__";
            }

            ImageCollection dt = this.Images.GetDataByArtistAndAlbum(artist, album);

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
                    image = ImageHelper.DownloadAndSaveImage(this, artist, album);
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

        #region Properties

        public ImageCollection Images
        {
            get { return _ImageCollection; }
        }

        public SongCollection Songs
        {
            get { return _SongCollection; }
        }

        public SongsAlbumCollection SongsAlbums
        {
            get { return _SongsAlbumCollection; }
        }

        #endregion

        #region DataTable Conversion

        public static Song ConvertToSongsRow(DataRow row)
        {
            Database.Song sr = new Database.Song();
            sr.Id = (int)row[0];
            sr.Path = (string)row[1];
            sr.Filename = (string)row[2];
            sr.Title = (string)row[3];
            sr.Artist = (string)row[4];
            sr.Album = (string)row[5];
            sr.Year = (string)row[6];
            sr.Length = (string)row[7];
            sr.Bitrate = (string)row[8];
            sr.Genre = (string)row[9];
            sr.Track = (string)row[10];
            sr.Comments = (string)row[11];

            return sr;
        }

        public DataTable ToDataTable<T>(List<T> items)
        {
            DataTable tb = new DataTable(typeof(T).Name);
            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in props)
            {
                Type t = GetCoreType(prop.PropertyType);
                tb.Columns.Add(prop.Name, t);
            }

            foreach (T item in items)
            {
                var values = new object[props.Length];

                for (int i = 0; i < props.Length; i++)
                {
                    values[i] = props[i].GetValue(item, null);
                }

                tb.Rows.Add(values);
            }

            return tb;
        }

        public static bool IsNullable(Type t)
        {
            return !t.IsValueType || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public static Type GetCoreType(Type t)
        {
            if (t != null && IsNullable(t))
            {
                if (!t.IsValueType)
                {
                    return t;
                }
                else
                {
                    return Nullable.GetUnderlyingType(t);
                }
            }
            else
            {
                return t;
            }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// Get the Application Guid
        /// </summary>
        public static Guid AppGuid
        {
            get
            {
                Assembly asm = Assembly.GetEntryAssembly();
                IEnumerable<Attribute> e = asm.GetCustomAttributes();
                object[] attr = (asm.GetCustomAttributes(typeof(GuidAttribute), true));
                return new Guid((attr[0] as GuidAttribute).Value);
            }
        }
        /// <summary>
        /// Get the current assembly Guid.
        /// <remarks>
        /// Note that the Assembly Guid is not necessarily the same as the
        /// Application Guid - if this code is in a DLL, the Assembly Guid
        /// will be the Guid for the DLL, not the active EXE file.
        /// </remarks>
        /// </summary>
        public static Guid AssemblyGuid
        {
            get
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                object[] attr = (asm.GetCustomAttributes(typeof(GuidAttribute), true));
                return new Guid((attr[0] as GuidAttribute).Value);
            }
        }

        /// <summary>
        /// Get the current user data folder
        /// </summary>
        public static string UserDataFolder
        {
            get
            {
                Guid appGuid = AppGuid;
                string folderBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dir = string.Format(@"{0}\{1}\", folderBase, appGuid.ToString("B").ToUpper());
                return CheckDir(dir);
            }
        }

        /// <summary>
        /// Get the current user roaming data folder
        /// </summary>
        public static string UserRoamingDataFolder
        {
            get
            {
                Guid appGuid = AppGuid;
                string folderBase = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dir = string.Format(@"{0}\{1}\", folderBase, appGuid.ToString("B").ToUpper());
                return CheckDir(dir);
            }
        }

        /// <summary>
        /// Get all users data folder
        /// </summary>
        public static string AllUsersDataFolder
        {
            get
            {
                Guid appGuid = AppGuid;
                string folderBase = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string dir = string.Format(@"{0}\{1}\", folderBase, appGuid.ToString("B").ToUpper());
                return CheckDir(dir);
            }
        }

        /// <summary>
        /// Check the specified folder, and create if it doesn't exist.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static string CheckDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public bool Serialize()
        {
            bool ret = true;
            string folder = UserDataFolder;

            /*
            try
            {
                using (Stream stream = File.Open(folder + "\\Images.bin", FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, this.listImages);
                }
            }
            catch (IOException)
            {
                ret = false;
            }
            */

            try
            {
                using (Stream stream = File.Open(folder + "\\ImagesCache.bin", FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, this._ImageCollection);
                }
            }
            catch (IOException)
            {
                ret = false;
            }

            try
            {
                using (Stream stream = File.Open(folder + "\\Songs.bin", FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, this._SongCollection);
                }
            }
            catch (IOException)
            {
                ret = false;
            }

            try
            {
                using (Stream stream = File.Open(folder + "\\SongsAlbums.bin", FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, this._SongsAlbumCollection);
                }
            }
            catch (IOException)
            {
                ret = false;
            }

            /*
            try
            {
                using (Stream stream = File.Open(folder + "\\SongsEditor.bin", FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, this.listSongsEditor);
                }
            }
            catch (IOException)
            {
                ret = false;
            }
            */

            return ret;
        }

        public bool Deserialize()
        {
            bool ret = true;
            string folder = UserDataFolder;

            try
            {
                using (Stream stream = File.Open(folder + "\\ImagesCache.bin", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    this._ImageCollection = (ImageCollection)bin.Deserialize(stream);
                    this.imagesCacheNextId = 10000000 + this._ImageCollection.Count;
                }
            }
            catch (IOException)
            {
                ret = false;
            }

            try
            {
                using (Stream stream = File.Open(folder + "\\Songs.bin", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    this._SongCollection = (SongCollection)bin.Deserialize(stream);
                    this.songsNextId = 10000000 + this._SongCollection.Count;
                }
            }
            catch (IOException)
            {
                ret = false;
            }

            try
            {
                using (Stream stream = File.Open(folder + "\\SongsAlbums.bin", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    this._SongsAlbumCollection = (SongsAlbumCollection)bin.Deserialize(stream);
                    this.songsAlbumsNextId = 10000000 + this._SongsAlbumCollection.Count;
                }
            }
            catch (IOException)
            {
                ret = false;
            }

            /*
            try
            {
                using (Stream stream = File.Open(folder + "\\SongsEditor.bin", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    this.listSongsEditor = (List<Database.SongsEditorRow>)bin.Deserialize(stream);
                }
            }
            catch (IOException)
            {
                ret = false;
            }
            */

            return ret;
        }

        #endregion

        #region Subclass Definitions

        #region Modified ObservableCollection


        /// <summary>
        /// Implementation of a dynamic data collection based on generic Collection<T>, 
        /// implementing INotifyCollectionChanged to notify listeners
        /// when items get added, removed or the whole list is refreshed.
        /// </summary>
        [Serializable()]
        public class ObservableDatabaseCollection<T> : Collection<T>, INotifyCollectionChanged, INotifyPropertyChanged
        {
            protected int maxId = -1;

            public int MaxId
            {
                get { return maxId; }
            }

            //----------------------------------------------------- 
            //
            //  Constructors 
            //
            //-----------------------------------------------------

            #region Constructors 
            /// <summary>
            /// Initializes a new instance of ObservableCollection that is empty and has default initial capacity. 
            /// </summary> 
            public ObservableDatabaseCollection() : base() { }

            /// <summary>
            /// Initializes a new instance of the ObservableCollection class
            /// that contains elements copied from the specified list
            /// </summary> 
            /// <param name="list">The list whose elements are copied to the new list.
            /// <remarks> 
            /// The elements are copied onto the ObservableCollection in the 
            /// same order they are read by the enumerator of the list.
            /// </remarks> 
            /// <exception cref="ArgumentNullException"> list is a null reference </exception>
            public ObservableDatabaseCollection(List<T> list)
                : base((list != null) ? new List<T>(list.Count) : list)
            {
                // Workaround for VSWhidbey bug 562681 (tracked by Windows bug 1369339).
                // We should be able to simply call the base(list) ctor.  But Collection<t> 
                // doesn't copy the list (contrary to the documentation) - it uses the 
                // list directly as its storage.  So we do the copying here.
                // 
                IList<T> items = Items;
                if (list != null && items != null)
                {
                    using (IEnumerator<T> enumerator = list.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            items.Add(enumerator.Current);
                        }
                    }
                }
            }

            #endregion Constructors


            //------------------------------------------------------
            // 
            //  Public Methods
            //
            //-----------------------------------------------------

            #region Public Methods

            /// <summary> 
            /// Move item at oldIndex to newIndex.
            /// </summary> 
            public void Move(int oldIndex, int newIndex)
            {
                MoveItem(oldIndex, newIndex);
            }

            #endregion Public Methods 


            //------------------------------------------------------ 
            //
            //  Public Events
            //
            //------------------------------------------------------ 

            #region Public Events 

            //-----------------------------------------------------
            #region INotifyPropertyChanged implementation 

            /// <summary>
            /// PropertyChanged event (per <see cref="INotifyPropertyChanged">).
            /// </see></summary> 
            event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
            {
                add
                {
                    PropertyChanged += value;
                }
                remove
                {
                    PropertyChanged -= value;
                }
            }
            #endregion INotifyPropertyChanged implementation 


            //------------------------------------------------------
            /// <summary>
            /// Occurs when the collection changes, either by adding or removing an item.
            /// </summary> 
            /// <remarks>
            /// see <seealso cref="INotifyCollectionChanged"> 
            /// </seealso></remarks> 
            public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

            #endregion Public Events


            //----------------------------------------------------- 
            //
            //  Protected Methods 
            // 
            //-----------------------------------------------------

            #region Protected Methods

            /// <summary>
            /// Called by base class Collection<T> when the list is being cleared; 
            /// raises a CollectionChanged event to any listeners.
            /// </summary> 
            protected override void ClearItems()
            {
                CheckReentrancy();
                base.ClearItems();
                this.maxId = -1;
                OnPropertyChanged(CountString);
                OnPropertyChanged(IndexerName);
                OnCollectionReset();
            }

            /// <summary> 
            /// Called by base class Collection<T> when an item is removed from list;
            /// raises a CollectionChanged event to any listeners. 
            /// </summary>
            protected override void RemoveItem(int index)
            {
                CheckReentrancy();
                T removedItem = this[index];

                base.RemoveItem(index);

                OnPropertyChanged(CountString);
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
            }

            /// <summary>
            /// Called by base class Collection<T> when an item is added to list; 
            /// raises a CollectionChanged event to any listeners. 
            /// </summary>
            protected override void InsertItem(int index, T item)
            {
                CheckReentrancy();

                CheckItemId(item);

                base.InsertItem(index, item);

                OnPropertyChanged(CountString);
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
            }

            private void CheckItemId(T item)
            {
                PropertyInfo pi = item.GetType().GetProperty("Id");
                if (pi != null)
                {
                    int id = (int)pi.GetValue(item);
                    if (id != null && id != -1)
                    {
                        if (id > this.maxId)
                        {
                            this.maxId = id;
                        }
                    }
                    else
                    {
                        this.maxId++;
                        pi.SetValue(item, this.maxId);
                    }
                }
            }

            /// <summary>
            /// Called by base class Collection<T> when an item is set in list;
            /// raises a CollectionChanged event to any listeners.
            /// </summary> 
            protected override void SetItem(int index, T item)
            {
                CheckReentrancy();
                T originalItem = this[index];
                base.SetItem(index, item);

                OnPropertyChanged(IndexerName);
                OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, item, index);
            }

            /// <summary> 
            /// Called by base class ObservableCollection<T> when an item is to be moved within the list; 
            /// raises a CollectionChanged event to any listeners.
            /// </summary> 
            protected virtual void MoveItem(int oldIndex, int newIndex)
            {
                CheckReentrancy();

                T removedItem = this[oldIndex];

                base.RemoveItem(oldIndex);
                base.InsertItem(newIndex, removedItem);

                OnPropertyChanged(IndexerName);
                OnCollectionChanged(NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex);
            }


            /// <summary> 
            /// Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged">). 
            /// </see></summary>
            protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, e);
                }
            }

            /// <summary>
            /// PropertyChanged event (per <see cref="INotifyPropertyChanged">). 
            /// </see></summary>
            protected virtual event PropertyChangedEventHandler PropertyChanged;

            /// <summary> 
            /// Raise CollectionChanged event to any listeners.
            /// Properties/methods modifying this ObservableCollection will raise 
            /// a collection changed event through this virtual method. 
            /// </summary>
            /// <remarks> 
            /// When overriding this method, either call its base implementation
            /// or call <see cref="BlockReentrancy"> to guard against reentrant collection changes.
            /// </see></remarks>
            protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                if (CollectionChanged != null)
                {
                    using (BlockReentrancy())
                    {
                        CollectionChanged(this, e);
                    }
                }
            }

            /// <summary> 
            /// Disallow reentrant attempts to change this collection. E.g. a event handler 
            /// of the CollectionChanged event is not allowed to make changes to this collection.
            /// </summary> 
            /// <remarks>
            /// typical usage is to wrap e.g. a OnCollectionChanged call with a using() scope:
            /// <code>
            ///         using (BlockReentrancy()) 
            ///         {
            ///             CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, item, index)); 
            ///         } 
            /// </code>
            /// </remarks> 
            protected IDisposable BlockReentrancy()
            {
                _monitor.Enter();
                return _monitor;
            }

            /// <summary> Check and assert for reentrant attempts to change this collection. </summary> 
            /// <exception cref="InvalidOperationException"> raised when changing the collection
            /// while another collection change is still being notified to other listeners </exception> 
            protected void CheckReentrancy()
            {
                if(_monitor == null)
                {
                    _monitor = new SimpleMonitor();
                }

                if (_monitor.Busy)
                {
                    // we can allow changes if there's only one listener - the problem
                    // only arises if reentrant changes make the original event args 
                    // invalid for later listeners.  This keeps existing code working 
                    // (e.g. Selector.SelectedItems).
                    if ((CollectionChanged != null) && (CollectionChanged.GetInvocationList().Length > 1))
                        throw new InvalidOperationException("Reentrancy not allowed.");
                }
            }

            #endregion Protected Methods


            //-----------------------------------------------------
            // 
            //  Private Methods
            //
            //------------------------------------------------------

            #region Private Methods
            /// <summary> 
            /// Helper to raise a PropertyChanged event  />). 
            /// </summary>
            private void OnPropertyChanged(string propertyName)
            {
                OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            }

            /// <summary>
            /// Helper to raise CollectionChanged event to any listeners 
            /// </summary> 
            private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
            }

            /// <summary> 
            /// Helper to raise CollectionChanged event to any listeners
            /// </summary> 
            private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
            }

            /// <summary>
            /// Helper to raise CollectionChanged event to any listeners 
            /// </summary>
            private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
            }

            /// <summary>
            /// Helper to raise CollectionChanged event with action == Reset to any listeners
            /// </summary> 
            private void OnCollectionReset()
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            #endregion Private Methods 

            //-----------------------------------------------------
            //
            //  Private Types 
            //
            //------------------------------------------------------ 

            #region Private Types

            // this class helps prevent reentrant calls
            [Serializable()]
            private class SimpleMonitor : IDisposable
            {
                public void Enter()
                {
                    ++_busyCount;
                }

                public void Dispose()
                {
                    --_busyCount;
                }

                public bool Busy { get { return _busyCount > 0; } }

                int _busyCount;
            }

            #endregion Private Types

            //------------------------------------------------------ 
            //
            //  Private Fields 
            // 
            //-----------------------------------------------------

            #region Private Fields

            private const string CountString = "Count";

            // This must agree with Binding.IndexerName.  It is declared separately
            // here so as to avoid a dependency on PresentationFramework.dll. 
            private const string IndexerName = "Item[]";

            private SimpleMonitor _monitor = new SimpleMonitor();

            #endregion Private Fields
        }

        #endregion

        #region Collections

        [Serializable]
        public class ImageCollection : ObservableDatabaseCollection<Image>
        {

            public ImageCollection() { }

            public int AddRow(string artist, string album, string uri, byte[] image, string dimensions, string name)
            {
                Database.Image sr = new Database.Image(-1, artist, album, uri, image, dimensions, name);
                this.Add(sr);
                return this.MaxId;
            }

            public int FillByArtistAndAlbum(ImageCollection collection, string Artist, string Album)
            {
                var q = from r in this.AsEnumerable()
                        where r.Artist.ToLower().Contains(Artist.ToLower())
                        select r;
                int i = 0;

                foreach (var r in q)
                {

                    if (r.Album.ToLower().Contains(Album.ToLower()))
                    {
                        collection.Add(r);
                        i++;
                    }
                }

                return i;
            }

            public ImageCollection GetDataByArtistAndAlbum(string Artist, string Album)
            {
                ImageCollection t = new ImageCollection();

                this.FillByArtistAndAlbum(t, Artist, Album);

                return t;
            }

        }

        [Serializable]
        public class SongCollection : ObservableDatabaseCollection<Song>
        {
            public SongCollection() { }

            public int Update(SongCollection collection)
            {
                int i = 0;
                foreach (Database.Song sr in collection)
                {
                    Database.Song target = this.FindById(sr.Id);

                    if (target == null)
                    {
                        this.Add(target);
                    }
                    else
                    {
                        if (sr.Comments == "Deleted")
                        {
                            this.Remove(target);
                        }
                        else
                        {
                            // this row was modified in the source table and exists in the datastore, so update it.
                            // we purposely do not update the Id, Filename, and Path properties because we do not
                            // want these modified at all.
                            target.Album = sr.Album;
                            target.Artist = sr.Artist;
                            target.Bitrate = sr.Bitrate;
                            target.Comments = sr.Comments;
                            target.Genre = sr.Genre;
                            target.Length = sr.Length;
                            target.Title = sr.Title;
                            target.Track = sr.Track;
                            target.Year = sr.Year;
                        }
                    }

                    i++;
                }

                return i;
            }

            private void FixMaxId()
            {
                int max = -1;

                foreach(Song s in this.AsEnumerable())
                {
                    if(s.Id > max)
                    {
                        max = s.Id;
                    }

                    this.maxId = max;
                }
            }

            public virtual Song AddRow(string path, string filename, string title, string artist, string album, string year, string length, string bitrate, string genre, string track, string comments)
            {

                if(this.MaxId <= 0)
                {
                    FixMaxId();
                }

                int id = this.MaxId;

                FileInfo f = new FileInfo(path + "\\" + filename);

                //Check to see if the song is already in the collection. If not, increment the maxid, create a new Song object with the new maxid, and add it to the collection, returning
                //the new row's ID. If it is already in the collection, do nothing and return -1 to indicate a new row was not created or added.

                int i = 0;


                if (!this.Contains(f))
                {
                    this.maxId++;
                    Database.Song sr = new Database.Song(this.MaxId, path, filename, title, artist, album, year, length, bitrate, genre, track, comments);
                    this.Add(sr);
                    return sr;
                }
                else
                {
                    Database.Song sr = this.FindByPathAndFileName(path, filename);
                    return sr;
                }
            }

            public Song AddFile(FileInfo f)
            {
                // this method is pretty prone to problems.
                // We try to read tag information from the audio file.
                // If there is any sort of problem with that, we will just
                // skip adding it to the database for now.

                TagLib.File file = null;
                TagLib.Tag tag = null;
                bool fail = false;

                try
                {
                    file = TagLib.File.Create(f.FullName);
                    tag = file.Tag;
                }
                catch (Exception e)
                {
                    fail = true;
                }


                // the file can't be added if there are no tags
                if (!fail)
                {
                    string title = tag.Title == null ? " " : tag.Title;
                    string artist = " ";
                    if (tag.AlbumArtists.Length > 0)
                    {
                        artist = tag.AlbumArtists[0];
                    }
                    else
                    {
                        if (tag.Artists.Length > 0)
                        {
                            artist = tag.Artists[0];
                        }
                    }
                    string album = tag.Album == null ? " " : tag.Album;
                    string year = tag.Year.ToString() == null ? " " : tag.Year.ToString();
                    string length = file.Properties.Duration == null ? "00:00:00" : file.Properties.Duration.ToString();
                    string bitrate = file.Properties.AudioBitrate.ToString() == null ? " " : file.Properties.AudioBitrate.ToString();
                    string genre = " ";
                    if (tag.Genres.Length > 0)
                    {
                        genre = tag.Genres[0];
                    }
                    string track = tag.Track.ToString();
                    string comments = " ";

                    char[] trimChars = " \r\n\t\0".ToCharArray();

                    title.Trim(trimChars);
                    artist.Trim(trimChars);
                    album.Trim(trimChars);
                    year.Trim(trimChars);
                    length.Trim(trimChars);
                    bitrate.Trim(trimChars);
                    genre.Trim(trimChars);
                    track.Trim(trimChars);
                    comments.Trim(trimChars);

                    string hour, min, sec;
                    string[] temp = length.Split(":".ToCharArray());
                    hour = temp[0];
                    min = temp[1];
                    sec = temp[2];
                    string[] temp2 = sec.Split(".".ToCharArray());
                    sec = temp2[0];
                    length = hour + ":" + min + ":" + sec;

                    return this.AddRow(f.Directory.FullName, f.Name, title, artist, album, year, length, bitrate, genre, track, comments);
                }
                else
                {
                    return null;
                }
            }

            public Song FindById(int i)
            {
                var query = from r in this.AsEnumerable()
                            where r.Id.Equals(i)
                            select r;
                return query.ElementAt(0);
            }

            public Song FindByPathAndFileName(string path, string filename)
            {
                var query = from r in this.AsEnumerable()
                            where r.Path == path && r.Filename == filename
                            select r;

                if (query.Count() > 0)
                {
                    return query.ElementAt(0);
                }
                else
                {
                    return null;
                }
            }

            public bool Contains(FileInfo f)
            {
                var query = from r in this.AsEnumerable()
                            where r.Path == f.Directory.FullName && r.Filename == f.Name
                            select r;

                if (query.Count() > 0)
                {
                    return true;
                } else
                {
                    return false;
                }
            }

            public bool Contains(int id)
            {
                var query = from r in this.AsEnumerable()
                            where r.Id == id
                            select r;

                if(query.Count() > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public SongCollection GetDataByPathAndFileName(string path, string filename)
            {
                SongCollection t = new SongCollection();
                this.FillByPathAndFileName(t, path, filename);

                return t;
            }

            private int FillByPathAndFileName(SongCollection collection, string path, string filename)
            {
                var query = from r in this.AsEnumerable()
                            where r.Path == path && r.Filename == filename
                            select r;

                int i = 0;

                foreach (var row in query)
                {
                    collection.Add(row);
                    i++;
                }

                return i;
            }

            public int FillByFullPath(SongCollection collection, string fullName)
            {
                var query = from r in this.AsEnumerable()
                            where r.Path + "\\" + r.Filename == fullName
                            select r;

                int i = 0;

                foreach (var row in query)
                {
                    collection.Add(row);
                    i++;
                }

                return i;
            }

            public SongCollection GetDataByFullPath(string fullName)
            {
                SongCollection t = new SongCollection();

                this.FillByFullPath(t, fullName);

                return t;
            }

            public int FillByArtist(SongCollection collection, string Artist)
            {

                //Specifically, what we want to pull is all rows where the Artist column contains our Artist string.
                //we use tolower on both simply to avoid case sensitivity.
                //we then order the results by album, then by track number.
                //we need to do the int.Parse() conversion otherwise the Tracks won't be sorted correctly

                var query = from r in this.AsEnumerable()
                            where r.Artist.ToLower().Contains(Artist.ToLower())
                            orderby r.Album, int.Parse(r.Track)
                            select r;
                int i = 0;

                foreach (var row in query)
                {
                    collection.Add(row);
                    i++;
                }

                return i;
            }

            public SongCollection GetDataByArtist(string Artist)
            {
                SongCollection t = new SongCollection();

                this.FillByArtist(t, Artist);

                return t;
            }

            public int FillByAlbum(SongCollection collection, string Album)
            {
                var query = from r in this.AsEnumerable()
                            where r.Album.ToLower().Contains(Album.ToLower())
                            orderby r.Album, int.Parse(r.Track)
                            select r;
                int i = 0;

                foreach (var row in query)
                {
                    collection.Add(row);
                    i++;
                }

                return i;
            }

            public SongCollection GetDataByAlbum(string Album)
            {
                SongCollection t = new SongCollection();

                this.FillByAlbum(t, Album);

                return t;
            }

            public int FillByTitle(SongCollection collection, string Title)
            {
                var query = from r in this.AsEnumerable()
                            where r.Title.ToLower().Contains(Title.ToLower())
                            orderby r.Artist, r.Album, int.Parse(r.Track)
                            select r;
                int i = 0;

                foreach (var row in query)
                {
                    collection.Add(row);
                    i++;
                }

                return i;
            }

            public SongCollection GetDataByTitle(string Title)
            {
                SongCollection t = new SongCollection();

                this.FillByTitle(t, Title);

                return t;
            }
        }

        [Serializable]
        public class SongsAlbumCollection : ObservableDatabaseCollection<SongsAlbum>
        {
            public SongsAlbumCollection() { }

            public int AddRow(string playlistName1, string artistName, string rowids, string fullName, string filename, string playlistName2)
            {
                Database.SongsAlbum sr = new Database.SongsAlbum(playlistName1, artistName, rowids, fullName, filename, playlistName2);
                this.Add(sr);

                return this.MaxId;
            }

            public int FillByRowId(SongsAlbumCollection collection, int rowId)
            {
                var query = from r in this.AsEnumerable()
                            where r.RowIds.Contains(rowId.ToString())
                            select r;
                int i = 0;

                foreach (var row in query)
                {
                    collection.Add(row);
                    i++;
                }

                return i;
            }

            public SongsAlbumCollection GetDataByRowId(int rowId)
            {
                SongsAlbumCollection t = new SongsAlbumCollection();

                this.FillByRowId(t, rowId);

                return t;
            }

            public int FillByName(SongsAlbumCollection collection, string name)
            {
                var query = from r in this.AsEnumerable()
                            where r.Name == name
                            select r;
                int i = 0;

                foreach (var row in query)
                {
                    collection.Add(row);
                    i++;
                }

                return i;
            }

            public SongsAlbumCollection GetDataByName(string name)
            {
                SongsAlbumCollection t = new SongsAlbumCollection();
                this.FillByName(t, name);

                return t;
            }

            public int FillByAlbumExact(SongsAlbumCollection collection, string Album)
            {
                var query = from r in this.AsEnumerable()
                            where r.Album.ToLower() == Album.ToLower()
                            select r;
                int i = 0;

                foreach (var row in query)
                {
                    collection.Add(row);
                    i++;
                }

                return i;
            }

            public SongsAlbumCollection GetDataByAlbumExact(string Album)
            {
                SongsAlbumCollection t = new SongsAlbumCollection();

                this.FillByAlbumExact(t, Album);

                return t;
            }
        }

        public class Playlist : Database.SongCollection
        {
            public string Name;
            public string FileName;
            private bool isloaded = false;
            private string data;

            #region Properties

            public bool IsLoaded
            {
                get
                {
                    return isloaded;
                }
            }

            #endregion

            public Playlist()
            {

            }

            public Playlist(string filename)
            {
                Load(filename);
            }

            public override Song AddRow(string path, string filename, string title, string artist, string album, string year, string length, string bitrate, string genre, string track, string comments)
            {
                this.maxId++;
                Database.Song sr = new Database.Song(this.MaxId, path, filename, title, artist, album, year, length, bitrate, genre, track, comments);
                this.Add(sr);
                return sr;
            }

            public void Save()
            {
                SaveM3U(FileName);
            }

            public void Save(string filename)
            {
                // by default we will save as M3U
                SaveM3U(filename);
            }

            private void SaveM3U(string filename)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("#EXTM3U");
                foreach (Database.Song song in this)
                {
                    string length = GetSongLength(song);
                    if (song.Artist != "" && song.Title != "")
                    {
                        sb.AppendLine("#EXTINF:" + length + "," + song.Artist + " - " + song.Title);
                    }
                    else
                    {
                        sb.AppendLine("#EXTINF:" + length + "," + song.Filename);
                    }

                    sb.AppendLine(song.Path + "\\" + song.Filename);
                }

                File.WriteAllText(filename, sb.ToString());
            }

            private string GetSongLength(Database.Song song)
            {
                if (song.Length != null && song.Length != "")
                {
                    string[] time = song.Length.Split(":".ToCharArray());
                    int hour = Convert.ToInt16(time[0]);
                    int min = Convert.ToInt16(time[1]);
                    int sec = Convert.ToInt16(time[2]);
                    return ((hour * 60 * 60) + (min * 60) + sec).ToString();
                }
                else
                {
                    return "0";
                }
            }

            public void Load(string filename)
            {

                // For now we will handle either M3U or WPL playlists, so we need to have a separate approach to parse each playlist format.

                FileName = filename;
                FileInfo fi = new FileInfo(filename);

                if (File.Exists(filename))
                {
                    data = File.ReadAllText(filename);

                    switch (fi.Extension.ToLower())
                    {
                        case ".m3u":
                            ParseM3U();
                            Name = Path.GetFileNameWithoutExtension(fi.Name);
                            break;

                        case ".wpl":
                            ParseWPL();
                            break;

                    }

                    isloaded = true;
                }
            }

            private string[] ConvertToLines(string data)
            {
                return data.Split("\n".ToCharArray());
            }

            private void ParseWPL()
            {
                string[] lines = data.Split("\n".ToCharArray());

                if (lines[0].Contains("?wpl"))
                {
                    XDocument xmlDoc = XDocument.Parse(data);
                    IEnumerable<XElement> items = xmlDoc.Descendants("media");
                    //WARNFIX
                    //int i = -1;
                    foreach (XElement item in items)
                    {
                        string uristring = item.Attribute("src").Value;
                        if (uristring.StartsWith("..\\"))
                        {
                            uristring = uristring.Substring(3);
                            FileInfo f = new FileInfo(FileName);
                            uristring = f.Directory.Parent.FullName + "\\" + uristring;
                        }

                        Uri uri = new Uri(uristring);

                        Database.Song s = GetSongObject(uri);

                        this.Add(s);
                    }

                }
                else
                {
                    // this is not a valid windows playlist file.
                }
            }

            private void ParseM3U()
            {
                data = data.Replace("\r", "");
                data = data.TrimStart("\n".ToCharArray());
                data = data.TrimStart("".ToCharArray());
                data = data.Replace("#EXTINF:", ">");
                string[] lines = data.Split(">".ToCharArray());

                if (lines[0].Contains("EXTM3U"))
                {
                    for (int i = 1; i < lines.Count(); i++)
                    {
                        string temp = lines[i];
                        temp = temp.TrimEnd("\n".ToCharArray());
                        string[] topbottom = temp.Split("\n".ToCharArray());
                        string[] top = topbottom[0].Split(",".ToCharArray());
                        //WARNFIX
                        //string length = top[0];
                        //string title = top[1];
                        string uristring = topbottom[1];

                        FileInfo f = new FileInfo(FileName);

                        if (uristring.StartsWith("..\\"))
                        {
                            uristring = uristring.Substring(3);
                            uristring = f.Directory.Parent.FullName + "\\" + uristring;
                        }
                        if (uristring.StartsWith("\\"))
                        {
                            uristring = uristring.Substring(1);
                        }

                        //this only really supports local files for now. no streaming yet.



                        if (f.Exists)
                        {

                            try
                            {
                                Uri uri = new Uri(uristring);

                                Database.Song s = GetSongObject(uri);

                                this.Add(s);
                            }
                            catch (Exception e)
                            {
                                // this uri is malformed somehow. just skip it.
                            }
                        }

                    }
                }
                else
                {
                    // this is not a valid m3u file.
                }
            }

            private Database.Song GetSongObject(Uri uri)
            {
                Database.Song s = new Database.Song();

                if (uri.IsFile)
                {
                    string uristring = uri.ToString();
                    uristring = uristring.Replace("file:///", "");
                    FileInfo f = new FileInfo(uristring);

                    TagLib.File file = null;
                    TagLib.Tag tag = null;
                    bool fail = false;

                    try
                    {
                        file = TagLib.File.Create(f.FullName);
                        tag = file.Tag;
                    }
                    catch (Exception e)
                    {
                        fail = true;
                    }


                    // the file can't be added if there are no tags
                    if (!fail)
                    {
                        string title = tag.Title == null ? " " : tag.Title;
                        string artist = " ";
                        if (tag.AlbumArtists.Length > 0)
                        {
                            artist = tag.AlbumArtists[0];
                        }
                        else
                        {
                            if (tag.Artists.Length > 0)
                            {
                                artist = tag.Artists[0];
                            }
                        }
                        string album = tag.Album == null ? " " : tag.Album;
                        string year = tag.Year.ToString() == null ? " " : tag.Year.ToString();
                        string length = file.Properties.Duration == null ? "00:00:00" : file.Properties.Duration.ToString();
                        string bitrate = file.Properties.AudioBitrate.ToString() == null ? " " : file.Properties.AudioBitrate.ToString();
                        string genre = " ";
                        if (tag.Genres.Length > 0)
                        {
                            genre = tag.Genres[0];
                        }
                        string track = tag.Track.ToString();
                        string comments = " ";

                        char[] trimChars = " \r\n\t\0".ToCharArray();

                        title.Trim(trimChars);
                        artist.Trim(trimChars);
                        album.Trim(trimChars);
                        year.Trim(trimChars);
                        length.Trim(trimChars);
                        bitrate.Trim(trimChars);
                        genre.Trim(trimChars);
                        track.Trim(trimChars);
                        comments.Trim(trimChars);

                        string hour, min, sec;
                        string[] temp = length.Split(":".ToCharArray());
                        hour = temp[0];
                        min = temp[1];
                        sec = temp[2];
                        string[] temp2 = sec.Split(".".ToCharArray());
                        sec = temp2[0];
                        length = hour + ":" + min + ":" + sec;

                        if (title == " " || artist == " ")
                        {
                            title = f.Name;
                        }

                        s = new Database.Song(-1, f.Directory.FullName, f.Name, title, artist, album, year, length, bitrate, genre, track, comments);
                    }
                    else
                    {
                        // the tags could not be read. this needs to be dealt with gracefully.
                        s = new Database.Song(-1, f.Directory.FullName, f.Name, f.Name, "", "", "", "", "", "", "", "");
                    }
                }
                else
                {
                    // TODO: this is streaming audio. Not yet implemented.
                }

                return s;
            }
        }

        #endregion

        #region Objects

        [Serializable]
        public class Image : INotifyPropertyChanged
        {
            private int _Id;
            private string _Artist;
            private string _Album;
            private string _Uri;
            private byte[] _ImageData;
            private string _Dimensions;
            private string _Name;

            public Image()
            {

            }

            #region Properties

            public int Id
            {
                get
                {
                    return _Id;
                }
                set
                {
                    _Id = value; NotifyPropertyChanged("Id");
                }
            }

            public string Artist
            {
                get
                {
                    return _Artist;
                }
                set
                {
                    _Artist = value; NotifyPropertyChanged("Artist");
                }
            }

            public string Album
            {
                get
                {
                    return _Album;
                }
                set
                {
                    _Album = value; NotifyPropertyChanged("Album");
                }
            }

            public string Uri
            {
                get
                {
                    return _Uri;
                }
                set
                {
                    _Uri = value; NotifyPropertyChanged("Uri");
                }
            }

            public byte[] ImageData
            {
                get
                {
                    return _ImageData;
                }
                set
                {
                    _ImageData = value; NotifyPropertyChanged("ImageData");
                }
            }

            public string Dimensions
            {
                get
                {
                    return _Dimensions;
                }
                set
                {
                    _Dimensions = value; NotifyPropertyChanged("Dimensions");
                }
            }

            public string Name
            {
                get
                {
                    return _Name;
                }
                set
                {
                    _Name = value; NotifyPropertyChanged("Name");
                }
            }

            #endregion

            public Image(int id, string artist, string album, string uri, byte[] image, string dimensions, string name)
            {
                this._Id = id;
                this._Artist = artist;
                this._Album = album;
                this._Uri = uri;
                this._ImageData = image;
                this._Dimensions = dimensions;
                this._Name = name;
            }

            #region INotifyPropertyChanged

            [field: NonSerialized]
            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public override string ToString()
            {
                return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", Id, Artist, Album, Uri, ImageData, Dimensions, Name);
            }

            #endregion
        }

        [Serializable]
        public class Song : INotifyPropertyChanged
        {

            private int _Id;
            private string _Path;
            private string _Filename;
            private string _Title;
            private string _Artist;
            private string _Album;
            private string _Year;
            private string _Length;
            private string _Bitrate;
            private string _Genre;
            private string _Track;
            private string _Comments;

            internal static string[] _songsPropertyNames = new string[] { "Id", "Path", "Filename", "Title", "Artist", "Album", "Year", "Length", "Bitrate", "Genre", "Track", "Comments" };

            #region Properties

            public int Id { get { return _Id; } set { _Id = value; NotifyPropertyChanged("Id"); } }
            public string Path { get { return _Path; } set { _Path = value; NotifyPropertyChanged(""); } }
            public string Filename { get { return _Filename; } set { _Filename = value; NotifyPropertyChanged("Filename"); } }
            public string Title { get { return _Title; } set { _Title = value; NotifyPropertyChanged("Title"); } }
            public string Artist { get { return _Artist; } set { _Artist = value; NotifyPropertyChanged("Artist"); } }
            public string Album { get { return _Album; } set { _Album = value; NotifyPropertyChanged("Album"); } }
            public string Year { get { return _Year; } set { _Year = value; NotifyPropertyChanged("Year"); } }
            public string Length { get { return _Length; } set { _Length = value; NotifyPropertyChanged("Length"); } }
            public string Bitrate { get { return _Bitrate; } set { _Bitrate = value; NotifyPropertyChanged("Bitrate"); } }
            public string Genre { get { return _Genre; } set { _Genre = value; NotifyPropertyChanged("Genre"); } }
            public string Track { get { return _Track; } set { _Track = value; NotifyPropertyChanged("Track"); } }
            public string Comments { get { return _Comments; } set { _Comments = value; NotifyPropertyChanged("Comments"); } }

            public object this[int index]
            {
                get
                {
                    Type myType = typeof(Song);
                    string PropertyName = _songsPropertyNames[index];
                    System.Reflection.PropertyInfo pi = myType.GetProperty(PropertyName);
                    return pi.GetValue(this, null); //not indexed property!
                }
                set
                {
                    object[] o = this.ToArray();

                    if (index >= 0 && index < o.Count())
                    {
                        Type myType = typeof(Song);
                        string PropertyName = _songsPropertyNames[index];
                        System.Reflection.PropertyInfo pi = myType.GetProperty(PropertyName);
                        pi.SetValue(this, value, null); //not indexed property!
                    }
                    else
                    {
                        Exception e = new Exception("Index is out of bounds.");
                        throw (e);
                    }
                }
            }

            #endregion

            public Song()
            {
                //default, empty row
            }

            public Song(int id, string path, string filename, string title, string artist, string album, string year, string length, string bitrate, string genre, string track, string comments)
            {
                this.Id = id;
                this.Path = path;
                this.Filename = filename;
                this.Title = title;
                this.Artist = artist;
                this.Album = album;
                this.Year = year;
                this.Length = length;
                this.Bitrate = bitrate;
                this.Genre = genre;
                this.Track = track;
                this.Comments = comments;

            }

            public object[] ToArray()
            {
                object[] o = { this.Id, this.Path, this.Filename, this.Title, this.Artist, this.Album, this.Year, this.Length, this.Bitrate, this.Genre, this.Track, this.Comments };

                return o;
            }

            #region INotifyPropertyChanged

            [field: NonSerialized]
            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public override string ToString()
            {
                return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}", Id, Path, Filename, Title, Artist, Album, Year, Length, Bitrate, Genre, Track, Comments);
            }

            #endregion
        }

        [Serializable]
        public class SongsAlbum : INotifyPropertyChanged
        {
            public string _Album;
            public string _Artist;
            public string _RowIds;
            public string _Filename;
            public string _Path;
            public string _Name;

            #region Properties

            public string Album { get { return _Album; } set { _Album = value; NotifyPropertyChanged("Album"); } }
            public string Artist { get { return _Artist; } set { _Artist = value; NotifyPropertyChanged("Artist"); } }
            public string RowIds { get { return _RowIds; } set { _RowIds = value; NotifyPropertyChanged("RowIds"); } }
            public string Filename { get { return _Filename; } set { _Filename = value; NotifyPropertyChanged("Filename"); } }
            public string Path { get { return _Path; } set { _Path = value; NotifyPropertyChanged("Path"); } }
            public string Name { get { return _Name; } set { _Name = value; NotifyPropertyChanged("Name"); } }

            #endregion

            public SongsAlbum()
            {

            }

            public SongsAlbum(string album, string artist, string rowids, string filename, string path, string name)
            {
                this.Album = album;
                this.Artist = artist;
                this.RowIds = rowids;
                this.Filename = filename;
                this.Path = path;
                this.Name = name;
            }

            #region INotifyPropertyChanged

            [field: NonSerialized]
            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(String propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public override string ToString()
            {
                return string.Format("{0}, {1}, {2}, {3}, {4}, {5}", Album, Artist, RowIds, Filename, Path, Name);
            }

            #endregion
        }

        #endregion

        #endregion
    }
}

