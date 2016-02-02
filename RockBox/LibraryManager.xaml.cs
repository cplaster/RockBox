using System;
using System.Collections.Generic;
using System.IO;
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
using System.ComponentModel;

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
            System.Windows.Forms.DialogResult results = dlg.ShowDialog();
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
                    if (!DirectoryHelper.IsInLibrary(sta, f))
                    {
                        DirectoryHelper.AddFile(sta, f);
                        i++;
                        Dispatcher.Invoke(updatePbDelegate,
                            System.Windows.Threading.DispatcherPriority.Background,
                            new object[] { ProgressBar.ValueProperty, i });
                    }

                }
            }
        }

        /*
        private void btnStart_ClickOLD(object sender, RoutedEventArgs e)
        {
            List<string> l = new List<string>();
            foreach (var item in lbDirectories.Items)
            {
                l.Add((string)item);
            }
            string[] s = { ".mp3", ".ogg" };
            DirectoryHelper.SuperDirectoryCollection coll = DirectoryHelper.ScanDirectories(l.ToArray());

            coll.ApplyExtensionFilter(s);

            int i = DirectoryHelper.AddCollection(coll);

            tbStatus.Text = "Directories: " + coll.DirectoryCount.ToString() + "  |  Files: " + i.ToString();
        }
        */
        private void btnEmpty_Click(object sender, RoutedEventArgs e)
        {
            //LibraryHelper.EmptyDatabase();
        }
    }
}


