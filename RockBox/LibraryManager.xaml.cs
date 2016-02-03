using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RockBox
{
    /// <summary>
    /// Interaction logic for LibraryManager.xaml
    /// </summary>
    public partial class LibraryManager : Window
    {
        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);

        public LibraryManager()
        {
            InitializeComponent();
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            //WARNFIX
            //System.Windows.Forms.DialogResult results = dlg.ShowDialog();
            dlg.ShowDialog();
            txtDirectory.Text = dlg.SelectedPath;
            btnAdd_Click(sender, e);
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            MainWindow w = this.Owner as MainWindow;
            w.DetachLibraryManager();
            this.Close();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            lbDirectories.Items.Add(txtDirectory.Text);
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            lbDirectories.Items.Remove(lbDirectories.SelectedItem);

        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            UpdateProgressBarDelegate updatePbDelegate = new UpdateProgressBarDelegate(pbProgress.SetValue);

            List<string> l = new List<string>();
            foreach (var item in lbDirectories.Items)
            {
                l.Add((string)item);
            }
            string[] s = { ".mp3", ".ogg", ".wma" };
            DirectoryHelper.SuperDirectoryCollection coll = DirectoryHelper.ScanDirectories(l.ToArray());

            coll.ApplyExtensionFilter(s);


            tbStatus.Text = "Directories: " + coll.DirectoryCount.ToString() + "  |  Files: " + coll.FileCount.ToString();

            pbProgress.Minimum = 0;
            pbProgress.Maximum = coll.FileCount;

            double i = pbProgress.Value;


            MainWindow w = this.Owner as MainWindow;
            Database sta = w.AudioEngine.Datastore;

            foreach (DirectoryHelper.SuperDirectory sd in coll.Items)
            {
                List<string> filelist = sd.GetFileList();
                foreach (string file in filelist)
                {
                    FileInfo f = new FileInfo(file);
                    DirectoryInfo d = f.Directory;
                    if (!sta.Songs.Contains(f))
                    {
                        sta.Songs.AddFile(f);
                        i++;
                        Dispatcher.Invoke(updatePbDelegate,
                            System.Windows.Threading.DispatcherPriority.Background,
                            new object[] { ProgressBar.ValueProperty, i });
                    }

                }
            }
        }

        private void btnEmpty_Click(object sender, RoutedEventArgs e)
        {
            //LibraryHelper.EmptyDatabase();
        }
    }
}


