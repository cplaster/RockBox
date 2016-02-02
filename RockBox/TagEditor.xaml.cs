using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for TagEditor.xaml
    /// </summary>
    public partial class TagEditor : Window
    {

        Database ds;
        Database.SongCollection tempds;
        DataGridRow dgr;
        DataGridColumn dgc;

        public TagEditor()
        {
            InitializeComponent();
        }

        #region Window Events

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            //base.OnContentRendered(e);
            MainWindow t = this.Owner as MainWindow;
            tempds = new Database.SongCollection();
            ds = t.AudioEngine.Datastore;
            this.DataContext = ds;
            this.cbDirectory.Items.Clear();

            this.PopulateDirectories();
        }

        private void PopulateDirectories()
        {
            if (this.cbDirectory.Items.IsEmpty)
            {
                var directories = (from Database.Song dr in ds.Songs
                                   select (string)dr.Path).Distinct().OrderBy(Path => Path);

                foreach (var dir in directories)
                {
                    this.cbDirectory.Items.Add(dir);
                }
            }

        }

        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            MainWindow w = this.Owner as MainWindow;
            w.DetachTagEditor();
            this.Close();
        }

        private void cbDirectorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string path = this.cbDirectory.SelectedItem as string;
            this.tempds.Clear();


            var songs = from r in ds.Songs.AsEnumerable()
                        where r.Path == path
                        select r;

            //System.Data.EnumerableRowCollection<Database.SongsRow> songs = from r in ds.Songs.AsEnumerable()
            //           where r.Path == path
            //            select r;

            foreach (Database.Song row in songs)
            {


                this.tempds.Add(row);
            }

            lbDirectories.ItemsSource = this.tempds;

        }

        #endregion

        #region Tokenizer

        public class Tokenizer
        {

            Dictionary<string, string> d = new Dictionary<string, string>();

            public Tokenizer()
            {


            }

            public Dictionary<string, string> GetDictionary()
            {
                Dictionary<string, string> d = new Dictionary<string, string>();
                d.Add("artist", "");
                d.Add("album", "");
                d.Add("track", "");
                d.Add("title", "");
                d.Add("year", "");
                d.Add("genre", "");
                d.Add("null", "");

                return d;
            }


            public void Toke(Database.SongCollection collection, string pathreg, string filereg)
            {

                foreach (Database.Song row in collection)
                {
                    Dictionary<string, string> d = GetDictionary();

                    string txt = row[2] as string;
                    TryTokenize(txt, filereg, d);

                    txt = row[1] as string;
                    TryTokenize(txt, pathreg, d);
                    /*
                    txt = row[2] as string;
                    TryTokenize(txt, filereg, d);
                    */
                    row.Artist = d["artist"];
                    row.Album = d["album"];
                    row.Track = d["track"];
                    row.Title = d["title"];
                    row.Year = d["year"];
                    row.Genre = d["genre"];

                }
            }

            public void TryTokenize(string subject, string tokens, Dictionary<string, string> d = null, string whitespace = null)
            {
                if (d == null)
                {
                    d = GetDictionary();
                }

                if (whitespace == null)
                {
                    whitespace = "_";
                }

                string toks = tokens;
                string words = @"([a-zA-Z\(\)\s\d!#$%&'*+\-/=?^_,.`{|}~]+)";
                var number = @"(\d+)";
                tokens = Regex.Replace(tokens, "%artist%", words);
                tokens = Regex.Replace(tokens, "%album%", words);
                tokens = Regex.Replace(tokens, "%track%", number);
                tokens = Regex.Replace(tokens, "%title%", words);
                tokens = Regex.Replace(tokens, "%year%", number);
                tokens = Regex.Replace(tokens, "%genre%", words);
                tokens = Regex.Replace(tokens, "%null%", words);
                tokens = Regex.Replace(tokens, "%d%", number);

                try
                {
                    Regex r = new Regex(tokens);
                    Match match = r.Match(subject);
                    GroupCollection matches = match.Groups;
                    List<string> tokenarray = GetTokenArray(toks);
                    int i = 0;
                    if (matches.Count > 0)
                    {
                        for (i = 0; i < matches.Count; i++)
                        {
                            if (i != 0)
                            {
                                string m = matches[i].Value;
                                string t = tokenarray[i - 1];
                                d[t] = m;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // most likely a malformed regex. Not a big deal, just recover gracefully.
                    MessageBox.Show("There is an error in the expression.");
                }





            }

            public List<string> GetTokenArray(string tokens)
            {
                List<string> ret = new List<string>();
                Regex r = new Regex("(%[a-z]+%)");
                MatchCollection matches = r.Matches(tokens);
                int index = -1;
                for (int i = 0; i <= matches.Count - 1; i++)
                {
                    index++;
                    Match val = matches[i];

                    ret.Add(val.Value.Replace("%", ""));
                    if (index <= matches.Count - 1)
                    {
                        // do stuff here.

                    }
                }

                return ret;
            }

        }

        #endregion

        private void btnApplyClick(object sender, RoutedEventArgs e)
        {
            Tokenizer t = new Tokenizer();

            string path = txtPath.Text;
            string file = txtFile.Text;

            path = path.Replace("\\", "\\\\");
            path = path.Replace("(", "\\(");
            path = path.Replace(")", "\\)");

            //Fixme: figure out how to use list as a defaultview

            Database.SongCollection songs = new Database.SongCollection();

            foreach (Database.Song row in this.tempds)
            {
                songs.Add(row);
            }

            //DataTable dt = ds.ToDataTable<Database.Song>(songs);

            t.Toke(songs, path, file);
        }

        private void btnSaveClick(object sender, RoutedEventArgs e)
        {

            if (this.tempds.Count > 0)
            {
                Database.SongCollection songs = new Database.SongCollection();

                foreach (Database.Song row in this.tempds)
                {
                    songs.Add(row);
                }

                ds.Songs.Update(songs);
            }
        }

        private void lbDirectoriesCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            dgr = e.Row;
            dgc = e.Column;
        }

        private void lbDirectoriesCellEdited(object sender, EventArgs e)
        {
            if (dgr != null)
            {
                Database.Song val = dgr.Item as Database.Song;
                string changed = val.ToArray()[dgc.DisplayIndex] as string;

                Database.SongCollection coll = lbDirectories.ItemsSource as Database.SongCollection;

                foreach (Database.Song r in coll)
                {
                    r[dgc.DisplayIndex] = changed;
                }

                dgr = null;
                dgc = null;

                lbDirectories.ItemsSource = coll;
            }
        }

    }
}

