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
    /// Interaction logic for AutoTagDialog.xaml
    /// </summary>
    public partial class AutoTagDialog : Window
    {
        MainWindow wndMainWindow;
        private delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);
        private delegate void UpdateProgressTextDelegate(DependencyProperty dp, object value);

        public AutoTagDialog()
        {
            InitializeComponent();
        }

        public void StartTagging()
        {
            wndMainWindow = this.Owner as MainWindow;

            UpdateProgressBarDelegate updatePbDelegate = new UpdateProgressBarDelegate(pbProgress.SetValue);
            UpdateProgressTextDelegate updatePtDelegate = new UpdateProgressTextDelegate(txtResults.SetValue);
            double i = 0.0;
            int total = wndMainWindow.AudioEngine.Playlist.Count;
            pbProgress.Minimum = i;
            pbProgress.Maximum = total;
            string t = "Initializing...";
            int idx = 0;

            int c = wndMainWindow.AudioEngine.Playlist.Count();
            Database.Song[] temp = new Database.Song[c];
            wndMainWindow.AudioEngine.Playlist.CopyTo(temp, 0);
            // can't modify the playlist if its the subject of the foreach, so we have to make a copy.


            foreach (Database.Song row in temp)
            {
                i++;
                t += "\n" + "Processing file " + i.ToString() + " of " + total.ToString() + " | " + row.Filename;

                Dispatcher.Invoke(updatePtDelegate,
                    System.Windows.Threading.DispatcherPriority.Background,
                    new object[] { TextBox.TextProperty, t });

                AudioEngine.Fingerprint(row);

                wndMainWindow.AudioEngine.Playlist.RemoveAt(idx);
                wndMainWindow.AudioEngine.Playlist.Insert(idx, row);

                Dispatcher.Invoke(updatePbDelegate,
                    System.Windows.Threading.DispatcherPriority.Background,
                    new object[] { ProgressBar.ValueProperty, i });

                idx++;
            }

            this.btnClose.IsEnabled = true;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
