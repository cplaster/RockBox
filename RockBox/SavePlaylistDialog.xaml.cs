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

namespace RockBox
{
    /// <summary>
    /// Interaction logic for SavePlaylistDialog.xaml
    /// </summary>
    public partial class SavePlaylistDialog : Window
    {


        public SavePlaylistDialog()
        {
            InitializeComponent();
        }

        #region Properties

        public string Filename
        {
            get
            {
                return txtSaveLocation.Text;
            }
        }

        public string ArtistName
        {
            get
            {
                return txtArtistName.Text;
            }
        }

        public string PlaylistName
        {
            get
            {
                return txtPlaylistName.Text;
            }
        }

        #endregion

        #region Window Events

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            //base.OnContentRendered(e);

        }

        private void btnSaveLocation_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".m3u";
            dlg.Filter = "MP3 Audio Playlist(.m3u)|*.m3u";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                txtSaveLocation.Text = dlg.FileName;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            txtArtistName.Text = "";
            txtPlaylistName.Text = "";
            txtSaveLocation.Text = "";
            this.DialogResult = false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Playlist p = this.Owner as Playlist;
            MainWindow w = p.Owner as MainWindow;

            Database.SongsAlbumCollection t = w.AudioEngine.Datastore.SongsAlbums.GetDataByName(txtPlaylistName.Text);

            if (t.Count > 0)
            {
                MessageBoxResult r = MessageBox.Show("A playlist with this name already exists! Do you want to overwrite it?", "Overwrite file", MessageBoxButton.YesNo);
                //WARNFIX
                //string s = "";
            }


            this.DialogResult = true;
        }

        #endregion

    }
}
