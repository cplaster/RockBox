using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace RockBox
{
    /// <summary>
    /// Interaction logic for Playlist.xaml
    /// </summary>
    public partial class Playlist : Window
    {

        MainWindow wndMainWindow;
        bool _dragenabled = false;

        public Playlist()
        {
            InitializeComponent();
        }

        #region Properties

        public int SelectedIndex
        {
            get
            {
                return lbPlaylist.SelectedIndex;
            }
            set
            {
                lbPlaylist.SelectedIndex = value;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the ContentRendered event and wires up a pointer to the main window.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            this.wndMainWindow = this.Owner as MainWindow;
        }

        /// <summary>
        /// Clears the playlist of all items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_new_Click(object sender, RoutedEventArgs e)
        {
            this.ClearPlaylist(sender, e);
        }

        /// <summary>
        /// Loads a .m3u playlist.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_open_Click(object sender, RoutedEventArgs e)
        {

            this.wndMainWindow = this.Owner as MainWindow;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".m3u";
            dlg.Multiselect = true;
            dlg.Filter = "MP3 Audio Playlist(.m3u)|*.m3u|Windows Media Playlist (.wpl)|*.wpl|Mpeg Audio Layer 3 File|*.mp3|Windows Media Audio File|*.wma";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                switch (dlg.FilterIndex)
                {
                    case 1:

                        foreach (string fn in dlg.FileNames)
                        {
                            this.wndMainWindow.AudioEngine.Playlist.Load(fn);
                        }

                        break;

                    case 2:

                        foreach (string fn in dlg.FileNames)
                        {
                            this.wndMainWindow.AudioEngine.Playlist.Load(fn);
                        }

                        break;

                    case 3:

                        foreach (string fn in dlg.FileNames)
                        {

                            //this.wndMainWindow.AudioEngine.Playlist.AddFile(new System.IO.FileInfo(fn));

                        }

                        break;

                    case 4:

                        foreach (string fn in dlg.FileNames)
                        {
                            this.wndMainWindow.AudioEngine.Playlist.AddFile(new System.IO.FileInfo(fn));
                        }

                        break;

                }

                this.DataContext = this.wndMainWindow.AudioEngine.Playlist;


                #region OldStuff
                /*
                switch (dlg.FilterIndex)
                {
                    case 1:
                        string f = "";
                        foreach (string fn in dlg.FileNames)
                        {
                            System.IO.FileInfo fi = new System.IO.FileInfo(fn);

                            string[] lines = System.IO.File.ReadAllLines(fn);

                            // this is a .m3u file, so we want to ignore any lines that begin with a '#' character

                            foreach (string line in lines)
                            {
                                if (line.Length > 0)
                                {
                                    if (line.Substring(0, 1) != "#")
                                    {
                                        // M3U can have fullpaths or just filenames, so we have to check for this.

                                        if (line.Contains("\\") || line.Contains("/"))
                                        {
                                            f = line;
                                        }
                                        else
                                        {
                                            f = fi.DirectoryName + "\\" + line;
                                        }

                                        System.IO.FileInfo fileinfo = new System.IO.FileInfo(f);

                                        if (fileinfo.Exists)
                                        {
                                            this.AddToPlaylist(fileinfo);
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    case 2:
                        foreach (string fn in dlg.FileNames)
                        {
                            System.IO.FileInfo fileinfo = new System.IO.FileInfo(fn);

                            if (fileinfo.Exists)
                            {
                                this.AddToPlaylist(fileinfo);
                            }
                        }
                        break;

                    case 3:
                        foreach (string fn in dlg.FileNames)
                        {
                            System.IO.FileInfo fileinfo = new System.IO.FileInfo(fn);
                            if (fileinfo.Exists)
                            {
                                this.AddToPlaylist(fileinfo);
                            }
                        }
                        break;
                }
                */
                #endregion

            }
        }

        /// <summary>
        /// Adds a file to the playlist, also adding it to the database if it doesn't already exist.
        /// </summary>
        /// <param name="fileinfo"></param>
        private void AddToPlaylist(System.IO.FileInfo fileinfo)
        {
            int id;
            Database.SongCollection sdt = this.wndMainWindow.AudioEngine.Datastore.Songs.GetDataByFullPath(fileinfo.FullName);
            if (sdt.Count == 0)
            {
                // FIXME:
                // the thing that sucks about this is that it blocks the ui thread. booo. 
                id = this.wndMainWindow.AudioEngine.Datastore.Songs.AddFile(fileinfo);
            }
            else
            {
                id = sdt[0].Id;
            }
            Database.Song sr = this.wndMainWindow.AudioEngine.Datastore.Songs.FindById(id);
            this.wndMainWindow.AudioEngine.Playlist.Add(sr);
            this.DataContext = this.wndMainWindow.AudioEngine.Playlist;
        }

        /// <summary>
        /// Saves the current playlist.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            SavePlaylistDialog dlg = new SavePlaylistDialog();
            dlg.Owner = this;

            Nullable<bool> result = dlg.ShowDialog();
            StringBuilder sb = new StringBuilder();
            //WARNFIX
            //string rowids = "";
            //bool first = true;

            if (result == true)
            {
                this.wndMainWindow.AudioEngine.Playlist.Save(dlg.Filename);
            }

            #region Old Stuff
            /*
            if (result == true)
            {
                sb.AppendLine("#EXTM3U");

                foreach (Database.Song row in this.wndMainWindow.AudioEngine.Playlist)
                {
                    sb.AppendLine(row.Path + "/" + row.Filename);
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        rowids += ",";
                    }
                    rowids += row.Id;
                }


                System.IO.FileInfo fi = new System.IO.FileInfo(dlg.Filename);

                this.wndMainWindow.AudioEngine.Datastore.SongsAlbums.AddRow(dlg.PlaylistName, dlg.ArtistName, rowids, fi.Directory.FullName, fi.Name, dlg.PlaylistName);

                System.IO.File.WriteAllText(dlg.Filename, sb.ToString());
            }
            */
            #endregion
        }

        /// <summary>
        /// Clears the playlist of all items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearPlaylist(object sender, RoutedEventArgs e)
        {
            MessageBoxResult r = MessageBox.Show("Are you sure you want to clear the playlist?", "Clear Playlist", MessageBoxButton.YesNo);

            if (r == MessageBoxResult.Yes)
            {
                this.wndMainWindow.AudioEngine.Playlist.Clear();
            }

        }

        /// <summary>
        /// Fires PlayItem on the owner window if a ListBoxItem is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnDoubleClick(object sender, MouseEventArgs e)
        {
            if (sender is ListBoxItem)
            {

                ListBoxItem draggedItem = sender as ListBoxItem;
                Database.Song target = ((ListBoxItem)(sender)).DataContext as Database.Song;
                draggedItem.IsSelected = true;
                int i = lbPlaylist.SelectedIndex;
                this.wndMainWindow.AudioEngine.PlayItem(i);
            }
        }

        /// <summary>
        /// Handles MouseMove events to allow ListItem Drag n' Drop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_dragenabled)
            {
                ListBoxItem draggedItem = sender as ListBoxItem;
                Database.Song row = draggedItem.Content as Database.Song;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
            else
            {
                if (e.LeftButton == MouseButtonState.Released && _dragenabled)
                {
                    _dragenabled = false;
                }
            }
        }

        /// <summary>
        /// Handles MouseLeftButtonDown events to allow ListItem selection for Drag n' Drop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem)
            {
                ListBoxItem draggedItem = sender as ListBoxItem;
                Database.Song row = draggedItem.Content as Database.Song;

                this.wndMainWindow.AudioEngine.SelectedIndex = lbPlaylist.Items.IndexOf(row);
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        /// <summary>
        /// Handles Drop events for ListItem Drag n' Drop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnDrop(object sender, DragEventArgs e)
        {

            Database.Song droppedData = e.Data.GetData(typeof(Database.Song)) as Database.Song;
            Database.Song target = ((ListBoxItem)(sender)).Content as Database.Song;

            int removedIdx = lbPlaylist.Items.IndexOf(droppedData);
            int targetIdx = lbPlaylist.Items.IndexOf(target);

            if (removedIdx != targetIdx)
            {
                if (removedIdx <= this.wndMainWindow.AudioEngine.SelectedIndex && targetIdx > this.wndMainWindow.AudioEngine.SelectedIndex)
                {
                    this.wndMainWindow.AudioEngine.SelectedIndex--;
                }

                if (removedIdx > this.wndMainWindow.AudioEngine.SelectedIndex && targetIdx <= this.wndMainWindow.AudioEngine.SelectedIndex)
                {
                    this.wndMainWindow.AudioEngine.SelectedIndex++;
                }

                this.MoveTableRow(this.wndMainWindow.AudioEngine.Playlist, removedIdx, targetIdx);

                this.DataContext = this.wndMainWindow.AudioEngine.Playlist;
            }

        }



        #endregion

        #region Helper Methods

        /// <summary>
        /// Moves a DataTable row from a given index to another.
        /// </summary>
        /// <param name="dt">Target DataTable</param>
        /// <param name="fromIndex">Source row index</param>
        /// <param name="toIndex">Destination row index</param>
        private void MoveTableRow(Database.SongCollection dt, int fromIndex, int toIndex)
        {
            Database.Song oldrow = dt[fromIndex];
            dt.Remove(oldrow);
            dt.Insert(toIndex, oldrow);
        }

        /*
        private void MoveTableRow(List<Database.SongsRow> songs, int removedIdx, int targetIdx)
        {
            Database.SongsRow oldrow = songs[removedIdx];
            Database.SongsRow newrow = oldrow;
            songs.RemoveAt(removedIdx);
            songs.Insert(targetIdx, newrow);
        }
        */

        /// <summary>
        /// Removes the currently selected item from the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveFromPlaylist(object sender, RoutedEventArgs e)
        {
            this.wndMainWindow.AudioEngine.Playlist.RemoveAt(lbPlaylist.SelectedIndex);
        }

        private void EditTags(object sender, RoutedEventArgs e)
        {
            this.wndMainWindow.Menu_ShowTagDialog(sender, e);

            Database.Song sr = this.wndMainWindow.AudioEngine.Playlist[lbPlaylist.SelectedIndex];

            this.wndMainWindow.TagDialog.LoadFile(sr, lbPlaylist.SelectedIndex);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the specified Music.SongsRow to the playlist.
        /// </summary>
        /// <param name="row">Target Music.SongsRow</param>
        public void Add(Database.Song row)
        {

            System.IO.FileInfo fileinfo = new System.IO.FileInfo(row.Path + "/" + row.Filename);

            if (fileinfo.Exists)
            {
                this.wndMainWindow.AudioEngine.Playlist.Add(row);
            }

            this.DataContext = this.wndMainWindow.AudioEngine.Playlist;
        }

        #endregion

        #region Tagging Methods

        private void btn_tag_Click(object sender, RoutedEventArgs e)
        {
            //this.wndMainWindow.Menu_ShowTagEditor(sender, e);
            this.wndMainWindow.Menu_ShowTagDialog(sender, e);
        }

        private void btn_find_Click(object sender, RoutedEventArgs e)
        {
            this.wndMainWindow.Menu_ShowLibrary(sender, e);
        }

        private void btn_tagall_Click(object sender, RoutedEventArgs e)
        {
            //LibraryHelper.Fingerprint(this.wndMainWindow.dsMusic);

            AutoTagDialog a = new AutoTagDialog();
            a.Owner = this.wndMainWindow;
            a.Show();
            a.StartTagging();
        }

        #endregion

    }
}
