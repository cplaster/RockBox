using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RockBox
{
    [Serializable]
    public class AlbumDataItem
    {

        public string Name
        {
            get;
            set;
        }

        public string Artist
        {
            get;
            set;
        }

        public Database.SongCollection Table
        {
            get;
            set;
        }

        public string Year
        {
            get;
            set;
        }

        public BitmapImage BitmapImage
        {
            get;
            set;
        }

        public string UID
        {
            get;
            set;
        }


        public AlbumDataItem(string name, string artist, Database.SongCollection table, string year = "Unknown")
        {
            Name = name;
            Artist = artist;
            Table = table;
            Year = year;
            BitmapImage = null;
        }
    }
}

